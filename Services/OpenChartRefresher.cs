namespace Zebrahoof_EMR.Services;

/// <summary>
/// Rebuilds the chart this circuit is showing after a clinical write.
/// The LRU entry stays invalidated, so the next open still loads from the source.
/// </summary>
public sealed class OpenChartRefresher : IDisposable
{
    private readonly MockClinicalDataService _clinical;
    private readonly IPatientChartLoader _loader;
    private readonly OpenChartView _view;
    private readonly ILogger<OpenChartRefresher> _logger;
    private int _generation;

    public OpenChartRefresher(
        MockClinicalDataService clinical,
        IPatientChartLoader loader,
        OpenChartView view,
        ILogger<OpenChartRefresher> logger)
    {
        _clinical = clinical;
        _loader = loader;
        _view = view;
        _logger = logger;
        _clinical.PatientDataChanged += OnPatientDataChanged;
    }

    private void OnPatientDataChanged(int patientId) => _ = RefreshAsync(patientId);

    private async Task RefreshAsync(int patientId)
    {
        if (_view.Snapshot is not { } open || open.PatientId != patientId)
        {
            return;
        }

        var generation = Interlocked.Increment(ref _generation);
        try
        {
            var loaded = await _loader.LoadAsync(patientId);
            if (loaded == null || generation != Volatile.Read(ref _generation))
            {
                return;
            }

            if (_view.Snapshot is not { } current || current.PatientId != patientId)
            {
                return;
            }

            _view.Publish(new ChartCacheOpen(loaded, ChartCacheSource.Loader, 0));
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Open chart refresh failed for patient {PatientId}", patientId);
        }
    }

    public void Dispose() => _clinical.PatientDataChanged -= OnPatientDataChanged;
}
