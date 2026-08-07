using NexusCRM.Domain.Common;

namespace NexusCRM.Domain.Pipelines;

public sealed class Pipeline : AggregateRoot, ITenantScoped
{
    private readonly List<PipelineStage> _stages = [];

    private Pipeline(Guid id, Guid tenantId, string name, PipelineType type)
        : base(id)
    {
        TenantId = tenantId;
        Name = name;
        Type = type;
        IsDefault = true;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    private Pipeline()
    {
    }

    public Guid TenantId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public PipelineType Type { get; private set; }

    public bool IsDefault { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public IReadOnlyCollection<PipelineStage> Stages => _stages.AsReadOnly();

    public static Pipeline Create(Guid tenantId, string name, PipelineType type, Guid? id = null)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new Pipeline(id ?? Guid.NewGuid(), tenantId, name.Trim(), type);
    }

    public PipelineStage AddStage(
        string name,
        int sortOrder,
        bool isWon = false,
        bool isLost = false,
        int? winProbability = null,
        Guid? id = null)
    {
        if (isWon && isLost)
        {
            throw new InvalidOperationException("A stage cannot be both won and lost.");
        }

        var stage = PipelineStage.Create(name, sortOrder, isWon, isLost, winProbability, id);
        _stages.Add(stage);
        return stage;
    }

    public PipelineStage? GetStage(Guid stageId) => _stages.FirstOrDefault(s => s.Id == stageId);

    public PipelineStage GetInitialStage() =>
        _stages.OrderBy(s => s.SortOrder).FirstOrDefault()
        ?? throw new InvalidOperationException("Pipeline has no stages.");
}

public enum PipelineType
{
    Lead = 0,
    Deal = 1
}

public sealed class PipelineStage
{
    private PipelineStage(
        Guid id,
        string name,
        int sortOrder,
        bool isWon,
        bool isLost,
        int? winProbability)
    {
        Id = id;
        Name = name;
        SortOrder = sortOrder;
        IsWon = isWon;
        IsLost = isLost;
        WinProbability = winProbability;
    }

    private PipelineStage()
    {
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public int SortOrder { get; private set; }

    public bool IsWon { get; private set; }

    public bool IsLost { get; private set; }

    public int? WinProbability { get; private set; }

    public bool IsTerminal => IsWon || IsLost;

    public static PipelineStage Create(
        string name,
        int sortOrder,
        bool isWon,
        bool isLost,
        int? winProbability,
        Guid? id = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new PipelineStage(
            id ?? Guid.NewGuid(),
            name.Trim(),
            sortOrder,
            isWon,
            isLost,
            winProbability);
    }
}
