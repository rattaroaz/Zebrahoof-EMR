using Microsoft.Extensions.Options;

namespace Zebrahoof_EMR.Services;

/// <summary>
/// Starts an already-installed local engine in the background. Never downloads
/// binaries or models on boot.
/// </summary>
public sealed class LocalAiStartupHostedService : BackgroundService
{
    private readonly LocalAiEngineService _engine;
    private readonly LocalAiLibraryCatalogService _catalog;
    private readonly IOptions<LocalAiOptions> _options;
    private readonly ILogger<LocalAiStartupHostedService> _logger;

    public LocalAiStartupHostedService(
        LocalAiEngineService engine,
        LocalAiLibraryCatalogService catalog,
        IOptions<LocalAiOptions> options,
        ILogger<LocalAiStartupHostedService> logger)
    {
        _engine = engine;
        _catalog = catalog;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await _catalog.EnsureFreshAsync(stoppingToken);
            await _engine.RefreshStatusAsync(stoppingToken);
            if (!_options.Value.AutoStart)
            {
                return;
            }

            await _engine.TryStartIfInstalledAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal shutdown.
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Local AI startup probe failed");
        }
    }
}
