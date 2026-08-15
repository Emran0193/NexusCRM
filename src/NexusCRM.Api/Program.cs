using System.Threading.RateLimiting;
using Asp.Versioning;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NexusCRM.Api.Health;
using NexusCRM.Api.Middleware;
using NexusCRM.Api.Realtime;
using NexusCRM.Api.Security;
using NexusCRM.Application;
using NexusCRM.Application.Abstractions.Identity;
using NexusCRM.Application.Abstractions.Notifications;
using NexusCRM.Application.Abstractions.Realtime;
using NexusCRM.Infrastructure.DependencyInjection;
using NexusCRM.Infrastructure.Observability;
using NexusCRM.Infrastructure.Persistence;
using NexusCRM.Infrastructure.Workflows;
using OpenTelemetry.Metrics;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.AddServiceDefaults();

    SecurityConfiguration.Validate(builder.Configuration, builder.Environment);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "NexusCRM.Api")
        .Enrich.WithEnvironmentName()
        .Enrich.WithMachineName()
        .WriteTo.Console());

    builder.WebHost.ConfigureKestrel(options =>
    {
        options.Limits.MaxRequestBodySize = 1_048_576; // 1 MB
    });

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddNexusObservability(builder.Configuration, "NexusCRM.Api");
    builder.Services.AddHostedService<WorkflowBackgroundProcessor>();
    builder.Services.AddSignalR();
    builder.Services.Replace(
        ServiceDescriptor.Singleton<IBoardRealtimePublisher, SignalRBoardRealtimePublisher>());
    builder.Services.Replace(
        ServiceDescriptor.Singleton<INotificationRealtimePublisher, SignalRNotificationRealtimePublisher>());

    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        // Clear defaults so reverse proxies in compose/k8s are trusted in demos.
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
    });

    builder.Services.AddControllers();
    builder.Services.AddProblemDetails();
    builder.Services.AddResponseCompression(options =>
    {
        options.EnableForHttps = true;
        options.Providers.Add<BrotliCompressionProvider>();
        options.Providers.Add<GzipCompressionProvider>();
    });

    builder.Services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.ReportApiVersions = true;
            options.ApiVersionReader = ApiVersionReader.Combine(
                new UrlSegmentApiVersionReader(),
                new HeaderApiVersionReader("X-Api-Version"));
        })
        .AddMvc()
        .AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        })
        .AddOpenApi();

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("Spa", policy =>
        {
            policy.WithOrigins(
                    builder.Configuration.GetSection("Cors:Origins").Get<string[]>()
                    ?? ["http://localhost:4200"])
                .WithHeaders(
                    "Authorization",
                    "Content-Type",
                    "X-Correlation-Id",
                    "X-Tenant-Id",
                    "X-Api-Version")
                .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS")
                .WithExposedHeaders("X-Correlation-Id", "X-Response-Time-Ms")
                .AllowCredentials();
        });
    });

    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.AddPolicy("auth", context =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                }));

        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        {
            var path = context.Request.Path;
            if (path.StartsWithSegments("/health") || path.StartsWithSegments("/metrics"))
            {
                return RateLimitPartition.GetNoLimiter("health");
            }

            return RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 200,
                    Window = TimeSpan.FromMinutes(1)
                });
        });
    });

    var healthChecks = builder.Services.AddHealthChecks()
        .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"])
        .AddDbContextCheck<NexusDbContext>("database", tags: ["ready"]);

    var redis = builder.Configuration.GetConnectionString("Redis");
    if (!string.IsNullOrWhiteSpace(redis))
    {
        healthChecks.AddRedis(redis, name: "redis", tags: ["ready"]);
    }

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NexusDbContext>();
        await db.Database.MigrateAsync();
        // Empty bytea concurrency tokens break updates under Npgsql — normalize once after migrate.
        await db.Database.ExecuteSqlRawAsync(
            """
            DO $$
            DECLARE r record;
            BEGIN
              FOR r IN
                SELECT table_schema, table_name
                FROM information_schema.columns
                WHERE column_name = 'row_version'
                  AND table_schema = 'public'
              LOOP
                EXECUTE format(
                  'UPDATE %I.%I SET row_version = decode(md5(random()::text || clock_timestamp()::text), ''hex'') WHERE octet_length(row_version) = 0',
                  r.table_schema, r.table_name);
              END LOOP;
            END $$;
            """);
        await DevelopmentDataSeeder.SeedAsync(app.Services);
    }

    app.UseForwardedHeaders();
    app.UseResponseCompression();
    app.UseMiddleware<SecurityHeadersMiddleware>();
    app.UseSerilogRequestLogging(options =>
    {
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("CorrelationId", httpContext.TraceIdentifier);
            diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
        };
    });
    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseMiddleware<RequestTimingMiddleware>();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi().WithDocumentPerVersion().AllowAnonymous();
    }

    app.UseExceptionHandler();
    app.UseHttpsRedirection();

    if (!app.Environment.IsDevelopment())
    {
        app.UseHsts();
    }

    app.UseCors("Spa");
    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseMiddleware<TenantResolutionMiddleware>();
    app.UseMiddleware<ObservabilityEnrichmentMiddleware>();
    app.UseAuthorization();

    app.MapControllers();
    app.MapHub<BoardHub>("/hubs/board");

    var security = app.Configuration.GetSection(AuthSecurityOptions.SectionName).Get<AuthSecurityOptions>()
        ?? new AuthSecurityOptions();
    var otelEnabled = app.Configuration.GetValue("OpenTelemetry:Enabled", true);
    if (otelEnabled && (app.Environment.IsDevelopment() || security.ExposeMetricsEndpoint))
    {
        app.MapPrometheusScrapingEndpoint("/metrics").AllowAnonymous();
    }

    app.MapHealthChecks("/health/live", new HealthCheckOptions
    {
        Predicate = r => r.Tags.Contains("live"),
        ResponseWriter = HealthCheckJsonWriter.WriteAsync
    }).AllowAnonymous();
    app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        Predicate = r => r.Tags.Contains("ready"),
        ResponseWriter = HealthCheckJsonWriter.WriteAsync
    }).AllowAnonymous();
    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        Predicate = r => r.Tags.Contains("ready"),
        ResponseWriter = HealthCheckJsonWriter.WriteAsync
    }).AllowAnonymous();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "NexusCRM.Api terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program;
