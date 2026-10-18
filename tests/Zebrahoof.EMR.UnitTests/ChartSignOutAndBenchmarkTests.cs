using System.Reflection;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit.Abstractions;
using Zebrahoof_EMR.Data;
using Zebrahoof_EMR.Models;
using Zebrahoof_EMR.Services;

namespace Zebrahoof.EMR.UnitTests;

public class ChartSignOutAndBenchmarkTests
{
    private readonly ITestOutputHelper _output;

    public ChartSignOutAndBenchmarkTests(ITestOutputHelper output) => _output = output;

    [Fact]
    public async Task SignOut_ClearsOnlyThatUsersChartCache_AndReleasesTheLock()
    {
        var loader = new CountingLoader();
        var cache = CreateCache(loader);
        var registry = new ActivePatientRegistry();
        var drafts = new NoteDraftService();
        drafts.Save("physician", "browser-a", 2, new NoteDraft { ChiefComplaint = "kept" });
        drafts.Save("physician", "browser-b", 2, new NoteDraft { ChiefComplaint = "device-b" });
        drafts.Save("nurse", "browser-n", 2, new NoteDraft { ChiefComplaint = "other" });
        var signOut = new ClinicSignOut(cache, registry, drafts);
        var physician = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, "Physician"),
            new Claim(ClaimTypes.Name, "physician")
        ], authenticationType: "test"));

        await cache.OpenAsync("Physician", 1);
        await cache.OpenAsync("Nurse", 1);
        registry.Switch("Physician", "circuit-a", 1, "John Doe");
        Assert.Equal(ChartCacheSource.Cache, (await cache.OpenAsync("Physician", 1)).Source);
        Assert.Equal(2, loader.Calls);

        signOut.ForgetCharts(physician);

        Assert.Equal("kept", drafts.Get("physician", "browser-a", 2)!.ChiefComplaint);
        Assert.Equal("device-b", drafts.Get("physician", "browser-b", 2)!.ChiefComplaint);
        Assert.Equal("other", drafts.Get("nurse", "browser-n", 2)!.ChiefComplaint);
        var epochA = drafts.CurrentEpoch("browser-a");
        var epochB = drafts.CurrentEpoch("browser-b");
        signOut.BeginExplicitSignOut(physician, "browser-a");
        Assert.Null(drafts.Get("physician", "browser-a", 2));
        Assert.False(drafts.TrySave("physician", "browser-a", 2, new NoteDraft { ChiefComplaint = "written back" }, epochA));
        Assert.Equal("device-b", drafts.Get("physician", "browser-b", 2)!.ChiefComplaint);
        Assert.True(drafts.TrySave("physician", "browser-b", 2, new NoteDraft { ChiefComplaint = "still" }, epochB));
        Assert.Equal("other", drafts.Get("nurse", "browser-n", 2)!.ChiefComplaint);
        Assert.False(cache.TryPeek("Physician", 1, out _));
        Assert.Null(registry.Current("Physician"));
        Assert.True(cache.TryPeek("Nurse", 1, out _));

        var again = await cache.OpenAsync("Physician", 1);
        Assert.Equal(ChartCacheSource.Loader, again.Source);
        Assert.Equal(3, loader.Calls);
        Assert.True(cache.TryPeek("Nurse", 1, out _));
    }

    [Fact]
    public async Task SeedLongitudinalChart_SecondCallDoesNotDuplicate()
    {
        var clinical = new MockClinicalDataService();
        var first = clinical.SeedLongitudinalChart(5, years: 6);
        var notes = await clinical.GetPatientNotesAsync(5);
        var second = clinical.SeedLongitudinalChart(5, years: 6);

        Assert.Equal(157, first.Notes);
        Assert.Equal(first, second);
        Assert.Equal(157, notes.Count(n => n.Id >= 800_000));
    }

    [Fact]
    public async Task SeedLongitudinalChart_DoesNotDuplicateAnEditedMedication()
    {
        var clinical = new MockClinicalDataService();
        await clinical.AddMedicationToListAsync(5, "Sertraline", "50 mg", "Oral", "Daily", "Dr. Sarah Smith", null, "edited");

        clinical.SeedLongitudinalChart(5, years: 6);
        clinical.SeedLongitudinalChart(5, years: 6);

        var meds = await clinical.GetMedicationsByPatientAsync(5);
        Assert.Equal(1, meds.Count(med => string.Equals(med.Name, "Sertraline", StringComparison.OrdinalIgnoreCase)));
        Assert.Contains(meds, med => med.Name == "Sertraline" && med.Dose == "50 mg");
    }

    [Fact]
    public void BenchmarkSeed_RunsOnlyInDevelopmentWhenTheFlagIsSet()
    {
        Assert.True(ClinicalDataPersistenceInitializer.ShouldSeedBenchmarkChart(isDevelopment: true, flag: "1"));
        Assert.False(ClinicalDataPersistenceInitializer.ShouldSeedBenchmarkChart(isDevelopment: false, flag: "1"));
        Assert.False(ClinicalDataPersistenceInitializer.ShouldSeedBenchmarkChart(isDevelopment: true, flag: null));
        Assert.False(ClinicalDataPersistenceInitializer.ShouldSeedBenchmarkChart(isDevelopment: true, flag: "0"));
        Assert.False(ClinicalDataPersistenceInitializer.ShouldSeedBenchmarkChart(isDevelopment: false, flag: null));
    }

    [Fact]
    public async Task LongitudinalChart_ColdOpenLoadsYearsOfData_WarmOpenDoesNot()
    {
        var patients = CreatePatients();
        var clinical = new MockClinicalDataService();
        var seeded = LargeChartFixture.Seed(clinical, years: 6);
        var patient = await patients.AddPatientAsync(new Patient
        {
            FirstName = "Longitudinal",
            LastName = "Benchmark",
            DateOfBirth = new DateTime(1958, 4, 2),
            Sex = "F",
            Allergies = ["Penicillin"],
            Alerts = ["Fall Risk"],
            PrimaryProvider = "Dr. Sarah Smith"
        });

        // The fixture writes only into this test's in-memory service, under the new patient id.
        LargeChartFixture.Retarget(clinical, seeded, patient.Id);

        var loader = new ClinicalChartLoader(patients, clinical);
        var cache = CreateCache(loader);

        var demoCold = await cache.OpenAsync("Physician", 1);
        var demoWarm = await cache.OpenAsync("Physician", 1);

        var coldSamples = new List<double>();
        ChartCacheOpen? cold = null;
        for (var i = 0; i < 5; i++)
        {
            cache.ClearUser("benchmark-user");
            cold = await cache.OpenAsync("benchmark-user", patient.Id);
            Assert.Equal(ChartCacheSource.Loader, cold.Source);
            coldSamples.Add(cold.LoadMilliseconds);
        }

        var warm = await cache.OpenAsync("benchmark-user", patient.Id);
        coldSamples.Sort();
        var coldMedian = coldSamples[coldSamples.Count / 2];

        Assert.Equal(seeded.Notes, cold!.Snapshot.Notes.Count);
        Assert.Equal(seeded.Labs, cold.Snapshot.Labs.Count);
        Assert.Equal(seeded.Vitals, cold.Snapshot.Vitals.Count);
        Assert.Equal(seeded.Medications, cold.Snapshot.Medications.Count);
        Assert.Equal(seeded.Encounters, cold.Snapshot.Encounters.Count);
        Assert.True(seeded.Notes > demoCold.Snapshot.Notes.Count * 10);
        Assert.True(seeded.Labs > demoCold.Snapshot.Labs.Count * 10);
        Assert.Equal(ChartCacheSource.Cache, warm.Source);
        Assert.Equal(0, warm.LoadMilliseconds);
        Assert.Equal(ChartCacheSource.Cache, demoWarm.Source);
        Assert.Equal(0, demoWarm.LoadMilliseconds);
        Assert.Contains(cold.Snapshot.Notes, note => note.CreatedAt < DateTime.UtcNow.AddYears(-5));
        Assert.Contains(cold.Snapshot.Medications, med => med.Name == "Lisinopril");

        _output.WriteLine(
            "demo patient 1 cold {0:0.###} ms ({1} notes, {2} labs, {3} vitals, {4} meds), warm {5:0.###} ms",
            demoCold.LoadMilliseconds,
            demoCold.Snapshot.Notes.Count,
            demoCold.Snapshot.Labs.Count,
            demoCold.Snapshot.Vitals.Count,
            demoCold.Snapshot.Medications.Count,
            demoWarm.LoadMilliseconds);
        _output.WriteLine(
            "6-year chart cold median {0:0.###} ms (samples {1}) warm {2:0.###} ms; {3} notes, {4} labs, {5} vitals, {6} meds, {7} encounters",
            coldMedian,
            string.Join(", ", coldSamples.Select(sample => sample.ToString("0.###"))),
            warm.LoadMilliseconds,
            seeded.Notes,
            seeded.Labs,
            seeded.Vitals,
            seeded.Medications,
            seeded.Encounters);
    }

    private static PatientChartCache CreateCache(IPatientChartLoader loader) =>
        new(loader, Options.Create(new ChartCacheOptions
        {
            Capacity = 10,
            IdleMinutes = 30,
            MaxSnapshots = 200
        }), TimeProvider.System);

    private static MockPatientService CreatePatients()
    {
        var services = new ServiceCollection();
        var path = Path.Combine(Path.GetTempPath(), $"zebra-bench-{Guid.NewGuid():N}.db");
        services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite($"Data Source={path}"));
        var provider = services.BuildServiceProvider();
        using (var scope = provider.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.EnsureCreated();
        }

        return new MockPatientService(provider.GetRequiredService<IServiceScopeFactory>());
    }

    private sealed class CountingLoader : IPatientChartLoader
    {
        public int Calls { get; private set; }

        public Task<PatientChartSnapshot?> LoadAsync(int patientId, CancellationToken cancellationToken = default)
        {
            Calls++;
            var patient = new Patient
            {
                Id = patientId,
                FirstName = "John",
                LastName = "Doe",
                MRN = "MRN001",
                Sex = "M",
                DateOfBirth = new DateTime(1985, 3, 15)
            };
            return Task.FromResult<PatientChartSnapshot?>(new PatientChartSnapshot
            {
                PatientId = patientId,
                Patient = patient,
                Fingerprint = "x"
            });
        }
    }
}

