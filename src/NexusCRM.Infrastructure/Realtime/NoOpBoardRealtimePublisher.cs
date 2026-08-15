using NexusCRM.Application.Abstractions.Realtime;
using NexusCRM.Contracts.Deals;
using NexusCRM.Contracts.Leads;

namespace NexusCRM.Infrastructure.Realtime;

/// <summary>
/// Fallback publisher for hosts that do not run SignalR (e.g. Worker).
/// </summary>
internal sealed class NoOpBoardRealtimePublisher : IBoardRealtimePublisher
{
    public Task PublishLeadChangedAsync(
        Guid tenantId,
        string changeType,
        LeadDto lead,
        Guid? fromStageId = null,
        Guid? actorUserId = null,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task PublishDealChangedAsync(
        Guid tenantId,
        string changeType,
        DealDto deal,
        Guid? fromStageId = null,
        Guid? actorUserId = null,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
