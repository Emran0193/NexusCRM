using NexusCRM.Domain.Common;

namespace NexusCRM.Domain.Customers;

public sealed class Customer : AggregateRoot, ITenantScoped, IAuditable, ISoftDeletable
{
    private readonly List<CustomerContact> _contacts = [];
    private readonly List<CustomerNote> _notes = [];
    private readonly List<CustomerTimelineEntry> _timeline = [];
    private readonly List<string> _tags = [];

    private Customer(
        Guid id,
        Guid tenantId,
        CustomerType type,
        string displayName,
        string? email,
        string? phone)
        : base(id)
    {
        TenantId = tenantId;
        Type = type;
        DisplayName = displayName;
        Email = email;
        Phone = phone;
        Status = CustomerStatus.Active;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    private Customer()
    {
    }

    public Guid TenantId { get; private set; }

    public CustomerType Type { get; private set; }

    public string DisplayName { get; private set; } = string.Empty;

    public string? Email { get; private set; }

    public string? Phone { get; private set; }

    public CustomerStatus Status { get; private set; }

    public IReadOnlyCollection<CustomerContact> Contacts => _contacts.AsReadOnly();

    public IReadOnlyCollection<CustomerNote> Notes => _notes.AsReadOnly();

    public IReadOnlyCollection<CustomerTimelineEntry> Timeline => _timeline.AsReadOnly();

    public IReadOnlyCollection<string> Tags => _tags.AsReadOnly();

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public Guid? CreatedBy { get; private set; }

    public DateTimeOffset? ModifiedAtUtc { get; private set; }

    public Guid? ModifiedBy { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTimeOffset? DeletedAtUtc { get; private set; }

    public Guid? DeletedBy { get; private set; }

    public static Customer CreateIndividual(
        Guid tenantId,
        string displayName,
        string? email = null,
        string? phone = null,
        Guid? createdBy = null)
        => Create(tenantId, CustomerType.Individual, displayName, email, phone, createdBy);

    public static Customer CreateOrganization(
        Guid tenantId,
        string displayName,
        string? email = null,
        string? phone = null,
        Guid? createdBy = null)
        => Create(tenantId, CustomerType.Organization, displayName, email, phone, createdBy);

    private static Customer Create(
        Guid tenantId,
        CustomerType type,
        string displayName,
        string? email,
        string? phone,
        Guid? createdBy)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        var customer = new Customer(
            Guid.NewGuid(),
            tenantId,
            type,
            displayName.Trim(),
            NormalizeOptional(email),
            NormalizeOptional(phone))
        {
            CreatedBy = createdBy
        };

        customer.AppendTimeline("customer.created", $"Customer '{customer.DisplayName}' created", createdBy);
        customer.Raise(new CustomerCreatedDomainEvent(customer.TenantId, customer.Id, customer.DisplayName));
        return customer;
    }

    public void UpdateProfile(string displayName, string? email, string? phone, Guid? modifiedBy = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        DisplayName = displayName.Trim();
        Email = NormalizeOptional(email);
        Phone = NormalizeOptional(phone);
        Touch(modifiedBy);
        AppendTimeline("customer.updated", "Profile updated", modifiedBy);
        Raise(new CustomerUpdatedDomainEvent(TenantId, Id));
    }

    public CustomerContact AddContact(string name, string? email, string? phone, bool isPrimary, Guid? actorId = null)
    {
        var contact = CustomerContact.Create(name, email, phone, isPrimary);
        if (isPrimary)
        {
            foreach (var existing in _contacts)
            {
                existing.ClearPrimary();
            }
        }

        _contacts.Add(contact);
        Touch(actorId);
        AppendTimeline("customer.contact_added", $"Contact '{contact.Name}' added", actorId);
        return contact;
    }

    public CustomerNote AddNote(string body, Guid? authorUserId = null)
    {
        var note = CustomerNote.Create(body, authorUserId);
        _notes.Add(note);
        Touch(authorUserId);
        AppendTimeline("customer.note_added", "Note added", authorUserId);
        return note;
    }

    public void AddTag(string tag, Guid? actorId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tag);
        var normalized = tag.Trim().ToLowerInvariant();

        if (!_tags.Contains(normalized, StringComparer.OrdinalIgnoreCase))
        {
            _tags.Add(normalized);
            Touch(actorId);
            AppendTimeline("customer.tag_added", $"Tag '{normalized}' added", actorId);
        }
    }

