using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NexusCRM.Application.Abstractions.Notifications;
using NexusCRM.Application.Abstractions.Persistence;
using NexusCRM.Application.Abstractions.Workflows;
using NexusCRM.Domain.Notifications;
using NexusCRM.Domain.Workflows;
using NexusCRM.Infrastructure.Persistence;

namespace NexusCRM.Infrastructure.Workflows;

internal sealed class WorkflowDefinitionRepository : IWorkflowDefinitionRepository
{
    private readonly NexusDbContext _db;

    public WorkflowDefinitionRepository(NexusDbContext db) => _db = db;

    public async Task<IReadOnlyList<WorkflowDefinition>> ListAsync(CancellationToken cancellationToken = default) =>
        await _db.WorkflowDefinitions.AsNoTracking().OrderBy(w => w.Name).ToListAsync(cancellationToken);

    public Task<WorkflowDefinition?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.WorkflowDefinitions.FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

    public async Task<IReadOnlyList<WorkflowDefinition>> ListEnabledByTriggerAsync(
        WorkflowTriggerType triggerType,
        CancellationToken cancellationToken = default) =>
        await _db.WorkflowDefinitions
            .Where(w => w.IsEnabled && w.TriggerType == triggerType)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default) =>
        await _db.WorkflowDefinitions.AddAsync(definition, cancellationToken);
}

internal sealed class WorkflowRunRepository : IWorkflowRunRepository
{
    private readonly NexusDbContext _db;

    public WorkflowRunRepository(NexusDbContext db) => _db = db;

    public async Task AddAsync(WorkflowRun run, CancellationToken cancellationToken = default) =>
        await _db.WorkflowRuns.AddAsync(run, cancellationToken);

    public async Task<IReadOnlyList<WorkflowRun>> ListRecentAsync(
        int take = 50,
        CancellationToken cancellationToken = default) =>
        await _db.WorkflowRuns.AsNoTracking()
            .OrderByDescending(r => r.CreatedAtUtc)
            .Take(take)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<WorkflowRun>> DequeuePendingAsync(
        int take,
        CancellationToken cancellationToken = default)
    {
        var pending = await _db.WorkflowRuns
            .Where(r => r.Status == WorkflowRunStatus.Pending)
            .OrderBy(r => r.CreatedAtUtc)
            .Take(take)
            .ToListAsync(cancellationToken);

        foreach (var run in pending)
        {
            run.MarkRunning();
        }

        if (pending.Count > 0)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }

        return pending;
    }

    public Task<WorkflowRun?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.WorkflowRuns.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
}

internal sealed class WorkflowDispatcher : IWorkflowDispatcher
{
    private readonly IWorkflowDefinitionRepository _definitions;
    private readonly IWorkflowRunRepository _runs;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<WorkflowDispatcher> _logger;

