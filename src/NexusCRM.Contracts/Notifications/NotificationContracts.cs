namespace NexusCRM.Contracts.Notifications;

public sealed record NotificationDto(
    Guid Id,
    Guid TenantId,
    Guid? RecipientUserId,
    string Title,
    string Body,
    string Category,
    string? EntityType,
    Guid? EntityId,
    string? Href,
    Guid? ActorUserId,
    bool IsRead,
    DateTimeOffset? ReadAtUtc,
    DateTimeOffset CreatedAtUtc);

public sealed record NotificationListResponse(
    IReadOnlyList<NotificationDto> Items,
    int UnreadCount);

public sealed record MarkNotificationsReadRequest(IReadOnlyList<Guid>? Ids);
