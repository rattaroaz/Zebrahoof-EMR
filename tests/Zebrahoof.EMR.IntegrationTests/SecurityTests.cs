using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Net;
using System.Net.Http;
using Zebrahoof_EMR.Data;
using Zebrahoof_EMR.Models;
using Zebrahoof_EMR.Services;
using Zebrahoof_EMR.Helpers;

namespace Zebrahoof_EMR.IntegrationTests;

public class SecurityTests : IClassFixture<CustomWebApplicationFactory>, IDisposable
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public SecurityTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Seed data
        Task.Run(async () => await InitializeAsync()).Wait();
    }

    private async Task InitializeAsync()
    {
        await _factory.ExecuteScopeAsync(async services =>
        {
            await TestDataSeeder.SeedTestUserAsync(services);
            await TestDataSeeder.SeedAdminUserAsync(services);
        });
    }

    public void Dispose()
    {
        Task.Run(async () => await ClearDataAsync()).Wait();
    }

    private async Task ClearDataAsync()
    {
        await _factory.ExecuteScopeAsync(async services =>
        {
            await TestDataSeeder.ClearAllDataAsync(services);
        });
    }

    [Fact(Skip = "Audit logging for failed login not fully implemented")]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var formData = new Dictionary<string, string>
        {
            {"Username", "nonexistent"},
            {"Password", "wrongpassword"},
            {"ReturnUrl", "/dashboard"}
        };

        var content = new FormUrlEncodedContent(formData);

        // Act
        var response = await _client.PostAsync("/account/login", content);

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("error=invalid", response.Headers.Location?.ToString());

        // Verify audit log was created for failed login attempt
        await _factory.ExecuteDbContextAsync(async db =>
        {
            var auditLogs = await db.AuditLogs
                .Where(log => log.Action == "login_failed")
                .ToListAsync();
            Assert.NotEmpty(auditLogs);
        });
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutAuthentication_ReturnsRedirect()
    {
        // Act
        var response = await _client.GetAsync("/patients");

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/login", response.Headers.Location?.ToString());
    }

    [Fact(Skip = "Admin endpoint authorization not yet implemented")]
    public async Task AdminEndpoint_WithNonAdminUser_ReturnsForbidden()
    {
        // First login as regular user
        var loginResponse = await _client.PostAsync("/account/login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            {"Username", "testuser"},
            {"Password", "TestPassword123!"},
            {"ReturnUrl", "/dashboard"}
        }));

        // Extract cookies
        var cookies = loginResponse.Headers.GetValues("Set-Cookie")?.ToArray() ?? Array.Empty<string>();
        var refreshCookie = cookies.FirstOrDefault(c => c.Contains(SessionCookieHelper.RefreshCookieName));
        var sessionIdCookie = cookies.FirstOrDefault(c => c.Contains(SessionCookieHelper.SessionIdCookieName));

        // Add cookies to client
        _client.DefaultRequestHeaders.Add("Cookie", $"{refreshCookie?.Split(';')[0]}; {sessionIdCookie?.Split(';')[0]}");

        // Act - try to access admin endpoint
        var adminResponse = await _client.GetAsync("/admin");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, adminResponse.StatusCode);

        // Verify audit log was created for unauthorized access attempt
        await _factory.ExecuteDbContextAsync(async db =>
        {
            var auditLogs = await db.AuditLogs
                .Where(log => log.Action == "unauthorized_access_attempt")
                .ToListAsync();
            Assert.NotEmpty(auditLogs);
        });
    }

    [Fact]
    public async Task Login_WithSQLInjection_Attempt_Blocked()
    {
        // Arrange
        var formData = new Dictionary<string, string>
        {
            {"Username", "admin'; DROP TABLE Users; --"},
            {"Password", "password"},
            {"ReturnUrl", "/dashboard"}
        };

        var content = new FormUrlEncodedContent(formData);

        // Act
        var response = await _client.PostAsync("/account/login", content);

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("error=invalid", response.Headers.Location?.ToString());

        // Verify database still exists and has users
        await _factory.ExecuteDbContextAsync(async db =>
        {
            var userCount = await db.Users.CountAsync();
            Assert.True(userCount > 0, "Database should still contain users after SQL injection attempt");
        });
    }

    [Fact]
    public async Task Login_WithXSS_Attempt_Blocked()
    {
        // Arrange
        var formData = new Dictionary<string, string>
        {
            {"Username", "<script>alert('xss')</script>"},
            {"Password", "password"},
            {"ReturnUrl", "/dashboard"}
        };

        var content = new FormUrlEncodedContent(formData);

        // Act
        var response = await _client.PostAsync("/account/login", content);

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("error=invalid", response.Headers.Location?.ToString());

        // Verify no script execution occurred (response should be safe)
        var responseContent = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("<script>", responseContent);
    }

    [Fact]
    public async Task Session_WithInvalidToken_Rejected()
    {
        // Arrange
        _client.DefaultRequestHeaders.Add("Cookie", 
            $"{SessionCookieHelper.RefreshCookieName}=invalid_token; {SessionCookieHelper.SessionIdCookieName}=invalid_session");

        // Act
        var response = await _client.GetAsync("/patients");

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/login", response.Headers.Location?.ToString());
    }

    [Fact(Skip = "Session hijacking detection not yet implemented")]
    public async Task Session_Hijacking_Attempt_Detected()
    {
        // First login as user
        var loginResponse = await _client.PostAsync("/account/login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            {"Username", "testuser"},
            {"Password", "TestPassword123!"},
            {"ReturnUrl", "/dashboard"}
        }));

        // Extract session cookies
        var cookies = loginResponse.Headers.GetValues("Set-Cookie")?.ToArray() ?? Array.Empty<string>();
        var refreshCookie = cookies.FirstOrDefault(c => c.Contains(SessionCookieHelper.RefreshCookieName));
        var sessionIdCookie = cookies.FirstOrDefault(c => c.Contains(SessionCookieHelper.SessionIdCookieName));

        // Create new client with same session but different IP/user agent
        var hijackedClient = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Simulate hijacking by using same cookies with different context
        hijackedClient.DefaultRequestHeaders.Add("Cookie", $"{refreshCookie?.Split(';')[0]}; {sessionIdCookie?.Split(';')[0]}");
        hijackedClient.DefaultRequestHeaders.Add("User-Agent", "DifferentBrowser/1.0");
        hijackedClient.DefaultRequestHeaders.Add("X-Forwarded-For", "192.168.1.100");

        // Act
        var response = await hijackedClient.GetAsync("/patients");

        // Assert - should detect suspicious activity and invalidate session
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/login", response.Headers.Location?.ToString());

        // Verify audit log was created for suspicious session activity
        await _factory.ExecuteDbContextAsync(async db =>
        {
            var auditLogs = await db.AuditLogs
                .Where(log => log.Action == "suspicious_session_activity")
                .ToListAsync();
            Assert.NotEmpty(auditLogs);
        });
    }

    [Fact(Skip = "Input sanitization not fully implemented")]
    public async Task Input_WithMaliciousData_Sanitized()
    {
        // First login
        await _client.PostAsync("/account/login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            {"Username", "testuser"},
            {"Password", "TestPassword123!"},
            {"ReturnUrl", "/dashboard"}
        }));

        // Try to submit malicious data to a form endpoint
        var maliciousData = new Dictionary<string, string>
        {
            {"FirstName", "<script>alert('xss')</script>"},
            {"LastName", "'; DROP TABLE Patients; --"},
            {"Email", "test@example.com"}
        };

        var content = new FormUrlEncodedContent(maliciousData);

        // Act
        var response = await _client.PostAsync("/api/patients", content);

        // Assert
        Assert.True(response.StatusCode >= HttpStatusCode.BadRequest);

        // Verify malicious content was not stored
        await _factory.ExecuteDbContextAsync(async db =>
        {
            var users = await db.Users
                .Where(u => u.FirstName.Contains("<script>") || u.LastName.Contains("DROP TABLE"))
                .ToListAsync();
            Assert.Empty(users);
        });
    }

    [Fact(Skip = "HTTPS enforcement not configured in test environment")]
    public async Task HTTPS_Enforced_RedirectsToHTTPS()
    {
        // Create HTTP client that doesn't follow redirects
        var httpClient = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Act - try to access via HTTP (simulated)
        var response = await httpClient.GetAsync("http://localhost/login");

        // Assert - should redirect to HTTPS
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith("https://", response.Headers.Location?.ToString());
    }

    [Fact(Skip = "Rate limiting not yet implemented")]
    public async Task RateLimiting_ExcessiveRequests_Blocked()
    {
        // Act - make multiple rapid login attempts
        var responses = new List<HttpResponseMessage>();
        
        for (int i = 0; i < 10; i++)
        {
            var formData = new Dictionary<string, string>
            {
                {"Username", "testuser"},
                {"Password", "wrongpassword"},
                {"ReturnUrl", "/dashboard"}
            };

            var content = new FormUrlEncodedContent(formData);
            var response = await _client.PostAsync("/account/login", content);
            responses.Add(response);
        }

        // Assert - should be rate limited after several attempts
        var lastResponse = responses.Last();
        Assert.True(lastResponse.StatusCode == HttpStatusCode.TooManyRequests || 
                   lastResponse.StatusCode == HttpStatusCode.Redirect);

        // Verify rate limiting audit log
        await _factory.ExecuteDbContextAsync(async db =>
        {
            var auditLogs = await db.AuditLogs
                .Where(log => log.Action == "rate_limit_exceeded")
                .ToListAsync();
            Assert.NotEmpty(auditLogs);
        });
    }
}
