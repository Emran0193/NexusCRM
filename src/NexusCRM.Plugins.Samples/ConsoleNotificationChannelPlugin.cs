using Microsoft.Extensions.Logging;
using NexusCRM.Plugins.Abstractions;

namespace NexusCRM.Plugins.Samples;

/// <summary>Demo notification channel that logs outbound messages (stand-in for Slack/email).</summary>
public sealed class ConsoleNotificationChannelPlugin : INotificationChannelPlugin
{
    private readonly ILogger<ConsoleNotificationChannelPlugin> _logger;

    public ConsoleNotificationChannelPlugin(ILogger<ConsoleNotificationChannelPlugin> logger) => _logger = logger;

    public string Id => "samples.console-notification-channel";

    public string Name => "Console notification channel";

    public string Version => "1.0.0";

    public string Description => "Delivers workflow/system notifications to the application log (demo channel).";

    public IReadOnlyList<string> Capabilities { get; } = [PluginCapabilities.NotificationChannel];

    public Task DeliverAsync(PluginNotificationMessage message, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Plugin channel {PluginId} delivered [{Category}] {Title}: {Body} (tenant {TenantId})",
            Id,
            message.Category,
            message.Title,
            message.Body,
            message.TenantId);
        return Task.CompletedTask;
    }
}
