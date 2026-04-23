using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using Zebrahoof_EMR.Data;
using Zebrahoof_EMR.Models;
using Xunit;

namespace Zebrahoof.EMR.ApiTests.Rest;

public class PatientEndpointsTests : IClassFixture<ApiTestFactory>, IAsyncLifetime
{
    private readonly ApiTestFactory _factory;
    private readonly HttpClient _client;
    private readonly HttpClient _authenticatedClient;
    private readonly HttpClient _adminClient;

    public PatientEndpointsTests(ApiTestFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
        _authenticatedClient = _factory.CreateAuthenticatedClient("api_test_user");
        _adminClient = _factory.CreateAuthenticatedClient("api_test_admin");
    }

    public async Task InitializeAsync()
    {
        // Additional setup if needed
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        _authenticatedClient?.Dispose();
        _adminClient?.Dispose();
    }

    [Fact]
    public async Task GetPatients_Unauthorized_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/patients");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetPatients_Authenticated_ReturnsOk()
    {
        // Act
        var response = await _authenticatedClient.GetAsync("/api/patients");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetPatients_WithPagination_ReturnsCorrectPage()
    {
        // Act
        var response = await _authenticatedClient.GetAsync("/api/patients?page=2&pageSize=5");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadFromJsonAsync<PatientListResponse>();
        content.Should().NotBeNull();
        content!.Page.Should().Be(2);
        content.PageSize.Should().Be(5);
    }

    [Fact]
    public async Task GetPatients_WithSearch_ReturnsFilteredResults()
    {
        // Act
        var response = await _authenticatedClient.GetAsync("/api/patients?search=API");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadFromJsonAsync<PatientListResponse>();
        content.Should().NotBeNull();
        content!.Patients.Should().All(p => 
            p.FirstName.Contains("API") || 
            p.LastName.Contains("API") || 
            p.MRN.Contains("API"));
    }

    [Fact]
    public async Task GetPatientById_ValidId_ReturnsPatient()
    {
        // Arrange - Get first patient
        var listResponse = await _authenticatedClient.GetAsync("/api/patients");
        var patientList = await listResponse.Content.ReadFromJsonAsync<PatientListResponse>();
        var firstPatient = patientList!.Patients.First();

        // Act
        var response = await _authenticatedClient.GetAsync($"/api/patients/{firstPatient.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var patient = await response.Content.ReadFromJsonAsync<Patient>();
        patient.Should().NotBeNull();
        patient!.Id.Should().Be(firstPatient.Id);
        patient.MRN.Should().Be(firstPatient.MRN);
    }

    [Fact]
    public async Task GetPatientById_InvalidId_ReturnsNotFound()
    {
        // Act
        var response = await _authenticatedClient.GetAsync("/api/patients/99999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreatePatient_WithValidData_ReturnsCreated()
    {
        // Arrange
        var newPatient = new CreatePatientRequest
        {
            MRN = "API_TEST_001",
            FirstName = "John",
            LastName = "Doe",
            DateOfBirth = new DateTime(1980, 1, 1),
            Sex = "M",
            Phone = "555-123-4567",
            Email = "john.doe@test.com",
            Address = "123 Main St",
            City = "Test City",
            State = "TS",
            ZipCode = "12345",
            PrimaryProvider = "Dr. Smith",
            InsuranceName = "Test Insurance",
            InsuranceId = "INS001",
            Allergies = new List<string> { "Penicillin", "Latex" },
            Alerts = new List<string> { "Diabetes" }
        };

        // Act
        var response = await _adminClient.PostAsJsonAsync("/api/patients", newPatient);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var location = response.Headers.Location?.ToString();
        location.Should().StartWith("/api/patients/");
        
        var createdPatient = await response.Content.ReadFromJsonAsync<Patient>();
        createdPatient.Should().NotBeNull();
        createdPatient!.MRN.Should().Be(newPatient.MRN);
        createdPatient.FirstName.Should().Be(newPatient.FirstName);
        createdPatient.LastName.Should().Be(newPatient.LastName);
    }

    [Fact]
    public async Task CreatePatient_DuplicateMRN_ReturnsConflict()
    {
        // Arrange
        var newPatient = new CreatePatientRequest
        {
            MRN = "API001", // This should already exist from test data
            FirstName = "Jane",
            LastName = "Doe",
            DateOfBirth = new DateTime(1985, 5, 15),
            Sex = "F"
        };

        // Act
        var response = await _adminClient.PostAsJsonAsync("/api/patients", newPatient);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        
        var error = await response.Content.ReadFromJsonAsync<object>();
        error.Should().NotBeNull();
    }

    [Fact]
    public async Task CreatePatient_InvalidData_ReturnsBadRequest()
    {
        // Arrange
        var invalidPatient = new CreatePatientRequest
        {
            MRN = "", // Invalid
            FirstName = "", // Invalid
            LastName = "", // Invalid
            DateOfBirth = default, // Invalid
            Sex = "X" // Invalid
        };

        // Act
        var response = await _adminClient.PostAsJsonAsync("/api/patients", invalidPatient);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        error.Should().NotBeNull();
        error!.Message.Should().Be("Invalid patient data");
        error.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreatePatient_UnauthorizedUser_ReturnsForbidden()
    {
        // Arrange
        var newPatient = new CreatePatientRequest
        {
            MRN = "API_TEST_002",
            FirstName = "Jane",
            LastName = "Smith",
            DateOfBirth = new DateTime(1990, 3, 15),
            Sex = "F"
        };

        // Act
        var response = await _authenticatedClient.PostAsJsonAsync("/api/patients", newPatient);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdatePatient_ValidData_ReturnsOk()
    {
        // Arrange - Get existing patient
        var listResponse = await _authenticatedClient.GetAsync("/api/patients");
        var patientList = await listResponse.Content.ReadFromJsonAsync<PatientListResponse>();
        var patient = patientList!.Patients.First();

        var updateData = new UpdatePatientRequest
        {
            Phone = "555-987-6543",
            Email = "updated@test.com",
            City = "Updated City"
        };

        // Act
        var response = await _adminClient.PutAsJsonAsync($"/api/patients/{patient.Id}", updateData);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var updatedPatient = await response.Content.ReadFromJsonAsync<Patient>();
        updatedPatient.Should().NotBeNull();
        updatedPatient!.Phone.Should().Be(updateData.Phone);
        updatedPatient.Email.Should().Be(updateData.Email);
        updatedPatient.City.Should().Be(updateData.City);
    }

    [Fact]
    public async Task UpdatePatient_InvalidId_ReturnsNotFound()
    {
        // Arrange
        var updateData = new UpdatePatientRequest
        {
            Phone = "555-987-6543"
        };

        // Act
        var response = await _adminClient.PutAsJsonAsync("/api/patients/99999", updateData);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdatePatient_UnauthorizedUser_ReturnsForbidden()
    {
        // Arrange
        var updateData = new UpdatePatientRequest
        {
            Phone = "555-987-6543"
        };

        // Get existing patient ID
        var listResponse = await _authenticatedClient.GetAsync("/api/patients");
        var patientList = await listResponse.Content.ReadFromJsonAsync<PatientListResponse>();
        var patient = patientList!.Patients.First();

        // Act
        var response = await _authenticatedClient.PutAsJsonAsync($"/api/patients/{patient.Id}", updateData);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeletePatient_AdminUser_ReturnsNoContent()
    {
        // Arrange - Create a patient to delete
        var newPatient = new CreatePatientRequest
        {
            MRN = "API_DELETE_001",
            FirstName = "Delete",
            LastName = "Me",
            DateOfBirth = new DateTime(1975, 8, 20),
            Sex = "M"
        };

        var createResponse = await _adminClient.PostAsJsonAsync("/api/patients", newPatient);
        var createdPatient = await createResponse.Content.ReadFromJsonAsync<Patient>();

        // Act
        var response = await _adminClient.DeleteAsync($"/api/patients/{createdPatient!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify patient is deleted
        var getResponse = await _adminClient.GetAsync($"/api/patients/{createdPatient.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeletePatient_UnauthorizedUser_ReturnsForbidden()
    {
        // Arrange
        var listResponse = await _authenticatedClient.GetAsync("/api/patients");
        var patientList = await listResponse.Content.ReadFromJsonAsync<PatientListResponse>();
        var patient = patientList!.Patients.First();

        // Act
        var response = await _authenticatedClient.DeleteAsync($"/api/patients/{patient.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetPatientAppointments_ValidId_ReturnsAppointments()
    {
        // Arrange
        var listResponse = await _authenticatedClient.GetAsync("/api/patients");
        var patientList = await listResponse.Content.ReadFromJsonAsync<PatientListResponse>();
        var patient = patientList!.Patients.First();

        // Act
        var response = await _authenticatedClient.GetAsync($"/api/patients/{patient.Id}/appointments");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var appointments = await response.Content.ReadFromJsonAsync<List<Appointment>>();
        appointments.Should().NotBeNull();
        appointments!.Should().BeAssignableTo<List<Appointment>>();
    }

    [Fact]
    public async Task GetPatientAppointments_InvalidId_ReturnsNotFound()
    {
        // Act
        var response = await _authenticatedClient.GetAsync("/api/patients/99999/appointments");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetPatientAppointments_WithDateRange_ReturnsFilteredAppointments()
    {
        // Arrange
        var listResponse = await _authenticatedClient.GetAsync("/api/patients");
        var patientList = await listResponse.Content.ReadFromJsonAsync<PatientListResponse>();
        var patient = patientList!.Patients.First();
        var fromDate = DateTime.Today.AddDays(-30);
        var toDate = DateTime.Today.AddDays(30);

        // Act
        var response = await _authenticatedClient.GetAsync(
            $"/api/patients/{patient.Id}/appointments?fromDate={fromDate:yyyy-MM-dd}&toDate={toDate:yyyy-MM-dd}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var appointments = await response.Content.ReadFromJsonAsync<List<Appointment>>();
        appointments.Should().NotBeNull();
    }

    [Fact]
    public async Task SearchPatients_ValidQuery_ReturnsResults()
    {
        // Act
        var response = await _authenticatedClient.GetAsync("/api/patients/search?q=API");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var patients = await response.Content.ReadFromJsonAsync<List<Patient>>();
        patients.Should().NotBeNull();
        patients!.Should().OnlyContain(p => 
            p.FirstName.Contains("API") || 
            p.LastName.Contains("API") || 
            p.MRN.Contains("API"));
    }

    [Fact]
    public async Task SearchPatients_EmptyQuery_ReturnsBadRequest()
    {
        // Act
        var response = await _authenticatedClient.GetAsync("/api/patients/search?q=");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        error.Should().NotBeNull();
        error!.Message.Should().Be("Search query 'q' is required");
    }

    [Fact]
    public async Task SearchPatients_WithLimit_ReturnsLimitedResults()
    {
        // Act
        var response = await _authenticatedClient.GetAsync("/api/patients/search?q=API&limit=2");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var patients = await response.Content.ReadFromJsonAsync<List<Patient>>();
        patients.Should().NotBeNull();
        patients!.Count.Should().BeLessThanOrEqualTo(2);
    }

    [Theory]
    [InlineData("GET", "/api/patients")]
    [InlineData("GET", "/api/patients/1")]
    [InlineData("POST", "/api/patients")]
    [InlineData("PUT", "/api/patients/1")]
    [InlineData("DELETE", "/api/patients/1")]
    public async Task Endpoints_WithoutAuthentication_ReturnsUnauthorized(string method, string url)
    {
        // Act
        HttpResponseMessage response = method switch
        {
            "GET" => await _client.GetAsync(url),
            "POST" => await _client.PostAsync(url, null),
            "PUT" => await _client.PutAsync(url, null),
            "DELETE" => await _client.DeleteAsync(url),
            _ => throw new ArgumentException($"Unsupported HTTP method: {method}")
        };

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetPatients_ConcurrentRequests_HandlesCorrectly()
    {
        // Act - Make concurrent requests
        var tasks = new List<Task<HttpResponseMessage>>();
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(_authenticatedClient.GetAsync("/api/patients"));
        }

        var responses = await Task.WhenAll(tasks);

        // Assert
        foreach (var response in responses)
        {
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Dispose();
        }
    }
}

// Helper classes for deserialization
public class ErrorResponse
{
    public string Message { get; set; } = string.Empty;
    public List<string> Errors { get; set; } = new();
}
