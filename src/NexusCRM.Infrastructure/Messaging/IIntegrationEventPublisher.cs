using Microsoft.Extensions.Logging;

namespace NexusCRM.Infrastructure.Messaging;

public interface IIntegrationEvent
{
    Guid EventId { get; }

    DateTimeOffset OccurredOnUtc { get; }

    string EventType { get; }
}

public interface IIntegrationEventPublisher
{
    Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken = default);
}

/// <summary>
/// Development publisher that logs events. Replaced by MassTransit/RabbitMQ in non-dev environments.
/// </summary>
internal sealed class LoggingIntegrationEventPublisher : IIntegrationEventPublisher
{
    private readonly Microsoft.Extensions.Logging.ILogger<LoggingIntegrationEventPublisher> _logger;

    public LoggingIntegrationEventPublisher(
        Microsoft.Extensions.Logging.ILogger<LoggingIntegrationEventPublisher> logger)
    {
        _logger = logger;
    }

    public Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Published integration event {EventType} ({EventId})",
            integrationEvent.EventType,
            integrationEvent.EventId);
        return Task.CompletedTask;
    }
}
