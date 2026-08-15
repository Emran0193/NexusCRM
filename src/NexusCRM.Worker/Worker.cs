using NexusCRM.Infrastructure.Workflows;

namespace NexusCRM.Worker;

/// <summary>
/// Polls pending workflow runs and executes matched actions.
/// </summary>
public sealed class Worker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<Worker> _logger;

    public Worker(IServiceScopeFactory scopeFactory, ILogger<Worker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("NexusCRM Worker started (workflow processor)");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var executor = scope.ServiceProvider.GetRequiredService<WorkflowExecutionService>();
                var processed = await executor.ProcessPendingAsync(20, stoppingToken);
                if (processed > 0)
                {
                    _logger.LogInformation("Processed {Count} workflow run(s)", processed);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Workflow processing loop failed");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}
