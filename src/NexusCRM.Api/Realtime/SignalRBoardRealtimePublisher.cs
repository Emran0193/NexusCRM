using Microsoft.AspNetCore.SignalR;
using NexusCRM.Application.Abstractions.Realtime;
using NexusCRM.Contracts.Deals;
using NexusCRM.Contracts.Leads;
using NexusCRM.Contracts.Realtime;

namespace NexusCRM.Api.Realtime;

public sealed class SignalRBoardRealtimePublisher : IBoardRealtimePublisher
{
    private readonly IHubContext<BoardHub> _hub;

    public SignalRBoardRealtimePublisher(IHubContext<BoardHub> hub)
    {
        _hub = hub;
    }

    public Task PublishLeadChangedAsync(
        Guid tenantId,
        string changeType,
        LeadDto lead,
        Guid? fromStageId = null,
        Guid? actorUserId = null,
        CancellationToken cancellationToken = default)
    {
        var payload = new LeadBoardChangedEvent(
            changeType,
            lead,
            fromStageId,
            actorUserId,
            DateTimeOffset.UtcNow);

        return _hub.Clients
            .Group(BoardHub.TenantGroup(tenantId))
            .SendAsync("LeadBoardChanged", payload, cancellationToken);
    }

    public Task PublishDealChangedAsync(
        Guid tenantId,
        string changeType,
        DealDto deal,
        Guid? fromStageId = null,
        Guid? actorUserId = null,
        CancellationToken cancellationToken = default)
    {
        var payload = new DealBoardChangedEvent(
            changeType,
            deal,
            fromStageId,
            actorUserId,
            DateTimeOffset.UtcNow);

        return _hub.Clients
            .Group(BoardHub.TenantGroup(tenantId))
            .SendAsync("DealBoardChanged", payload, cancellationToken);
    }
}
