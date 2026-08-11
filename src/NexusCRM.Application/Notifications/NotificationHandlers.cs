using FluentValidation;
using NexusCRM.Application.Abstractions.Messaging;
using NexusCRM.Application.Abstractions.Notifications;
using NexusCRM.Application.Abstractions.Persistence;
using NexusCRM.Application.Abstractions.Tenancy;
using NexusCRM.Contracts.Notifications;
using NexusCRM.Shared.Results;

namespace NexusCRM.Application.Notifications;

internal static class NotificationMappings
{
    public static NotificationDto ToDto(this Domain.Notifications.AppNotification n) =>
        new(
            n.Id,
            n.TenantId,
            n.RecipientUserId,
            n.Title,
            n.Body,
            n.Category,
            n.EntityType,
            n.EntityId,
            n.Href,
            n.ActorUserId,
            n.IsRead,
            n.ReadAtUtc,
            n.CreatedAtUtc);
}

public sealed record ListNotificationsQuery(int Take = 40) : IQuery<NotificationListResponse>;

public sealed class ListNotificationsQueryHandler
    : MediatR.IRequestHandler<ListNotificationsQuery, Result<NotificationListResponse>>
{
    private readonly INotificationRepository _notifications;
    private readonly ICurrentUser _currentUser;

    public ListNotificationsQueryHandler(INotificationRepository notifications, ICurrentUser currentUser)
    {
        _notifications = notifications;
        _currentUser = currentUser;
    }

    public async Task<Result<NotificationListResponse>> Handle(
        ListNotificationsQuery request,
        CancellationToken cancellationToken)
    {
        var take = Math.Clamp(request.Take, 1, 100);
        var items = await _notifications.ListForUserAsync(_currentUser.UserId, take, cancellationToken);
        var unread = await _notifications.CountUnreadForUserAsync(_currentUser.UserId, cancellationToken);
        return Result.Success(new NotificationListResponse(items.Select(n => n.ToDto()).ToList(), unread));
    }
}

public sealed record MarkNotificationReadCommand(Guid NotificationId) : ICommand<NotificationDto>;

public sealed class MarkNotificationReadCommandValidator : AbstractValidator<MarkNotificationReadCommand>
{
    public MarkNotificationReadCommandValidator() => RuleFor(x => x.NotificationId).NotEmpty();
}

public sealed class MarkNotificationReadCommandHandler
    : MediatR.IRequestHandler<MarkNotificationReadCommand, Result<NotificationDto>>
{
    private readonly INotificationRepository _notifications;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public MarkNotificationReadCommandHandler(
        INotificationRepository notifications,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _notifications = notifications;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<NotificationDto>> Handle(
        MarkNotificationReadCommand request,
        CancellationToken cancellationToken)
    {
        var notification = await _notifications.GetByIdAsync(request.NotificationId, cancellationToken);
        if (notification is null)
        {
            return Result.Failure<NotificationDto>(Error.NotFound("Notification", request.NotificationId));
        }

        if (notification.RecipientUserId is not null &&
            notification.RecipientUserId != _currentUser.UserId)
        {
            return Result.Failure<NotificationDto>(Error.Forbidden("Notification is not for this user."));
        }

        notification.MarkRead();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(notification.ToDto());
    }
}

public sealed record MarkAllNotificationsReadCommand : ICommand<int>;

public sealed class MarkAllNotificationsReadCommandHandler
    : MediatR.IRequestHandler<MarkAllNotificationsReadCommand, Result<int>>
{
    private readonly INotificationRepository _notifications;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public MarkAllNotificationsReadCommandHandler(
        INotificationRepository notifications,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _notifications = notifications;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<int>> Handle(
        MarkAllNotificationsReadCommand request,
        CancellationToken cancellationToken)
    {
        var unread = await _notifications.ListUnreadForUserAsync(_currentUser.UserId, cancellationToken);
        foreach (var item in unread)
        {
            item.MarkRead();
        }

        if (unread.Count > 0)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Result.Success(unread.Count);
    }
}
