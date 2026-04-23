using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Zebrahoof_EMR.Data;
using Zebrahoof_EMR.Models;

namespace Zebrahoof_EMR.Services;

public class MockPatientService
{
    private readonly List<Patient> _patients;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly SemaphoreSlim _hydrationLock = new(1, 1);
    private bool _hydrated;

    public MockPatientService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
        _patients = GenerateMockPatients();
    }

    public async Task<List<Patient>> GetAllPatientsAsync()
    {
        await EnsureHydratedAsync();
        return _patients.ToList();
    }

    public async Task<Patient?> GetPatientByIdAsync(int id)
    {
        await EnsureHydratedAsync();
        return _patients.FirstOrDefault(p => p.Id == id);
    }

    public async Task<Patient?> GetPatientByMRNAsync(string mrn)
    {
        await EnsureHydratedAsync();
        return _patients.FirstOrDefault(p => p.MRN == mrn);
    }

    public async Task<List<Patient>> SearchPatientsAsync(string searchTerm)
    {
        await EnsureHydratedAsync();
        return _patients.Where(p =>
            p.FullName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
            p.MRN.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
            (p.Phone?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
            (p.Email?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false)
        ).ToList();
    }

    public async Task<Patient> AddPatientAsync(Patient patient)
    {
        await EnsureHydratedAsync();

        patient.Id = _patients.Max(p => p.Id) + 1;
        patient.MRN = $"MRN{patient.Id:D3}";

        // Persist to the local database so the new patient survives restarts and
        // satisfies foreign keys from Documents / clinical tables. Raw SQL is used so
        // we can supply an explicit Id (EF would otherwise let SQLite generate one,
        // which would diverge from our in-memory Id).
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var allergiesJson = System.Text.Json.JsonSerializer.Serialize(patient.Allergies ?? new List<string>());
        var alertsJson = System.Text.Json.JsonSerializer.Serialize(patient.Alerts ?? new List<string>());

        await db.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT INTO Patients
                (Id, MRN, FirstName, LastName, DateOfBirth, Sex, Phone, Email,
                 Address, City, State, ZipCode, PrimaryProvider, InsuranceName,
                 InsuranceId, Allergies, Alerts, LastVisit)
            VALUES
                ({patient.Id}, {patient.MRN}, {patient.FirstName ?? string.Empty},
                 {patient.LastName ?? string.Empty}, {patient.DateOfBirth},
                 {patient.Sex ?? string.Empty}, {patient.Phone}, {patient.Email},
                 {patient.Address}, {patient.City}, {patient.State}, {patient.ZipCode},
                 {patient.PrimaryProvider}, {patient.InsuranceName}, {patient.InsuranceId},
                 {allergiesJson}, {alertsJson}, {patient.LastVisit});");

        _patients.Add(patient);
        return patient;
    }

    /// <summary>
    /// Lazily merges any patients persisted in the database into the in-memory list.
    /// Seeded mock patients (Ids 1–5) are kept as-is; DB patients with the same Id win.
    /// </summary>
    private async Task EnsureHydratedAsync()
    {
        if (_hydrated) return;
        await _hydrationLock.WaitAsync();
        try
        {
            if (_hydrated) return;

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var dbPatients = await db.Patients.AsNoTracking().ToListAsync();

            foreach (var dbPatient in dbPatients)
            {
                var existing = _patients.FirstOrDefault(p => p.Id == dbPatient.Id);
                if (existing != null)
                {
                    _patients.Remove(existing);
                }
                _patients.Add(dbPatient);
            }

            _hydrated = true;
        }
        finally
        {
            _hydrationLock.Release();
        }
    }

    private static List<Patient> GenerateMockPatients() =>
    [
        new() {
            Id = 1, MRN = "MRN001", FirstName = "John", LastName = "Doe",
            DateOfBirth = new DateTime(1985, 3, 15), Sex = "Male",
            Phone = "(555) 123-4567", Email = "john.doe@email.com",
            Address = "123 Main St", City = "Springfield", State = "IL", ZipCode = "62701",
            PrimaryProvider = "Dr. Sarah Smith", InsuranceName = "Blue Cross", InsuranceId = "BC123456",
            Allergies = ["Penicillin", "Sulfa"], Alerts = ["Fall Risk"],
            LastVisit = DateTime.Today.AddDays(-14)
        },
        new() {
            Id = 2, MRN = "MRN002", FirstName = "Jane", LastName = "Smith",
            DateOfBirth = new DateTime(1992, 7, 22), Sex = "Female",
            Phone = "(555) 234-5678", Email = "jane.smith@email.com",
            Address = "456 Oak Ave", City = "Springfield", State = "IL", ZipCode = "62702",
            PrimaryProvider = "Dr. Sarah Smith", InsuranceName = "Aetna", InsuranceId = "AET789012",
            Allergies = [], Alerts = [],
            LastVisit = DateTime.Today.AddDays(-7)
        },
        new() {
            Id = 3, MRN = "MRN003", FirstName = "Robert", LastName = "Johnson",
            DateOfBirth = new DateTime(1958, 11, 8), Sex = "Male",
            Phone = "(555) 345-6789", Email = "robert.j@email.com",
            Address = "789 Elm Blvd", City = "Springfield", State = "IL", ZipCode = "62703",
            PrimaryProvider = "Dr. Sarah Smith", InsuranceName = "Medicare", InsuranceId = "MED345678",
            Allergies = ["Aspirin", "Codeine"], Alerts = ["Diabetes", "Hypertension"],
            LastVisit = DateTime.Today.AddDays(-3)
        },
        new() {
            Id = 4, MRN = "MRN004", FirstName = "Maria", LastName = "Garcia",
            DateOfBirth = new DateTime(1978, 5, 30), Sex = "Female",
            Phone = "(555) 456-7890", Email = "maria.garcia@email.com",
            Address = "321 Pine St", City = "Springfield", State = "IL", ZipCode = "62704",
            PrimaryProvider = "Dr. Sarah Smith", InsuranceName = "United Health", InsuranceId = "UH901234",
            Allergies = ["Latex"], Alerts = ["Spanish Preferred"],
            LastVisit = DateTime.Today.AddDays(-30)
        },
        new() {
            Id = 5, MRN = "MRN005", FirstName = "William", LastName = "Brown",
            DateOfBirth = new DateTime(2010, 9, 12), Sex = "Male",
            Phone = "(555) 567-8901", Email = "w.brown.parent@email.com",
            Address = "654 Maple Dr", City = "Springfield", State = "IL", ZipCode = "62705",
            PrimaryProvider = "Dr. Sarah Smith", InsuranceName = "Cigna", InsuranceId = "CIG567890",
            Allergies = ["Peanuts"], Alerts = [],
            LastVisit = DateTime.Today.AddDays(-60)
        }
    ];
}
