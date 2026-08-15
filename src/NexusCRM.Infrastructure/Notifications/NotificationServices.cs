using Microsoft.EntityFrameworkCore;
using NexusCRM.Application.Abstractions.Notifications;
using NexusCRM.Application.Abstractions.Persistence;
using NexusCRM.Application.Abstractions.Plugins;
using NexusCRM.Contracts.Notifications;
using NexusCRM.Domain.Notifications;
using NexusCRM.Infrastructure.Persistence;

namespace NexusCRM.Infrastructure.Notifications;

internal sealed class NotificationRepository : INotificationRepository
{
    private readonly NexusDbContext _db;

    public NotificationRepository(NexusDbContext db) => _db = db;

    public async Task AddAsync(AppNotification notification, CancellationToken cancellationToken = default) =>
        await _db.Notifications.AddAsync(notification, cancellationToken);

    public Task<AppNotification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.Notifications.FirstOrDefaultAsync(n => n.Id == id, cancellationToken);

    public async Task<IReadOnlyList<AppNotification>> ListForUserAsync(
        Guid? userId,
        int take,
        CancellationToken cancellationToken = default) =>
        await VisibleTo(userId)
            .AsNoTracking()
            .OrderByDescending(n => n.CreatedAtUtc)
            .Take(take)
            .ToListAsync(cancellationToken);

    public Task<int> CountUnreadForUserAsync(Guid? userId, CancellationToken cancellationToken = default) =>
        VisibleTo(userId).CountAsync(n => !n.IsRead, cancellationToken);

    public async Task<IReadOnlyList<AppNotification>> ListUnreadForUserAsync(
        Guid? userId,
        CancellationToken cancellationToken = default) =>
        await VisibleTo(userId)
            .Where(n => !n.IsRead)
            .OrderByDescending(n => n.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    private IQueryable<AppNotification> VisibleTo(Guid? userId) =>
        _db.Notifications.Where(n =>
            n.RecipientUserId == null ||
            (userId.HasValue && n.RecipientUserId == userId));
}

internal sealed class NotificationService : INotificationService
{
    private readonly INotificationRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationRealtimePublisher _realtime;
    private readonly IPluginRuntime _plugins;

    public NotificationService(
        INotificationRepository repository,
        IUnitOfWork unitOfWork,
        INotificationRealtimePublisher realtime,
        IPluginRuntime plugins)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _realtime = realtime;
        _plugins = plugins;
    }

    public async Task<NotificationDto> CreateAsync(
        Guid tenantId,
        string title,
        string body,
        string category,
        Guid? recipientUserId = null,
        string? entityType = null,
        Guid? entityId = null,
        string? href = null,
        Guid? actorUserId = null,
        CancellationToken cancellationToken = default)
    {
        var notification = AppNotification.Create(
            tenantId,
            title,
            body,
            category,
            recipientUserId,
            entityType,
            entityId,
            href,
            actorUserId);

        await _repository.AddAsync(notification, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = new NotificationDto(
            notification.Id,
            notification.TenantId,
            notification.RecipientUserId,
            notification.Title,
            notification.Body,
            notification.Category,
            notification.EntityType,
            notification.EntityId,
            notification.Href,
            notification.ActorUserId,
            notification.IsRead,
            notification.ReadAtUtc,
            notification.CreatedAtUtc);

        await _realtime.PublishCreatedAsync(tenantId, dto, cancellationToken);

        await _plugins.DeliverNotificationAsync(
            tenantId,
            title,
            body,
            category,
            entityType,
            entityId,
            cancellationToken);

        return dto;
    }
}
