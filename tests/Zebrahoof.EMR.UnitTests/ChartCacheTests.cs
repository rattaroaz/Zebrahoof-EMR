using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Zebrahoof_EMR.Data;
using Zebrahoof_EMR.Models;
using Zebrahoof_EMR.Services;

namespace Zebrahoof.EMR.UnitTests;

public class ChartCacheTests
{
    [Fact]
    public async Task NoChartDataIsFetched_UntilTheUserOpensAPatient()
    {
        var loader = new FakeChartLoader();
        var cache = CreateCache(loader);

        Assert.False(cache.TryPeek("physician", 1, out _));
        cache.Sweep();
        cache.InvalidatePatient(1);
        cache.ClearUser("physician");
        Assert.Null(await cache.RefreshAsync("physician", 1));
        Assert.Equal(0, loader.Calls);
        Assert.Empty(loader.Requested);

        await cache.OpenAsync("physician", 1);

        Assert.Equal(1, loader.Calls);
        Assert.Equal(new[] { 1 }, loader.Requested);
    }

    [Fact]
    public async Task OpeningAnEleventhPatient_EvictsTheOldest()
    {
        var loader = new FakeChartLoader();
        var cache = CreateCache(loader, capacity: 10);

        for (var id = 1; id <= 11; id++)
        {
            await cache.OpenAsync("physician", id);
        }

        var oldestFirst = cache.PatientIdsOldestFirst("physician");
        Assert.Equal(10, oldestFirst.Count);
        Assert.DoesNotContain(1, oldestFirst);
        Assert.Equal(2, oldestFirst[0]);
        Assert.Equal(11, oldestFirst[^1]);
        Assert.Equal(11, loader.Calls);

        await cache.OpenAsync("physician", 11);
        Assert.Equal(11, loader.Calls);
        Assert.Equal(ChartCacheSource.Cache, (await cache.OpenAsync("physician", 11)).Source);
    }

    [Fact]
    public async Task ReopeningACachedPatient_DoesNotEvictItWhenANewPatientOpens()
    {
        var loader = new FakeChartLoader();
        var cache = CreateCache(loader, capacity: 10);

        for (var id = 1; id <= 10; id++)
        {
            await cache.OpenAsync("physician", id);
        }

        await cache.OpenAsync("physician", 1);
        await cache.OpenAsync("physician", 11);

        var remaining = cache.PatientIdsOldestFirst("physician");
        Assert.Contains(1, remaining);
        Assert.DoesNotContain(2, remaining);
        Assert.Equal(11, remaining[^1]);
    }

    [Fact]
    public async Task UsersDoNotShareCachedCharts()
    {
        var loader = new FakeChartLoader();
        var cache = CreateCache(loader);

        var physician = await cache.OpenAsync("physician", 1);
        Assert.Equal(ChartCacheSource.Loader, physician.Source);
        Assert.False(cache.TryPeek("nurse", 1, out _));
        Assert.Equal(1, loader.Calls);

        var nurse = await cache.OpenAsync("nurse", 1);
        Assert.Equal(ChartCacheSource.Loader, nurse.Source);
        Assert.Equal(2, loader.Calls);
        Assert.True(cache.TryPeek("physician", 1, out var stillPhysician));
        Assert.Equal("before", stillPhysician!.Patient.FirstName);

        cache.ClearUser("physician");
        Assert.False(cache.TryPeek("physician", 1, out _));
        Assert.True(cache.TryPeek("nurse", 1, out _));
    }

    [Fact]
    public async Task InvalidateAfterEdit_DropsTheSnapshot_AndTheNextOpenReloads()
    {
        var loader = new FakeChartLoader();
        var cache = CreateCache(loader);

        var first = await cache.OpenAsync("physician", 1);
        Assert.Equal("before", first.Snapshot.Patient.FirstName);
        Assert.Equal(ChartCacheSource.Loader, first.Source);

        loader.Label = "after-edit";
        var stillCached = await cache.OpenAsync("physician", 1);
        Assert.Equal(ChartCacheSource.Cache, stillCached.Source);
        Assert.Equal("before", stillCached.Snapshot.Patient.FirstName);
        Assert.Equal(1, loader.Calls);

        cache.InvalidatePatient(1);
        Assert.False(cache.TryPeek("physician", 1, out _));

        var reloaded = await cache.OpenAsync("physician", 1);
        Assert.Equal(ChartCacheSource.Loader, reloaded.Source);
        Assert.Equal("after-edit", reloaded.Snapshot.Patient.FirstName);
        Assert.Equal(2, loader.Calls);
    }

