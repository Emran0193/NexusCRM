namespace NexusCRM.Contracts.Pipelines;

public sealed record PipelineDto(
    Guid Id,
    string Name,
    string Type,
    bool IsDefault,
    IReadOnlyList<PipelineStageDto> Stages);

public sealed record PipelineStageDto(
    Guid Id,
    string Name,
    int SortOrder,
    bool IsWon,
    bool IsLost,
    int? WinProbability);
