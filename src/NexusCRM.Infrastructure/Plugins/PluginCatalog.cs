using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NexusCRM.Application.Abstractions.Plugins;
using NexusCRM.Contracts.Plugins;
using NexusCRM.Domain.Plugins;
using NexusCRM.Infrastructure.Persistence;
using NexusCRM.Plugins.Abstractions;

namespace NexusCRM.Infrastructure.Plugins;

internal sealed class PluginCatalog : IPluginCatalog
{
    private readonly IInstalledPluginRegistry _registry;
    private readonly NexusDbContext _db;
    private readonly PluginOptions _options;

    public PluginCatalog(
        IInstalledPluginRegistry registry,
        NexusDbContext db,
        IOptions<PluginOptions> options)
    {
        _registry = registry;
        _db = db;
        _options = options.Value;
    }

    public IReadOnlyList<INexusPlugin> GetInstalled() => _registry.All;

    public async Task<IReadOnlyList<PluginDescriptorDto>> ListForTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var settings = await _db.TenantPluginSettings
            .AsNoTracking()
            .Where(s => s.TenantId == tenantId)
            .ToListAsync(cancellationToken);

        var byId = settings.ToDictionary(s => s.PluginId, StringComparer.OrdinalIgnoreCase);

        return _registry.All
            .Select(plugin =>
            {
                var enabled = byId.TryGetValue(plugin.Id, out var setting)
                    ? setting.IsEnabled
                    : _options.DefaultEnabled;

                return new PluginDescriptorDto(
                    plugin.Id,
                    plugin.Name,
                    plugin.Version,
                    plugin.Description,
                    plugin.Capabilities,
                    enabled,
                    _registry.GetSource(plugin.Id));
            })
            .ToList();
    }

    public async Task<bool> IsEnabledAsync(
        Guid tenantId,
        string pluginId,
        CancellationToken cancellationToken = default)
    {
        var setting = await _db.TenantPluginSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(
                s => s.TenantId == tenantId && s.PluginId == pluginId.Trim().ToLowerInvariant(),
                cancellationToken);

        return setting?.IsEnabled ?? _options.DefaultEnabled;
    }

    public async Task SetEnabledAsync(
        Guid tenantId,
        string pluginId,
        bool isEnabled,
        CancellationToken cancellationToken = default)
    {
        var normalized = pluginId.Trim().ToLowerInvariant();
        var setting = await _db.TenantPluginSettings
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.PluginId == normalized, cancellationToken);

        if (setting is null)
        {
            setting = TenantPluginSetting.Create(tenantId, normalized, isEnabled);
            await _db.TenantPluginSettings.AddAsync(setting, cancellationToken);
        }
        else
        {
            setting.SetEnabled(isEnabled);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }
}
