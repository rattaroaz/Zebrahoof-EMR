using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace Zebrahoof.EMR.ApiTests.Security;

public class ApiSecurityTests : IClassFixture<ApiTestFactory>, IAsyncLifetime
{
    private readonly ApiTestFactory _factory;
    private readonly HttpClient _client;
    private readonly HttpClient _authenticatedClient;
    private readonly HttpClient _adminClient;

    public ApiSecurityTests(ApiTestFactory factory)
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
    public async Task Endpoints_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        var endpoints = new[]
        {
            "/api/patients",
            "/api/patients/1",
            "/api/patients/search?q=test",
            "/api/patients/1/appointments"
        };

        // Act & Assert
        foreach (var endpoint in endpoints)
        {
            var response = await _client.GetAsync(endpoint);
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }

    [Fact]
    public async Task ProtectedEndpoints_WithInvalidToken_ReturnsUnauthorized()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "invalid_token");

        var endpoints = new[]
        {
            "/api/patients",
            "/api/patients/1",
            "/api/patients/search?q=test"
        };

        // Act & Assert
        foreach (var endpoint in endpoints)
        {
            var response = await _client.GetAsync(endpoint);
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }

    [Fact]
    public async Task AdminEndpoints_WithRegularUser_ReturnsForbidden()
    {
        // Arrange
        var newPatient = new
        {
            MRN = "SECURITY_TEST_001",
            FirstName = "Security",
            LastName = "Test",
            DateOfBirth = DateTime.Today.AddYears(-30),
            Sex = "M"
        };

        // Act
        var response = await _authenticatedClient.PostAsJsonAsync("/api/patients", newPatient);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteEndpoint_WithRegularUser_ReturnsForbidden()
    {
        // Act
        var response = await _authenticatedClient.DeleteAsync("/api/patients/999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AdminEndpoints_WithAdminUser_ReturnsSuccess()
    {
        // Arrange
        var newPatient = new
        {
            MRN = "SECURITY_ADMIN_001",
            FirstName = "Admin",
            LastName = "Test",
            DateOfBirth = DateTime.Today.AddYears(-40),
            Sex = "F"
        };

        // Act
        var response = await _adminClient.PostAsJsonAsync("/api/patients", newPatient);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task SQL_Injection_Attempts_HandlesSafely()
    {
        // Arrange
        var sqlInjectionPayloads = new[]
        {
            "'; DROP TABLE Patients; --",
            "1' OR '1'='1",
            "1; DELETE FROM Patients WHERE 1=1; --",
            "1' UNION SELECT * FROM Users --",
            "'; EXEC xp_cmdshell('format c:'); --"
        };

        foreach (var payload in sqlInjectionPayloads)
        {
            // Act
            var response = await _authenticatedClient.GetAsync($"/api/patients/search?q={Uri.EscapeDataString(payload)}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            
            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotContain("DROP TABLE");
            content.Should().NotContain("DELETE FROM");
            content.Should().NotContain("xp_cmdshell");
        }
    }

    [Fact]
    public async Task XSS_Attempts_HandlesSafely()
    {
        // Arrange
        var xssPayloads = new[]
        {
            "<script>alert('xss')</script>",
            "<img src=x onerror=alert('xss')>",
            "javascript:alert('xss')",
            "<svg onload=alert('xss')>",
            "';alert('xss');//"
        };

        foreach (var payload in xssPayloads)
        {
            // Act
            var response = await _authenticatedClient.GetAsync($"/api/patients/search?q={Uri.EscapeDataString(payload)}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            
            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotContain("<script>");
            content.Should().NotContain("<img");
            content.Should().NotContain("javascript:");
            content.Should().NotContain("<svg");
        }
    }

    [Fact]
    public async Task LargePayload_Requests_HandlesGracefully()
    {
        // Arrange
        var largePayload = new
        {
            MRN = new string('A', 10000), // Very long MRN
            FirstName = new string('B', 10000), // Very long first name
            LastName = new string('C', 10000), // Very long last name
            DateOfBirth = DateTime.Today.AddYears(-30),
            Sex = "M",
            Phone = new string('1', 10000), // Very long phone number
            Email = new string('x', 10000) + "@test.com", // Very long email
            Address = new string('D', 50000), // Very long address
            Notes = new string('E', 100000) // Very long notes
        };

        // Act
        var response = await _adminClient.PostAsJsonAsync("/api/patients", largePayload);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.RequestEntityTooLarge);
    }

    [Fact]
    public async Task RateLimiting_ExcessiveRequests_HandlesCorrectly()
    {
        // Arrange
        var loginData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Username", "api_test_user"),
            new KeyValuePair<string, string>("Password", "WrongPassword")
        });

        // Act - Make multiple failed login attempts
        var responses = new List<HttpResponseMessage>();
        for (int i = 0; i < 10; i++)
        {
            responses.Add(await _client.PostAsync("/account/login", loginData));
        }

        // Assert
        var lastResponse = responses.Last();
        lastResponse.StatusCode.Should().Be(HttpStatusCode.Redirect);
        
        // Check if we're getting locked out
        var redirectLocation = lastResponse.Headers.Location?.ToString();
        if (redirectLocation != null)
        {
            redirectLocation.Should().Contain("error=locked");
        }

        // Cleanup
        foreach (var response in responses)
        {
            response.Dispose();
        }
    }

    [Fact]
    public async Task CORS_Headers_ArePresent()
    {
        // Act
        var response = await _client.GetAsync("/api/patients");

        // Assert
        response.Headers.Contains("Access-Control-Allow-Origin").Should().BeTrue();
    }

    [Fact]
    public async Task Security_Headers_ArePresent()
    {
        // Act
        var response = await _client.GetAsync("/api/patients");

        // Assert
        var headers = response.Headers;
        
        // Check for common security headers
        headers.Contains("X-Content-Type-Options").Should().BeTrue();
        headers.Contains("X-Frame-Options").Should().BeTrue();
        headers.Contains("X-XSS-Protection").Should().BeTrue();
    }

    [Fact]
    public async Task Sensitive_Data_IsNotExposed()
    {
        // Act
        var response = await _authenticatedClient.GetAsync("/api/patients");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        
        // Ensure sensitive data is not exposed
        content.Should().NotContain("password");
        content.Should().NotContain("secret");
        content.Should().NotContain("token");
        content.Should().NotContain("key");
    }

    [Fact]
    public async Task Input_Validation_HandlesMalformedData()
    {
        // Arrange
        var malformedData = new
        {
            MRN = (string?)null, // Null MRN
            FirstName = "", // Empty first name
            LastName = "", // Empty last name
            DateOfBirth = DateTime.MaxValue, // Invalid date
            Sex = "X" // Invalid sex
        };

        // Act
        var response = await _adminClient.PostAsJsonAsync("/api/patients", malformedData);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Invalid patient data");
    }

    [Fact]
    public async Task File_Upload_Attempts_HandlesCorrectly()
    {
        // Arrange
        var multipartContent = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes("test file content"));
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");
        multipartContent.Add(fileContent, "file", "test.txt");

        // Act
        var response = await _authenticatedClient.PostAsync("/api/patients/upload", multipartContent);

        // Assert
        // Should return 404 or 405 since file upload is not implemented
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.MethodNotAllowed);
    }

    [Fact]
    public async Task HTTP_Methods_OnlyAllowedMethodsWork()
    {
        // Arrange
        var endpoints = new[]
        {
            ("/api/patients", new[] { "GET", "POST" }),
            ("/api/patients/1", new[] { "GET", "PUT", "DELETE" })
        };

        var allMethods = new[] { "GET", "POST", "PUT", "DELETE", "PATCH", "HEAD", "OPTIONS" };

        foreach (var (endpoint, allowedMethods) in endpoints)
        {
            foreach (var method in allMethods)
            {
                // Act
                HttpResponseMessage response = method switch
                {
                    "GET" => await _authenticatedClient.GetAsync(endpoint),
                    "POST" => await _authenticatedClient.PostAsync(endpoint, null),
                    "PUT" => await _authenticatedClient.PutAsync(endpoint, null),
                    "DELETE" => await _authenticatedClient.DeleteAsync(endpoint),
                    "PATCH" => await _authenticatedClient.PatchAsync(endpoint, null),
                    "HEAD" => await _authenticatedClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, endpoint)),
                    "OPTIONS" => await _authenticatedClient.SendAsync(new HttpRequestMessage(HttpMethod.Options, endpoint)),
                    _ => throw new ArgumentException($"Unsupported HTTP method: {method}")
                };

                // Assert
                if (allowedMethods.Contains(method))
                {
                    response.StatusCode.Should().NotBe(HttpStatusCode.MethodNotAllowed);
                }
                else
                {
                    response.StatusCode.Should().BeOneOf(HttpStatusCode.MethodNotAllowed, HttpStatusCode.NotFound);
                }

                response.Dispose();
            }
        }
    }

    [Fact]
    public async Task Authentication_Token_Validation_HandlesExpiredTokens()
    {
        // Arrange - Create an expired token scenario
        var expiredToken = "expired_token_12345";
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", expiredToken);

        // Act
        var response = await _client.GetAsync("/api/patients");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Request_Size_Limits_HandlesOversizedRequests()
    {
        // Arrange
        var oversizedData = new string('A', 10 * 1024 * 1024); // 10MB string
        var content = new StringContent(oversizedData, System.Text.Encoding.UTF8, "application/json");

        // Act
        var response = await _authenticatedClient.PostAsync("/api/patients", content);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.RequestEntityTooLarge, HttpStatusCode.UnsupportedMediaType);
    }
}