/// <summary>
/// Builds years of notes, labs, vitals, and medications on a throwaway clinical service.
/// Nothing here is added to the shipped demo seed.
/// </summary>
internal static class LargeChartFixture
{
    private static readonly string[] LabNames =
    [
        "WBC", "RBC", "Hemoglobin", "Hematocrit", "Platelets", "MCV", "MCH", "MCHC",
        "Sodium", "Potassium", "Chloride", "CO2", "BUN", "Creatinine", "Glucose", "Calcium",
        "AST", "ALT", "Alkaline Phosphatase", "Total Bilirubin", "Albumin", "Total Protein"
    ];

    public static SeededChart Seed(MockClinicalDataService clinical, int years)
    {
        const int placeholderPatientId = 900_001;
        var start = new DateTime(2019, 1, 7, 14, 30, 0, DateTimeKind.Utc);
        var end = start.AddYears(years);
        var notes = new List<ClinicalNote>();
        var labs = new List<LabResult>();
        var vitals = new List<VitalSigns>();
        var encounters = new List<Encounter>();
        var medications = new List<Medication>();
        var visit = 0;
        for (var day = start; day < end; day = day.AddDays(14))
        {
            visit++;
            var idBase = 800_000 + visit;
            encounters.Add(new Encounter
            {
                Id = idBase,
                PatientId = placeholderPatientId,
                PatientName = "Benchmark, Longitudinal",
                DateTime = day,
                VisitType = visit % 8 == 0 ? "Annual wellness" : "Follow-up",
                Provider = "Dr. Sarah Smith",
                Location = "Primary Care",
                Status = EncounterStatus.Signed,
                ChiefComplaint = "Hypertension follow-up"
            });
            notes.Add(new ClinicalNote
            {
                Id = idBase,
                EncounterId = idBase,
                PatientId = placeholderPatientId,
                AuthorName = "Dr. Sarah Smith",
                CreatedAt = day,
                SignedAt = day.AddMinutes(20),
                Status = NoteStatus.Signed,
                ChiefComplaint = "Hypertension follow-up",
                HistoryOfPresentIllness =
                    $"Visit {visit} on {day:yyyy-MM-dd}. Blood pressure has been in the 130s at home. " +
                    "No chest pain, shortness of breath, or edema. Taking lisinopril each morning. " +
                    "Reviewed home readings, diet, and adherence. No interval hospitalizations.",
                Assessment = "Essential hypertension, controlled.",
                Plan = "Continue lisinopril 10 mg daily. Recheck BMP. Return in two weeks."
            });
            vitals.Add(new VitalSigns
            {
                Id = idBase,
                PatientId = placeholderPatientId,
                EncounterId = idBase,
                RecordedAt = day,
                RecordedBy = "MA",
                Temperature = 98.4m,
                SystolicBP = 128 + (visit % 7),
                DiastolicBP = 78,
                HeartRate = 72,
                RespiratoryRate = 16,
                OxygenSaturation = 98,
                Weight = 180,
                Height = 68
            });
            foreach (var name in LabNames)
            {
                labs.Add(new LabResult
                {
                    Id = 1_000_000 + labs.Count,
                    PatientId = placeholderPatientId,
                    TestName = name,
                    PanelName = name is "WBC" or "RBC" or "Hemoglobin" or "Hematocrit" or "Platelets" or "MCV" or "MCH" or "MCHC"
                        ? "CBC"
                        : "CMP",
                    Value = name == "Potassium" ? "4.2" : "1",
                    Units = "see panel",
                    ReferenceRange = "normal",
                    Status = LabResultStatus.Final,
                    CollectedAt = day,
                    ResultedAt = day.AddHours(4),
                    IsAbnormal = false,
                    IsCritical = false
                });
            }
        }

        var medNames = new[]
        {
            "Lisinopril", "Atorvastatin", "Metformin", "Amlodipine", "Hydrochlorothiazide",
            "Levothyroxine", "Omeprazole", "Aspirin", "Vitamin D", "Sertraline"
        };
        for (var i = 0; i < medNames.Length; i++)
        {
            medications.Add(new Medication
            {
                Id = 700_000 + i,
                PatientId = placeholderPatientId,
                Name = medNames[i],
                Dose = "10 mg",
                Route = "Oral",
                Frequency = "Daily",
                StartDate = start.AddMonths(i),
                Status = MedicationStatus.Active,
                IsLongTerm = true,
                Prescriber = "Dr. Sarah Smith"
            });
        }

        Add(clinical, "_notes", notes);
        Add(clinical, "_labResults", labs);
        Add(clinical, "_vitals", vitals);
        Add(clinical, "_encounters", encounters);
        Add(clinical, "_medications", medications);
        return new SeededChart(placeholderPatientId, notes.Count, labs.Count, vitals.Count, medications.Count, encounters.Count);
    }

