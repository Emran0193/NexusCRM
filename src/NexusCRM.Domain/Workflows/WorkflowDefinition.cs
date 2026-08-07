using NexusCRM.Domain.Common;

namespace NexusCRM.Domain.Workflows;

public sealed class WorkflowDefinition : AggregateRoot, ITenantScoped
{
    private readonly List<WorkflowCondition> _conditions = [];
    private readonly List<WorkflowAction> _actions = [];

    private WorkflowDefinition(
        Guid id,
        Guid tenantId,
        string name,
        string description,
        WorkflowTriggerType triggerType)
        : base(id)
    {
        TenantId = tenantId;
        Name = name;
        Description = description;
        TriggerType = triggerType;
        IsEnabled = true;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    private WorkflowDefinition()
    {
    }

    public Guid TenantId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public WorkflowTriggerType TriggerType { get; private set; }

    public bool IsEnabled { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public IReadOnlyCollection<WorkflowCondition> Conditions => _conditions.AsReadOnly();

    public IReadOnlyCollection<WorkflowAction> Actions => _actions.AsReadOnly();

    public static WorkflowDefinition Create(
        Guid tenantId,
        string name,
        string description,
        WorkflowTriggerType triggerType,
        Guid? id = null)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new WorkflowDefinition(
            id ?? Guid.NewGuid(),
            tenantId,
            name.Trim(),
            description?.Trim() ?? string.Empty,
            triggerType);
    }

    public void AddCondition(string field, WorkflowOperator op, string value)
    {
        _conditions.Add(WorkflowCondition.Create(field, op, value));
    }

    public void AddAction(WorkflowActionType type, string? target = null, string? value = null)
    {
        _actions.Add(WorkflowAction.Create(type, target, value));
    }

    public void Enable() => IsEnabled = true;

    public void Disable() => IsEnabled = false;

    public bool Matches(IReadOnlyDictionary<string, string?> context)
    {
        if (!IsEnabled || _actions.Count == 0)
        {
            return false;
        }

        return _conditions.All(c => c.Evaluate(context));
    }
}

public enum WorkflowTriggerType
{
    LeadStageChanged = 0,
    DealStageChanged = 1,
    LeadScoreChanged = 2,
    LeadCreated = 3
}

public enum WorkflowOperator
{
    Equals = 0,
    NotEquals = 1,
    GreaterThan = 2,
    LessThan = 3,
    Contains = 4
}

public enum WorkflowActionType
{
    LogNotification = 0,
    SetLeadScore = 1,
    AssignLeadOwner = 2,
    CreateFollowUpTask = 3
}

public sealed class WorkflowCondition
{
    private WorkflowCondition(Guid id, string field, WorkflowOperator op, string value)
    {
        Id = id;
        Field = field;
        Operator = op;
        Value = value;
    }

    private WorkflowCondition()
    {
    }

    public Guid Id { get; private set; }

    public string Field { get; private set; } = string.Empty;

    public WorkflowOperator Operator { get; private set; }

    public string Value { get; private set; } = string.Empty;

    public static WorkflowCondition Create(string field, WorkflowOperator op, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(field);
        return new WorkflowCondition(Guid.NewGuid(), field.Trim(), op, value?.Trim() ?? string.Empty);
    }

    public bool Evaluate(IReadOnlyDictionary<string, string?> context)
    {
        context.TryGetValue(Field, out var actual);
        actual ??= string.Empty;

        return Operator switch
        {
            WorkflowOperator.Equals => string.Equals(actual, Value, StringComparison.OrdinalIgnoreCase),
            WorkflowOperator.NotEquals => !string.Equals(actual, Value, StringComparison.OrdinalIgnoreCase),
            WorkflowOperator.Contains => actual.Contains(Value, StringComparison.OrdinalIgnoreCase),
            WorkflowOperator.GreaterThan => decimal.TryParse(actual, out var a) && decimal.TryParse(Value, out var b) && a > b,
            WorkflowOperator.LessThan => decimal.TryParse(actual, out var c) && decimal.TryParse(Value, out var d) && c < d,
            _ => false
        };
    }
}

public sealed class WorkflowAction
{
    private WorkflowAction(Guid id, WorkflowActionType type, string? target, string? value)
    {
        Id = id;
        Type = type;
        Target = target;
        Value = value;
    }

    private WorkflowAction()
    {
    }

    public Guid Id { get; private set; }

    public WorkflowActionType Type { get; private set; }

    public string? Target { get; private set; }

    public string? Value { get; private set; }

    public static WorkflowAction Create(WorkflowActionType type, string? target, string? value) =>
        new(Guid.NewGuid(), type, target, value);
}
