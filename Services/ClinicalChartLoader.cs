using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Zebrahoof_EMR.Data;
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
    private readonly IServiceScopeFactory? _scopes;

    public ClinicalChartLoader(
        MockPatientService patients,
        MockClinicalDataService clinical,
        IServiceScopeFactory? scopes = null)
    {
        _patients = patients;
        _clinical = clinical;
        _scopes = scopes;
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
        var documents = await LoadDocumentsAsync(patientId, cancellationToken);

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
            Documents = documents,
            LoadedAt = DateTimeOffset.UtcNow,
            Fingerprint = PatientChartSnapshot.BuildFingerprint(
                patient, problems, medications, allergies, labs, vitals, notes, encounters, documents)
        };
    }

    private async Task<IReadOnlyList<Document>> LoadDocumentsAsync(int patientId, CancellationToken cancellationToken)
    {
        if (_scopes == null)
        {
            return Array.Empty<Document>();
        }

        using var scope = _scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await db.Documents.AsNoTracking()
            .Where(d => d.PatientId == patientId)
            .OrderByDescending(d => d.Date)
            .Select(d => new Document
            {
                Id = d.Id,
                PatientId = d.PatientId,
                Name = d.Name,
                Category = d.Category,
                Type = d.Type,
                Date = d.Date,
                UploadedBy = d.UploadedBy,
                FileName = d.FileName
            })
            .ToListAsync(cancellationToken);
    }
}
