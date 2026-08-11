using NexusCRM.Application.Abstractions.Messaging;
using NexusCRM.Application.Abstractions.Persistence;
using NexusCRM.Application.Abstractions.Workflows;
using NexusCRM.Contracts.Workflows;
using NexusCRM.Domain.Workflows;
using NexusCRM.Shared.Results;

namespace NexusCRM.Application.Workflows;

internal static class WorkflowMappings
{
    public static WorkflowDefinitionDto ToDto(this WorkflowDefinition definition) =>
        new(
            definition.Id,
            definition.Name,
            definition.Description,
            definition.TriggerType.ToString(),
            definition.IsEnabled,
            definition.Conditions.Select(c => new WorkflowConditionDto(c.Field, c.Operator.ToString(), c.Value)).ToList(),
            definition.Actions.Select(a => new WorkflowActionDto(a.Type.ToString(), a.Target, a.Value)).ToList(),
            definition.CreatedAtUtc);

    public static WorkflowRunDto ToDto(this WorkflowRun run) =>
        new(
            run.Id,
            run.WorkflowDefinitionId,
            run.TriggerType.ToString(),
            run.EntityType,
            run.EntityId,
            run.Status.ToString(),
            run.ResultSummary,
            run.Error,
            run.CreatedAtUtc,
            run.CompletedAtUtc);
}

public sealed record ListWorkflowsQuery : IQuery<IReadOnlyList<WorkflowDefinitionDto>>;

public sealed class ListWorkflowsQueryHandler
    : MediatR.IRequestHandler<ListWorkflowsQuery, Result<IReadOnlyList<WorkflowDefinitionDto>>>
{
    private readonly IWorkflowDefinitionRepository _workflows;

    public ListWorkflowsQueryHandler(IWorkflowDefinitionRepository workflows) => _workflows = workflows;

    public async Task<Result<IReadOnlyList<WorkflowDefinitionDto>>> Handle(
        ListWorkflowsQuery request,
        CancellationToken cancellationToken)
    {
        var items = await _workflows.ListAsync(cancellationToken);
        return Result.Success<IReadOnlyList<WorkflowDefinitionDto>>(items.Select(w => w.ToDto()).ToList());
    }
}

public sealed record ListWorkflowRunsQuery(int Take = 50) : IQuery<IReadOnlyList<WorkflowRunDto>>;

public sealed class ListWorkflowRunsQueryHandler
    : MediatR.IRequestHandler<ListWorkflowRunsQuery, Result<IReadOnlyList<WorkflowRunDto>>>
{
    private readonly IWorkflowRunRepository _runs;

    public ListWorkflowRunsQueryHandler(IWorkflowRunRepository runs) => _runs = runs;

    public async Task<Result<IReadOnlyList<WorkflowRunDto>>> Handle(
        ListWorkflowRunsQuery request,
        CancellationToken cancellationToken)
    {
        var items = await _runs.ListRecentAsync(request.Take, cancellationToken);
        return Result.Success<IReadOnlyList<WorkflowRunDto>>(items.Select(r => r.ToDto()).ToList());
    }
}

public sealed record ToggleWorkflowCommand(Guid Id, bool IsEnabled) : ICommand<WorkflowDefinitionDto>;

public sealed class ToggleWorkflowCommandHandler
    : MediatR.IRequestHandler<ToggleWorkflowCommand, Result<WorkflowDefinitionDto>>
{
    private readonly IWorkflowDefinitionRepository _workflows;
    private readonly IUnitOfWork _unitOfWork;

    public ToggleWorkflowCommandHandler(IWorkflowDefinitionRepository workflows, IUnitOfWork unitOfWork)
    {
        _workflows = workflows;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<WorkflowDefinitionDto>> Handle(
        ToggleWorkflowCommand request,
        CancellationToken cancellationToken)
    {
        var workflow = await _workflows.GetByIdAsync(request.Id, cancellationToken);
        if (workflow is null)
        {
            return Result.Failure<WorkflowDefinitionDto>(Error.NotFound("Workflow", request.Id));
        }

        if (request.IsEnabled)
        {
            workflow.Enable();
        }
        else
        {
            workflow.Disable();
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(workflow.ToDto());
    }
}
