using NexusCRM.Domain.Common;

namespace NexusCRM.Domain.Tenancy;

public sealed class Tenant : AggregateRoot, IAuditable
{
    private Tenant(Guid id, string name, string slug, string plan)
        : base(id)
    {
        Name = name;
        Slug = slug;
        Plan = plan;
        Status = TenantStatus.Active;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    private Tenant()
    {
    }

    public string Name { get; private set; } = string.Empty;

    public string Slug { get; private set; } = string.Empty;

    public string Plan { get; private set; } = string.Empty;

    public TenantStatus Status { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public Guid? CreatedBy { get; private set; }

    public DateTimeOffset? ModifiedAtUtc { get; private set; }

    public Guid? ModifiedBy { get; private set; }

    public static Tenant Create(string name, string slug, string plan = "standard", Guid? id = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);

        var tenant = new Tenant(id ?? Guid.NewGuid(), name.Trim(), slug.Trim().ToLowerInvariant(), plan);
        tenant.Raise(new TenantCreatedDomainEvent(tenant.Id, tenant.Slug));
        return tenant;
    }

    public void Suspend()
    {
        Status = TenantStatus.Suspended;
        ModifiedAtUtc = DateTimeOffset.UtcNow;
    }

    public void Activate()
    {
        Status = TenantStatus.Active;
        ModifiedAtUtc = DateTimeOffset.UtcNow;
    }
}

public enum TenantStatus
{
    Active = 0,
    Suspended = 1,
    Cancelled = 2
}

public sealed record TenantCreatedDomainEvent(Guid TenantId, string Slug) : DomainEvent;
