using NexusCRM.Domain.Common;

namespace NexusCRM.Domain.Notifications;

public sealed class AppNotification : AggregateRoot, ITenantScoped
{
    private AppNotification(
        Guid id,
        Guid tenantId,
        string title,
        string body,
        string category,
        Guid? recipientUserId,
        string? entityType,
        Guid? entityId,
        string? href,
        Guid? actorUserId)
        : base(id)
    {
        TenantId = tenantId;
        Title = title;
        Body = body;
        Category = category;
        RecipientUserId = recipientUserId;
        EntityType = entityType;
        EntityId = entityId;
        Href = href;
        ActorUserId = actorUserId;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    private AppNotification()
    {
    }

    public Guid TenantId { get; private set; }

    public Guid? RecipientUserId { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string Body { get; private set; } = string.Empty;

    public string Category { get; private set; } = string.Empty;

    public string? EntityType { get; private set; }

    public Guid? EntityId { get; private set; }

    public string? Href { get; private set; }

    public Guid? ActorUserId { get; private set; }

    public bool IsRead { get; private set; }

    public DateTimeOffset? ReadAtUtc { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static AppNotification Create(
        Guid tenantId,
        string title,
        string body,
        string category,
        Guid? recipientUserId = null,
        string? entityType = null,
        Guid? entityId = null,
        string? href = null,
        Guid? actorUserId = null)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(body);
        ArgumentException.ThrowIfNullOrWhiteSpace(category);

        return new AppNotification(
            Guid.NewGuid(),
            tenantId,
            title.Trim(),
            body.Trim(),
            category.Trim().ToLowerInvariant(),
            recipientUserId,
            string.IsNullOrWhiteSpace(entityType) ? null : entityType.Trim(),
            entityId,
            string.IsNullOrWhiteSpace(href) ? null : href.Trim(),
            actorUserId);
    }

    public void MarkRead()
    {
        if (IsRead)
        {
            return;
        }

        IsRead = true;
        ReadAtUtc = DateTimeOffset.UtcNow;
    }
}

public static class NotificationCategories
{
    public const string Workflow = "workflow";
    public const string Lead = "lead";
    public const string Deal = "deal";
    public const string System = "system";
}
