using NexusCRM.Domain.Common;

namespace NexusCRM.Domain.Deals;

public sealed class Deal : AggregateRoot, ITenantScoped, IAuditable
{
    private Deal(
        Guid id,
        Guid tenantId,
        Guid pipelineId,
        Guid stageId,
        string title,
        decimal amount,
        string currency,
        Guid? ownerUserId,
        Guid? customerId,
        Guid? leadId,
        DateOnly? expectedCloseDate)
        : base(id)
    {
        TenantId = tenantId;
        PipelineId = pipelineId;
        StageId = stageId;
        Title = title;
        Amount = amount;
        Currency = currency;
        OwnerUserId = ownerUserId;
        CustomerId = customerId;
        LeadId = leadId;
        ExpectedCloseDate = expectedCloseDate;
        Status = DealStatus.Open;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    private Deal()
    {
    }

    public Guid TenantId { get; private set; }

    public Guid PipelineId { get; private set; }

    public Guid StageId { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public decimal Amount { get; private set; }

    public string Currency { get; private set; } = "INR";

    public DealStatus Status { get; private set; }

    public Guid? OwnerUserId { get; private set; }

    public Guid? CustomerId { get; private set; }

    public Guid? LeadId { get; private set; }

    public DateOnly? ExpectedCloseDate { get; private set; }

    public DateTimeOffset? ClosedAtUtc { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public Guid? CreatedBy { get; private set; }

    public DateTimeOffset? ModifiedAtUtc { get; private set; }

    public Guid? ModifiedBy { get; private set; }

    public static Deal Create(
        Guid tenantId,
        Guid pipelineId,
        Guid stageId,
        string title,
        decimal amount,
        string currency = "INR",
        Guid? ownerUserId = null,
        Guid? customerId = null,
        Guid? leadId = null,
        DateOnly? expectedCloseDate = null,
        Guid? createdBy = null)
    {
        if (tenantId == Guid.Empty || pipelineId == Guid.Empty || stageId == Guid.Empty)
        {
            throw new ArgumentException("Tenant, pipeline, and stage are required.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount));
        }

        var deal = new Deal(
            Guid.NewGuid(),
            tenantId,
            pipelineId,
            stageId,
            title.Trim(),
            amount,
            string.IsNullOrWhiteSpace(currency) ? "INR" : currency.Trim().ToUpperInvariant(),
            ownerUserId,
            customerId,
            leadId,
            expectedCloseDate)
        {
            CreatedBy = createdBy
        };

        deal.Raise(new DealCreatedDomainEvent(deal.TenantId, deal.Id, deal.Title, deal.Amount, deal.StageId));
        return deal;
    }

    public void Update(
        string title,
        decimal amount,
        string currency,
        DateOnly? expectedCloseDate,
        Guid? modifiedBy = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount));
        }

        Title = title.Trim();
        Amount = amount;
        Currency = currency.Trim().ToUpperInvariant();
        ExpectedCloseDate = expectedCloseDate;
        Touch(modifiedBy);
    }

    public void MoveToStage(Guid stageId, bool isWon, bool isLost, Guid? modifiedBy = null)
    {
        if (StageId == stageId)
        {
            return;
        }

        var from = StageId;
        StageId = stageId;

        if (isWon)
        {
            Status = DealStatus.Won;
            ClosedAtUtc = DateTimeOffset.UtcNow;
        }
        else if (isLost)
        {
            Status = DealStatus.Lost;
            ClosedAtUtc = DateTimeOffset.UtcNow;
        }
        else
        {
            Status = DealStatus.Open;
            ClosedAtUtc = null;
        }

        // Bump concurrency token material for optimistic UI moves.
        RowVersion = Guid.NewGuid().ToByteArray();
        Touch(modifiedBy);
        Raise(new DealStageChangedDomainEvent(TenantId, Id, from, stageId, Status.ToString(), Amount));
    }

    public void AssignOwner(Guid? ownerUserId, Guid? modifiedBy = null)
    {
        OwnerUserId = ownerUserId;
        Touch(modifiedBy);
    }

    private void Touch(Guid? actorId)
    {
        ModifiedAtUtc = DateTimeOffset.UtcNow;
        ModifiedBy = actorId;
    }
}

public enum DealStatus
{
    Open = 0,
    Won = 1,
    Lost = 2
}

public sealed record DealCreatedDomainEvent(
    Guid TenantId,
    Guid DealId,
    string Title,
    decimal Amount,
    Guid StageId) : DomainEvent;

public sealed record DealStageChangedDomainEvent(
    Guid TenantId,
    Guid DealId,
    Guid FromStageId,
    Guid ToStageId,
    string Status,
    decimal Amount) : DomainEvent;