    public static void Retarget(MockClinicalDataService clinical, SeededChart seeded, int patientId)
    {
        Retarget<ClinicalNote>(clinical, "_notes", seeded.PlaceholderPatientId, note => note.PatientId = patientId);
        Retarget<LabResult>(clinical, "_labResults", seeded.PlaceholderPatientId, lab => lab.PatientId = patientId);
        Retarget<VitalSigns>(clinical, "_vitals", seeded.PlaceholderPatientId, vital => vital.PatientId = patientId);
        Retarget<Encounter>(clinical, "_encounters", seeded.PlaceholderPatientId, encounter => encounter.PatientId = patientId);
        Retarget<Medication>(clinical, "_medications", seeded.PlaceholderPatientId, medication => medication.PatientId = patientId);
    }

    private static void Add<T>(MockClinicalDataService clinical, string fieldName, List<T> items)
    {
        var list = (List<T>)typeof(MockClinicalDataService)
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(clinical)!;
        list.AddRange(items);
    }

    private static void Retarget<T>(
        MockClinicalDataService clinical,
        string fieldName,
        int fromPatientId,
        Action<T> assign)
    {
        var list = (List<T>)typeof(MockClinicalDataService)
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(clinical)!;
        foreach (var item in list)
        {
            var patient = (int)typeof(T).GetProperty("PatientId")!.GetValue(item)!;
            if (patient == fromPatientId)
            {
                assign(item);
            }
        }
    }

    internal sealed record SeededChart(int PlaceholderPatientId, int Notes, int Labs, int Vitals, int Medications, int Encounters);
}
