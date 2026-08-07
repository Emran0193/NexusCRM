namespace NexusCRM.Contracts.Workflows;

public sealed record WorkflowDefinitionDto(
    Guid Id,
    string Name,
    string Description,
    string TriggerType,
    bool IsEnabled,
    IReadOnlyList<WorkflowConditionDto> Conditions,
    IReadOnlyList<WorkflowActionDto> Actions,
    DateTimeOffset CreatedAtUtc);

public sealed record WorkflowConditionDto(string Field, string Operator, string Value);

public sealed record WorkflowActionDto(string Type, string? Target, string? Value);

public sealed record WorkflowRunDto(
    Guid Id,
    Guid WorkflowDefinitionId,
    string TriggerType,
    string EntityType,
    Guid EntityId,
    string Status,
    string? ResultSummary,
    string? Error,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? CompletedAtUtc);

public sealed record ToggleWorkflowRequest(bool IsEnabled);
