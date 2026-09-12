namespace NexusCRM.Plugins.Abstractions;

public interface INotificationChannelPlugin : INexusPlugin
{
    Task DeliverAsync(PluginNotificationMessage message, CancellationToken cancellationToken = default);
}

public sealed record PluginNotificationMessage(
    Guid TenantId,
    string Title,
    string Body,
    string Category,
    string? EntityType,
    Guid? EntityId);
