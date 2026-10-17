namespace Zebrahoof_EMR.Services;

/// <summary>
/// Loads one patient's chart. The cache calls this only from a real open or a refresh of a chart already opened.
/// </summary>
public interface IPatientChartLoader
{
    Task<PatientChartSnapshot?> LoadAsync(int patientId, CancellationToken cancellationToken = default);
}
