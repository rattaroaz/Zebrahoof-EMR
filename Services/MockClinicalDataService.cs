using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Zebrahoof_EMR.Data;
using Zebrahoof_EMR.Models;
using LocationModel = Zebrahoof_EMR.Models.Location;

namespace Zebrahoof_EMR.Services;

public class MockClinicalDataService
{
    /// <summary>Raised when inbox content or read state may have changed (demo UI refresh).</summary>
    public event Action? InboxChanged;

    private readonly IServiceScopeFactory? _scopeFactory;
    private readonly ILogger<MockClinicalDataService> _logger;
    private readonly object _persistenceLock = new();

    private readonly List<Encounter> _encounters;
    private readonly List<Problem> _problems;
    private readonly List<Medication> _medications;
    private readonly List<Allergy> _allergies;
    private readonly List<VitalSigns> _vitals;
    private readonly List<LabResult> _labResults;
    private readonly List<ClinicalAlert> _alerts;
    private readonly List<PatientInteraction> _interactions;
    private readonly List<EmergencyContact> _emergencyContacts;
    private readonly List<Insurance> _insurances;
    private readonly List<ImagingStudy> _imagingStudies;
    private readonly List<Immunization> _immunizations;
    private readonly List<CareTeamMember> _careTeamMembers;
    private readonly List<ClinicalNote> _notes;
    private readonly List<SmartPhrase> _smartPhrases;
    private readonly List<Prescription> _prescriptions;
    private readonly List<MedicationCatalog> _medicationCatalog;
    private readonly Dictionary<int, List<RosChecklistSection>> _rosByPatient;
    private readonly Dictionary<int, List<PhysicalExamSection>> _physicalExamByPatient;
    private readonly Dictionary<int, List<RiskScoreEntry>> _riskScoresByPatient;
    private readonly List<LabPanel> _labPanels;
    private readonly List<ImagingCatalog> _imagingCatalog;
    private readonly List<string> _specialties;
    private readonly List<LabOrder> _labOrders;
    private readonly List<ImagingOrder> _imagingOrders;
    private readonly List<ReferralOrder> _referralOrders;
    private readonly List<InboxMessage> _inboxMessages;
    private readonly List<ClinicalTask> _clinicalTasks;
    private readonly List<SystemUser> _systemUsers;
    private readonly List<LocationModel> _locations;
    private readonly List<Department> _departments;
    private readonly List<NoteTemplate> _noteTemplates;
    private readonly List<OrderSet> _orderSets;
    private SystemSettings _systemSettings;

    // Fired after Replace*ForPatient mutations so the UI can refresh.
    public event Action<int>? PatientDataChanged;

    public void NotifyPatientDataChanged(int patientId) => PatientDataChanged?.Invoke(patientId);

    public MockClinicalDataService()
        : this(null, NullLogger<MockClinicalDataService>.Instance)
    {
    }

    public MockClinicalDataService(
        IServiceScopeFactory? scopeFactory,
        ILogger<MockClinicalDataService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;

        _encounters = GenerateMockEncounters();
        _problems = GenerateMockProblems();
        _medications = GenerateMockMedications();
        _allergies = GenerateMockAllergies();
        _vitals = GenerateMockVitals();
        _labResults = GenerateMockLabResults();
        _alerts = GenerateMockAlerts();
        _interactions = GenerateMockInteractions();
        _emergencyContacts = GenerateMockEmergencyContacts();
        _insurances = GenerateMockInsurances();
        _imagingStudies = GenerateMockImagingStudies();
        _immunizations = GenerateMockImmunizations();
        _careTeamMembers = GenerateMockCareTeamMembers();
        _notes = GenerateMockNotes();
        _smartPhrases = GenerateMockSmartPhrases();
        _prescriptions = GenerateMockPrescriptions();
        _medicationCatalog = GenerateMockMedicationCatalog();
        _rosByPatient = GenerateMockRosData();
        _physicalExamByPatient = GenerateMockPhysicalExamData();
        _riskScoresByPatient = GenerateMockRiskScores();
        _labPanels = GenerateMockLabPanels();
        _imagingCatalog = GenerateMockImagingCatalog();
        _specialties = GenerateMockSpecialties();
        _labOrders = new List<LabOrder>();
        _imagingOrders = new List<ImagingOrder>();
        _referralOrders = new List<ReferralOrder>();
        _inboxMessages = GenerateMockInboxMessages();
        _clinicalTasks = GenerateMockClinicalTasks();
        _systemUsers = GenerateMockSystemUsers();
        _locations = GenerateMockLocations();
        _departments = GenerateMockDepartments();
        _noteTemplates = GenerateMockNoteTemplates();
        _orderSets = GenerateMockOrderSets();
        _systemSettings = new SystemSettings();
    }

    // ----------------------------------------------------------------
    // Persistence: hydrate from local DB on startup, seed on first run,
    // and write every mutation back so chart data survives restarts.
    // ----------------------------------------------------------------

    /// <summary>
    /// Called once at app startup. Applies pending EF migrations, then for
    /// each persisted patient-chart entity either replaces the in-memory
    /// list with what's in the database (subsequent runs) or seeds the
    /// database with the current in-memory mock data (very first run).
    /// </summary>
    public async Task InitializeFromDatabaseAsync(CancellationToken cancellationToken = default)
    {
        if (_scopeFactory == null) return;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            await db.Database.MigrateAsync(cancellationToken);

            await HydrateOrSeedAsync(db, db.Encounters, _encounters, cancellationToken);
            await HydrateOrSeedAsync(db, db.Problems, _problems, cancellationToken);
            await HydrateOrSeedAsync(db, db.Medications, _medications, cancellationToken);
            await HydrateOrSeedAsync(db, db.Allergies, _allergies, cancellationToken);
            await HydrateOrSeedAsync(db, db.VitalSigns, _vitals, cancellationToken);
            await HydrateOrSeedAsync(db, db.LabResults, _labResults, cancellationToken);
            await HydrateOrSeedAsync(db, db.ClinicalAlerts, _alerts, cancellationToken);
            await HydrateOrSeedAsync(db, db.PatientInteractions, _interactions, cancellationToken);
            await HydrateOrSeedAsync(db, db.EmergencyContacts, _emergencyContacts, cancellationToken);
            await HydrateOrSeedAsync(db, db.Insurances, _insurances, cancellationToken);
            await HydrateOrSeedAsync(db, db.ImagingStudies, _imagingStudies, cancellationToken);
            await HydrateOrSeedAsync(db, db.Immunizations, _immunizations, cancellationToken);
            await HydrateOrSeedAsync(db, db.CareTeamMembers, _careTeamMembers, cancellationToken);
            await HydrateOrSeedAsync(db, db.ClinicalNotes, _notes, cancellationToken);
            await HydrateOrSeedAsync(db, db.Prescriptions, _prescriptions, cancellationToken);
            await HydrateOrSeedAsync(db, db.LabOrders, _labOrders, cancellationToken);
            await HydrateOrSeedAsync(db, db.ImagingOrders, _imagingOrders, cancellationToken);
            await HydrateOrSeedAsync(db, db.ReferralOrders, _referralOrders, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to hydrate clinical data from database; running with in-memory mock state only.");
        }
    }

    private static async Task HydrateOrSeedAsync<T>(
        ApplicationDbContext db,
        DbSet<T> set,
        List<T> memoryList,
        CancellationToken ct) where T : class
    {
        var existing = await set.AsNoTracking().ToListAsync(ct);
        if (existing.Count > 0)
        {
            memoryList.Clear();
            memoryList.AddRange(existing);
        }
        else if (memoryList.Count > 0)
        {
            // First run: persist the generated mock data so it becomes the
            // ongoing record. Detach any tracked clones first so EF doesn't
            // complain about duplicate keys.
            set.AddRange(memoryList);
            await db.SaveChangesAsync(ct);
        }
    }

