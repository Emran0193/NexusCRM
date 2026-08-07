using NexusCRM.Domain.Common;

namespace NexusCRM.Domain.Workflows;

public sealed class WorkflowRun : AggregateRoot, ITenantScoped
{
    private WorkflowRun(
        Guid id,
        Guid tenantId,
        Guid workflowDefinitionId,
        WorkflowTriggerType triggerType,
        string entityType,
        Guid entityId,
        string contextJson)
        : base(id)
    {
        TenantId = tenantId;
        WorkflowDefinitionId = workflowDefinitionId;
        TriggerType = triggerType;
        EntityType = entityType;
        EntityId = entityId;
        ContextJson = contextJson;
        Status = WorkflowRunStatus.Pending;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    private WorkflowRun()
    {
    }

    public Guid TenantId { get; private set; }

    public Guid WorkflowDefinitionId { get; private set; }

    public WorkflowTriggerType TriggerType { get; private set; }

    public string EntityType { get; private set; } = string.Empty;

    public Guid EntityId { get; private set; }

    public string ContextJson { get; private set; } = "{}";

    public WorkflowRunStatus Status { get; private set; }

    public string? ResultSummary { get; private set; }

    public string? Error { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset? StartedAtUtc { get; private set; }

    public DateTimeOffset? CompletedAtUtc { get; private set; }

    public static WorkflowRun Create(
        Guid tenantId,
        Guid workflowDefinitionId,
        WorkflowTriggerType triggerType,
        string entityType,
        Guid entityId,
        string contextJson)
    {
        return new WorkflowRun(
            Guid.NewGuid(),
            tenantId,
            workflowDefinitionId,
            triggerType,
            entityType,
            entityId,
            contextJson);
    }

    public void MarkRunning()
    {
        Status = WorkflowRunStatus.Running;
        StartedAtUtc = DateTimeOffset.UtcNow;
    }

    public void MarkSucceeded(string summary)
    {
        Status = WorkflowRunStatus.Succeeded;
        ResultSummary = summary;
        CompletedAtUtc = DateTimeOffset.UtcNow;
    }

    public void MarkFailed(string error)
    {
        Status = WorkflowRunStatus.Failed;
        Error = error;
        CompletedAtUtc = DateTimeOffset.UtcNow;
    }
}

public enum WorkflowRunStatus
{
    Pending = 0,
    Running = 1,
    Succeeded = 2,
    Failed = 3
}
