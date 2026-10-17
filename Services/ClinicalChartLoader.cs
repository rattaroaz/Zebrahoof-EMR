using Zebrahoof_EMR.Models;

namespace Zebrahoof_EMR.Services;

/// <summary>
/// Reads the chart sections a reopen should paint immediately.
/// Schedule and inbox are intentionally not loaded here.
/// </summary>
public sealed class ClinicalChartLoader : IPatientChartLoader
{
    private readonly MockPatientService _patients;
    private readonly MockClinicalDataService _clinical;

    public ClinicalChartLoader(MockPatientService patients, MockClinicalDataService clinical)
    {
        _patients = patients;
        _clinical = clinical;
    }

    public async Task<PatientChartSnapshot?> LoadAsync(int patientId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var patient = await _patients.GetPatientByIdAsync(patientId);
        if (patient == null)
        {
            return null;
        }

        var problems = await _clinical.GetProblemsByPatientAsync(patientId);
        var medications = await _clinical.GetMedicationsByPatientAsync(patientId);
        var allergies = await _clinical.GetAllergiesByPatientAsync(patientId);
        var labs = await _clinical.GetLabResultsByPatientAsync(patientId);
        var vitals = await _clinical.GetVitalsByPatientAsync(patientId);
        var notes = await _clinical.GetPatientNotesAsync(patientId);
        var encounters = await _clinical.GetEncountersByPatientAsync(patientId);
        var riskScores = await _clinical.GetRiskScoresByPatientAsync(patientId);
        var orders = await _clinical.GetSimulatedOrdersByPatientAsync(patientId);
        var imaging = await _clinical.GetImagingByPatientAsync(patientId);
        var immunizations = await _clinical.GetImmunizationsByPatientAsync(patientId);
        var careTeam = await _clinical.GetCareTeamByPatientAsync(patientId);

        return new PatientChartSnapshot
        {
            PatientId = patientId,
            Patient = patient,
            Problems = problems,
            Medications = medications,
            Allergies = allergies,
            Labs = labs,
            Vitals = vitals,
            Notes = notes,
            Encounters = encounters,
            RiskScores = riskScores,
            Orders = orders,
            Imaging = imaging,
            Immunizations = immunizations,
            CareTeam = careTeam,
            LoadedAt = DateTimeOffset.UtcNow,
            Fingerprint = PatientChartSnapshot.BuildFingerprint(
                patient, problems, medications, allergies, labs, vitals, notes, encounters)
        };
    }
}
