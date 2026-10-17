using Microsoft.Extensions.Hosting;

namespace Zebrahoof_EMR.Services;

/// <summary>
/// Drops idle snapshots and invalidates a cached chart when clinical data changes.
/// Invalidation removes the snapshot. It does not load a replacement.
/// </summary>
public sealed class PatientChartCacheMaintenance : BackgroundService
{
    private readonly MockClinicalDataService _clinical;
    private readonly PatientChartCache _cache;

    public PatientChartCacheMaintenance(MockClinicalDataService clinical, PatientChartCache cache)
    {
        _clinical = clinical;
        _cache = cache;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _clinical.PatientDataChanged += OnPatientDataChanged;
        try
        {
            using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                _cache.Sweep();
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        finally
        {
            _clinical.PatientDataChanged -= OnPatientDataChanged;
        }
    }

    private void OnPatientDataChanged(int patientId) => _cache.InvalidatePatient(patientId);
}
