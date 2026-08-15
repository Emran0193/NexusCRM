using Microsoft.Extensions.Logging;
using NexusCRM.Application.Abstractions.Plugins;
using NexusCRM.Domain.Leads;
using NexusCRM.Plugins.Abstractions;

namespace NexusCRM.Infrastructure.Plugins;

internal sealed class PluginRuntime : IPluginRuntime
{
    private readonly IPluginCatalog _catalog;
    private readonly IInstalledPluginRegistry _registry;
    private readonly ILogger<PluginRuntime> _logger;

    public PluginRuntime(
        IPluginCatalog catalog,
        IInstalledPluginRegistry registry,
        ILogger<PluginRuntime> logger)
    {
        _catalog = catalog;
        _registry = registry;
        _logger = logger;
    }

    public async Task ApplyLeadEnrichmentAsync(
        Lead lead,
        string stageName,
        Guid? actorUserId,
        CancellationToken cancellationToken = default)
    {
        var context = new LeadEnrichmentContext(
            lead.TenantId,
            lead.Id,
            lead.Title,
            lead.Email,
            lead.CompanyName,
            lead.Source,
            lead.Score,
            lead.Status.ToString(),
            stageName,
            lead.Tags);

        foreach (var plugin in _registry.All.OfType<ILeadEnrichmentPlugin>())
        {
            if (!await _catalog.IsEnabledAsync(lead.TenantId, plugin.Id, cancellationToken))
            {
                continue;
            }

            try
            {
                var result = await plugin.EnrichAsync(context, cancellationToken);
                ApplyEnrichment(lead, result, actorUserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Lead enrichment plugin {PluginId} failed for lead {LeadId}",
                    plugin.Id,
                    lead.Id);
            }
        }
    }

    public async Task DeliverNotificationAsync(
        Guid tenantId,
        string title,
        string body,
        string category,
        string? entityType,
        Guid? entityId,
        CancellationToken cancellationToken = default)
    {
        var message = new PluginNotificationMessage(tenantId, title, body, category, entityType, entityId);

        foreach (var plugin in _registry.All.OfType<INotificationChannelPlugin>())
        {
            if (!await _catalog.IsEnabledAsync(tenantId, plugin.Id, cancellationToken))
            {
                continue;
            }

            try
            {
                await plugin.DeliverAsync(message, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Notification channel plugin {PluginId} failed for tenant {TenantId}",
                    plugin.Id,
                    tenantId);
            }
        }
    }

    private static void ApplyEnrichment(Lead lead, LeadEnrichmentResult result, Guid? actorUserId)
    {
        if (result.SuggestedScore is { } score)
        {
            lead.SetScore(score, actorUserId);
        }

        if (result.TagsToAdd is { Count: > 0 })
        {
            foreach (var tag in result.TagsToAdd)
            {
                lead.AddTag(tag, actorUserId);
            }
        }

        if (!string.IsNullOrWhiteSpace(result.NoteToAppend))
        {
            lead.AppendNote(result.NoteToAppend, actorUserId);
        }
    }
}
