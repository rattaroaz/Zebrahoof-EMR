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
    private readonly IHostEnvironment _environment;
    private readonly ILogger<ClinicalDataPersistenceInitializer> _logger;

    public ClinicalDataPersistenceInitializer(
        MockClinicalDataService clinicalData,
        IHostEnvironment environment,
        ILogger<ClinicalDataPersistenceInitializer> logger)
    {
        _clinicalData = clinicalData;
        _environment = environment;
        _logger = logger;
    }

    /// <summary>
    /// The six-year chart writes into a real patient id and the clinical id space.
    /// It runs only for local Development and tests, and only when ZH_BENCH=1.
    /// </summary>
    public static bool ShouldSeedBenchmarkChart(bool isDevelopment, string? flag) =>
        isDevelopment && string.Equals(flag, "1", StringComparison.Ordinal);

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

        // After hydration. A database that already has chart rows replaces the
        // in-memory lists, so seeding any earlier is wiped on the next start.
        if (ShouldSeedBenchmarkChart(_environment.IsDevelopment(), Environment.GetEnvironmentVariable("ZH_BENCH")))
        {
            var seeded = _clinicalData.SeedLongitudinalChart(5, years: 6);
            _logger.LogInformation(
                "ZH_BENCH 6-year chart for patient 5: {Notes} notes, {Labs} labs, {Vitals} vitals, {Medications} meds, {Encounters} encounters",
                seeded.Notes, seeded.Labs, seeded.Vitals, seeded.Medications, seeded.Encounters);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
