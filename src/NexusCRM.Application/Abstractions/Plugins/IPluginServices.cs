using NexusCRM.Contracts.Plugins;
using NexusCRM.Domain.Leads;
using NexusCRM.Plugins.Abstractions;

namespace NexusCRM.Application.Abstractions.Plugins;

public interface IPluginCatalog
{
    IReadOnlyList<INexusPlugin> GetInstalled();

    Task<IReadOnlyList<PluginDescriptorDto>> ListForTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    Task<bool> IsEnabledAsync(Guid tenantId, string pluginId, CancellationToken cancellationToken = default);

    Task SetEnabledAsync(Guid tenantId, string pluginId, bool isEnabled, CancellationToken cancellationToken = default);
}

public interface IPluginRuntime
{
    Task ApplyLeadEnrichmentAsync(
        Lead lead,
        string stageName,
        Guid? actorUserId,
        CancellationToken cancellationToken = default);

    Task DeliverNotificationAsync(
        Guid tenantId,
        string title,
        string body,
        string category,
        string? entityType,
        Guid? entityId,
        CancellationToken cancellationToken = default);
}
