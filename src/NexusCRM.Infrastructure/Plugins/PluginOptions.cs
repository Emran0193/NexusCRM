namespace NexusCRM.Infrastructure.Plugins;

public sealed class PluginOptions
{
    public const string SectionName = "Plugins";

    /// <summary>Register built-in sample plugins from NexusCRM.Plugins.Samples.</summary>
    public bool LoadSamples { get; set; } = true;

    /// <summary>
    /// Optional folder of plugin assemblies (*.dll). Loaded into isolated contexts at startup.
    /// </summary>
    public string? ExternalDirectory { get; set; }

    /// <summary>When no tenant setting row exists, treat the plugin as enabled.</summary>
    public bool DefaultEnabled { get; set; } = true;
}
