using NexusCRM.Contracts.Notifications;

namespace NexusCRM.Application.Abstractions.Notifications;

public interface INotificationRepository
{
    Task AddAsync(Domain.Notifications.AppNotification notification, CancellationToken cancellationToken = default);

    Task<Domain.Notifications.AppNotification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Domain.Notifications.AppNotification>> ListForUserAsync(
        Guid? userId,
        int take,
        CancellationToken cancellationToken = default);

    Task<int> CountUnreadForUserAsync(Guid? userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Domain.Notifications.AppNotification>> ListUnreadForUserAsync(
        Guid? userId,
        CancellationToken cancellationToken = default);
}

public interface INotificationService
{
    Task<NotificationDto> CreateAsync(
        Guid tenantId,
        string title,
        string body,
        string category,
        Guid? recipientUserId = null,
        string? entityType = null,
        Guid? entityId = null,
        string? href = null,
        Guid? actorUserId = null,
        CancellationToken cancellationToken = default);
}

public interface INotificationRealtimePublisher
{
    Task PublishCreatedAsync(
        Guid tenantId,
        NotificationDto notification,
        CancellationToken cancellationToken = default);
}
