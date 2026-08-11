using NexusCRM.Domain.Workflows;

namespace NexusCRM.Application.Abstractions.Workflows;

public interface IWorkflowDefinitionRepository
{
    Task<IReadOnlyList<WorkflowDefinition>> ListAsync(CancellationToken cancellationToken = default);

    Task<WorkflowDefinition?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WorkflowDefinition>> ListEnabledByTriggerAsync(
        WorkflowTriggerType triggerType,
        CancellationToken cancellationToken = default);

    Task AddAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default);
}

public interface IWorkflowRunRepository
{
    Task AddAsync(WorkflowRun run, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WorkflowRun>> ListRecentAsync(int take = 50, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WorkflowRun>> DequeuePendingAsync(int take, CancellationToken cancellationToken = default);

    Task<WorkflowRun?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface IWorkflowDispatcher
{
    Task DispatchAsync(
        WorkflowTriggerType triggerType,
        string entityType,
        Guid entityId,
        Guid tenantId,
        IReadOnlyDictionary<string, string?> context,
        CancellationToken cancellationToken = default);
}

public interface IWorkflowActionExecutor
{
    Task<string> ExecuteAsync(
        WorkflowAction action,
        Guid tenantId,
        Guid entityId,
        string entityType,
        IReadOnlyDictionary<string, string?> context,
        CancellationToken cancellationToken = default);
}