    /// <summary>
    /// Fire-and-forget background save for one or more entities.
    /// Caller passes a delegate that mutates a fresh DbContext; we open a
    /// scope, run it, save, and log any exceptions. We never let DB errors
    /// bubble up into UI code paths since the in-memory state is still
    /// authoritative for the lifetime of the process.
    /// </summary>
    private void Persist(Action<ApplicationDbContext> mutate)
    {
        if (_scopeFactory == null) return;

        // Snapshot the work onto a thread-pool task so the calling UI
        // method returns immediately. Lock prevents concurrent SaveChanges
        // from racing each other on the same connection.
        _ = Task.Run(() =>
        {
            try
            {
                lock (_persistenceLock)
                {
                    using var scope = _scopeFactory!.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    mutate(db);
                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to persist clinical data mutation to database.");
            }
        });
    }

    private void PersistAdd<T>(T entity) where T : class =>
        Persist(db => db.Set<T>().Add(entity));

    private void PersistUpdate<T>(T entity) where T : class =>
        Persist(db =>
        {
            db.Set<T>().Attach(entity);
            db.Entry(entity).State = EntityState.Modified;
        });

    private void PersistRemove<T>(T entity) where T : class =>
        Persist(db =>
        {
            db.Set<T>().Attach(entity);
            db.Set<T>().Remove(entity);
        });

    private void PersistReplaceForPatient<T>(
        int patientId,
        IEnumerable<T> rows,
        System.Linq.Expressions.Expression<Func<T, bool>> patientFilter)
        where T : class
    {
        var snapshot = rows.ToList();
        Persist(db =>
        {
            var existing = db.Set<T>().Where(patientFilter).ToList();
            db.Set<T>().RemoveRange(existing);
            db.Set<T>().AddRange(snapshot);
        });
    }

    // Encounters
    public Task<List<Encounter>> GetEncountersByPatientAsync(int patientId) =>
        Task.FromResult(_encounters.Where(e => e.PatientId == patientId).OrderByDescending(e => e.DateTime).ToList());

    public Task<List<Encounter>> GetRecentEncountersAsync(int count = 10) =>
        Task.FromResult(_encounters.OrderByDescending(e => e.DateTime).Take(count).ToList());

    // Bulk replace operations used by the AI "Update Records" workflow.
    // PatientRecordUpdateService also writes these directly to the DB inside
    // a transaction, so here we only persist if no scope factory was wired
    // (legacy callers / tests). When the DB-backed path is in play, the
    // transactional persistence already happened upstream.
    public void ReplaceProblemsForPatient(int patientId, IEnumerable<Problem> problems)
    {
        _problems.RemoveAll(p => p.PatientId == patientId);
        var nextId = _problems.Count == 0 ? 1 : _problems.Max(p => p.Id) + 1;
        foreach (var problem in problems)
        {
            problem.PatientId = patientId;
            if (problem.Id == 0) problem.Id = nextId++;
            _problems.Add(problem);
        }
    }

    public void ReplaceMedicationsForPatient(int patientId, IEnumerable<Medication> medications)
    {
        _medications.RemoveAll(m => m.PatientId == patientId);
        var nextId = _medications.Count == 0 ? 1 : _medications.Max(m => m.Id) + 1;
        foreach (var med in medications)
        {
            med.PatientId = patientId;
            if (med.Id == 0) med.Id = nextId++;
            _medications.Add(med);
        }
    }

    public void ReplaceAllergiesForPatient(int patientId, IEnumerable<Allergy> allergies)
    {
        _allergies.RemoveAll(a => a.PatientId == patientId);
        var nextId = _allergies.Count == 0 ? 1 : _allergies.Max(a => a.Id) + 1;
        foreach (var allergy in allergies)
        {
            allergy.PatientId = patientId;
            if (allergy.Id == 0) allergy.Id = nextId++;
            _allergies.Add(allergy);
        }
    }

    // Problems
    public Task<List<Problem>> GetProblemsByPatientAsync(int patientId) =>
        Task.FromResult(_problems.Where(p => p.PatientId == patientId).ToList());

    public Task<List<Problem>> GetActiveProblemsByPatientAsync(int patientId) =>
        Task.FromResult(_problems.Where(p => p.PatientId == patientId && p.Status == ProblemStatus.Active).ToList());

    // Medications
    public Task<List<Medication>> GetMedicationsByPatientAsync(int patientId) =>
        Task.FromResult(_medications.Where(m => m.PatientId == patientId).ToList());

    public Task<List<Medication>> GetActiveMedicationsByPatientAsync(int patientId) =>
        Task.FromResult(_medications.Where(m => m.PatientId == patientId && m.Status == MedicationStatus.Active).ToList());

    // Get only long-term medications for the medication list
    public Task<List<Medication>> GetMedicationListAsync(int patientId) =>
        Task.FromResult(_medications.Where(m => m.PatientId == patientId && m.IsLongTerm && m.Status == MedicationStatus.Active).ToList());

    // Check if medication already exists on the list (no duplicates)
    public Task<bool> IsMedicationOnListAsync(int patientId, string medicationName) =>
        Task.FromResult(_medications.Any(m => m.PatientId == patientId && 
            m.Name.Equals(medicationName, StringComparison.OrdinalIgnoreCase) && 
            m.IsLongTerm && m.Status == MedicationStatus.Active));

    // Add a new medication (for long-term) or just create a prescription (for short-term)
    public Task<(Medication? medication, Prescription prescription)> PrescribeMedicationAsync(
        int patientId, 
        string name, 
        string dose, 
        string route, 
        string frequency,
        bool isLongTerm,
        int? daysSupply,
        int? quantity,
        int? refills,
        string? instructions,
        string? prescriber,
        string? pharmacy,
        bool isHighRisk = false)
    {
        Medication? medication = null;
        
        // For long-term medications, add to the medication list if not already present
        if (isLongTerm)
        {
            var existingMed = _medications.FirstOrDefault(m => 
                m.PatientId == patientId && 
                m.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && 
                m.IsLongTerm && m.Status == MedicationStatus.Active);
            
            if (existingMed == null)
            {
                medication = new Medication
                {
                    Id = _medications.Count > 0 ? _medications.Max(m => m.Id) + 1 : 1,
                    PatientId = patientId,
                    Name = name,
                    Dose = dose,
                    Route = route,
                    Frequency = frequency,
                    StartDate = DateTime.Today,
                    Prescriber = prescriber,
                    Status = MedicationStatus.Active,
                    IsLongTerm = true,
                    IsHighRisk = isHighRisk,
                    Instructions = instructions,
                    RefillsRemaining = refills,
                    DaysSupply = daysSupply,
                    Pharmacy = pharmacy
                };
                _medications.Add(medication);
                PersistAdd(medication);
            }
            else
            {
                medication = existingMed;
            }
        }
        
        // Create the prescription record
        var prescription = new Prescription
        {
            Id = _prescriptions.Count > 0 ? _prescriptions.Max(p => p.Id) + 1 : 1,
            PatientId = patientId,
            MedicationId = medication?.Id,
            MedicationName = name,
            Dose = dose,
            Route = route,
            Frequency = frequency,
            DaysSupply = daysSupply,
            Quantity = quantity,
            Refills = refills,
            Instructions = instructions,
            Prescriber = prescriber,
            PrescribedDate = DateTime.Now,
            Type = isLongTerm ? PrescriptionType.LongTerm : PrescriptionType.ShortTerm,
            Status = PrescriptionStatus.Active,
            Pharmacy = pharmacy,
            IsRefill = false
        };
        _prescriptions.Add(prescription);
        PersistAdd(prescription);

        return Task.FromResult((medication, prescription));
    }

    // Order a refill for an existing medication
    public Task<Prescription> OrderRefillAsync(int patientId, int medicationId, string? prescriber, int? daysSupply = null, int? quantity = null)
    {
        var medication = _medications.FirstOrDefault(m => m.Id == medicationId && m.PatientId == patientId);
        if (medication == null)
            throw new InvalidOperationException("Medication not found");
        
        var prescription = new Prescription
        {
            Id = _prescriptions.Count > 0 ? _prescriptions.Max(p => p.Id) + 1 : 1,
            PatientId = patientId,
            MedicationId = medicationId,
            MedicationName = medication.Name,
            Dose = medication.Dose,
            Route = medication.Route,
            Frequency = medication.Frequency,
            DaysSupply = daysSupply ?? medication.DaysSupply ?? 30,
            Quantity = quantity,
            Refills = 0,
            Instructions = medication.Instructions,
            Prescriber = prescriber ?? medication.Prescriber,
            PrescribedDate = DateTime.Now,
            Type = PrescriptionType.LongTerm,
            Status = PrescriptionStatus.Active,
            Pharmacy = medication.Pharmacy,
            IsRefill = true
        };
        _prescriptions.Add(prescription);
        PersistAdd(prescription);

        return Task.FromResult(prescription);
    }

    // Add medication to list without prescription (e.g., patient brings in medication from another provider)
    public Task<Medication> AddMedicationToListAsync(
        int patientId,
        string name,
        string dose,
        string route,
        string frequency,
        string? prescriber,
        string? pharmacy,
        string? instructions,
        bool isHighRisk = false)
    {
        var medication = new Medication
        {
            Id = _medications.Count > 0 ? _medications.Max(m => m.Id) + 1 : 1,
            PatientId = patientId,
            Name = name,
            Dose = dose,
            Route = route,
            Frequency = frequency,
            StartDate = DateTime.Today,
            Prescriber = prescriber,
            Status = MedicationStatus.Active,
            IsLongTerm = true,
            IsHighRisk = isHighRisk,
            Instructions = instructions,
            Pharmacy = pharmacy
        };
        _medications.Add(medication);
        PersistAdd(medication);
        return Task.FromResult(medication);
    }

    // Update/edit an existing medication
    public Task<Medication?> UpdateMedicationAsync(
        int medicationId,
        string? dose = null,
        string? route = null,
        string? frequency = null,
        string? prescriber = null,
        string? pharmacy = null,
        string? instructions = null,
        int? refillsRemaining = null,
        int? daysSupply = null,
        bool? isHighRisk = null)
    {
        var medication = _medications.FirstOrDefault(m => m.Id == medicationId);
        if (medication != null)
        {
            if (dose != null) medication.Dose = dose;
            if (route != null) medication.Route = route;
            if (frequency != null) medication.Frequency = frequency;
            if (prescriber != null) medication.Prescriber = prescriber;
            if (pharmacy != null) medication.Pharmacy = pharmacy;
            if (instructions != null) medication.Instructions = instructions;
            if (refillsRemaining.HasValue) medication.RefillsRemaining = refillsRemaining;
            if (daysSupply.HasValue) medication.DaysSupply = daysSupply;
            if (isHighRisk.HasValue) medication.IsHighRisk = isHighRisk.Value;
            PersistUpdate(medication);
        }
        return Task.FromResult(medication);
    }

    // Discontinue a medication
    public Task DiscontinueMedicationAsync(int medicationId, string? reason)
    {
        var medication = _medications.FirstOrDefault(m => m.Id == medicationId);
        if (medication != null)
        {
            medication.Status = MedicationStatus.Discontinued;
            medication.EndDate = DateTime.Today;
            PersistUpdate(medication);
        }
        return Task.CompletedTask;
    }

    // Get prescription history for a patient
    public Task<List<Prescription>> GetPrescriptionsByPatientAsync(int patientId) =>
        Task.FromResult(_prescriptions.Where(p => p.PatientId == patientId)
            .OrderByDescending(p => p.PrescribedDate).ToList());

    // Get prescription history for a specific medication
    public Task<List<Prescription>> GetPrescriptionsByMedicationAsync(int medicationId) =>
        Task.FromResult(_prescriptions.Where(p => p.MedicationId == medicationId)
            .OrderByDescending(p => p.PrescribedDate).ToList());

    // Allergies
    public Task<List<Allergy>> GetAllergiesByPatientAsync(int patientId) =>
        Task.FromResult(_allergies.Where(a => a.PatientId == patientId).ToList());

    public Task<List<Allergy>> GetActiveAllergiesByPatientAsync(int patientId) =>
        Task.FromResult(_allergies.Where(a => a.PatientId == patientId && a.Status == AllergyStatus.Active).ToList());

    // Vitals
    public Task<List<VitalSigns>> GetVitalsByPatientAsync(int patientId) =>
        Task.FromResult(_vitals.Where(v => v.PatientId == patientId).OrderByDescending(v => v.RecordedAt).ToList());

    public Task<VitalSigns?> GetLatestVitalsByPatientAsync(int patientId) =>
        Task.FromResult(_vitals.Where(v => v.PatientId == patientId).OrderByDescending(v => v.RecordedAt).FirstOrDefault());

    // Lab Results
    public Task<List<LabResult>> GetLabResultsByPatientAsync(int patientId) =>
        Task.FromResult(_labResults.Where(l => l.PatientId == patientId).OrderByDescending(l => l.CollectedAt).ToList());

    public Task<List<LabResult>> GetRecentLabResultsByPatientAsync(int patientId, int count = 10) =>
        Task.FromResult(_labResults.Where(l => l.PatientId == patientId).OrderByDescending(l => l.CollectedAt).Take(count).ToList());

    // Alerts
    public Task<List<ClinicalAlert>> GetUnacknowledgedAlertsAsync() =>
        Task.FromResult(_alerts.Where(a => !a.IsAcknowledged).OrderByDescending(a => a.Severity).ThenByDescending(a => a.CreatedAt).ToList());

    public Task<List<ClinicalAlert>> GetAlertsByPatientAsync(int patientId) =>
        Task.FromResult(_alerts.Where(a => a.PatientId == patientId).ToList());

    public Task AcknowledgeAlertAsync(int alertId)
    {
        var alert = _alerts.FirstOrDefault(a => a.Id == alertId);
        if (alert != null)
        {
            alert.IsAcknowledged = true;
            alert.AcknowledgedAt = DateTime.Now;
            PersistUpdate(alert);
        }
        return Task.CompletedTask;
    }

    // Interactions
    public Task<List<PatientInteraction>> GetRecentInteractionsAsync(int count = 10) =>
        Task.FromResult(_interactions.OrderByDescending(i => i.DateTime).Take(count).ToList());

    public Task<List<PatientInteraction>> GetInteractionsByPatientAsync(int patientId) =>
        Task.FromResult(_interactions.Where(i => i.PatientId == patientId).OrderByDescending(i => i.DateTime).ToList());

    // Emergency Contacts
    public Task<List<EmergencyContact>> GetEmergencyContactsByPatientAsync(int patientId) =>
        Task.FromResult(_emergencyContacts.Where(e => e.PatientId == patientId).ToList());

    // Insurance
    public Task<List<Insurance>> GetInsurancesByPatientAsync(int patientId) =>
        Task.FromResult(_insurances.Where(i => i.PatientId == patientId).ToList());

    // Imaging
    public Task<List<ImagingStudy>> GetImagingByPatientAsync(int patientId) =>
        Task.FromResult(_imagingStudies.Where(i => i.PatientId == patientId).OrderByDescending(i => i.StudyDate).ToList());

    // Immunizations
    public Task<List<Immunization>> GetImmunizationsByPatientAsync(int patientId) =>
        Task.FromResult(_immunizations.Where(i => i.PatientId == patientId).OrderByDescending(i => i.AdministeredDate).ToList());

    // Care Team
    public Task<List<CareTeamMember>> GetCareTeamByPatientAsync(int patientId) =>
        Task.FromResult(_careTeamMembers.Where(c => c.PatientId == patientId).OrderByDescending(c => c.IsPrimary).ToList());

    // Notes
    public Task<List<ClinicalNote>> GetNotesByEncounterAsync(int encounterId) =>
        Task.FromResult(_notes.Where(n => n.EncounterId == encounterId).ToList());

    public Task<ClinicalNote?> GetNoteByIdAsync(int noteId) =>
        Task.FromResult(_notes.FirstOrDefault(n => n.Id == noteId));

    public Task<List<ClinicalNote>> GetPatientNotesAsync(int patientId) =>
        Task.FromResult(_notes.Where(n => n.PatientId == patientId).OrderByDescending(n => n.CreatedAt).ToList());

    public Task<ClinicalNote> CreateNoteAsync(ClinicalNote note)
    {
        note.Id = _notes.Count > 0 ? _notes.Max(n => n.Id) + 1 : 1;
        note.CreatedAt = DateTime.UtcNow;
        _notes.Add(note);
        PersistAdd(note);
        return Task.FromResult(note);
    }

    public Task<ClinicalNote> UpdateNoteAsync(ClinicalNote note)
    {
        var existingNote = _notes.FirstOrDefault(n => n.Id == note.Id);
        if (existingNote != null)
        {
            existingNote.ChiefComplaint = note.ChiefComplaint;
            existingNote.HistoryOfPresentIllness = note.HistoryOfPresentIllness;
            existingNote.ReviewOfSystems = note.ReviewOfSystems;
            existingNote.PhysicalExam = note.PhysicalExam;
            existingNote.Assessment = note.Assessment;
            existingNote.Plan = note.Plan;
            existingNote.Status = note.Status;
            existingNote.SignedAt = note.SignedAt;
            PersistUpdate(existingNote);
        }
        return Task.FromResult(note);
    }

    public Task<bool> DeleteNoteAsync(int noteId)
    {
        var note = _notes.FirstOrDefault(n => n.Id == noteId);
        if (note != null)
        {
            _notes.Remove(note);
            PersistRemove(note);
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }

    public Task<Encounter?> GetEncounterByIdAsync(int encounterId) =>
        Task.FromResult(_encounters.FirstOrDefault(e => e.Id == encounterId));

    // Smart Phrases
    public Task<List<SmartPhrase>> GetSmartPhrasesAsync() =>
        Task.FromResult(_smartPhrases.ToList());

    public Task<List<SmartPhrase>> GetSmartPhrasesByCategoryAsync(string category) =>
        Task.FromResult(_smartPhrases.Where(s => s.Category == category).ToList());

    // Structured Data
    public Task<List<RosChecklistSection>> GetRosChecklistByPatientAsync(int patientId) =>
        Task.FromResult(CloneRosSections(GetRosSectionsInternal(patientId)));

    public Task<List<PhysicalExamSection>> GetPhysicalExamByPatientAsync(int patientId) =>
        Task.FromResult(ClonePhysicalExamSections(GetPhysicalExamSectionsInternal(patientId)));

    public Task<List<RiskScoreEntry>> GetRiskScoresByPatientAsync(int patientId) =>
        Task.FromResult(CloneRiskScores(GetRiskScoresInternal(patientId)));

    // Medication Catalog
    public Task<List<MedicationCatalog>> GetMedicationCatalogAsync() =>
        Task.FromResult(_medicationCatalog.ToList());

    // Mock data generation
    private static List<Encounter> GenerateMockEncounters()
    {
        var today = DateTime.Today;
        return
        [
            new() { Id = 1, PatientId = 1, PatientName = "John Doe", DateTime = today.AddDays(-14), VisitType = "Follow-up", Provider = "Dr. Sarah Smith", Location = "Clinic A", Status = EncounterStatus.Signed, ChiefComplaint = "Medication refill", Assessment = "Hypertension well-controlled", Plan = "Continue current regimen" },
            new() { Id = 2, PatientId = 2, PatientName = "Jane Smith", DateTime = today.AddDays(-7), VisitType = "Annual Exam", Provider = "Dr. Sarah Smith", Location = "Clinic A", Status = EncounterStatus.Signed, ChiefComplaint = "Routine physical", Assessment = "Healthy adult", Plan = "Continue preventive care" },
            new() { Id = 3, PatientId = 3, PatientName = "Robert Johnson", DateTime = today.AddDays(-3), VisitType = "Diabetes Management", Provider = "Dr. Sarah Smith", Location = "Clinic A", Status = EncounterStatus.Signed, ChiefComplaint = "A1c review", Assessment = "Diabetes type 2, improved control", Plan = "Adjust insulin dosing" },
            new() { Id = 4, PatientId = 1, PatientName = "John Doe", DateTime = today.AddDays(-60), VisitType = "Sick Visit", Provider = "Dr. Sarah Smith", Location = "Clinic A", Status = EncounterStatus.Signed, ChiefComplaint = "Upper respiratory symptoms", Assessment = "Viral URI", Plan = "Symptomatic treatment" },
            new() { Id = 5, PatientId = 4, PatientName = "Maria Garcia", DateTime = today.AddDays(-30), VisitType = "New Patient", Provider = "Dr. Sarah Smith", Location = "Clinic A", Status = EncounterStatus.Signed, ChiefComplaint = "Establish care", Assessment = "Healthy adult with mild anxiety", Plan = "Lifestyle modifications" }
        ];
    }

    private static List<Problem> GenerateMockProblems() =>
    [
        new() { Id = 1, PatientId = 1, Name = "Essential Hypertension", IcdCode = "I10", OnsetDate = DateTime.Today.AddYears(-5), Status = ProblemStatus.Active, Severity = "Moderate" },
        new() { Id = 2, PatientId = 1, Name = "Hyperlipidemia", IcdCode = "E78.5", OnsetDate = DateTime.Today.AddYears(-3), Status = ProblemStatus.Active },
        new() { Id = 3, PatientId = 3, Name = "Type 2 Diabetes Mellitus", IcdCode = "E11.9", OnsetDate = DateTime.Today.AddYears(-10), Status = ProblemStatus.Active, Severity = "Moderate" },
        new() { Id = 4, PatientId = 3, Name = "Essential Hypertension", IcdCode = "I10", OnsetDate = DateTime.Today.AddYears(-8), Status = ProblemStatus.Active },
        new() { Id = 5, PatientId = 3, Name = "Chronic Kidney Disease Stage 3", IcdCode = "N18.3", OnsetDate = DateTime.Today.AddYears(-2), Status = ProblemStatus.Active, Severity = "Moderate" },
        new() { Id = 6, PatientId = 4, Name = "Generalized Anxiety Disorder", IcdCode = "F41.1", OnsetDate = DateTime.Today.AddMonths(-6), Status = ProblemStatus.Active, Severity = "Mild" },
        new() { Id = 7, PatientId = 5, Name = "Asthma, mild intermittent", IcdCode = "J45.20", OnsetDate = DateTime.Today.AddYears(-5), Status = ProblemStatus.Active }
    ];

    private static List<Medication> GenerateMockMedications() =>
    [
        new() { Id = 1, PatientId = 1, Name = "Lisinopril", Dose = "10mg", Route = "Oral", Frequency = "Once daily", StartDate = DateTime.Today.AddYears(-5), Prescriber = "Dr. Sarah Smith", Status = MedicationStatus.Active, IsLongTerm = true, RefillsRemaining = 3, DaysSupply = 30, Pharmacy = "CVS Pharmacy" },
        new() { Id = 2, PatientId = 1, Name = "Atorvastatin", Dose = "20mg", Route = "Oral", Frequency = "Once daily at bedtime", StartDate = DateTime.Today.AddYears(-3), Prescriber = "Dr. Sarah Smith", Status = MedicationStatus.Active, IsLongTerm = true, RefillsRemaining = 2, DaysSupply = 30, Pharmacy = "CVS Pharmacy" },
        new() { Id = 3, PatientId = 3, Name = "Metformin", Dose = "1000mg", Route = "Oral", Frequency = "Twice daily", StartDate = DateTime.Today.AddYears(-10), Prescriber = "Dr. Sarah Smith", Status = MedicationStatus.Active, IsLongTerm = true, RefillsRemaining = 5, DaysSupply = 30, Pharmacy = "Walgreens" },
        new() { Id = 4, PatientId = 3, Name = "Insulin Glargine", Dose = "20 units", Route = "Subcutaneous", Frequency = "Once daily at bedtime", StartDate = DateTime.Today.AddYears(-5), Prescriber = "Dr. Sarah Smith", Status = MedicationStatus.Active, IsHighRisk = true, IsLongTerm = true, RefillsRemaining = 1, DaysSupply = 30, Pharmacy = "Walgreens" },
        new() { Id = 5, PatientId = 3, Name = "Lisinopril", Dose = "20mg", Route = "Oral", Frequency = "Once daily", StartDate = DateTime.Today.AddYears(-8), Prescriber = "Dr. Sarah Smith", Status = MedicationStatus.Active, IsLongTerm = true, RefillsRemaining = 4, DaysSupply = 30, Pharmacy = "Walgreens" },
        new() { Id = 6, PatientId = 5, Name = "Albuterol", Dose = "90mcg", Route = "Inhalation", Frequency = "As needed", StartDate = DateTime.Today.AddYears(-5), Prescriber = "Dr. Sarah Smith", Status = MedicationStatus.Active, IsLongTerm = true, RefillsRemaining = 2, DaysSupply = 30, Pharmacy = "Rite Aid" },
        // Example of a discontinued medication for history
        new() { Id = 7, PatientId = 1, Name = "Hydrochlorothiazide", Dose = "25mg", Route = "Oral", Frequency = "Once daily", StartDate = DateTime.Today.AddYears(-6), EndDate = DateTime.Today.AddYears(-5), Prescriber = "Dr. Sarah Smith", Status = MedicationStatus.Discontinued, IsLongTerm = true }
    ];

    private static List<Prescription> GenerateMockPrescriptions()
    {
        var today = DateTime.Today;
        return
        [
            new() { Id = 1, PatientId = 1, MedicationId = 1, MedicationName = "Lisinopril", Dose = "10mg", Route = "Oral", Frequency = "Once daily", DaysSupply = 30, Quantity = 30, Refills = 5, Prescriber = "Dr. Sarah Smith", PrescribedDate = today.AddYears(-5), Type = PrescriptionType.LongTerm, Status = PrescriptionStatus.Filled, Pharmacy = "CVS Pharmacy", IsRefill = false },
            new() { Id = 2, PatientId = 1, MedicationId = 1, MedicationName = "Lisinopril", Dose = "10mg", Route = "Oral", Frequency = "Once daily", DaysSupply = 30, Quantity = 30, Refills = 0, Prescriber = "Dr. Sarah Smith", PrescribedDate = today.AddMonths(-3), Type = PrescriptionType.LongTerm, Status = PrescriptionStatus.Filled, Pharmacy = "CVS Pharmacy", IsRefill = true },
            new() { Id = 3, PatientId = 1, MedicationId = 2, MedicationName = "Atorvastatin", Dose = "20mg", Route = "Oral", Frequency = "Once daily at bedtime", DaysSupply = 30, Quantity = 30, Refills = 3, Prescriber = "Dr. Sarah Smith", PrescribedDate = today.AddYears(-3), Type = PrescriptionType.LongTerm, Status = PrescriptionStatus.Filled, Pharmacy = "CVS Pharmacy", IsRefill = false },
            new() { Id = 4, PatientId = 1, MedicationName = "Amoxicillin", Dose = "500mg", Route = "Oral", Frequency = "Three times daily", DaysSupply = 10, Quantity = 30, Refills = 0, Instructions = "Take with food. Complete entire course.", Prescriber = "Dr. Sarah Smith", PrescribedDate = today.AddDays(-60), Type = PrescriptionType.ShortTerm, Status = PrescriptionStatus.Filled, Pharmacy = "CVS Pharmacy", IsRefill = false },
            new() { Id = 5, PatientId = 3, MedicationId = 3, MedicationName = "Metformin", Dose = "1000mg", Route = "Oral", Frequency = "Twice daily", DaysSupply = 30, Quantity = 60, Refills = 11, Prescriber = "Dr. Sarah Smith", PrescribedDate = today.AddYears(-10), Type = PrescriptionType.LongTerm, Status = PrescriptionStatus.Filled, Pharmacy = "Walgreens", IsRefill = false },
            new() { Id = 6, PatientId = 3, MedicationId = 4, MedicationName = "Insulin Glargine", Dose = "20 units", Route = "Subcutaneous", Frequency = "Once daily at bedtime", DaysSupply = 30, Quantity = 1, Refills = 5, Instructions = "Inject subcutaneously at bedtime. Rotate injection sites.", Prescriber = "Dr. Sarah Smith", PrescribedDate = today.AddYears(-5), Type = PrescriptionType.LongTerm, Status = PrescriptionStatus.Filled, Pharmacy = "Walgreens", IsRefill = false }
        ];
    }

    private static List<Allergy> GenerateMockAllergies() =>
    [
        new() { Id = 1, PatientId = 1, Allergen = "Penicillin", Reaction = "Rash, hives", Severity = AllergySeverity.Moderate, Status = AllergyStatus.Active },
        new() { Id = 2, PatientId = 1, Allergen = "Sulfa drugs", Reaction = "Difficulty breathing", Severity = AllergySeverity.Severe, Status = AllergyStatus.Active },
        new() { Id = 3, PatientId = 3, Allergen = "Aspirin", Reaction = "GI upset", Severity = AllergySeverity.Mild, Status = AllergyStatus.Active },
        new() { Id = 4, PatientId = 3, Allergen = "Codeine", Reaction = "Nausea, vomiting", Severity = AllergySeverity.Moderate, Status = AllergyStatus.Active },
        new() { Id = 5, PatientId = 4, Allergen = "Latex", Reaction = "Contact dermatitis", Severity = AllergySeverity.Mild, Status = AllergyStatus.Active },
        new() { Id = 6, PatientId = 5, Allergen = "Peanuts", Reaction = "Anaphylaxis", Severity = AllergySeverity.LifeThreatening, Status = AllergyStatus.Active }
    ];

    private static List<VitalSigns> GenerateMockVitals()
    {
        var today = DateTime.Today;
        return
        [
            new() { Id = 1, PatientId = 1, RecordedAt = today.AddDays(-14).AddHours(9), RecordedBy = "MA Jones", Temperature = 98.6m, SystolicBP = 128, DiastolicBP = 82, HeartRate = 72, RespiratoryRate = 16, OxygenSaturation = 98, Weight = 185, Height = 70, BMI = 26.5m },
            new() { Id = 2, PatientId = 2, RecordedAt = today.AddDays(-7).AddHours(10), RecordedBy = "MA Jones", Temperature = 98.2m, SystolicBP = 118, DiastolicBP = 76, HeartRate = 68, RespiratoryRate = 14, OxygenSaturation = 99, Weight = 145, Height = 65, BMI = 24.1m },
            new() { Id = 3, PatientId = 3, RecordedAt = today.AddDays(-3).AddHours(14), RecordedBy = "MA Jones", Temperature = 98.4m, SystolicBP = 142, DiastolicBP = 88, HeartRate = 78, RespiratoryRate = 18, OxygenSaturation = 96, Weight = 210, Height = 68, BMI = 31.9m },
            new() { Id = 4, PatientId = 1, RecordedAt = today.AddDays(-60).AddHours(11), RecordedBy = "MA Smith", Temperature = 100.2m, SystolicBP = 132, DiastolicBP = 84, HeartRate = 88, RespiratoryRate = 20, OxygenSaturation = 97, Weight = 183, Height = 70, BMI = 26.3m }
        ];
    }

    private static List<LabResult> GenerateMockLabResults()
    {
        var today = DateTime.Today;
        return
        [
            new() { Id = 1, PatientId = 1, TestName = "Hemoglobin A1c", PanelName = "Diabetes Panel", Value = "5.8", Units = "%", ReferenceRange = "<5.7", CollectedAt = today.AddDays(-14), ResultedAt = today.AddDays(-12), Status = LabResultStatus.Final, IsAbnormal = true },
            new() { Id = 2, PatientId = 1, TestName = "LDL Cholesterol", PanelName = "Lipid Panel", Value = "98", Units = "mg/dL", ReferenceRange = "<100", CollectedAt = today.AddDays(-14), ResultedAt = today.AddDays(-12), Status = LabResultStatus.Final },
            new() { Id = 3, PatientId = 3, TestName = "Hemoglobin A1c", PanelName = "Diabetes Panel", Value = "7.2", Units = "%", ReferenceRange = "<7.0", CollectedAt = today.AddDays(-3), ResultedAt = today.AddDays(-1), Status = LabResultStatus.Final, IsAbnormal = true },
            new() { Id = 4, PatientId = 3, TestName = "Creatinine", PanelName = "Basic Metabolic Panel", Value = "1.8", Units = "mg/dL", ReferenceRange = "0.7-1.3", CollectedAt = today.AddDays(-3), ResultedAt = today.AddDays(-1), Status = LabResultStatus.Final, IsAbnormal = true },
            new() { Id = 5, PatientId = 3, TestName = "eGFR", PanelName = "Basic Metabolic Panel", Value = "45", Units = "mL/min", ReferenceRange = ">60", CollectedAt = today.AddDays(-3), ResultedAt = today.AddDays(-1), Status = LabResultStatus.Final, IsAbnormal = true },
            new() { Id = 6, PatientId = 3, TestName = "Potassium", PanelName = "Basic Metabolic Panel", Value = "5.6", Units = "mEq/L", ReferenceRange = "3.5-5.0", CollectedAt = today.AddDays(-3), ResultedAt = today.AddDays(-1), Status = LabResultStatus.Final, IsAbnormal = true, IsCritical = true }
        ];
    }

    private static List<ClinicalAlert> GenerateMockAlerts()
    {
        var today = DateTime.Today;
        return
        [
            new() { Id = 1, PatientId = 3, PatientName = "Robert Johnson", Title = "Critical Lab Result", Description = "Potassium 5.6 mEq/L - above critical threshold", Type = AlertType.CriticalResult, Severity = AlertSeverity.Critical, CreatedAt = today.AddDays(-1) },
            new() { Id = 2, PatientId = 1, PatientName = "John Doe", Title = "Medication Renewal Due", Description = "Lisinopril prescription expires in 7 days", Type = AlertType.MedicationRenewal, Severity = AlertSeverity.Medium, CreatedAt = today.AddDays(-2) },
            new() { Id = 3, PatientId = 4, PatientName = "Maria Garcia", Title = "Overdue Screening", Description = "Cervical cancer screening overdue by 6 months", Type = AlertType.OverdueCare, Severity = AlertSeverity.Medium, CreatedAt = today.AddDays(-5) },
            new() { Id = 4, PatientId = 5, PatientName = "William Brown", Title = "Immunization Due", Description = "Annual flu vaccine recommended", Type = AlertType.OverdueCare, Severity = AlertSeverity.Low, CreatedAt = today.AddDays(-3) },
            new() { Id = 5, Title = "Unsigned Notes", Description = "3 encounter notes pending signature", Type = AlertType.DocumentReview, Severity = AlertSeverity.High, CreatedAt = today }
        ];
    }

    private static List<PatientInteraction> GenerateMockInteractions()
    {
        var today = DateTime.Today;
        return
        [
            new() { Id = 1, PatientId = 1, PatientName = "John Doe", DateTime = today.AddDays(-1), Type = InteractionType.PhoneCall, Summary = "Called about lab results", Provider = "Dr. Sarah Smith" },
            new() { Id = 2, PatientId = 3, PatientName = "Robert Johnson", DateTime = today.AddDays(-1), Type = InteractionType.LabReview, Summary = "Reviewed critical potassium result", Provider = "Dr. Sarah Smith" },
            new() { Id = 3, PatientId = 2, PatientName = "Jane Smith", DateTime = today.AddDays(-2), Type = InteractionType.Message, Summary = "Responded to patient portal message", Provider = "Dr. Sarah Smith" },
            new() { Id = 4, PatientId = 1, PatientName = "John Doe", DateTime = today.AddDays(-3), Type = InteractionType.Prescription, Summary = "Refilled Lisinopril 10mg", Provider = "Dr. Sarah Smith" },
            new() { Id = 5, PatientId = 4, PatientName = "Maria Garcia", DateTime = today.AddDays(-5), Type = InteractionType.Referral, Summary = "Referred to psychology for anxiety", Provider = "Dr. Sarah Smith" },
            new() { Id = 6, PatientId = 3, PatientName = "Robert Johnson", DateTime = today.AddDays(-3), Type = InteractionType.OfficeVisit, Summary = "Diabetes management visit", Provider = "Dr. Sarah Smith" }
        ];
    }

    private static List<EmergencyContact> GenerateMockEmergencyContacts() =>
    [
        new() { Id = 1, PatientId = 1, Name = "Mary Doe", Relationship = "Spouse", Phone = "(555) 123-4568", IsPrimary = true },
        new() { Id = 2, PatientId = 2, Name = "John Smith Sr.", Relationship = "Father", Phone = "(555) 234-5679", IsPrimary = true },
        new() { Id = 3, PatientId = 3, Name = "Linda Johnson", Relationship = "Wife", Phone = "(555) 345-6780", Email = "linda.j@email.com", IsPrimary = true },
        new() { Id = 4, PatientId = 3, Name = "Michael Johnson", Relationship = "Son", Phone = "(555) 345-6781", IsPrimary = false },
        new() { Id = 5, PatientId = 4, Name = "Carlos Garcia", Relationship = "Husband", Phone = "(555) 456-7891", IsPrimary = true },
        new() { Id = 6, PatientId = 5, Name = "Susan Brown", Relationship = "Mother", Phone = "(555) 567-8902", IsPrimary = true }
    ];

    private static List<Insurance> GenerateMockInsurances() =>
    [
        new() { Id = 1, PatientId = 1, PayerName = "Blue Cross Blue Shield", PlanName = "PPO Gold", MemberId = "BC123456", GroupNumber = "GRP001", Type = InsuranceType.Commercial, EffectiveDate = DateTime.Today.AddYears(-2), IsPrimary = true },
        new() { Id = 2, PatientId = 2, PayerName = "Aetna", PlanName = "HMO Select", MemberId = "AET789012", GroupNumber = "GRP002", Type = InsuranceType.Commercial, EffectiveDate = DateTime.Today.AddYears(-1), IsPrimary = true },
        new() { Id = 3, PatientId = 3, PayerName = "Medicare", PlanName = "Part A & B", MemberId = "MED345678", Type = InsuranceType.Medicare, EffectiveDate = DateTime.Today.AddYears(-5), IsPrimary = true },
        new() { Id = 4, PatientId = 3, PayerName = "AARP Medicare Supplement", PlanName = "Plan F", MemberId = "AARP999", Type = InsuranceType.Commercial, EffectiveDate = DateTime.Today.AddYears(-5), IsPrimary = false },
        new() { Id = 5, PatientId = 4, PayerName = "United Healthcare", PlanName = "Choice Plus", MemberId = "UH901234", GroupNumber = "GRP004", Type = InsuranceType.Commercial, EffectiveDate = DateTime.Today.AddMonths(-6), IsPrimary = true },
        new() { Id = 6, PatientId = 5, PayerName = "Cigna", PlanName = "Open Access Plus", MemberId = "CIG567890", GroupNumber = "GRP005", Type = InsuranceType.Commercial, EffectiveDate = DateTime.Today.AddYears(-3), IsPrimary = true }
    ];

    private static List<ImagingStudy> GenerateMockImagingStudies()
    {
        var today = DateTime.Today;
        return
        [
            new() { Id = 1, PatientId = 1, StudyDate = today.AddMonths(-6), Modality = "X-Ray", BodyPart = "Chest", Description = "Chest X-Ray PA and Lateral", Impression = "No acute cardiopulmonary findings.", OrderingProvider = "Dr. Sarah Smith", Radiologist = "Dr. James Wong", Status = ImagingStatus.Completed },
            new() { Id = 2, PatientId = 3, StudyDate = today.AddMonths(-3), Modality = "CT", BodyPart = "Abdomen/Pelvis", Description = "CT Abdomen and Pelvis with Contrast", Impression = "No acute abnormality. Mild fatty infiltration of the liver.", OrderingProvider = "Dr. Sarah Smith", Radiologist = "Dr. James Wong", Status = ImagingStatus.Completed },
            new() { Id = 3, PatientId = 3, StudyDate = today.AddDays(-30), Modality = "Ultrasound", BodyPart = "Kidneys", Description = "Renal Ultrasound", Impression = "Bilateral kidneys normal in size. No hydronephrosis.", OrderingProvider = "Dr. Sarah Smith", Radiologist = "Dr. James Wong", Status = ImagingStatus.Completed },
            new() { Id = 4, PatientId = 1, StudyDate = today.AddDays(7), Modality = "MRI", BodyPart = "Lumbar Spine", Description = "MRI Lumbar Spine without Contrast", OrderingProvider = "Dr. Sarah Smith", Status = ImagingStatus.Scheduled },
            new() { Id = 5, PatientId = 4, StudyDate = today.AddMonths(-2), Modality = "X-Ray", BodyPart = "Knee", Description = "X-Ray Right Knee 3 Views", Impression = "Mild degenerative changes. No acute fracture.", OrderingProvider = "Dr. Sarah Smith", Radiologist = "Dr. James Wong", Status = ImagingStatus.Completed }
        ];
    }

    private static List<Immunization> GenerateMockImmunizations()
    {
        var today = DateTime.Today;
        return
        [
            new() { Id = 1, PatientId = 1, VaccineName = "Influenza (Flu)", Manufacturer = "Sanofi Pasteur", LotNumber = "FL2024A", AdministeredDate = today.AddMonths(-2), Site = "Left Deltoid", Route = "IM", AdministeredBy = "RN Jane Wilson", Status = ImmunizationStatus.Completed },
            new() { Id = 2, PatientId = 1, VaccineName = "COVID-19 (Pfizer)", Manufacturer = "Pfizer", LotNumber = "CV2024B", AdministeredDate = today.AddMonths(-6), Site = "Right Deltoid", Route = "IM", AdministeredBy = "RN Jane Wilson", Status = ImmunizationStatus.Completed },
            new() { Id = 3, PatientId = 1, VaccineName = "Tdap", Manufacturer = "GSK", LotNumber = "TD2020C", AdministeredDate = today.AddYears(-4), Site = "Left Deltoid", Route = "IM", AdministeredBy = "RN Jane Wilson", Status = ImmunizationStatus.Completed },
            new() { Id = 4, PatientId = 3, VaccineName = "Pneumococcal (PPSV23)", Manufacturer = "Merck", LotNumber = "PN2023D", AdministeredDate = today.AddYears(-1), Site = "Left Deltoid", Route = "IM", AdministeredBy = "RN Jane Wilson", Status = ImmunizationStatus.Completed },
            new() { Id = 5, PatientId = 3, VaccineName = "Shingrix", Manufacturer = "GSK", LotNumber = "SH2023E", AdministeredDate = today.AddMonths(-8), Site = "Right Deltoid", Route = "IM", AdministeredBy = "RN Jane Wilson", Status = ImmunizationStatus.Completed, Notes = "Dose 1 of 2" },
            new() { Id = 6, PatientId = 5, VaccineName = "MMR", Manufacturer = "Merck", LotNumber = "MM2020F", AdministeredDate = today.AddYears(-10), Status = ImmunizationStatus.Historical, Notes = "Per parent report" },
            new() { Id = 7, PatientId = 5, VaccineName = "Varicella", Manufacturer = "Merck", LotNumber = "VA2020G", AdministeredDate = today.AddYears(-10), Status = ImmunizationStatus.Historical, Notes = "Per parent report" }
        ];
    }

    private static List<CareTeamMember> GenerateMockCareTeamMembers() =>
    [
        new() { Id = 1, PatientId = 1, Name = "Dr. Sarah Smith", Role = "Primary Care Physician", Specialty = "Internal Medicine", Phone = "(555) 100-0001", Organization = "Zebrahoof Medical Group", IsPrimary = true, StartDate = DateTime.Today.AddYears(-5) },
        new() { Id = 2, PatientId = 1, Name = "Dr. Michael Chen", Role = "Cardiologist", Specialty = "Cardiology", Phone = "(555) 100-0002", Fax = "(555) 100-0003", Organization = "Heart Specialists LLC", IsPrimary = false, StartDate = DateTime.Today.AddYears(-2) },
        new() { Id = 3, PatientId = 3, Name = "Dr. Sarah Smith", Role = "Primary Care Physician", Specialty = "Internal Medicine", Phone = "(555) 100-0001", Organization = "Zebrahoof Medical Group", IsPrimary = true, StartDate = DateTime.Today.AddYears(-10) },
        new() { Id = 4, PatientId = 3, Name = "Dr. Emily Wilson", Role = "Endocrinologist", Specialty = "Endocrinology", Phone = "(555) 100-0004", Fax = "(555) 100-0005", Organization = "Diabetes Care Center", IsPrimary = false, StartDate = DateTime.Today.AddYears(-5) },
        new() { Id = 5, PatientId = 3, Name = "Dr. Robert Lee", Role = "Nephrologist", Specialty = "Nephrology", Phone = "(555) 100-0006", Organization = "Kidney Associates", IsPrimary = false, StartDate = DateTime.Today.AddYears(-2) },
        new() { Id = 6, PatientId = 3, Name = "Susan Martinez, RN", Role = "Care Coordinator", Phone = "(555) 100-0007", Organization = "Zebrahoof Medical Group", IsPrimary = false, StartDate = DateTime.Today.AddYears(-1) },
        new() { Id = 7, PatientId = 4, Name = "Dr. Sarah Smith", Role = "Primary Care Physician", Specialty = "Internal Medicine", Phone = "(555) 100-0001", Organization = "Zebrahoof Medical Group", IsPrimary = true, StartDate = DateTime.Today.AddMonths(-6) },
        new() { Id = 8, PatientId = 4, Name = "Dr. Amanda Torres", Role = "Psychologist", Specialty = "Psychology", Phone = "(555) 100-0008", Organization = "Behavioral Health Center", IsPrimary = false, StartDate = DateTime.Today.AddMonths(-3) }
    ];

    private static List<ClinicalNote> GenerateMockNotes()
    {
        var today = DateTime.Today;
        return
        [
            // Jane Smith - 15 visits over 3 years
            new() { Id = 1, EncounterId = 1, PatientId = 1, AuthorName = "Dr. Sarah Smith", CreatedAt = today.AddDays(-14), SignedAt = today.AddDays(-14), Status = NoteStatus.Signed, ChiefComplaint = "Follow-up for hypertension", HistoryOfPresentIllness = "Patient returns for routine blood pressure check.", ReviewOfSystems = "Constitutional: No fever, fatigue. Cardiovascular: No chest pain.", PhysicalExam = "Vitals: BP 128/82, HR 72. General: Alert, oriented.", Assessment = "Essential hypertension, well-controlled.", Plan = "Continue Lisinopril 10mg daily." },
            
            // Jane Smith - Annual Exam 2024
            new() { Id = 2, EncounterId = 2, PatientId = 2, AuthorName = "Dr. Michael Chen", CreatedAt = today.AddDays(-365), SignedAt = today.AddDays(-365), Status = NoteStatus.Signed, ChiefComplaint = "Annual wellness exam", HistoryOfPresentIllness = "Patient presents for annual physical examination.", ReviewOfSystems = "Complete review of systems negative.", PhysicalExam = "Vitals: BP 118/76, HR 68. Complete physical exam within normal limits.", Assessment = "Healthy adult female. Annual screening up to date.", Plan = "Continue current health maintenance. Return for annual exam next year." },
            
            // Jane Smith - Office Visit - URI
            new() { Id = 3, EncounterId = 3, PatientId = 2, AuthorName = "Dr. Michael Chen", CreatedAt = today.AddDays(-320), SignedAt = today.AddDays(-320), Status = NoteStatus.Signed, ChiefComplaint = "Sore throat and cough", HistoryOfPresentIllness = "Patient reports 3 days of sore throat, productive cough, low-grade fever.", ReviewOfSystems = "Constitutional: Fever 100.8°F, fatigue. HEENT: Sore throat, nasal congestion.", PhysicalExam = "Vitals: Temp 100.8°F, BP 120/78, HR 72. Throat: Erythematous, no exudate.", Assessment = "Upper respiratory infection, likely viral.", Plan = "Supportive care with rest, fluids. Symptomatic relief with acetaminophen." },
            
            // Jane Smith - Follow-up - Hypertension
            new() { Id = 4, EncounterId = 4, PatientId = 2, AuthorName = "Dr. Michael Chen", CreatedAt = today.AddDays(-280), SignedAt = today.AddDays(-280), Status = NoteStatus.Signed, ChiefComplaint = "Blood pressure follow-up", HistoryOfPresentIllness = "Patient returns for BP check after elevated reading at urgent care.", ReviewOfSystems = "Constitutional: No fatigue. Cardiovascular: No chest pain, occasional mild dizziness.", PhysicalExam = "Vitals: BP 132/84, HR 70. No orthostatic changes.", Assessment = "Hypertension, improving on medication.", Plan = "Continue lisinopril 10mg daily. Monitor BP at home. Follow up in 4 weeks." },
            
            // Jane Smith - Office Visit - Migraine
            new() { Id = 5, EncounterId = 5, PatientId = 2, AuthorName = "Dr. Michael Chen", CreatedAt = today.AddDays(-240), SignedAt = today.AddDays(-240), Status = NoteStatus.Signed, ChiefComplaint = "Migraine headache", HistoryOfPresentIllness = "Patient with history of migraines presents with typical migraine.", ReviewOfSystems = "Neurologic: Headache, photophobia, nausea. No focal deficits.", PhysicalExam = "Vitals: BP 125/80, HR 68. Neuro: Alert, oriented, cranial nerves II-XII intact.", Assessment = "Migraine headache, partially treated.", Plan = "Continue sumatriptan as needed. Consider preventive therapy if frequency increases." },
            
            // Jane Smith - Annual Exam 2023
            new() { Id = 6, EncounterId = 6, PatientId = 2, AuthorName = "Dr. Michael Chen", CreatedAt = today.AddDays(-730), SignedAt = today.AddDays(-730), Status = NoteStatus.Signed, ChiefComplaint = "Annual physical exam", HistoryOfPresentIllness = "Patient presents for annual wellness visit.", ReviewOfSystems = "Complete review of systems negative except as noted.", PhysicalExam = "Vitals: BP 122/78, HR 66, Weight 143 lbs. Complete physical exam within normal limits.", Assessment = "Healthy adult female. All screenings current.", Plan = "Continue current health regimen. Mammogram ordered. Return for annual exam next year." },
            
            // Jane Smith - Office Visit - Back Pain
            new() { Id = 7, EncounterId = 7, PatientId = 2, AuthorName = "Dr. Michael Chen", CreatedAt = today.AddDays(-690), SignedAt = today.AddDays(-690), Status = NoteStatus.Signed, ChiefComplaint = "Lower back pain", HistoryOfPresentIllness = "Patient reports 2 weeks of lower back pain after lifting heavy boxes.", ReviewOfSystems = "Musculoskeletal: Lower back pain, no extremity numbness/weakness.", PhysicalExam = "Vitals: Normal. Spine: Tenderness L4-L5 area. Full range of motion limited by pain.", Assessment = "Mechanical low back pain, acute.", Plan = "NSAIDs as needed, heat therapy, gentle stretching. Physical therapy if not improved in 2 weeks." },
            
            // Jane Smith - Follow-up - Back Pain
            new() { Id = 8, EncounterId = 8, PatientId = 2, AuthorName = "Dr. Michael Chen", CreatedAt = today.AddDays(-670), SignedAt = today.AddDays(-670), Status = NoteStatus.Signed, ChiefComplaint = "Back pain follow-up", HistoryOfPresentIllness = "Patient reports back pain much improved with NSAIDs and stretching.", ReviewOfSystems = "Musculoskeletal: Mild residual back discomfort, no neurological symptoms.", PhysicalExam = "Vitals: Normal. Spine: Mild tenderness L4-L5, improved range of motion.", Assessment = "Mechanical back pain, resolving.", Plan = "Continue stretching exercises. NSAIDs PRN. Follow up as needed." },
            
            // Jane Smith - Office Visit - Anxiety
            new() { Id = 9, EncounterId = 9, PatientId = 2, AuthorName = "Dr. Michael Chen", CreatedAt = today.AddDays(-650), SignedAt = today.AddDays(-650), Status = NoteStatus.Signed, ChiefComplaint = "Anxiety and stress", HistoryOfPresentIllness = "Patient reports increased work stress causing anxiety, difficulty sleeping.", ReviewOfSystems = "Psychiatric: Anxiety, sleep disturbance. Otherwise negative.", PhysicalExam = "Vitals: BP 128/82, HR 72. Appears anxious but cooperative.", Assessment = "Generalized anxiety disorder, mild.", Plan = "Discussed stress management techniques. Started on sertraline 50mg daily. Follow up in 4 weeks." },
            
            // Jane Smith - Follow-up - Anxiety
            new() { Id = 10, EncounterId = 10, PatientId = 2, AuthorName = "Dr. Michael Chen", CreatedAt = today.AddDays(-620), SignedAt = today.AddDays(-620), Status = NoteStatus.Signed, ChiefComplaint = "Medication follow-up", HistoryOfPresentIllness = "Patient reports improvement in anxiety symptoms on sertraline.", ReviewOfSystems = "Psychiatric: Improved anxiety, better sleep. Otherwise negative.", PhysicalExam = "Vitals: BP 124/80, HR 68. Appears more relaxed.", Assessment = "Generalized anxiety, responding well to treatment.", Plan = "Continue sertraline 50mg daily. Consider increase if symptoms return. Follow up in 3 months." },
            
            // Jane Smith - Office Visit - UTI
            new() { Id = 11, EncounterId = 11, PatientId = 2, AuthorName = "Dr. Michael Chen", CreatedAt = today.AddDays(-580), SignedAt = today.AddDays(-580), Status = NoteStatus.Signed, ChiefComplaint = "Urinary symptoms", HistoryOfPresentIllness = "Patient reports 2 days of dysuria, urinary frequency, urgency.", ReviewOfSystems = "GU: Dysuria, frequency, urgency. No hematuria, no flank pain.", PhysicalExam = "Vitals: Temp 98.6°F, BP 120/78. Abdomen: Suprapubic tenderness, no CVA tenderness.", Assessment = "Uncomplicated urinary tract infection.", Plan = "Urine culture sent. Started on nitrofurantoin 100mg BID for 5 days. Follow up if symptoms persist." },
            
            // Jane Smith - Annual Exam 2022
            new() { Id = 12, EncounterId = 12, PatientId = 2, AuthorName = "Dr. Michael Chen", CreatedAt = today.AddDays(-1095), SignedAt = today.AddDays(-1095), Status = NoteStatus.Signed, ChiefComplaint = "Annual wellness visit", HistoryOfPresentIllness = "Patient presents for annual physical. Reports good health, regular exercise.", ReviewOfSystems = "Complete review of systems negative.", PhysicalExam = "Vitals: BP 120/76, HR 64, Weight 142 lbs. Complete physical exam within normal limits.", Assessment = "Excellent health status. All preventive care up to date.", Plan = "Continue current healthy lifestyle. Return for annual exam next year." },
            
            // Jane Smith - Office Visit - Allergies
            new() { Id = 13, EncounterId = 13, PatientId = 2, AuthorName = "Dr. Michael Chen", CreatedAt = today.AddDays(-1050), SignedAt = today.AddDays(-1050), Status = NoteStatus.Signed, ChiefComplaint = "Seasonal allergies", HistoryOfPresentIllness = "Patient presents with typical seasonal allergy symptoms.", ReviewOfSystems = "HEENT: Sneezing, nasal congestion, itchy eyes, clear nasal discharge.", PhysicalExam = "Vitals: Normal. HEENT: Allergic shiners, nasal turbinates boggy, clear discharge.", Assessment = "Seasonal allergic rhinitis, moderate.", Plan = "Start on daily nasal steroid spray. Continue antihistamines as needed. Follow up as needed." },
            
            // Jane Smith - Follow-up - Hypertension
            new() { Id = 14, EncounterId = 14, PatientId = 2, AuthorName = "Dr. Michael Chen", CreatedAt = today.AddDays(-1000), SignedAt = today.AddDays(-1000), Status = NoteStatus.Signed, ChiefComplaint = "Blood pressure check", HistoryOfPresentIllness = "Patient returns for BP recheck after starting lisinopril 3 months ago.", ReviewOfSystems = "Cardiovascular: No chest pain, palpitations. No dizziness.", PhysicalExam = "Vitals: BP 126/80, HR 68. No orthostatic changes.", Assessment = "Hypertension, well-controlled on medication.", Plan = "Continue lisinopril 10mg daily. Recheck BP in 6 months. Continue lifestyle modifications." },
            
            // Jane Smith - Office Visit - Flu-like illness
            new() { Id = 15, EncounterId = 15, PatientId = 2, AuthorName = "Dr. Michael Chen", CreatedAt = today.AddDays(-950), SignedAt = today.AddDays(-950), Status = NoteStatus.Signed, ChiefComplaint = "Fever and body aches", HistoryOfPresentIllness = "Patient reports 2 days of fever 101°F, body aches, headache, fatigue.", ReviewOfSystems = "Constitutional: Fever, myalgias, fatigue, headache. Otherwise negative.", PhysicalExam = "Vitals: Temp 100.8°F, BP 118/76, HR 72. General: Mildly ill appearance.", Assessment = "Influenza-like illness, despite vaccination.", Plan = "Supportive care with rest, fluids, acetaminophen for fever/aches. Tamiflu prescribed." },
            
            // Robert Johnson - Diabetes management
            new() { Id = 16, EncounterId = 3, PatientId = 3, AuthorName = "Dr. Sarah Smith", CreatedAt = today.AddDays(-3), SignedAt = today.AddDays(-3), Status = NoteStatus.Signed, ChiefComplaint = "Diabetes management", HistoryOfPresentIllness = "Patient with Type 2 DM here for A1c review.", ReviewOfSystems = "Negative except as noted in HPI.", PhysicalExam = "Vitals: BP 142/88, HR 78. Foot exam: No lesions, sensation intact.", Assessment = "Type 2 Diabetes Mellitus with improved control. CKD Stage 3 stable.", Plan = "Continue current insulin regimen. Recheck A1c in 3 months. Nephrology follow-up." },
            
            // John Doe - Current visit
            new() { Id = 17, EncounterId = 6, PatientId = 1, AuthorName = "Dr. Sarah Smith", CreatedAt = today, Status = NoteStatus.InProgress, ChiefComplaint = "Annual wellness visit", HistoryOfPresentIllness = "Patient presents for annual physical exam. No acute concerns." }
        ];
    }

    private static List<SmartPhrase> GenerateMockSmartPhrases() =>
    [
        new() { Id = 1, Name = "Normal Physical Exam", Abbreviation = ".npe", Content = "General: Alert, oriented, no acute distress.\nHEENT: Normocephalic, PERRLA, TMs clear, oropharynx normal.\nNeck: Supple, no lymphadenopathy.\nCardiovascular: RRR, no murmurs, rubs, or gallops.\nLungs: Clear to auscultation bilaterally.\nAbdomen: Soft, non-tender, non-distended, normal bowel sounds.\nExtremities: No edema, pulses 2+ bilaterally.\nNeurologic: Grossly intact.", Category = "Physical Exam", IsShared = true },
        new() { Id = 2, Name = "Negative ROS", Abbreviation = ".negros", Content = "Constitutional: No fever, chills, fatigue, or weight changes.\nEyes: No vision changes.\nENT: No hearing loss, sore throat, or congestion.\nCardiovascular: No chest pain, palpitations, or edema.\nRespiratory: No dyspnea, cough, or wheezing.\nGI: No nausea, vomiting, diarrhea, or constipation.\nGU: No dysuria or frequency.\nMusculoskeletal: No joint pain or swelling.\nNeurologic: No headache, dizziness, or numbness.", Category = "Review of Systems", IsShared = true },
        new() { Id = 3, Name = "Hypertension Follow-up", Abbreviation = ".htnfu", Content = "Patient with essential hypertension returns for follow-up. Reports good compliance with medications. Denies headaches, visual changes, chest pain, or shortness of breath. Home BP readings have been in target range.", Category = "HPI", IsShared = true },
        new() { Id = 4, Name = "Diabetes Follow-up", Abbreviation = ".dmfu", Content = "Patient with Type 2 Diabetes Mellitus returns for routine management. Reports good adherence to medication regimen. Checking blood sugars regularly with readings typically in range. No hypoglycemic episodes. Denies polyuria, polydipsia, or numbness/tingling in extremities.", Category = "HPI", IsShared = true },
        new() { Id = 5, Name = "Return PRN", Abbreviation = ".rtnprn", Content = "Return to clinic as needed for worsening symptoms or new concerns. Call if any questions.", Category = "Plan", IsShared = true }
    ];

    private static List<MedicationCatalog> GenerateMockMedicationCatalog() =>
    [
        new() { Id = 1, Name = "Atorvastatin", BrandNames = ["Lipitor"], CommonDoses = ["10mg", "20mg", "40mg", "80mg"], CommonFrequencies = ["Once daily"], CommonRoutes = ["Oral"], IsHighRisk = false },
        new() { Id = 2, Name = "Metformin", BrandNames = ["Glucophage", "Fortamet", "Riomet"], CommonDoses = ["500mg", "850mg", "1000mg"], CommonFrequencies = ["Twice daily", "Three times daily"], CommonRoutes = ["Oral"], IsHighRisk = false },
        new() { Id = 3, Name = "Lisinopril", BrandNames = ["Prinivil", "Zestril"], CommonDoses = ["5mg", "10mg", "20mg", "40mg"], CommonFrequencies = ["Once daily"], CommonRoutes = ["Oral"], IsHighRisk = false },
        new() { Id = 4, Name = "Amlodipine", BrandNames = ["Norvasc"], CommonDoses = ["2.5mg", "5mg", "10mg"], CommonFrequencies = ["Once daily"], CommonRoutes = ["Oral"], IsHighRisk = false },
        new() { Id = 5, Name = "Metoprolol", BrandNames = ["Lopressor", "Toprol-XL"], CommonDoses = ["25mg", "50mg", "100mg"], CommonFrequencies = ["Twice daily", "Once daily"], CommonRoutes = ["Oral"], IsHighRisk = false },
        new() { Id = 6, Name = "Albuterol", BrandNames = ["ProAir", "Ventolin", "Proventil"], CommonDoses = ["90mcg", "180mcg"], CommonFrequencies = ["As needed", "Every 4-6 hours"], CommonRoutes = ["Inhalation"], IsHighRisk = false },
        new() { Id = 7, Name = "Losartan", BrandNames = ["Cozaar"], CommonDoses = ["25mg", "50mg", "100mg"], CommonFrequencies = ["Once daily"], CommonRoutes = ["Oral"], IsHighRisk = false },
        new() { Id = 8, Name = "Gabapentin", BrandNames = ["Neurontin", "Gralise"], CommonDoses = ["100mg", "300mg", "400mg", "600mg", "800mg"], CommonFrequencies = ["Three times daily", "Once daily at bedtime"], CommonRoutes = ["Oral"], IsHighRisk = false },
        new() { Id = 9, Name = "Omeprazole", BrandNames = ["Prilosec"], CommonDoses = ["20mg", "40mg"], CommonFrequencies = ["Once daily", "Twice daily"], CommonRoutes = ["Oral"], IsHighRisk = false },
        new() { Id = 10, Name = "Sertraline", BrandNames = ["Zoloft"], CommonDoses = ["25mg", "50mg", "100mg", "150mg", "200mg"], CommonFrequencies = ["Once daily"], CommonRoutes = ["Oral"], IsHighRisk = false },
        new() { Id = 11, Name = "Rosuvastatin", BrandNames = ["Crestor"], CommonDoses = ["5mg", "10mg", "20mg", "40mg"], CommonFrequencies = ["Once daily"], CommonRoutes = ["Oral"], IsHighRisk = false },
        new() { Id = 12, Name = "Pantoprazole", BrandNames = ["Protonix"], CommonDoses = ["20mg", "40mg"], CommonFrequencies = ["Once daily"], CommonRoutes = ["Oral"], IsHighRisk = false },
        new() { Id = 13, Name = "Escitalopram", BrandNames = ["Lexapro"], CommonDoses = ["5mg", "10mg", "20mg"], CommonFrequencies = ["Once daily"], CommonRoutes = ["Oral"], IsHighRisk = false },
        new() { Id = 14, Name = "Amphetamine/Dextroamphetamine", BrandNames = ["Adderall", "Adderall XR"], CommonDoses = ["5mg", "10mg", "20mg", "30mg"], CommonFrequencies = ["Once or twice daily"], CommonRoutes = ["Oral"], IsHighRisk = true },
        new() { Id = 15, Name = "Hydrochlorothiazide", BrandNames = ["Microzide", "HydroDIURIL"], CommonDoses = ["12.5mg", "25mg", "50mg"], CommonFrequencies = ["Once daily"], CommonRoutes = ["Oral"], IsHighRisk = false },
        new() { Id = 16, Name = "Bupropion", BrandNames = ["Wellbutrin", "Wellbutrin XL", "Zyban"], CommonDoses = ["150mg", "300mg"], CommonFrequencies = ["Once daily", "Twice daily"], CommonRoutes = ["Oral"], IsHighRisk = false },
        new() { Id = 17, Name = "Fluoxetine", BrandNames = ["Prozac", "Sarafem"], CommonDoses = ["10mg", "20mg", "40mg"], CommonFrequencies = ["Once daily"], CommonRoutes = ["Oral"], IsHighRisk = false },
        new() { Id = 18, Name = "Semaglutide", BrandNames = ["Ozempic", "Wegovy", "Rybelsus"], CommonDoses = ["0.25mg", "0.5mg", "1mg", "1.7mg", "2.4mg"], CommonFrequencies = ["Once weekly"], CommonRoutes = ["Subcutaneous"], IsHighRisk = false },
        new() { Id = 19, Name = "Montelukast", BrandNames = ["Singulair"], CommonDoses = ["10mg"], CommonFrequencies = ["Once daily"], CommonRoutes = ["Oral"], IsHighRisk = false },
        new() { Id = 20, Name = "Trazodone", BrandNames = ["Desyrel", "Oleptro"], CommonDoses = ["50mg", "100mg", "150mg"], CommonFrequencies = ["Once daily at bedtime"], CommonRoutes = ["Oral"], IsHighRisk = false },
        new() { Id = 21, Name = "Simvastatin", BrandNames = ["Zocor"], CommonDoses = ["10mg", "20mg", "40mg", "80mg"], CommonFrequencies = ["Once daily at bedtime"], CommonRoutes = ["Oral"], IsHighRisk = false },
        new() { Id = 22, Name = "Amoxicillin", BrandNames = ["Amoxil", "Trimox"], CommonDoses = ["250mg", "500mg", "875mg"], CommonFrequencies = ["Three times daily", "Twice daily"], CommonRoutes = ["Oral"], IsHighRisk = false },
        new() { Id = 23, Name = "Tamsulosin", BrandNames = ["Flomax"], CommonDoses = ["0.4mg"], CommonFrequencies = ["Once daily"], CommonRoutes = ["Oral"], IsHighRisk = false },
        new() { Id = 24, Name = "Hydrocodone/Acetaminophen", BrandNames = ["Norco", "Vicodin", "Lortab"], CommonDoses = ["5mg/325mg", "7.5mg/325mg", "10mg/325mg"], CommonFrequencies = ["Every 4-6 hours as needed"], CommonRoutes = ["Oral"], IsHighRisk = true },
        new() { Id = 25, Name = "Fluticasone", BrandNames = ["Flovent", "Flonase", "Arnuity"], CommonDoses = ["50mcg", "100mcg", "250mcg"], CommonFrequencies = ["Twice daily"], CommonRoutes = ["Inhalation"], IsHighRisk = false },
        new() { Id = 26, Name = "Meloxicam", BrandNames = ["Mobic"], CommonDoses = ["7.5mg", "15mg"], CommonFrequencies = ["Once daily"], CommonRoutes = ["Oral"], IsHighRisk = false },
        new() { Id = 27, Name = "Apixaban", BrandNames = ["Eliquis"], CommonDoses = ["2.5mg", "5mg"], CommonFrequencies = ["Twice daily"], CommonRoutes = ["Oral"], IsHighRisk = true },
        new() { Id = 28, Name = "Furosemide", BrandNames = ["Lasix"], CommonDoses = ["20mg", "40mg", "80mg"], CommonFrequencies = ["Once or twice daily"], CommonRoutes = ["Oral"], IsHighRisk = false },
        new() { Id = 29, Name = "Insulin Glargine", BrandNames = ["Lantus", "Basaglar", "Toujeo"], CommonDoses = ["Variable units"], CommonFrequencies = ["Once daily"], CommonRoutes = ["Subcutaneous"], IsHighRisk = true },
        new() { Id = 30, Name = "Duloxetine", BrandNames = ["Cymbalta"], CommonDoses = ["20mg", "30mg", "60mg"], CommonFrequencies = ["Once daily", "Twice daily"], CommonRoutes = ["Oral"], IsHighRisk = false },
        new() { Id = 31, Name = "Ibuprofen", BrandNames = ["Advil", "Motrin"], CommonDoses = ["200mg", "400mg", "600mg", "800mg"], CommonFrequencies = ["Every 6-8 hours as needed"], CommonRoutes = ["Oral"], IsHighRisk = false },
        new() { Id = 32, Name = "Famotidine", BrandNames = ["Pepcid"], CommonDoses = ["20mg", "40mg"], CommonFrequencies = ["Once or twice daily"], CommonRoutes = ["Oral"], IsHighRisk = false },
        new() { Id = 33, Name = "Empagliflozin", BrandNames = ["Jardiance"], CommonDoses = ["10mg", "25mg"], CommonFrequencies = ["Once daily"], CommonRoutes = ["Oral"], IsHighRisk = false },
        new() { Id = 34, Name = "Carvedilol", BrandNames = ["Coreg"], CommonDoses = ["3.125mg", "6.25mg", "12.5mg", "25mg"], CommonFrequencies = ["Twice daily"], CommonRoutes = ["Oral"], IsHighRisk = false },
        new() { Id = 35, Name = "Tramadol", BrandNames = ["Ultram", "ConZip"], CommonDoses = ["50mg", "100mg"], CommonFrequencies = ["Every 4-6 hours as needed"], CommonRoutes = ["Oral"], IsHighRisk = false },
        new() { Id = 36, Name = "Alprazolam", BrandNames = ["Xanax"], CommonDoses = ["0.25mg", "0.5mg", "1mg", "2mg"], CommonFrequencies = ["As needed"], CommonRoutes = ["Oral"], IsHighRisk = true },
        new() { Id = 37, Name = "Prednisone", BrandNames = ["Deltasone", "Rayos"], CommonDoses = ["5mg", "10mg", "20mg", "40mg"], CommonFrequencies = ["Once or twice daily"], CommonRoutes = ["Oral"], IsHighRisk = false },
        new() { Id = 38, Name = "Hydroxyzine", BrandNames = ["Vistaril", "Atarax"], CommonDoses = ["10mg", "25mg", "50mg"], CommonFrequencies = ["Every 6 hours as needed"], CommonRoutes = ["Oral"], IsHighRisk = false },
        new() { Id = 39, Name = "Buspirone", BrandNames = ["Buspar"], CommonDoses = ["5mg", "10mg", "15mg", "30mg"], CommonFrequencies = ["Twice or three times daily"], CommonRoutes = ["Oral"], IsHighRisk = false },
        new() { Id = 40, Name = "Clopidogrel", BrandNames = ["Plavix"], CommonDoses = ["75mg"], CommonFrequencies = ["Once daily"], CommonRoutes = ["Oral"], IsHighRisk = false },
        new() { Id = 41, Name = "Glipizide", BrandNames = ["Glucotrol"], CommonDoses = ["5mg", "10mg"], CommonFrequencies = ["Once or twice daily"], CommonRoutes = ["Oral"], IsHighRisk = false },
        new() { Id = 42, Name = "Citalopram", BrandNames = ["Celexa"], CommonDoses = ["10mg", "20mg", "40mg"], CommonFrequencies = ["Once daily"], CommonRoutes = ["Oral"], IsHighRisk = false },
        new() { Id = 43, Name = "Potassium Chloride", BrandNames = ["Klor-Con", "K-Dur"], CommonDoses = ["10mEq", "20mEq"], CommonFrequencies = ["Once or twice daily"], CommonRoutes = ["Oral"], IsHighRisk = false },
        new() { Id = 44, Name = "Allopurinol", BrandNames = ["Zyloprim", "Aloprim"], CommonDoses = ["100mg", "300mg"], CommonFrequencies = ["Once daily"], CommonRoutes = ["Oral"], IsHighRisk = false },
        new() { Id = 45, Name = "Aspirin", BrandNames = ["Bayer", "Ecotrin"], CommonDoses = ["81mg", "325mg"], CommonFrequencies = ["Once daily"], CommonRoutes = ["Oral"], IsHighRisk = false },
        new() { Id = 46, Name = "Cyclobenzaprine", BrandNames = ["Flexeril", "Amrix"], CommonDoses = ["5mg", "10mg"], CommonFrequencies = ["Three times daily"], CommonRoutes = ["Oral"], IsHighRisk = false },
        new() { Id = 47, Name = "Cholecalciferol", BrandNames = ["Vitamin D3", "Drisdol"], CommonDoses = ["1000 IU", "2000 IU", "5000 IU"], CommonFrequencies = ["Once daily"], CommonRoutes = ["Oral"], IsHighRisk = false },
        new() { Id = 48, Name = "Oxycodone", BrandNames = ["OxyContin", "Roxicodone"], CommonDoses = ["5mg", "10mg", "15mg", "20mg", "30mg"], CommonFrequencies = ["Every 4-6 hours as needed"], CommonRoutes = ["Oral"], IsHighRisk = true },
        new() { Id = 49, Name = "Methylphenidate", BrandNames = ["Ritalin", "Concerta", "Daytrana"], CommonDoses = ["5mg", "10mg", "18mg", "27mg", "36mg", "54mg"], CommonFrequencies = ["Once or twice daily"], CommonRoutes = ["Oral"], IsHighRisk = true },
        new() { Id = 50, Name = "Venlafaxine", BrandNames = ["Effexor", "Effexor XR"], CommonDoses = ["37.5mg", "75mg", "150mg"], CommonFrequencies = ["Once or twice daily"], CommonRoutes = ["Oral"], IsHighRisk = false },
        new() { Id = 51, Name = "Spironolactone", BrandNames = ["Aldactone"], CommonDoses = ["25mg", "50mg", "100mg"], CommonFrequencies = ["Once or twice daily"], CommonRoutes = ["Oral"], IsHighRisk = false },
        new() { Id = 52, Name = "Ondansetron", BrandNames = ["Zofran"], CommonDoses = ["4mg", "8mg"], CommonFrequencies = ["Every 8 hours as needed"], CommonRoutes = ["Oral"], IsHighRisk = false },
        new() { Id = 53, Name = "Zolpidem", BrandNames = ["Ambien", "Ambien CR"], CommonDoses = ["5mg", "10mg"], CommonFrequencies = ["Once daily at bedtime"], CommonRoutes = ["Oral"], IsHighRisk = false },
        new() { Id = 54, Name = "Cetirizine", BrandNames = ["Zyrtec"], CommonDoses = ["5mg", "10mg"], CommonFrequencies = ["Once daily"], CommonRoutes = ["Oral"], IsHighRisk = false },
        new() { Id = 55, Name = "Estradiol", BrandNames = ["Estrace", "Vivelle-Dot"], CommonDoses = ["0.5mg", "1mg", "2mg"], CommonFrequencies = ["Once daily"], CommonRoutes = ["Oral"], IsHighRisk = false },
        new() { Id = 56, Name = "Pravastatin", BrandNames = ["Pravachol"], CommonDoses = ["10mg", "20mg", "40mg"], CommonFrequencies = ["Once daily"], CommonRoutes = ["Oral"], IsHighRisk = false },
        new() { Id = 57, Name = "Lisinopril/HCTZ", BrandNames = ["Zestoretic", "Prinzide"], CommonDoses = ["10mg/12.5mg", "20mg/12.5mg", "20mg/25mg"], CommonFrequencies = ["Once daily"], CommonRoutes = ["Oral"], IsHighRisk = false },
        new() { Id = 58, Name = "Lamotrigine", BrandNames = ["Lamictal"], CommonDoses = ["25mg", "50mg", "100mg", "200mg"], CommonFrequencies = ["Once or twice daily"], CommonRoutes = ["Oral"], IsHighRisk = false },
        new() { Id = 59, Name = "Quetiapine", BrandNames = ["Seroquel", "Seroquel XR"], CommonDoses = ["25mg", "50mg", "100mg", "200mg", "300mg"], CommonFrequencies = ["Once or twice daily"], CommonRoutes = ["Oral"], IsHighRisk = false },
        new() { Id = 60, Name = "Fluticasone/Salmeterol", BrandNames = ["Advair", "AirDuo"], CommonDoses = ["100/50mcg", "250/50mcg", "500/50mcg"], CommonFrequencies = ["Twice daily"], CommonRoutes = ["Inhalation"], IsHighRisk = false },
        new() { Id = 61, Name = "Clonazepam", BrandNames = ["Klonopin"], CommonDoses = ["0.25mg", "0.5mg", "1mg", "2mg"], CommonFrequencies = ["Twice or three times daily"], CommonRoutes = ["Oral"], IsHighRisk = true },
        new() { Id = 62, Name = "Dulaglutide", BrandNames = ["Trulicity"], CommonDoses = ["0.75mg", "1.5mg"], CommonFrequencies = ["Once weekly"], CommonRoutes = ["Subcutaneous"], IsHighRisk = false },
        new() { Id = 63, Name = "Azithromycin", BrandNames = ["Zithromax", "Z-Pack"], CommonDoses = ["250mg", "500mg"], CommonFrequencies = ["Once daily for 5 days"], CommonRoutes = ["Oral"], IsHighRisk = false },
        new() { Id = 64, Name = "Losartan/HCTZ", BrandNames = ["Hyzaar"], CommonDoses = ["50mg/12.5mg", "100mg/12.5mg", "100mg/25mg"], CommonFrequencies = ["Once daily"], CommonRoutes = ["Oral"], IsHighRisk = false },
        new() { Id = 65, Name = "Amoxicillin/Clavulanate", BrandNames = ["Augmentin"], CommonDoses = ["250mg/125mg", "500mg/125mg", "875mg/125mg"], CommonFrequencies = ["Twice daily"], CommonRoutes = ["Oral"], IsHighRisk = false }
    ];

    // Structured Data Helper Methods
    private List<RosChecklistSection> GetRosSectionsInternal(int patientId)
    {
        if (_rosByPatient.TryGetValue(patientId, out var sections))
            return sections;
        return _rosByPatient.TryGetValue(0, out var fallback) ? fallback : [];
    }

    private List<PhysicalExamSection> GetPhysicalExamSectionsInternal(int patientId)
    {
        if (_physicalExamByPatient.TryGetValue(patientId, out var sections))
            return sections;
        return _physicalExamByPatient.TryGetValue(0, out var fallback) ? fallback : [];
    }

    private List<RiskScoreEntry> GetRiskScoresInternal(int patientId)
    {
        if (_riskScoresByPatient.TryGetValue(patientId, out var scores))
            return scores;
        return [];
    }

    private static List<RosChecklistSection> CloneRosSections(List<RosChecklistSection> source) =>
        source.Select(s => new RosChecklistSection
        {
            Name = s.Name,
            Items = s.Items.Select(i => new RosChecklistItem
            {
                Label = i.Label,
                IsPositive = i.IsPositive,
                Notes = i.Notes
            }).ToList()
        }).ToList();

    private static List<PhysicalExamSection> ClonePhysicalExamSections(List<PhysicalExamSection> source) =>
        source.Select(s => new PhysicalExamSection
        {
            Name = s.Name,
            IsNormal = s.IsNormal,
            Notes = s.Notes,
            Findings = s.Findings.Select(f => new PhysicalExamFinding
            {
                Label = f.Label,
                IsNormal = f.IsNormal,
                Details = f.Details
            }).ToList()
        }).ToList();

    private static List<RiskScoreEntry> CloneRiskScores(List<RiskScoreEntry> source) =>
        source.Select(r => new RiskScoreEntry
        {
            Name = r.Name,
            Score = r.Score,
            RiskLevel = r.RiskLevel,
            Scale = r.Scale,
            Description = r.Description,
            CalculatedAt = r.CalculatedAt
        }).ToList();

    private static Dictionary<int, List<RosChecklistSection>> GenerateMockRosData()
    {
        var defaultSections = new List<RosChecklistSection>
        {
            new() { Name = "Constitutional", Items = [
                new() { Label = "Fever" },
                new() { Label = "Chills" },
                new() { Label = "Fatigue" },
                new() { Label = "Weight loss" },
                new() { Label = "Weight gain" }
            ]},
            new() { Name = "Eyes", Items = [
                new() { Label = "Vision changes" },
                new() { Label = "Eye pain" },
                new() { Label = "Double vision" }
            ]},
            new() { Name = "ENT", Items = [
                new() { Label = "Hearing loss" },
                new() { Label = "Sore throat" },
                new() { Label = "Nasal congestion" },
                new() { Label = "Sinus pain" }
            ]},
            new() { Name = "Cardiovascular", Items = [
                new() { Label = "Chest pain" },
                new() { Label = "Palpitations" },
                new() { Label = "Edema" },
                new() { Label = "Shortness of breath on exertion" }
            ]},
            new() { Name = "Respiratory", Items = [
                new() { Label = "Dyspnea" },
                new() { Label = "Cough" },
                new() { Label = "Wheezing" },
                new() { Label = "Hemoptysis" }
            ]},
            new() { Name = "Gastrointestinal", Items = [
                new() { Label = "Nausea" },
                new() { Label = "Vomiting" },
                new() { Label = "Diarrhea" },
                new() { Label = "Constipation" },
                new() { Label = "Abdominal pain" }
            ]},
            new() { Name = "Genitourinary", Items = [
                new() { Label = "Dysuria" },
                new() { Label = "Frequency" },
                new() { Label = "Hematuria" }
            ]},
            new() { Name = "Musculoskeletal", Items = [
                new() { Label = "Joint pain" },
                new() { Label = "Joint swelling" },
                new() { Label = "Back pain" },
                new() { Label = "Muscle weakness" }
            ]},
            new() { Name = "Neurologic", Items = [
                new() { Label = "Headache" },
                new() { Label = "Dizziness" },
                new() { Label = "Numbness/tingling" },
                new() { Label = "Weakness" },
                new() { Label = "Seizures" }
            ]},
            new() { Name = "Psychiatric", Items = [
                new() { Label = "Depression" },
                new() { Label = "Anxiety" },
                new() { Label = "Sleep disturbance" }
            ]},
            new() { Name = "Skin", Items = [
                new() { Label = "Rash" },
                new() { Label = "Itching" },
                new() { Label = "Skin lesions" }
            ]}
        };

        return new Dictionary<int, List<RosChecklistSection>>
        {
            { 0, defaultSections }
        };
    }

    private static Dictionary<int, List<PhysicalExamSection>> GenerateMockPhysicalExamData()
    {
        var defaultSections = new List<PhysicalExamSection>
        {
            new() { Name = "General", Findings = [
                new() { Label = "Appearance" },
                new() { Label = "Alertness" },
                new() { Label = "Distress level" }
            ]},
            new() { Name = "HEENT", Findings = [
                new() { Label = "Head/Scalp" },
                new() { Label = "Pupils" },
                new() { Label = "Conjunctivae" },
                new() { Label = "Tympanic membranes" },
                new() { Label = "Oropharynx" }
            ]},
            new() { Name = "Neck", Findings = [
                new() { Label = "Supple" },
                new() { Label = "Lymphadenopathy" },
                new() { Label = "Thyroid" },
                new() { Label = "JVD" }
            ]},
            new() { Name = "Cardiovascular", Findings = [
                new() { Label = "Heart rate/rhythm" },
                new() { Label = "Murmurs" },
                new() { Label = "Rubs/Gallops" },
                new() { Label = "Peripheral pulses" }
            ]},
            new() { Name = "Lungs", Findings = [
                new() { Label = "Breath sounds" },
                new() { Label = "Wheezes" },
                new() { Label = "Rales/Rhonchi" },
                new() { Label = "Respiratory effort" }
            ]},
            new() { Name = "Abdomen", Findings = [
                new() { Label = "Bowel sounds" },
                new() { Label = "Tenderness" },
                new() { Label = "Distension" },
                new() { Label = "Masses" },
                new() { Label = "Organomegaly" }
            ]},
            new() { Name = "Extremities", Findings = [
                new() { Label = "Edema" },
                new() { Label = "Cyanosis" },
                new() { Label = "Clubbing" },
                new() { Label = "Range of motion" }
            ]},
            new() { Name = "Neurologic", Findings = [
                new() { Label = "Mental status" },
                new() { Label = "Cranial nerves" },
                new() { Label = "Motor strength" },
                new() { Label = "Sensation" },
                new() { Label = "Reflexes" },
                new() { Label = "Coordination" }
            ]},
            new() { Name = "Skin", Findings = [
                new() { Label = "Color" },
                new() { Label = "Lesions" },
                new() { Label = "Rashes" },
                new() { Label = "Turgor" }
            ]}
        };

        return new Dictionary<int, List<PhysicalExamSection>>
        {
            { 0, defaultSections }
        };
    }

    private static Dictionary<int, List<RiskScoreEntry>> GenerateMockRiskScores()
    {
        return new Dictionary<int, List<RiskScoreEntry>>
        {
            { 1, [
                new() { Name = "Framingham CVD Risk", Score = 12, RiskLevel = "Moderate", Scale = "0-30%", Description = "10-year cardiovascular disease risk", CalculatedAt = DateTime.Today.AddDays(-14) },
                new() { Name = "ASCVD Risk Score", Score = 8, RiskLevel = "Low-Moderate", Scale = "0-100%", Description = "10-year atherosclerotic cardiovascular disease risk", CalculatedAt = DateTime.Today.AddDays(-14) }
            ]},
            { 3, [
                new() { Name = "Framingham CVD Risk", Score = 22, RiskLevel = "High", Scale = "0-30%", Description = "10-year cardiovascular disease risk", CalculatedAt = DateTime.Today.AddDays(-3) },
                new() { Name = "CKD-EPI eGFR", Score = 45, RiskLevel = "Stage 3a", Scale = "mL/min/1.73m²", Description = "Estimated glomerular filtration rate", CalculatedAt = DateTime.Today.AddDays(-3) },
                new() { Name = "UKPDS Risk Engine", Score = 18, RiskLevel = "Moderate", Scale = "0-100%", Description = "10-year coronary heart disease risk in diabetes", CalculatedAt = DateTime.Today.AddDays(-3) }
            ]},
            { 4, [
                new() { Name = "PHQ-9", Score = 8, RiskLevel = "Mild", Scale = "0-27", Description = "Depression screening score", CalculatedAt = DateTime.Today.AddMonths(-1) },
                new() { Name = "GAD-7", Score = 12, RiskLevel = "Moderate", Scale = "0-21", Description = "Generalized anxiety disorder screening", CalculatedAt = DateTime.Today.AddMonths(-1) }
            ]}
        };
    }

    // Order Entry Methods
    public Task<List<LabPanel>> GetLabPanelsAsync() => Task.FromResult(_labPanels.ToList());

    public Task<List<ImagingCatalog>> GetImagingCatalogAsync() => Task.FromResult(_imagingCatalog.ToList());

    public Task<List<string>> GetSpecialtiesAsync() => Task.FromResult(_specialties.ToList());

    public Task<List<LabOrder>> GetLabOrdersByPatientAsync(int patientId) =>
        Task.FromResult(_labOrders.Where(o => o.PatientId == patientId).OrderByDescending(o => o.OrderedAt).ToList());

    public Task<List<ImagingOrder>> GetImagingOrdersByPatientAsync(int patientId) =>
        Task.FromResult(_imagingOrders.Where(o => o.PatientId == patientId).OrderByDescending(o => o.OrderedAt).ToList());

    public Task<List<ReferralOrder>> GetReferralOrdersByPatientAsync(int patientId) =>
        Task.FromResult(_referralOrders.Where(o => o.PatientId == patientId).OrderByDescending(o => o.OrderedAt).ToList());

    public Task<LabOrder> CreateLabOrderAsync(LabOrder order)
    {
        order.Id = _labOrders.Count > 0 ? _labOrders.Max(o => o.Id) + 1 : 1;
        order.OrderedAt = DateTime.Now;
        order.Status = OrderStatus.Ordered;
        _labOrders.Add(order);
        PersistAdd(order);
        return Task.FromResult(order);
    }

    public Task<ImagingOrder> CreateImagingOrderAsync(ImagingOrder order)
    {
        order.Id = _imagingOrders.Count > 0 ? _imagingOrders.Max(o => o.Id) + 1 : 1;
        order.OrderedAt = DateTime.Now;
        order.Status = OrderStatus.Ordered;
        _imagingOrders.Add(order);
        PersistAdd(order);
        return Task.FromResult(order);
    }

    public Task<ReferralOrder> CreateReferralOrderAsync(ReferralOrder order)
    {
        order.Id = _referralOrders.Count > 0 ? _referralOrders.Max(o => o.Id) + 1 : 1;
        order.OrderedAt = DateTime.Now;
        order.Status = OrderStatus.Ordered;
        _referralOrders.Add(order);
        PersistAdd(order);
        return Task.FromResult(order);
    }

    public List<OrderWarning> CheckOrderWarnings(int patientId, OrderCartItem item)
    {
        var warnings = new List<OrderWarning>();
        var allergies = _allergies.Where(a => a.PatientId == patientId && a.Status == AllergyStatus.Active).ToList();

        if (item.Type == OrderType.Medication && item.OrderData is Prescription rx)
        {
            foreach (var allergy in allergies)
            {
                if (rx.MedicationName.Contains(allergy.Allergen, StringComparison.OrdinalIgnoreCase) ||
                    allergy.Allergen.Contains("Penicillin", StringComparison.OrdinalIgnoreCase) && 
                    (rx.MedicationName.Contains("Amoxicillin", StringComparison.OrdinalIgnoreCase) || 
                     rx.MedicationName.Contains("Ampicillin", StringComparison.OrdinalIgnoreCase)))
                {
                    warnings.Add(new OrderWarning
                    {
                        Message = $"Allergy Alert: Patient is allergic to {allergy.Allergen}",
                        Severity = allergy.Severity == AllergySeverity.LifeThreatening ? OrderWarningSeverity.Critical : OrderWarningSeverity.Warning,
                        Details = $"Reaction: {allergy.Reaction}"
                    });
                }
            }
        }

        if (item.Type == OrderType.Imaging && item.OrderData is ImagingOrder imgOrder && imgOrder.WithContrast)
        {
            var ckdProblem = _problems.FirstOrDefault(p => p.PatientId == patientId && p.Name.Contains("Kidney", StringComparison.OrdinalIgnoreCase));
            if (ckdProblem != null)
            {
                warnings.Add(new OrderWarning
                {
                    Message = "Caution: Patient has kidney disease - contrast may be contraindicated",
                    Severity = OrderWarningSeverity.Warning,
                    Details = ckdProblem.Name
                });
            }
        }

        return warnings;
    }

    // Inbox and Messaging Methods
    public Task<List<InboxMessage>> GetInboxMessagesAsync(string? category = null, bool? unreadOnly = null, bool? flaggedOnly = null, int? patientId = null) =>
        Task.FromResult(_inboxMessages
            .Where(m => category == null || m.Category.ToString() == category)
            .Where(m => unreadOnly != true || !m.IsRead)
            .Where(m => flaggedOnly != true || m.IsFlagged)
            .Where(m => patientId == null || m.PatientId == patientId)
            .OrderByDescending(m => m.SentAt)
            .ToList());

    public Task<InboxMessage?> GetMessageByIdAsync(int messageId) =>
        Task.FromResult(_inboxMessages.FirstOrDefault(m => m.Id == messageId));

    public Task<List<InboxMessage>> GetMessageThreadAsync(int messageId)
    {
        var message = _inboxMessages.FirstOrDefault(m => m.Id == messageId);
        if (message == null) return Task.FromResult(new List<InboxMessage>());

        var rootId = message.ParentMessageId ?? message.Id;
        return Task.FromResult(_inboxMessages
            .Where(m => m.Id == rootId || m.ParentMessageId == rootId)
            .OrderBy(m => m.SentAt)
            .ToList());
    }

    public Task MarkMessageAsReadAsync(int messageId)
    {
        var message = _inboxMessages.FirstOrDefault(m => m.Id == messageId);
        if (message == null) return Task.CompletedTask;

        var wasUnread = !message.IsRead;
        message.IsRead = true;
        message.ReadAt = DateTime.Now;
        message.Status = MessageStatus.Read;
        if (wasUnread) InboxChanged?.Invoke();
        return Task.CompletedTask;
    }

    public Task ToggleMessageFlagAsync(int messageId)
    {
        var message = _inboxMessages.FirstOrDefault(m => m.Id == messageId);
        if (message != null)
        {
            message.IsFlagged = !message.IsFlagged;
        }
        return Task.CompletedTask;
    }

    public Task<InboxMessage> SendMessageAsync(InboxMessage message)
    {
        message.Id = _inboxMessages.Count > 0 ? _inboxMessages.Max(m => m.Id) + 1 : 1;
        message.SentAt = DateTime.Now;
        message.Status = MessageStatus.New;
        _inboxMessages.Add(message);
        InboxChanged?.Invoke();
        return Task.FromResult(message);
    }

    // Task Methods
    public Task<List<ClinicalTask>> GetTasksAsync(string? assignedTo = null, ClinicalTaskStatus? status = null) =>
        Task.FromResult(_clinicalTasks
            .Where(t => assignedTo == null || t.AssignedTo == assignedTo)
            .Where(t => !status.HasValue || t.Status == status.Value)
            .OrderBy(t => t.DueDate)
            .ThenByDescending(t => t.Priority)
            .ToList());

    public Task<ClinicalTask?> GetTaskByIdAsync(int taskId) =>
        Task.FromResult(_clinicalTasks.FirstOrDefault(t => t.Id == taskId));

    public Task CompleteTaskAsync(int taskId)
    {
        var task = _clinicalTasks.FirstOrDefault(t => t.Id == taskId);
        if (task != null)
        {
            task.Status = ClinicalTaskStatus.Completed;
            task.CompletedAt = DateTime.Now;
        }
        return Task.CompletedTask;
    }

    public Task<ClinicalTask> CreateTaskAsync(ClinicalTask task)
    {
        task.Id = _clinicalTasks.Count > 0 ? _clinicalTasks.Max(t => t.Id) + 1 : 1;
        task.CreatedAt = DateTime.Now;
        task.Status = ClinicalTaskStatus.Pending;
        _clinicalTasks.Add(task);
        return Task.FromResult(task);
    }

    public Task<int> GetUnreadMessageCountAsync() =>
        Task.FromResult(_inboxMessages.Count(m => !m.IsRead));

    public Task<int> GetPendingTaskCountAsync() =>
        Task.FromResult(_clinicalTasks.Count(t => t.Status == ClinicalTaskStatus.Pending));

    // Mock Data Generators for Orders and Inbox
    private static List<LabPanel> GenerateMockLabPanels() =>
    [
        new() { Id = 1, Name = "Basic Metabolic Panel (BMP)", Category = "Chemistry", IncludedTests = ["Glucose", "BUN", "Creatinine", "Sodium", "Potassium", "Chloride", "CO2", "Calcium"] },
        new() { Id = 2, Name = "Comprehensive Metabolic Panel (CMP)", Category = "Chemistry", IncludedTests = ["Glucose", "BUN", "Creatinine", "Sodium", "Potassium", "Chloride", "CO2", "Calcium", "Total Protein", "Albumin", "Bilirubin", "ALT", "AST", "Alkaline Phosphatase"] },
        new() { Id = 3, Name = "Complete Blood Count (CBC)", Category = "Hematology", IncludedTests = ["WBC", "RBC", "Hemoglobin", "Hematocrit", "Platelets", "MCV", "MCH", "MCHC"] },
        new() { Id = 4, Name = "Lipid Panel", Category = "Chemistry", IncludedTests = ["Total Cholesterol", "HDL", "LDL", "Triglycerides"], RequiresFasting = true },
        new() { Id = 5, Name = "Hemoglobin A1c", Category = "Chemistry", IncludedTests = ["HbA1c"] },
        new() { Id = 6, Name = "Thyroid Panel", Category = "Chemistry", IncludedTests = ["TSH", "Free T4", "Free T3"] },
        new() { Id = 7, Name = "Liver Function Tests (LFTs)", Category = "Chemistry", IncludedTests = ["AST", "ALT", "Alkaline Phosphatase", "Total Bilirubin", "Direct Bilirubin", "Albumin", "Total Protein"] },
        new() { Id = 8, Name = "Urinalysis", Category = "Urinalysis", IncludedTests = ["pH", "Specific Gravity", "Protein", "Glucose", "Ketones", "Blood", "Leukocyte Esterase", "Nitrites"] },
        new() { Id = 9, Name = "PT/INR", Category = "Coagulation", IncludedTests = ["Prothrombin Time", "INR"] },
        new() { Id = 10, Name = "Urine Drug Screen", Category = "Toxicology", IncludedTests = ["Amphetamines", "Barbiturates", "Benzodiazepines", "Cannabinoids", "Cocaine", "Opiates", "PCP"] }
    ];

    private static List<ImagingCatalog> GenerateMockImagingCatalog() =>
    [
        new() { Id = 1, Modality = "X-Ray", BodyPart = "Chest", Description = "Chest X-Ray PA and Lateral" },
        new() { Id = 2, Modality = "X-Ray", BodyPart = "Abdomen", Description = "Abdominal X-Ray" },
        new() { Id = 3, Modality = "X-Ray", BodyPart = "Spine", Description = "Lumbar Spine X-Ray" },
        new() { Id = 4, Modality = "X-Ray", BodyPart = "Extremity", Description = "Extremity X-Ray" },
        new() { Id = 5, Modality = "CT", BodyPart = "Head", Description = "CT Head without Contrast", CanHaveContrast = true },
        new() { Id = 6, Modality = "CT", BodyPart = "Chest", Description = "CT Chest", CanHaveContrast = true },
        new() { Id = 7, Modality = "CT", BodyPart = "Abdomen/Pelvis", Description = "CT Abdomen and Pelvis", CanHaveContrast = true },
        new() { Id = 8, Modality = "MRI", BodyPart = "Brain", Description = "MRI Brain", CanHaveContrast = true },
        new() { Id = 9, Modality = "MRI", BodyPart = "Spine", Description = "MRI Spine", CanHaveContrast = true },
        new() { Id = 10, Modality = "MRI", BodyPart = "Knee", Description = "MRI Knee" },
        new() { Id = 11, Modality = "Ultrasound", BodyPart = "Abdomen", Description = "Abdominal Ultrasound" },
        new() { Id = 12, Modality = "Ultrasound", BodyPart = "Pelvis", Description = "Pelvic Ultrasound" },
        new() { Id = 13, Modality = "Ultrasound", BodyPart = "Thyroid", Description = "Thyroid Ultrasound" },
        new() { Id = 14, Modality = "Echocardiogram", BodyPart = "Heart", Description = "Transthoracic Echocardiogram" }
    ];

    private static List<string> GenerateMockSpecialties() =>
    [
        "Cardiology", "Dermatology", "Endocrinology", "Gastroenterology", "Nephrology",
        "Neurology", "Oncology", "Ophthalmology", "Orthopedics", "Otolaryngology (ENT)",
        "Psychiatry", "Psychology", "Pulmonology", "Rheumatology", "Urology",
        "General Surgery", "Vascular Surgery", "Pain Management", "Physical Therapy",
        "Podiatry", "Allergy/Immunology", "Infectious Disease", "Hematology"
    ];

    private static List<InboxMessage> GenerateMockInboxMessages()
    {
        var today = DateTime.Today;
        return
        [
            new() { Id = 1, PatientId = 1, PatientName = "John Doe", Subject = "Question about medication", Body = "Dr. Smith, I wanted to ask about the timing of my blood pressure medication. Should I take it in the morning or at night? I've been taking it in the morning but read online that nighttime might be better. Thank you.", FromName = "John Doe", FromRole = "Patient", ToName = "Dr. Sarah Smith", Category = MessageCategory.PatientMessage, SentAt = today.AddHours(-2), IsRead = false, IsUrgent = false, Status = MessageStatus.New },
            new() { Id = 2, PatientId = 3, PatientName = "Robert Johnson", Subject = "Critical Lab Result - Potassium 5.6", Body = "Critical value alert: Patient Robert Johnson has a potassium level of 5.6 mEq/L (critical high). Please review and acknowledge.", FromName = "Lab System", ToName = "Dr. Sarah Smith", Category = MessageCategory.LabResult, SentAt = today.AddDays(-1), IsRead = false, IsUrgent = true, Status = MessageStatus.ActionRequired },
            new() { Id = 3, PatientId = 1, PatientName = "John Doe", Subject = "Refill Request - Lisinopril 10mg", Body = "Patient John Doe has requested a refill of Lisinopril 10mg. Last filled 25 days ago. 3 refills remaining on original prescription.", FromName = "Pharmacy System", ToName = "Dr. Sarah Smith", Category = MessageCategory.RefillRequest, SentAt = today.AddDays(-1).AddHours(-5), IsRead = true, ReadAt = today.AddDays(-1), Status = MessageStatus.Read },
            new() { Id = 4, PatientId = null, PatientName = null, Subject = "Staff Meeting Tomorrow", Body = "Reminder: Monthly staff meeting tomorrow at 12:00 PM in Conference Room A. Lunch will be provided.", FromName = "Office Manager", ToName = "All Staff", Category = MessageCategory.Administrative, SentAt = today.AddDays(-2), IsRead = true, ReadAt = today.AddDays(-1), Status = MessageStatus.Completed },
            new() { Id = 5, PatientId = 4, PatientName = "Maria Garcia", Subject = "Cardiology Consultation Complete", Body = "Dr. Smith, I have completed my evaluation of Maria Garcia. Impression: Mild anxiety-related palpitations. No structural heart disease. Recommend continued management with you. Full report attached.", FromName = "Dr. Michael Chen", FromRole = "Cardiologist", ToName = "Dr. Sarah Smith", Category = MessageCategory.ReferralResponse, SentAt = today.AddDays(-3), IsRead = true, ReadAt = today.AddDays(-2), Status = MessageStatus.Completed },
            new() { Id = 6, PatientId = 2, PatientName = "Jane Smith", Subject = "Annual labs ready for review", Body = "Lab results for patient Jane Smith are now available for review. All values within normal limits.", FromName = "Lab System", ToName = "Dr. Sarah Smith", Category = MessageCategory.LabResult, SentAt = today.AddHours(-6), IsRead = false, Status = MessageStatus.New },
            new() { Id = 7, PatientId = 5, PatientName = "William Brown", Subject = "Question from parent", Body = "Hello Dr. Smith, William has been having more frequent asthma symptoms lately. Should we schedule an appointment or can we try increasing his inhaler use first?", FromName = "Susan Brown", FromRole = "Parent", ToName = "Dr. Sarah Smith", Category = MessageCategory.PatientMessage, SentAt = today.AddHours(-4), IsRead = false, IsFlagged = true, Status = MessageStatus.New }
        ];
    }

    private static List<ClinicalTask> GenerateMockClinicalTasks()
    {
        var today = DateTime.Today;
        return
        [
            new() { Id = 1, PatientId = 3, PatientName = "Robert Johnson", Title = "Review critical potassium result", Description = "Potassium 5.6 - needs immediate review and action", Type = ClinicalTaskType.ReviewResults, Priority = ClinicalTaskPriority.Urgent, Status = ClinicalTaskStatus.Pending, AssignedTo = "Dr. Sarah Smith", CreatedBy = "Lab System", CreatedAt = today.AddDays(-1), DueDate = today },
            new() { Id = 2, PatientId = 1, PatientName = "John Doe", Title = "Refill Lisinopril", Description = "Patient requested refill - 3 refills remaining", Type = ClinicalTaskType.MedicationRenewal, Priority = ClinicalTaskPriority.Normal, Status = ClinicalTaskStatus.Pending, AssignedTo = "Dr. Sarah Smith", CreatedBy = "Pharmacy", CreatedAt = today.AddDays(-1), DueDate = today.AddDays(2) },
            new() { Id = 3, PatientId = 1, PatientName = "John Doe", Title = "Sign encounter note", Description = "Annual wellness visit note pending signature", Type = ClinicalTaskType.SignNote, Priority = ClinicalTaskPriority.Normal, Status = ClinicalTaskStatus.Pending, AssignedTo = "Dr. Sarah Smith", CreatedBy = "System", CreatedAt = today, DueDate = today.AddDays(3) },
            new() { Id = 4, PatientId = 5, PatientName = "William Brown", Title = "Call parent re: asthma symptoms", Description = "Mother messaged about increased asthma symptoms - needs callback", Type = ClinicalTaskType.PhoneCall, Priority = ClinicalTaskPriority.High, Status = ClinicalTaskStatus.Pending, AssignedTo = "Dr. Sarah Smith", CreatedBy = "Dr. Sarah Smith", CreatedAt = today, DueDate = today },
            new() { Id = 5, PatientId = 4, PatientName = "Maria Garcia", Title = "Follow up on cardiology consult", Description = "Review cardiology report and update care plan", Type = ClinicalTaskType.Referral, Priority = ClinicalTaskPriority.Low, Status = ClinicalTaskStatus.Completed, AssignedTo = "Dr. Sarah Smith", CreatedBy = "System", CreatedAt = today.AddDays(-5), DueDate = today.AddDays(-2), CompletedAt = today.AddDays(-2) }
        ];
    }

    // Administration Methods
    public Task<List<SystemUser>> GetSystemUsersAsync() => Task.FromResult(_systemUsers.ToList());
    
    public Task<SystemUser?> GetSystemUserByIdAsync(int id) => 
        Task.FromResult(_systemUsers.FirstOrDefault(u => u.Id == id));

    public Task<SystemUser> CreateSystemUserAsync(SystemUser user)
    {
        user.Id = _systemUsers.Count > 0 ? _systemUsers.Max(u => u.Id) + 1 : 1;
        user.CreatedAt = DateTime.Now;
        user.Status = UserStatus.Active;
        _systemUsers.Add(user);
        return Task.FromResult(user);
    }

    public Task<SystemUser?> UpdateSystemUserAsync(SystemUser user)
    {
        var existing = _systemUsers.FirstOrDefault(u => u.Id == user.Id);
        if (existing != null)
        {
            existing.Username = user.Username;
            existing.FirstName = user.FirstName;
            existing.LastName = user.LastName;
            existing.Email = user.Email;
            existing.Phone = user.Phone;
            existing.Role = user.Role;
            existing.Specialty = user.Specialty;
            existing.NPI = user.NPI;
            existing.DefaultLocationId = user.DefaultLocationId;
            existing.DefaultLocationName = user.DefaultLocationName;
            existing.Status = user.Status;
        }
        return Task.FromResult(existing);
    }

    public Task<List<LocationModel>> GetLocationsAsync() => Task.FromResult(_locations.ToList());
    
    public Task<LocationModel?> GetLocationByIdAsync(int id) => 
        Task.FromResult(_locations.FirstOrDefault(l => l.Id == id));

    public Task<LocationModel> CreateLocationAsync(LocationModel location)
    {
        location.Id = _locations.Count > 0 ? _locations.Max(l => l.Id) + 1 : 1;
        _locations.Add(location);
        return Task.FromResult(location);
    }

    public Task<List<Department>> GetDepartmentsAsync() => Task.FromResult(_departments.ToList());
    
    public Task<Department?> GetDepartmentByIdAsync(int id) => 
        Task.FromResult(_departments.FirstOrDefault(d => d.Id == id));

    public Task<Department> CreateDepartmentAsync(Department department)
    {
        department.Id = _departments.Count > 0 ? _departments.Max(d => d.Id) + 1 : 1;
        _departments.Add(department);
        return Task.FromResult(department);
    }

    public Task<List<NoteTemplate>> GetNoteTemplatesAsync() => Task.FromResult(_noteTemplates.ToList());
    
    public Task<NoteTemplate?> GetNoteTemplateByIdAsync(int id) => 
        Task.FromResult(_noteTemplates.FirstOrDefault(t => t.Id == id));

    public Task<NoteTemplate> CreateNoteTemplateAsync(NoteTemplate template)
    {
        template.Id = _noteTemplates.Count > 0 ? _noteTemplates.Max(t => t.Id) + 1 : 1;
        template.CreatedAt = DateTime.Now;
        _noteTemplates.Add(template);
        return Task.FromResult(template);
    }

    public Task<List<OrderSet>> GetOrderSetsAsync() => Task.FromResult(_orderSets.ToList());
    
    public Task<OrderSet?> GetOrderSetByIdAsync(int id) => 
        Task.FromResult(_orderSets.FirstOrDefault(o => o.Id == id));

    public Task<OrderSet> CreateOrderSetAsync(OrderSet orderSet)
    {
        orderSet.Id = _orderSets.Count > 0 ? _orderSets.Max(o => o.Id) + 1 : 1;
        orderSet.CreatedAt = DateTime.Now;
        _orderSets.Add(orderSet);
        return Task.FromResult(orderSet);
    }

    public Task<SystemSettings> GetSystemSettingsAsync() => Task.FromResult(_systemSettings);
    
    public Task<SystemSettings> UpdateSystemSettingsAsync(SystemSettings settings)
    {
        _systemSettings = settings;
        return Task.FromResult(_systemSettings);
    }

    // Admin Mock Data Generators
    private static List<SystemUser> GenerateMockSystemUsers() =>
    [
        new() { Id = 1, Username = "ssmith", FirstName = "Sarah", LastName = "Smith", Email = "sarah.smith@zebrahoof.com", Phone = "(555) 100-0001", Role = UserRole.Physician, Specialty = "Internal Medicine", NPI = "1234567890", DefaultLocationId = 1, DefaultLocationName = "Main Clinic", Status = UserStatus.Active, CreatedAt = DateTime.Today.AddYears(-3), LastLoginAt = DateTime.Now.AddHours(-1) },
        new() { Id = 2, Username = "mchen", FirstName = "Michael", LastName = "Chen", Email = "michael.chen@zebrahoof.com", Phone = "(555) 100-0002", Role = UserRole.Physician, Specialty = "Cardiology", NPI = "2345678901", DefaultLocationId = 1, DefaultLocationName = "Main Clinic", Status = UserStatus.Active, CreatedAt = DateTime.Today.AddYears(-2), LastLoginAt = DateTime.Now.AddDays(-1) },
        new() { Id = 3, Username = "jwilson", FirstName = "Jane", LastName = "Wilson", Email = "jane.wilson@zebrahoof.com", Phone = "(555) 100-0003", Role = UserRole.Nurse, DefaultLocationId = 1, DefaultLocationName = "Main Clinic", Status = UserStatus.Active, CreatedAt = DateTime.Today.AddYears(-2), LastLoginAt = DateTime.Now.AddHours(-3) },
        new() { Id = 4, Username = "rjones", FirstName = "Robert", LastName = "Jones", Email = "robert.jones@zebrahoof.com", Phone = "(555) 100-0004", Role = UserRole.Nurse, DefaultLocationId = 1, DefaultLocationName = "Main Clinic", Status = UserStatus.Active, CreatedAt = DateTime.Today.AddYears(-1), LastLoginAt = DateTime.Now.AddHours(-2) },
        new() { Id = 5, Username = "agarcia", FirstName = "Anna", LastName = "Garcia", Email = "anna.garcia@zebrahoof.com", Phone = "(555) 100-0005", Role = UserRole.FrontDesk, DefaultLocationId = 1, DefaultLocationName = "Main Clinic", Status = UserStatus.Active, CreatedAt = DateTime.Today.AddMonths(-8), LastLoginAt = DateTime.Now.AddHours(-1) },
        new() { Id = 6, Username = "tlee", FirstName = "Thomas", LastName = "Lee", Email = "thomas.lee@zebrahoof.com", Phone = "(555) 100-0006", Role = UserRole.Billing, DefaultLocationId = 1, DefaultLocationName = "Main Clinic", Status = UserStatus.Active, CreatedAt = DateTime.Today.AddMonths(-6), LastLoginAt = DateTime.Now.AddDays(-2) },
        new() { Id = 7, Username = "admin", FirstName = "System", LastName = "Administrator", Email = "admin@zebrahoof.com", Role = UserRole.Admin, Status = UserStatus.Active, CreatedAt = DateTime.Today.AddYears(-5), LastLoginAt = DateTime.Now },
        new() { Id = 8, Username = "kbrown", FirstName = "Karen", LastName = "Brown", Email = "karen.brown@zebrahoof.com", Phone = "(555) 100-0008", Role = UserRole.Physician, Specialty = "Pediatrics", NPI = "3456789012", DefaultLocationId = 2, DefaultLocationName = "Pediatrics Center", Status = UserStatus.Inactive, CreatedAt = DateTime.Today.AddYears(-1) }
    ];

    private static List<LocationModel> GenerateMockLocations() =>
    [
        new() { Id = 1, Name = "Main Clinic", Address = "123 Medical Center Dr", City = "Springfield", State = "IL", ZipCode = "62701", Phone = "(555) 200-0001", Fax = "(555) 200-0002", IsActive = true, Type = LocationType.Clinic },
        new() { Id = 2, Name = "Pediatrics Center", Address = "456 Children's Way", City = "Springfield", State = "IL", ZipCode = "62702", Phone = "(555) 200-0003", Fax = "(555) 200-0004", IsActive = true, Type = LocationType.Clinic },
        new() { Id = 3, Name = "Urgent Care West", Address = "789 Quick Care Blvd", City = "Springfield", State = "IL", ZipCode = "62703", Phone = "(555) 200-0005", IsActive = true, Type = LocationType.UrgentCare },
        new() { Id = 4, Name = "Telehealth Services", IsActive = true, Type = LocationType.Telehealth },
        new() { Id = 5, Name = "Springfield General Hospital", Address = "1000 Hospital Way", City = "Springfield", State = "IL", ZipCode = "62704", Phone = "(555) 200-0010", IsActive = true, Type = LocationType.Hospital }
    ];

    private static List<Department> GenerateMockDepartments() =>
    [
        new() { Id = 1, Name = "Primary Care", Description = "General internal medicine and family practice", LocationId = 1, LocationName = "Main Clinic", ManagerName = "Dr. Sarah Smith", IsActive = true },
        new() { Id = 2, Name = "Cardiology", Description = "Cardiovascular services", LocationId = 1, LocationName = "Main Clinic", ManagerName = "Dr. Michael Chen", IsActive = true },
        new() { Id = 3, Name = "Pediatrics", Description = "Pediatric care services", LocationId = 2, LocationName = "Pediatrics Center", ManagerName = "Dr. Karen Brown", IsActive = true },
        new() { Id = 4, Name = "Urgent Care", Description = "Walk-in urgent care services", LocationId = 3, LocationName = "Urgent Care West", IsActive = true },
        new() { Id = 5, Name = "Administration", Description = "Administrative and billing services", LocationId = 1, LocationName = "Main Clinic", IsActive = true },
        new() { Id = 6, Name = "Laboratory", Description = "In-house laboratory services", LocationId = 1, LocationName = "Main Clinic", IsActive = true }
    ];

    private static List<NoteTemplate> GenerateMockNoteTemplates() =>
    [
        new() { Id = 1, Name = "Annual Wellness Visit", Category = "Preventive", VisitType = "Annual Exam", Content = "CHIEF COMPLAINT:\nAnnual wellness visit\n\nHISTORY OF PRESENT ILLNESS:\n[Patient Name] presents for annual wellness examination. Patient reports [general health status].\n\nREVIEW OF SYSTEMS:\n[ROS]\n\nPHYSICAL EXAMINATION:\n[PE]\n\nASSESSMENT AND PLAN:\n1. Health Maintenance\n   - [Screenings due]\n   - [Immunizations due]\n2. [Additional diagnoses]", CreatedBy = "Dr. Sarah Smith", CreatedAt = DateTime.Today.AddMonths(-6), IsShared = true, IsActive = true },
        new() { Id = 2, Name = "Follow-up Visit", Category = "General", VisitType = "Follow-up", Content = "CHIEF COMPLAINT:\nFollow-up for [condition]\n\nHISTORY OF PRESENT ILLNESS:\nPatient returns for follow-up of [condition]. [Progress since last visit].\n\nCurrent medications: [med list]\n\nREVIEW OF SYSTEMS:\n[ROS]\n\nPHYSICAL EXAMINATION:\n[PE]\n\nASSESSMENT AND PLAN:\n1. [Diagnosis] - [status]\n   - [Plan]", CreatedBy = "Dr. Sarah Smith", CreatedAt = DateTime.Today.AddMonths(-6), IsShared = true, IsActive = true },
        new() { Id = 3, Name = "Sick Visit - URI", Category = "Acute", VisitType = "Sick Visit", Content = "CHIEF COMPLAINT:\nUpper respiratory symptoms\n\nHISTORY OF PRESENT ILLNESS:\nPatient presents with [duration] of [symptoms]. Denies [pertinent negatives].\n\nREVIEW OF SYSTEMS:\nConstitutional: [fever, chills, fatigue]\nENT: [congestion, sore throat, ear pain]\nRespiratory: [cough, SOB]\n\nPHYSICAL EXAMINATION:\nVitals: [vitals]\nGeneral: [appearance]\nENT: [findings]\nLungs: [findings]\n\nASSESSMENT AND PLAN:\n1. Acute upper respiratory infection\n   - Supportive care\n   - [Medications if indicated]\n   - Return if worsening or no improvement in [timeframe]", CreatedBy = "Dr. Sarah Smith", CreatedAt = DateTime.Today.AddMonths(-3), IsShared = true, IsActive = true },
        new() { Id = 4, Name = "Diabetes Management", Category = "Chronic Disease", VisitType = "Follow-up", Content = "CHIEF COMPLAINT:\nDiabetes management\n\nHISTORY OF PRESENT ILLNESS:\nPatient with Type 2 Diabetes Mellitus presents for routine management. Last A1c: [value] on [date]. Reports [medication compliance]. Home glucose readings: [values].\n\nCurrent Diabetes Medications:\n[med list]\n\nREVIEW OF SYSTEMS:\n[Diabetes-focused ROS]\n\nPHYSICAL EXAMINATION:\nVitals: [vitals]\nFoot Exam: [findings]\nMonofilament: [result]\n\nLABORATORY:\nA1c: [pending/value]\nBMP: [pending/value]\nLipids: [pending/value]\n\nASSESSMENT AND PLAN:\n1. Type 2 Diabetes Mellitus - [control status]\n   - [Medication adjustments]\n   - A1c goal: <7%\n   - Recheck labs in [timeframe]\n2. Diabetic complications screening\n   - [Eye exam, nephropathy screening status]", CreatedBy = "Dr. Sarah Smith", CreatedAt = DateTime.Today.AddMonths(-2), IsShared = true, IsActive = true }
    ];

    private static List<OrderSet> GenerateMockOrderSets() =>
    [
        new() { Id = 1, Name = "Annual Wellness Labs", Category = "Preventive", Description = "Standard lab panel for annual wellness visits", Items = [
            new() { Type = OrderType.Lab, Name = "Comprehensive Metabolic Panel", Details = "CMP" },
            new() { Type = OrderType.Lab, Name = "Complete Blood Count", Details = "CBC" },
            new() { Type = OrderType.Lab, Name = "Lipid Panel", Details = "Fasting preferred" },
            new() { Type = OrderType.Lab, Name = "TSH", Details = "Thyroid screening" }
        ], CreatedBy = "Dr. Sarah Smith", CreatedAt = DateTime.Today.AddMonths(-6), IsShared = true, IsActive = true },
        new() { Id = 2, Name = "Diabetes Monitoring", Category = "Chronic Disease", Description = "Labs for diabetes follow-up", Items = [
            new() { Type = OrderType.Lab, Name = "Hemoglobin A1c", Details = "Glycemic control" },
            new() { Type = OrderType.Lab, Name = "Basic Metabolic Panel", Details = "Renal function" },
            new() { Type = OrderType.Lab, Name = "Urinalysis", Details = "Proteinuria screening" },
            new() { Type = OrderType.Lab, Name = "Lipid Panel", Details = "Cardiovascular risk" }
        ], CreatedBy = "Dr. Sarah Smith", CreatedAt = DateTime.Today.AddMonths(-4), IsShared = true, IsActive = true },
        new() { Id = 3, Name = "Chest Pain Workup", Category = "Acute", Description = "Initial workup for chest pain", Items = [
            new() { Type = OrderType.Lab, Name = "Troponin", Details = "Serial x2" },
            new() { Type = OrderType.Lab, Name = "BMP", Details = "Electrolytes" },
            new() { Type = OrderType.Lab, Name = "CBC", Details = "Complete blood count" },
            new() { Type = OrderType.Imaging, Name = "Chest X-Ray", Details = "PA and Lateral" },
            new() { Type = OrderType.Lab, Name = "BNP", Details = "If CHF suspected" }
        ], CreatedBy = "Dr. Michael Chen", CreatedAt = DateTime.Today.AddMonths(-3), IsShared = true, IsActive = true },
        new() { Id = 4, Name = "Hypertension New Diagnosis", Category = "Chronic Disease", Description = "Initial workup for new hypertension", Items = [
            new() { Type = OrderType.Lab, Name = "Basic Metabolic Panel", Details = "Renal function, electrolytes" },
            new() { Type = OrderType.Lab, Name = "Lipid Panel", Details = "CV risk assessment" },
            new() { Type = OrderType.Lab, Name = "Urinalysis", Details = "Proteinuria" },
            new() { Type = OrderType.Lab, Name = "TSH", Details = "Secondary causes" },
            new() { Type = OrderType.Imaging, Name = "ECG", Details = "Baseline cardiac" }
        ], CreatedBy = "Dr. Sarah Smith", CreatedAt = DateTime.Today.AddMonths(-5), IsShared = true, IsActive = true }
    ];
}
