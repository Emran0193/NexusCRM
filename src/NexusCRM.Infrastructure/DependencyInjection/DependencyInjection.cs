using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexusCRM.Application.Abstractions.Analytics;
using NexusCRM.Application.Abstractions.Notifications;
using NexusCRM.Application.Abstractions.Persistence;
using NexusCRM.Application.Abstractions.Tenancy;
using NexusCRM.Application.Abstractions.Realtime;
using NexusCRM.Application.Abstractions.Workflows;
using NexusCRM.Infrastructure.Analytics;
using NexusCRM.Infrastructure.Identity;
using NexusCRM.Infrastructure.Messaging;
using NexusCRM.Infrastructure.Notifications;
using NexusCRM.Infrastructure.Persistence;
using NexusCRM.Infrastructure.Plugins;
using NexusCRM.Infrastructure.Realtime;
using NexusCRM.Infrastructure.Storage;
using NexusCRM.Infrastructure.Tenancy;
using NexusCRM.Infrastructure.Workflows;

namespace NexusCRM.Infrastructure.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<TenantContext>();
        services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContext>());
        services.AddScoped<CurrentUser>();
        services.AddScoped<ICurrentUser>(sp => sp.GetRequiredService<CurrentUser>());

        var connectionString = configuration.GetConnectionString("Default")
            ?? "Host=localhost;Port=5432;Database=nexuscrm;Username=postgres;Password=postgres";

        var isDevelopment = string.Equals(
            configuration["ASPNETCORE_ENVIRONMENT"]
            ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT"),
            "Development",
            StringComparison.OrdinalIgnoreCase);

        services.AddDbContext<NexusDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsAssembly(typeof(NexusDbContext).Assembly.FullName);
                npgsql.EnableRetryOnFailure(maxRetryCount: 3);
                npgsql.CommandTimeout(30);
            });

            if (isDevelopment)
            {
                options.EnableDetailedErrors();
                options.EnableSensitiveDataLogging();
            }
        });

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<NexusDbContext>());
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IPipelineRepository, PipelineRepository>();
        services.AddScoped<ILeadRepository, LeadRepository>();
        services.AddScoped<IDealRepository, DealRepository>();
        services.AddScoped<IWorkflowDefinitionRepository, WorkflowDefinitionRepository>();
        services.AddScoped<IWorkflowRunRepository, WorkflowRunRepository>();
        services.AddScoped<IWorkflowDispatcher, WorkflowDispatcher>();
        services.AddScoped<IWorkflowActionExecutor, WorkflowActionExecutor>();
        services.AddScoped<WorkflowExecutionService>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<ICrmAnalyticsReader, CrmAnalyticsReader>();
        services.AddSingleton<IBoardRealtimePublisher, NoOpBoardRealtimePublisher>();
        services.AddSingleton<INotificationRealtimePublisher, NoOpNotificationRealtimePublisher>();
        services.AddNexusPlugins(configuration);

        services.AddNexusIdentity(configuration);

        services.AddMemoryCache();

        var redis = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrWhiteSpace(redis))
        {
            services.AddStackExchangeRedisCache(options => options.Configuration = redis);
        }
        else
        {
            services.AddDistributedMemoryCache();
        }

        var storageRoot = configuration["Storage:LocalRoot"]
            ?? Path.Combine(Path.GetTempPath(), "nexuscrm-files");
        services.AddSingleton<IFileStorage>(_ => new LocalFileStorage(storageRoot));
        services.AddSingleton<IIntegrationEventPublisher, LoggingIntegrationEventPublisher>();

        return services;
    }
}
