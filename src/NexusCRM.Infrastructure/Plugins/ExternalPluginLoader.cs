using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusCRM.Plugins.Abstractions;

namespace NexusCRM.Infrastructure.Plugins;

internal static class ExternalPluginLoader
{
    public static IReadOnlyList<(INexusPlugin Plugin, string Source)> LoadFromDirectory(
        string directory,
        IServiceProvider services,
        ILogger logger)
    {
        if (!Directory.Exists(directory))
        {
            logger.LogWarning("Plugins:ExternalDirectory '{Directory}' does not exist; skipping.", directory);
            return [];
        }

        var results = new List<(INexusPlugin, string)>();
        foreach (var path in Directory.EnumerateFiles(directory, "*.dll", SearchOption.TopDirectoryOnly))
        {
            try
            {
                var alc = new PluginLoadContext(path);
                var assembly = alc.LoadFromAssemblyPath(path);
                foreach (var type in assembly.GetTypes().Where(IsConcretePlugin))
                {
                    var plugin = (INexusPlugin)ActivatorUtilities.CreateInstance(services, type);
                    results.Add((plugin, "external"));
                    logger.LogInformation("Loaded external plugin {PluginId} from {Path}", plugin.Id, path);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to load plugin assembly {Path}", path);
            }
        }

        return results;
    }

    private static bool IsConcretePlugin(Type type) =>
        typeof(INexusPlugin).IsAssignableFrom(type)
        && type is { IsAbstract: false, IsInterface: false }
        && type.GetConstructors(BindingFlags.Public | BindingFlags.Instance).Length > 0;

    private sealed class PluginLoadContext(string pluginPath) : AssemblyLoadContext(isCollectible: false)
    {
        private readonly AssemblyDependencyResolver _resolver = new(pluginPath);

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            var path = _resolver.ResolveAssemblyToPath(assemblyName);
            return path is null ? null : LoadFromAssemblyPath(path);
        }
    }
}