    public WorkflowDispatcher(
        IWorkflowDefinitionRepository definitions,
        IWorkflowRunRepository runs,
        IUnitOfWork unitOfWork,
        ILogger<WorkflowDispatcher> logger)
    {
        _definitions = definitions;
        _runs = runs;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task DispatchAsync(
        WorkflowTriggerType triggerType,
        string entityType,
        Guid entityId,
        Guid tenantId,
        IReadOnlyDictionary<string, string?> context,
        CancellationToken cancellationToken = default)
    {
        var workflows = await _definitions.ListEnabledByTriggerAsync(triggerType, cancellationToken);
        var contextJson = JsonSerializer.Serialize(context);

        foreach (var workflow in workflows.Where(w => w.Matches(context)))
        {
            var run = WorkflowRun.Create(
                tenantId,
                workflow.Id,
                triggerType,
                entityType,
                entityId,
                contextJson);

            await _runs.AddAsync(run, cancellationToken);
            _logger.LogInformation(
                "Queued workflow {WorkflowName} ({WorkflowId}) for {EntityType} {EntityId}",
                workflow.Name,
                workflow.Id,
                entityType,
                entityId);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

internal sealed class WorkflowActionExecutor : IWorkflowActionExecutor
{
    private readonly NexusDbContext _db;
    private readonly INotificationService _notifications;
    private readonly ILogger<WorkflowActionExecutor> _logger;

    public WorkflowActionExecutor(
        NexusDbContext db,
        INotificationService notifications,
        ILogger<WorkflowActionExecutor> logger)
    {
        _db = db;
        _notifications = notifications;
        _logger = logger;
    }

    public async Task<string> ExecuteAsync(
        WorkflowAction action,
        Guid tenantId,
        Guid entityId,
        string entityType,
        IReadOnlyDictionary<string, string?> context,
        CancellationToken cancellationToken = default)
    {
        switch (action.Type)
        {
            case WorkflowActionType.LogNotification:
            {
                var message = action.Value ?? "Workflow notification";
                var href = entityType.Equals("Lead", StringComparison.OrdinalIgnoreCase)
                    ? "/leads"
                    : entityType.Equals("Deal", StringComparison.OrdinalIgnoreCase)
                        ? "/deals"
                        : null;

                await _notifications.CreateAsync(
                    tenantId,
                    title: "Workflow",
                    body: message,
                    category: NotificationCategories.Workflow,
                    entityType: entityType,
                    entityId: entityId,
                    href: href,
                    cancellationToken: cancellationToken);

                _logger.LogInformation(
                    "Workflow notification for {EntityType} {EntityId}: {Message}",
                    entityType,
                    entityId,
                    message);
                return $"Notified: {message}";
            }
            case WorkflowActionType.CreateFollowUpTask:
            {
                var task = action.Value ?? "Follow up";
                _logger.LogInformation(
                    "Workflow created follow-up task '{Task}' for {EntityType} {EntityId}",
                    task,
                    entityType,
                    entityId);
                return $"Task created: {task}";
            }
            case WorkflowActionType.SetLeadScore when entityType.Equals("Lead", StringComparison.OrdinalIgnoreCase):
            {
                if (!int.TryParse(action.Value, out var score))
                {
                    throw new InvalidOperationException("SetLeadScore requires numeric Value.");
                }

                var lead = await _db.Leads.FirstOrDefaultAsync(l => l.Id == entityId, cancellationToken)
                    ?? throw new InvalidOperationException("Lead not found for SetLeadScore.");
                lead.SetScore(score);
                await _db.SaveChangesAsync(cancellationToken);
                return $"Lead score set to {score}";
            }
            case WorkflowActionType.AssignLeadOwner when entityType.Equals("Lead", StringComparison.OrdinalIgnoreCase):
            {
                if (!Guid.TryParse(action.Target, out var ownerId))
                {
                    throw new InvalidOperationException("AssignLeadOwner requires Target user id.");
                }

                var lead = await _db.Leads.FirstOrDefaultAsync(l => l.Id == entityId, cancellationToken)
                    ?? throw new InvalidOperationException("Lead not found for AssignLeadOwner.");
                lead.AssignOwner(ownerId);
                await _db.SaveChangesAsync(cancellationToken);
                return $"Lead assigned to {ownerId}";
            }
            default:
                throw new InvalidOperationException($"Unsupported action {action.Type} for {entityType}.");
        }
    }
}

public sealed class WorkflowExecutionService
{
    private readonly IWorkflowRunRepository _runs;
    private readonly IWorkflowDefinitionRepository _definitions;
    private readonly IWorkflowActionExecutor _executor;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<WorkflowExecutionService> _logger;

    public WorkflowExecutionService(
        IWorkflowRunRepository runs,
        IWorkflowDefinitionRepository definitions,
        IWorkflowActionExecutor executor,
        IUnitOfWork unitOfWork,
        ILogger<WorkflowExecutionService> logger)
    {
        _runs = runs;
        _definitions = definitions;
        _executor = executor;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<int> ProcessPendingAsync(int batchSize, CancellationToken cancellationToken)
    {
        var pending = await _runs.DequeuePendingAsync(batchSize, cancellationToken);
        foreach (var run in pending)
        {
            try
            {
                var definition = await _definitions.GetByIdAsync(run.WorkflowDefinitionId, cancellationToken);
                if (definition is null)
                {
                    run.MarkFailed("Workflow definition missing.");
                    continue;
                }

                var context = JsonSerializer.Deserialize<Dictionary<string, string?>>(run.ContextJson)
                    ?? new Dictionary<string, string?>();

                var results = new List<string>();
                foreach (var action in definition.Actions)
                {
                    results.Add(await _executor.ExecuteAsync(
                        action,
                        run.TenantId,
                        run.EntityId,
                        run.EntityType,
                        context,
                        cancellationToken));
                }

                run.MarkSucceeded(string.Join(" | ", results));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Workflow run {RunId} failed", run.Id);
                run.MarkFailed(ex.Message);
            }
        }

        if (pending.Count > 0)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return pending.Count;
    }
}
