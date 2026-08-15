using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace NexusCRM.Infrastructure.Observability;

public static class ObservabilityExtensions
{
    public const string ApplicationActivitySource = "NexusCRM.Application";

    public static IServiceCollection AddNexusObservability(
        this IServiceCollection services,
        IConfiguration configuration,
        string serviceName,
        bool includeAspNetCore = true)
    {
        var options = configuration.GetSection(OpenTelemetryOptions.SectionName).Get<OpenTelemetryOptions>()
            ?? new OpenTelemetryOptions();

        if (!options.Enabled)
        {
            return services;
        }

        var resolvedName = options.ServiceName ?? serviceName;
        var version = typeof(ObservabilityExtensions).Assembly.GetName().Version?.ToString() ?? "1.0.0";

        services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService(serviceName: resolvedName, serviceVersion: version))
            .WithTracing(tracing =>
            {
                tracing
                    .AddSource(ApplicationActivitySource)
                    .AddHttpClientInstrumentation()
                    .AddEntityFrameworkCoreInstrumentation();

                if (includeAspNetCore)
                {
                    tracing.AddAspNetCoreInstrumentation(o =>
                    {
                        o.Filter = context =>
                            !context.Request.Path.StartsWithSegments("/health") &&
                            !context.Request.Path.StartsWithSegments("/metrics");
                    });
                }

                if (!string.IsNullOrWhiteSpace(options.OtlpEndpoint))
                {
                    tracing.AddOtlpExporter(o => o.Endpoint = new Uri(options.OtlpEndpoint));
                }
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();

                if (includeAspNetCore)
                {
                    metrics
                        .AddAspNetCoreInstrumentation()
                        .AddPrometheusExporter();
                }

                if (!string.IsNullOrWhiteSpace(options.OtlpEndpoint))
                {
                    metrics.AddOtlpExporter(o => o.Endpoint = new Uri(options.OtlpEndpoint));
                }
            });

        return services;
    }
}

public sealed class OpenTelemetryOptions
{
    public const string SectionName = "OpenTelemetry";

    public bool Enabled { get; set; } = true;

    public string? ServiceName { get; set; }

    /// <summary>OTLP gRPC endpoint, e.g. http://localhost:4317</summary>
    public string? OtlpEndpoint { get; set; } = "http://localhost:4317";
}
