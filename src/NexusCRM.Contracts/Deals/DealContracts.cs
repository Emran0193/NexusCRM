namespace NexusCRM.Contracts.Deals;

public sealed record DealDto(
    Guid Id,
    Guid TenantId,
    Guid PipelineId,
    Guid StageId,
    string Title,
    decimal Amount,
    string Currency,
    string Status,
    Guid? OwnerUserId,
    Guid? CustomerId,
    Guid? LeadId,
    DateOnly? ExpectedCloseDate,
    string RowVersion,
    DateTimeOffset CreatedAtUtc);

public sealed record CreateDealRequest(
    string Title,
    decimal Amount,
    string? Currency,
    Guid? CustomerId,
    Guid? LeadId,
    DateOnly? ExpectedCloseDate,
    Guid? StageId);

public sealed record MoveDealStageRequest(Guid StageId, string? RowVersion);

public sealed record DealBoardDto(
    Guid PipelineId,
    string PipelineName,
    decimal OpenPipelineValue,
    IReadOnlyList<DealColumnDto> Columns);

public sealed record DealColumnDto(
    Guid StageId,
    string StageName,
    int SortOrder,
    bool IsWon,
    bool IsLost,
    decimal ColumnValue,
    IReadOnlyList<DealDto> Items);
