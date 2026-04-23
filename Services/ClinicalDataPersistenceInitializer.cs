namespace Zebrahoof_EMR.Services;

/// <summary>
/// On app startup, applies pending EF Core migrations and then asks the
/// singleton <see cref="MockClinicalDataService"/> to hydrate its in-memory
/// chart state from the local database. On the very first run (when the
/// chart tables are empty) the service instead seeds the database with the
/// generated mock data, turning that initial mock state into the ongoing
/// record that survives application restarts.
/// </summary>
public class ClinicalDataPersistenceInitializer : IHostedService
{
    private readonly MockClinicalDataService _clinicalData;
    private readonly ILogger<ClinicalDataPersistenceInitializer> _logger;

    public ClinicalDataPersistenceInitializer(
        MockClinicalDataService clinicalData,
        ILogger<ClinicalDataPersistenceInitializer> logger)
    {
        _clinicalData = clinicalData;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _clinicalData.InitializeFromDatabaseAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Clinical data hydration failed; the in-memory mock chart will still be available.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
