namespace Zebrahoof_EMR.Services;

/// <summary>
/// Drops note drafts that have sat longer than 24 hours.
/// </summary>
public sealed class NoteDraftSweepService : BackgroundService
{
    private readonly NoteDraftService _drafts;
    private readonly ILogger<NoteDraftSweepService> _logger;

    public NoteDraftSweepService(NoteDraftService drafts, ILogger<NoteDraftSweepService> logger)
    {
        _drafts = drafts;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(10));
        while (!stoppingToken.IsCancellationRequested)
        {
            Sweep();
            try
            {
                if (!await timer.WaitForNextTickAsync(stoppingToken))
                {
                    break;
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private void Sweep()
    {
        var removed = _drafts.Sweep();
        if (removed > 0)
        {
            _logger.LogInformation("Removed {Count} expired note drafts", removed);
        }
    }
}
