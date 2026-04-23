using NBomber.Contracts;
using NBomber.CSharp;
using NBomber.Http.CSharp;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace Zebrahoof.EMR.ApiTests.Performance;

public class ApiPerformanceTests : IClassFixture<ApiTestFactory>, IAsyncLifetime
{
    private readonly ApiTestFactory _factory;
    private readonly HttpClient _client;
    private readonly HttpClient _authenticatedClient;

    public ApiPerformanceTests(ApiTestFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
        _authenticatedClient = _factory.CreateAuthenticatedClient("api_test_user");
    }

    public async Task InitializeAsync()
    {
        // Additional setup if needed
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        _authenticatedClient?.Dispose();
    }

    [Fact]
    public async Task GetPatients_LoadTest_HandlesConcurrentRequests()
    {
        // Arrange
        var scenario = Scenario.Create("get_patients_load_test", async context =>
            {
                var response = await _authenticatedClient.GetAsync("/api/patients");
                
                return response.IsSuccessStatusCode
                    ? Response.Ok()
                    : Response.Fail();
            })
            .WithoutWarmUp()
            .WithLoadSimulations(
                Simulation.Inject(rate: 50, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(10))
            );

        // Act
        var stats = NBomberRunner.Register(scenario)
            .WithTestSuite("API Performance Tests")
            .WithTestName("Get Patients Load Test")
            .Run();

        // Assert
        stats.ScenarioStats[0].OkCount.Should().BeGreaterThan(0);
        stats.ScenarioStats[0].FailCount.Should().BeLessThan(stats.ScenarioStats[0].OkCount / 10); // Less than 10% failure rate
        stats.ScenarioStats[0].OkRequest.Percent95.Should().BeLessThan(TimeSpan.FromSeconds(2)); // 95th percentile under 2 seconds
    }

    [Fact]
    public async Task GetPatients_StressTest_HandlesHighLoad()
    {
        // Arrange
        var scenario = Scenario.Create("get_patients_stress_test", async context =>
            {
                var response = await _authenticatedClient.GetAsync("/api/patients");
                
                return response.IsSuccessStatusCode
                    ? Response.Ok()
                    : Response.Fail();
            })
            .WithoutWarmUp()
            .WithLoadSimulations(
                Simulation.KeepConstant(copies: 100, during: TimeSpan.FromSeconds(30))
            );

        // Act
        var stats = NBomberRunner.Register(scenario)
            .WithTestSuite("API Performance Tests")
            .WithTestName("Get Patients Stress Test")
            .Run();

        // Assert
        stats.ScenarioStats[0].OkCount.Should().BeGreaterThan(0);
        stats.ScenarioStats[0].FailCount.Should().BeLessThan(stats.ScenarioStats[0].OkCount / 5); // Less than 20% failure rate
        stats.ScenarioStats[0].OkRequest.Percent99.Should().BeLessThan(TimeSpan.FromSeconds(5)); // 99th percentile under 5 seconds
    }

    [Fact]
    public async Task CreatePatient_LoadTest_HandlesConcurrentCreations()
    {
        // Arrange
        var counter = 0;
        var scenario = Scenario.Create("create_patient_load_test", async context =>
            {
                var patientData = new
                {
                    MRN = $"PERF_{Interlocked.Increment(ref counter):D6}",
                    FirstName = "Performance",
                    LastName = $"Test_{context.ScenarioInfo.CurrentNumber}",
                    DateOfBirth = DateTime.Today.AddYears(-30),
                    Sex = "M",
                    Phone = "555-123-4567",
                    Email = $"perf.test{context.ScenarioInfo.CurrentNumber}@test.com"
                };

                var response = await _authenticatedClient.PostAsJsonAsync("/api/patients", patientData);
                
                return response.StatusCode == System.Net.HttpStatusCode.Created
                    ? Response.Ok()
                    : Response.Fail();
            })
            .WithoutWarmUp()
            .WithLoadSimulations(
                Simulation.Inject(rate: 20, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(15))
            );

        // Act
        var stats = NBomberRunner.Register(scenario)
            .WithTestSuite("API Performance Tests")
            .WithTestName("Create Patient Load Test")
            .Run();

        // Assert
        stats.ScenarioStats[0].OkCount.Should().BeGreaterThan(0);
        stats.ScenarioStats[0].FailCount.Should().BeLessThan(stats.ScenarioStats[0].OkCount / 10); // Less than 10% failure rate
        stats.ScenarioStats[0].OkRequest.Percent95.Should().BeLessThan(TimeSpan.FromSeconds(3)); // 95th percentile under 3 seconds
    }

    [Fact]
    public async Task SearchPatients_LoadTest_HandlesSearchQueries()
    {
        // Arrange
        var searchTerms = new[] { "API", "Test", "Patient", "Performance", "Load" };
        var random = new Random();

        var scenario = Scenario.Create("search_patients_load_test", async context =>
            {
                var searchTerm = searchTerms[random.Next(searchTerms.Length)];
                var response = await _authenticatedClient.GetAsync($"/api/patients/search?q={searchTerm}&limit=10");
                
                return response.IsSuccessStatusCode
                    ? Response.Ok()
                    : Response.Fail();
            })
            .WithoutWarmUp()
            .WithLoadSimulations(
                Simulation.Inject(rate: 30, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(10))
            );

        // Act
        var stats = NBomberRunner.Register(scenario)
            .WithTestSuite("API Performance Tests")
            .WithTestName("Search Patients Load Test")
            .Run();

        // Assert
        stats.ScenarioStats[0].OkCount.Should().BeGreaterThan(0);
        stats.ScenarioStats[0].FailCount.Should().BeLessThan(stats.ScenarioStats[0].OkCount / 10); // Less than 10% failure rate
        stats.ScenarioStats[0].OkRequest.Percent95.Should().BeLessThan(TimeSpan.FromSeconds(2)); // 95th percentile under 2 seconds
    }

