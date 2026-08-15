using NexusCRM.Plugins.Abstractions;

namespace NexusCRM.Infrastructure.Plugins;

public interface IInstalledPluginRegistry
{
    IReadOnlyList<INexusPlugin> All { get; }

    INexusPlugin? Find(string pluginId);

    string GetSource(string pluginId);
}

internal sealed class InstalledPluginRegistry : IInstalledPluginRegistry
{
    private readonly IReadOnlyList<INexusPlugin> _plugins;
    private readonly IReadOnlyDictionary<string, string> _sources;

    public InstalledPluginRegistry(
        IEnumerable<INexusPlugin> plugins,
        IReadOnlyDictionary<string, string> sources)
    {
        _plugins = plugins
            .GroupBy(p => p.Id, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        _sources = sources;
    }

    public IReadOnlyList<INexusPlugin> All => _plugins;

    public INexusPlugin? Find(string pluginId) =>
        _plugins.FirstOrDefault(p => string.Equals(p.Id, pluginId, StringComparison.OrdinalIgnoreCase));

    public string GetSource(string pluginId) =>
        _sources.TryGetValue(pluginId, out var source) ? source : "unknown";
}
