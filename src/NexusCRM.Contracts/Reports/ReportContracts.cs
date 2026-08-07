namespace NexusCRM.Contracts.Reports;

public sealed record CrmSummaryReportDto(
    int CustomerCount,
    int OpenLeadCount,
    int QualifiedLeadCount,
    int OpenDealCount,
    int WonDealCount,
    int LostDealCount,
    decimal OpenPipelineAmount,
    decimal WonAmount,
    string Currency,
    DateTimeOffset GeneratedAtUtc);

public sealed record FunnelStageDto(
    Guid StageId,
    string StageName,
    int SortOrder,
    bool IsWon,
    bool IsLost,
    int Count,
    decimal Amount);

public sealed record PipelineFunnelReportDto(
    Guid PipelineId,
    string PipelineName,
    string PipelineType,
    IReadOnlyList<FunnelStageDto> Stages);

public sealed record PipelineFunnelsResponse(IReadOnlyList<PipelineFunnelReportDto> Pipelines);