    [Fact]
    public async Task Login_LoadTest_HandlesConcurrentLogins()
    {
        // Arrange
        var scenario = Scenario.Create("login_load_test", async context =>
            {
                var loginData = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("Username", "api_test_user"),
                    new KeyValuePair<string, string>("Password", "TestPassword123!")
                });

                var response = await _client.PostAsync("/account/login", loginData);
                
                return response.StatusCode == System.Net.HttpStatusCode.Redirect
                    ? Response.Ok()
                    : Response.Fail();
            })
            .WithoutWarmUp()
            .WithLoadSimulations(
                Simulation.Inject(rate: 25, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(10))
            );

        // Act
        var stats = NBomberRunner.Register(scenario)
            .WithTestSuite("API Performance Tests")
            .WithTestName("Login Load Test")
            .Run();

        // Assert
        stats.ScenarioStats[0].OkCount.Should().BeGreaterThan(0);
        stats.ScenarioStats[0].FailCount.Should().BeLessThan(stats.ScenarioStats[0].OkCount / 10); // Less than 10% failure rate
        stats.ScenarioStats[0].OkRequest.Percent95.Should().BeLessThan(TimeSpan.FromSeconds(3)); // 95th percentile under 3 seconds
    }

    [Fact]
    public async Task MixedWorkload_Simulation_HandlesMixedRequests()
    {
        // Arrange
        var random = new Random();
        var counter = 0;

        var scenario = Scenario.Create("mixed_workload_test", async context =>
            {
                var operation = random.Next(4);
                HttpResponseMessage response;

                switch (operation)
                {
                    case 0: // Get patients
                        response = await _authenticatedClient.GetAsync("/api/patients");
                        break;
                    case 1: // Search patients
                        response = await _authenticatedClient.GetAsync("/api/patients/search?q=API&limit=5");
                        break;
                    case 2: // Get patient by ID
                        response = await _authenticatedClient.GetAsync("/api/patients/1");
                        break;
                    case 3: // Get patient appointments
                        response = await _authenticatedClient.GetAsync("/api/patients/1/appointments");
                        break;
                    default:
                        response = await _authenticatedClient.GetAsync("/api/patients");
                        break;
                }

                return response.IsSuccessStatusCode
                    ? Response.Ok()
                    : Response.Fail();
            })
            .WithoutWarmUp()
            .WithLoadSimulations(
                Simulation.RampingInject(rate: 10, interval: TimeSpan.FromSeconds(1), to: 50, during: TimeSpan.FromSeconds(20))
            );

        // Act
        var stats = NBomberRunner.Register(scenario)
            .WithTestSuite("API Performance Tests")
            .WithTestName("Mixed Workload Test")
            .Run();

        // Assert
        stats.ScenarioStats[0].OkCount.Should().BeGreaterThan(0);
        stats.ScenarioStats[0].FailCount.Should().BeLessThan(stats.ScenarioStats[0].OkCount / 10); // Less than 10% failure rate
        stats.ScenarioStats[0].OkRequest.Percent95.Should().BeLessThan(TimeSpan.FromSeconds(3)); // 95th percentile under 3 seconds
    }

    [Fact]
    public async Task SpikeTest_HandlesTrafficSpikes()
    {
        // Arrange
        var scenario = Scenario.Create("spike_test", async context =>
            {
                var response = await _authenticatedClient.GetAsync("/api/patients");
                
                return response.IsSuccessStatusCode
                    ? Response.Ok()
                    : Response.Fail();
            })
            .WithoutWarmUp()
            .WithLoadSimulations(
                Simulation.RampingInject(rate: 5, interval: TimeSpan.FromSeconds(1), to: 100, during: TimeSpan.FromSeconds(30))
            );

        // Act
        var stats = NBomberRunner.Register(scenario)
            .WithTestSuite("API Performance Tests")
            .WithTestName("Spike Test")
            .Run();

        // Assert
        stats.ScenarioStats[0].OkCount.Should().BeGreaterThan(0);
        stats.ScenarioStats[0].FailCount.Should().BeLessThan(stats.ScenarioStats[0].OkCount / 5); // Less than 20% failure rate during spike
        stats.ScenarioStats[0].OkRequest.Percent99.Should().BeLessThan(TimeSpan.FromSeconds(10)); // 99th percentile under 10 seconds during spike
    }

    [Fact]
    public async Task EnduranceTest_HandlesSustainedLoad()
    {
        // Arrange
        var scenario = Scenario.Create("endurance_test", async context =>
            {
                var response = await _authenticatedClient.GetAsync("/api/patients");
                
                return response.IsSuccessStatusCode
                    ? Response.Ok()
                    : Response.Fail();
            })
            .WithoutWarmUp()
            .WithLoadSimulations(
                Simulation.KeepConstant(copies: 25, during: TimeSpan.FromMinutes(2)) // 2 minutes sustained load
            );

        // Act
        var stats = NBomberRunner.Register(scenario)
            .WithTestSuite("API Performance Tests")
            .WithTestName("Endurance Test")
            .Run();

        // Assert
        stats.ScenarioStats[0].OkCount.Should().BeGreaterThan(0);
        stats.ScenarioStats[0].FailCount.Should().BeLessThan(stats.ScenarioStats[0].OkCount / 20); // Less than 5% failure rate
        stats.ScenarioStats[0].OkRequest.Percent95.Should().BeLessThan(TimeSpan.FromSeconds(2)); // 95th percentile under 2 seconds
    }
}
