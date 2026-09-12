namespace NexusCRM.Plugins.Abstractions;

/// <summary>
/// Root contract for all NexusCRM plugins. External assemblies implement this (and optional capability interfaces).
/// </summary>
public interface INexusPlugin
{
    string Id { get; }

    string Name { get; }

    string Version { get; }

    string Description { get; }

    IReadOnlyList<string> Capabilities { get; }
}

public static class PluginCapabilities
{
    public const string LeadEnrichment = "lead.enrichment";
    public const string NotificationChannel = "notification.channel";
    public const string Diagnostics = "diagnostics";
}
