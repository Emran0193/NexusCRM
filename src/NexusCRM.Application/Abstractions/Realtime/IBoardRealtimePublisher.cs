using NexusCRM.Contracts.Deals;
using NexusCRM.Contracts.Leads;

namespace NexusCRM.Application.Abstractions.Realtime;

public interface IBoardRealtimePublisher
{
    Task PublishLeadChangedAsync(
        Guid tenantId,
        string changeType,
        LeadDto lead,
        Guid? fromStageId = null,
        Guid? actorUserId = null,
        CancellationToken cancellationToken = default);

    Task PublishDealChangedAsync(
        Guid tenantId,
        string changeType,
        DealDto deal,
        Guid? fromStageId = null,
        Guid? actorUserId = null,
        CancellationToken cancellationToken = default);
}

public static class BoardChangeTypes
{
    public const string Created = "created";
    public const string Moved = "moved";
    public const string Updated = "updated";
}
