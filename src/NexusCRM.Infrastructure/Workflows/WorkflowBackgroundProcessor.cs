using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace NexusCRM.Infrastructure.Workflows;

/// <summary>
/// Processes pending workflow runs inside the API host (handy for local demos without a separate worker).
/// </summary>
public sealed class WorkflowBackgroundProcessor : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<WorkflowBackgroundProcessor> _logger;

    public WorkflowBackgroundProcessor(
        IServiceScopeFactory scopeFactory,
        ILogger<WorkflowBackgroundProcessor> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var executor = scope.ServiceProvider.GetRequiredService<WorkflowExecutionService>();
                var processed = await executor.ProcessPendingAsync(20, stoppingToken);
                if (processed > 0)
                {
                    _logger.LogInformation("API host processed {Count} workflow run(s)", processed);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Inline workflow processor failed");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}