    [Fact]
    public async Task Refresh_LoadsOnlyAChartTheUserAlreadyOpened_AndReplacesItWhenDataChanges()
    {
        var loader = new FakeChartLoader();
        var cache = CreateCache(loader);

        Assert.Null(await cache.RefreshAsync("physician", 4));
        Assert.Equal(0, loader.Calls);

        await cache.OpenAsync("physician", 4);
        Assert.Null(await cache.RefreshAsync("physician", 4));
        Assert.Equal(2, loader.Calls);
        Assert.Equal("before", cache.PatientIdsOldestFirst("physician").Single() == 4
            ? (cache.TryPeek("physician", 4, out var same) ? same!.Patient.FirstName : "")
            : "");

        loader.Label = "server-change";
        var updated = await cache.RefreshAsync("physician", 4);
        Assert.NotNull(updated);
        Assert.Equal("server-change", updated!.Patient.FirstName);
        Assert.True(cache.TryPeek("physician", 4, out var peeked));
        Assert.Equal("server-change", peeked!.Patient.FirstName);
        Assert.Equal(3, loader.Calls);
    }

    [Fact]
    public async Task IdleTimeout_DropsAChartThatWasNotOpenedAgain()
    {
        var loader = new FakeChartLoader();
        var time = new ManualTime();
        var cache = CreateCache(loader, idleMinutes: 30, time: time);

        await cache.OpenAsync("physician", 1);
        time.Advance(TimeSpan.FromMinutes(29));
        cache.Sweep();
        Assert.True(cache.TryPeek("physician", 1, out _));

        time.Advance(TimeSpan.FromMinutes(1));
        cache.Sweep();
        Assert.False(cache.TryPeek("physician", 1, out _));
        Assert.Equal(1, loader.Calls);

        var reopened = await cache.OpenAsync("physician", 1);
        Assert.Equal(ChartCacheSource.Loader, reopened.Source);
        Assert.Equal(2, loader.Calls);
    }

    [Fact]
    public async Task MemoryCap_EvictsTheOldestSnapshotAcrossUsers()
    {
        var loader = new FakeChartLoader();
        var cache = CreateCache(loader, capacity: 10, maxSnapshots: 3);

        await cache.OpenAsync("physician", 1);
        await cache.OpenAsync("physician", 2);
        await cache.OpenAsync("nurse", 3);
        await cache.OpenAsync("physician", 4);

        Assert.False(cache.TryPeek("physician", 1, out _));
        Assert.True(cache.TryPeek("physician", 2, out _));
        Assert.True(cache.TryPeek("nurse", 3, out _));
        Assert.True(cache.TryPeek("physician", 4, out _));
    }

