using NexusCRM.Application.Abstractions.Notifications;
using NexusCRM.Contracts.Notifications;

namespace NexusCRM.Infrastructure.Realtime;

internal sealed class NoOpNotificationRealtimePublisher : INotificationRealtimePublisher
{
    public Task PublishCreatedAsync(
        Guid tenantId,
        NotificationDto notification,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
