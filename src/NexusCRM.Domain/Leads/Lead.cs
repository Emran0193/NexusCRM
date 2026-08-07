using NexusCRM.Domain.Common;

namespace NexusCRM.Domain.Leads;

public sealed class Lead : AggregateRoot, ITenantScoped, IAuditable
{
    private readonly List<string> _tags = [];

    private Lead(
        Guid id,
        Guid tenantId,
        Guid pipelineId,
        Guid stageId,
        string title,
        string? source,
        string? email,
        string? phone,
        string? companyName,
        Guid? ownerUserId,
        Guid? customerId)
        : base(id)
    {
        TenantId = tenantId;
        PipelineId = pipelineId;
        StageId = stageId;
        Title = title;
        Source = source;
        Email = email;
        Phone = phone;
        CompanyName = companyName;
        OwnerUserId = ownerUserId;
        CustomerId = customerId;
        Score = 0;
        Status = LeadStatus.Open;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    private Lead()
    {
    }

    public Guid TenantId { get; private set; }

    public Guid PipelineId { get; private set; }

    public Guid StageId { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string? Source { get; private set; }

    public string? Email { get; private set; }

    public string? Phone { get; private set; }

    public string? CompanyName { get; private set; }

    public int Score { get; private set; }

    public LeadStatus Status { get; private set; }

    public Guid? OwnerUserId { get; private set; }

    public Guid? CustomerId { get; private set; }

    public string? Notes { get; private set; }

    public IReadOnlyCollection<string> Tags => _tags.AsReadOnly();

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public Guid? CreatedBy { get; private set; }

    public DateTimeOffset? ModifiedAtUtc { get; private set; }

    public Guid? ModifiedBy { get; private set; }

    public static Lead Create(
        Guid tenantId,
        Guid pipelineId,
        Guid stageId,
        string title,
        string? source = null,
        string? email = null,
        string? phone = null,
        string? companyName = null,
        Guid? ownerUserId = null,
        Guid? customerId = null,
        Guid? createdBy = null)
    {
        if (tenantId == Guid.Empty || pipelineId == Guid.Empty || stageId == Guid.Empty)
        {
            throw new ArgumentException("Tenant, pipeline, and stage are required.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        var lead = new Lead(
            Guid.NewGuid(),
            tenantId,
            pipelineId,
            stageId,
            title.Trim(),
            Normalize(source),
            Normalize(email),
            Normalize(phone),
            Normalize(companyName),
            ownerUserId,
            customerId)
        {
            CreatedBy = createdBy
        };

        lead.Raise(new LeadCreatedDomainEvent(lead.TenantId, lead.Id, lead.Title, lead.StageId));
        return lead;
    }

    public void UpdateDetails(
        string title,
        string? source,
        string? email,
        string? phone,
        string? companyName,
        string? notes,
        Guid? modifiedBy = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        Title = title.Trim();
        Source = Normalize(source);
        Email = Normalize(email);
        Phone = Normalize(phone);
        CompanyName = Normalize(companyName);
        Notes = Normalize(notes);
        Touch(modifiedBy);
    }

    public void SetScore(int score, Guid? modifiedBy = null)
    {
        if (score is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(score), "Score must be between 0 and 100.");
        }

        Score = score;
        Touch(modifiedBy);
        Raise(new LeadScoreChangedDomainEvent(TenantId, Id, Score));
    }

    public void MoveToStage(Guid stageId, bool isWon, bool isLost, Guid? modifiedBy = null)
    {
        if (StageId == stageId)
        {
            return;
        }

        var from = StageId;
        StageId = stageId;
        Status = isWon ? LeadStatus.Won : isLost ? LeadStatus.Lost : LeadStatus.Open;
        Touch(modifiedBy);
        Raise(new LeadStageChangedDomainEvent(TenantId, Id, from, stageId, Status.ToString()));
    }

    public void AssignOwner(Guid? ownerUserId, Guid? modifiedBy = null)
    {
        OwnerUserId = ownerUserId;
        Touch(modifiedBy);
    }

    public void LinkCustomer(Guid customerId, Guid? modifiedBy = null)
    {
        if (customerId == Guid.Empty)
        {
            throw new ArgumentException("CustomerId is required.", nameof(customerId));
        }

        CustomerId = customerId;
        Touch(modifiedBy);
    }

    public void AddTag(string tag, Guid? modifiedBy = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tag);
        var normalized = tag.Trim().ToLowerInvariant();
        if (_tags.Contains(normalized, StringComparer.OrdinalIgnoreCase))
        {
            return;
        }

        _tags.Add(normalized);
        Touch(modifiedBy);
    }

    public void AppendNote(string note, Guid? modifiedBy = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(note);
        var fragment = note.Trim();
        Notes = string.IsNullOrWhiteSpace(Notes) ? fragment : $"{Notes}\n{fragment}";
        Touch(modifiedBy);
    }

    private void Touch(Guid? actorId)
    {
        ModifiedAtUtc = DateTimeOffset.UtcNow;
        ModifiedBy = actorId;
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public enum LeadStatus
{
    Open = 0,
    Won = 1,
    Lost = 2
}

public sealed record LeadCreatedDomainEvent(Guid TenantId, Guid LeadId, string Title, Guid StageId) : DomainEvent;

public sealed record LeadStageChangedDomainEvent(
    Guid TenantId,
    Guid LeadId,
    Guid FromStageId,
    Guid ToStageId,
    string Status) : DomainEvent;

public sealed record LeadScoreChangedDomainEvent(Guid TenantId, Guid LeadId, int Score) : DomainEvent;