    public void SoftDelete(Guid? deletedBy = null)
    {
        if (IsDeleted)
        {
            return;
        }

        IsDeleted = true;
        DeletedAtUtc = DateTimeOffset.UtcNow;
        DeletedBy = deletedBy;
        Status = CustomerStatus.Archived;
        AppendTimeline("customer.deleted", "Customer archived", deletedBy);
        Raise(new CustomerDeletedDomainEvent(TenantId, Id));
    }

    private void AppendTimeline(string eventType, string summary, Guid? actorUserId)
    {
        _timeline.Add(CustomerTimelineEntry.Create(eventType, summary, actorUserId));
    }

    private void Touch(Guid? actorId)
    {
        ModifiedAtUtc = DateTimeOffset.UtcNow;
        ModifiedBy = actorId;
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public enum CustomerType
{
    Individual = 0,
    Organization = 1
}

public enum CustomerStatus
{
    Active = 0,
    Inactive = 1,
    Archived = 2
}

public sealed class CustomerContact
{
    private CustomerContact(Guid id, string name, string? email, string? phone, bool isPrimary)
    {
        Id = id;
        Name = name;
        Email = email;
        Phone = phone;
        IsPrimary = isPrimary;
    }

    private CustomerContact()
    {
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string? Email { get; private set; }

    public string? Phone { get; private set; }

    public bool IsPrimary { get; private set; }

    public static CustomerContact Create(string name, string? email, string? phone, bool isPrimary)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new CustomerContact(
            Guid.NewGuid(),
            name.Trim(),
            string.IsNullOrWhiteSpace(email) ? null : email.Trim(),
            string.IsNullOrWhiteSpace(phone) ? null : phone.Trim(),
            isPrimary);
    }

    internal void ClearPrimary() => IsPrimary = false;
}

public sealed class CustomerNote
{
    private CustomerNote(Guid id, string body, Guid? authorUserId)
    {
        Id = id;
        Body = body;
        AuthorUserId = authorUserId;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    private CustomerNote()
    {
    }

    public Guid Id { get; private set; }

    public string Body { get; private set; } = string.Empty;

    public Guid? AuthorUserId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static CustomerNote Create(string body, Guid? authorUserId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(body);
        return new CustomerNote(Guid.NewGuid(), body.Trim(), authorUserId);
    }
}

public sealed class CustomerTimelineEntry
{
    private CustomerTimelineEntry(Guid id, string eventType, string summary, Guid? actorUserId)
    {
        Id = id;
        EventType = eventType;
        Summary = summary;
        ActorUserId = actorUserId;
        OccurredAtUtc = DateTimeOffset.UtcNow;
    }

    private CustomerTimelineEntry()
    {
    }

    public Guid Id { get; private set; }

    public string EventType { get; private set; } = string.Empty;

    public string Summary { get; private set; } = string.Empty;

    public Guid? ActorUserId { get; private set; }

    public DateTimeOffset OccurredAtUtc { get; private set; }

    public static CustomerTimelineEntry Create(string eventType, string summary, Guid? actorUserId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        ArgumentException.ThrowIfNullOrWhiteSpace(summary);
        return new CustomerTimelineEntry(Guid.NewGuid(), eventType, summary.Trim(), actorUserId);
    }
}

public sealed record CustomerCreatedDomainEvent(Guid TenantId, Guid CustomerId, string DisplayName) : DomainEvent;

public sealed record CustomerUpdatedDomainEvent(Guid TenantId, Guid CustomerId) : DomainEvent;

public sealed record CustomerDeletedDomainEvent(Guid TenantId, Guid CustomerId) : DomainEvent;
