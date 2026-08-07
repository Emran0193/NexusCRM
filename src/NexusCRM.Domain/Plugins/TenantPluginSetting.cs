using NexusCRM.Domain.Common;

namespace NexusCRM.Domain.Plugins;

public sealed class TenantPluginSetting : AggregateRoot, ITenantScoped
{
    private TenantPluginSetting(Guid id, Guid tenantId, string pluginId, bool isEnabled)
        : base(id)
    {
        TenantId = tenantId;
        PluginId = pluginId;
        IsEnabled = isEnabled;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        ModifiedAtUtc = CreatedAtUtc;
    }

    private TenantPluginSetting()
    {
    }

    public Guid TenantId { get; private set; }

    public string PluginId { get; private set; } = string.Empty;

    public bool IsEnabled { get; private set; }

    public string? SettingsJson { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset ModifiedAtUtc { get; private set; }

    public static TenantPluginSetting Create(Guid tenantId, string pluginId, bool isEnabled = true)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(pluginId);
        return new TenantPluginSetting(Guid.NewGuid(), tenantId, pluginId.Trim().ToLowerInvariant(), isEnabled);
    }

    public void SetEnabled(bool isEnabled)
    {
        IsEnabled = isEnabled;
        ModifiedAtUtc = DateTimeOffset.UtcNow;
    }

    public void SetSettingsJson(string? settingsJson)
    {
        SettingsJson = string.IsNullOrWhiteSpace(settingsJson) ? null : settingsJson.Trim();
        ModifiedAtUtc = DateTimeOffset.UtcNow;
    }
}