    [Fact]
    public void CacheAndLoader_DoNotTakeAnAuditLogger()
    {
        Assert.DoesNotContain(Dependencies(typeof(PatientChartCache)), type => type == typeof(IAuditLogger));
        Assert.DoesNotContain(Dependencies(typeof(ClinicalChartLoader)), type => type == typeof(IAuditLogger));
        var names = typeof(PatientChartCache)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Select(method => method.Name);
        Assert.DoesNotContain(names, name =>
            name.Contains("Prefetch", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Schedule", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Inbox", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task RealChartLoader_FillsOnlyTheOpenedPatient_AndReopenIsACacheHit()
    {
        var clinical = new MockClinicalDataService();
        var loader = new CountingChartLoader(new ClinicalChartLoader(CreatePatients(), clinical));
        var cache = CreateCache(loader);
        clinical.PatientDataChanged += cache.InvalidatePatient;

        Assert.Equal(0, loader.Calls);
        var cold = await cache.OpenAsync("physician", 1);
        Assert.Equal(ChartCacheSource.Loader, cold.Source);
        Assert.Equal("John", cold.Snapshot.Patient.FirstName);
        Assert.Contains(cold.Snapshot.Patient.Allergies, name => name == "Penicillin");
        Assert.Contains(cold.Snapshot.Allergies, allergy => allergy.Allergen == "Penicillin");
        Assert.Contains(cold.Snapshot.Problems, problem => problem.Name == "Essential Hypertension");
        Assert.Contains(cold.Snapshot.Labs, lab => lab.TestName.Contains("A1c", StringComparison.OrdinalIgnoreCase) || lab.Value == "5.8");
        Assert.Equal(new[] { 1 }, loader.Requested);

        var warm = await cache.OpenAsync("physician", 1);
        Assert.Equal(ChartCacheSource.Cache, warm.Source);
        Assert.Equal(0, warm.LoadMilliseconds);
        Assert.Equal(1, loader.Calls);

        clinical.NotifyPatientDataChanged(1);
        Assert.False(cache.TryPeek("physician", 1, out _));
        await cache.OpenAsync("physician", 1);
        Assert.Equal(2, loader.Calls);
        Assert.Equal(new[] { 1, 1 }, loader.Requested);
    }

    private static MockPatientService CreatePatients()
    {
        var services = new ServiceCollection();
        var path = Path.Combine(Path.GetTempPath(), $"zebra-cache-{Guid.NewGuid():N}.db");
        services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite($"Data Source={path}"));
        var provider = services.BuildServiceProvider();
        using (var scope = provider.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.EnsureCreated();
        }

        return new MockPatientService(provider.GetRequiredService<IServiceScopeFactory>());
    }

    private static PatientChartCache CreateCache(
        IPatientChartLoader loader,
        int capacity = 10,
        int idleMinutes = 30,
        int maxSnapshots = 200,
        TimeProvider? time = null) =>
        new(loader, Options.Create(new ChartCacheOptions
        {
            Capacity = capacity,
            IdleMinutes = idleMinutes,
            MaxSnapshots = maxSnapshots
        }), time ?? TimeProvider.System);

    private static IEnumerable<Type> Dependencies(Type type)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        return type.GetFields(flags).Select(field => field.FieldType)
            .Concat(type.GetConstructors().SelectMany(ctor => ctor.GetParameters().Select(parameter => parameter.ParameterType)));
    }

    private sealed class FakeChartLoader : IPatientChartLoader
    {
        public int Calls { get; private set; }
        public List<int> Requested { get; } = new();
        public string Label { get; set; } = "before";

        public Task<PatientChartSnapshot?> LoadAsync(int patientId, CancellationToken cancellationToken = default)
        {
            Calls++;
            Requested.Add(patientId);
            var patient = new Patient
            {
                Id = patientId,
                FirstName = Label,
                LastName = $"Patient{patientId}",
                MRN = $"MRN{patientId}",
                Sex = "F",
                DateOfBirth = new DateTime(1990, 1, 1)
            };
            var problems = new List<Problem>();
            var medications = new List<Medication>();
            var allergies = new List<Allergy>();
            var labs = new List<LabResult>();
            var vitals = new List<VitalSigns>();
            var notes = new List<ClinicalNote>();
            var encounters = new List<Encounter>();
            return Task.FromResult<PatientChartSnapshot?>(new PatientChartSnapshot
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
                Fingerprint = PatientChartSnapshot.BuildFingerprint(
                    patient, problems, medications, allergies, labs, vitals, notes, encounters)
            });
        }
    }

    private sealed class CountingChartLoader : IPatientChartLoader
    {
        private readonly IPatientChartLoader _inner;
        public int Calls { get; private set; }
        public List<int> Requested { get; } = new();

        public CountingChartLoader(IPatientChartLoader inner) => _inner = inner;

        public async Task<PatientChartSnapshot?> LoadAsync(int patientId, CancellationToken cancellationToken = default)
        {
            Calls++;
            Requested.Add(patientId);
            return await _inner.LoadAsync(patientId, cancellationToken);
        }
    }

    private sealed class ManualTime : TimeProvider
    {
        public DateTimeOffset Utc { get; set; } = new(2026, 10, 17, 12, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Utc;
        public void Advance(TimeSpan by) => Utc = Utc.Add(by);
    }
}
