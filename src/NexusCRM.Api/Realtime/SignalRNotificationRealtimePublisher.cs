using Microsoft.AspNetCore.SignalR;
using NexusCRM.Application.Abstractions.Notifications;
using NexusCRM.Contracts.Notifications;

namespace NexusCRM.Api.Realtime;

public sealed class SignalRNotificationRealtimePublisher : INotificationRealtimePublisher
{
    private readonly IHubContext<BoardHub> _hub;

    public SignalRNotificationRealtimePublisher(IHubContext<BoardHub> hub) => _hub = hub;

    public Task PublishCreatedAsync(
        Guid tenantId,
        NotificationDto notification,
        CancellationToken cancellationToken = default) =>
        _hub.Clients
            .Group(BoardHub.TenantGroup(tenantId))
            .SendAsync("NotificationCreated", notification, cancellationToken);
}
