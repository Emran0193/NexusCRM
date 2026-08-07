namespace NexusCRM.Contracts.Plugins;

public sealed record PluginDescriptorDto(
    string Id,
    string Name,
    string Version,
    string Description,
    IReadOnlyList<string> Capabilities,
    bool IsEnabled,
    string Source);

public sealed record TogglePluginRequest(bool IsEnabled);
