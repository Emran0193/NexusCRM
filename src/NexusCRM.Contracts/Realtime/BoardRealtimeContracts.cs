using NexusCRM.Contracts.Deals;
using NexusCRM.Contracts.Leads;

namespace NexusCRM.Contracts.Realtime;

public sealed record LeadBoardChangedEvent(
    string ChangeType,
    LeadDto Lead,
    Guid? FromStageId,
    Guid? ActorUserId,
    DateTimeOffset OccurredAtUtc);

public sealed record DealBoardChangedEvent(
    string ChangeType,
    DealDto Deal,
    Guid? FromStageId,
    Guid? ActorUserId,
    DateTimeOffset OccurredAtUtc);
