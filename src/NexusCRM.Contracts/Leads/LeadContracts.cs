namespace NexusCRM.Contracts.Leads;

public sealed record LeadDto(
    Guid Id,
    Guid TenantId,
    Guid PipelineId,
    Guid StageId,
    string Title,
    string? Source,
    string? Email,
    string? Phone,
    string? CompanyName,
    int Score,
    string Status,
    Guid? OwnerUserId,
    Guid? CustomerId,
    string? Notes,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyList<string> Tags);

public sealed record CreateLeadRequest(
    string Title,
    string? Source,
    string? Email,
    string? Phone,
    string? CompanyName,
    Guid? CustomerId,
    Guid? StageId);

public sealed record UpdateLeadRequest(
    string Title,
    string? Source,
    string? Email,
    string? Phone,
    string? CompanyName,
    string? Notes);

public sealed record MoveLeadStageRequest(Guid StageId);

public sealed record SetLeadScoreRequest(int Score);

public sealed record LeadBoardDto(
    Guid PipelineId,
    string PipelineName,
    IReadOnlyList<PipelineColumnDto> Columns);

public sealed record PipelineColumnDto(
    Guid StageId,
    string StageName,
    int SortOrder,
    bool IsWon,
    bool IsLost,
    IReadOnlyList<LeadDto> Items);
