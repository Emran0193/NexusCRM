using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NexusCRM.Application.Abstractions.Plugins;
using NexusCRM.Plugins.Abstractions;
using NexusCRM.Plugins.Samples;

namespace NexusCRM.Infrastructure.Plugins;

public static class PluginServiceCollectionExtensions
{
    public static IServiceCollection AddNexusPlugins(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<PluginOptions>(configuration.GetSection(PluginOptions.SectionName));

        services.AddSingleton<IInstalledPluginRegistry>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<PluginOptions>>().Value;
            var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("NexusCRM.Plugins");
            var plugins = new List<INexusPlugin>();
            var sources = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (options.LoadSamples)
            {
                Register(plugins, sources, ActivatorUtilities.CreateInstance<HighValueLeadTaggerPlugin>(sp), "samples");
                Register(plugins, sources, ActivatorUtilities.CreateInstance<CompanyDomainHintPlugin>(sp), "samples");
                Register(plugins, sources, ActivatorUtilities.CreateInstance<ConsoleNotificationChannelPlugin>(sp), "samples");
            }

            if (!string.IsNullOrWhiteSpace(options.ExternalDirectory))
            {
                foreach (var (plugin, source) in ExternalPluginLoader.LoadFromDirectory(
                             options.ExternalDirectory,
                             sp,
                             logger))
                {
                    Register(plugins, sources, plugin, source);
                }
            }

            logger.LogInformation("Installed {Count} plugin(s)", plugins.Count);
            return new InstalledPluginRegistry(plugins, sources);
        });

        services.AddScoped<IPluginCatalog, PluginCatalog>();
        services.AddScoped<IPluginRuntime, PluginRuntime>();
        return services;
    }

    private static void Register(
        List<INexusPlugin> plugins,
        Dictionary<string, string> sources,
        INexusPlugin plugin,
        string source)
    {
        if (sources.ContainsKey(plugin.Id))
        {
            return;
        }

        plugins.Add(plugin);
        sources[plugin.Id] = source;
    }
}
