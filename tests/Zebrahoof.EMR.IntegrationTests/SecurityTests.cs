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

    [Fact]
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
    public Task AdminEndpoint_WithNonAdminUser_ReturnsForbidden() => Task.CompletedTask;

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
    public Task Session_Hijacking_Attempt_Detected() => Task.CompletedTask;

    [Fact(Skip = "Input sanitization not fully implemented")]
    public Task Input_WithMaliciousData_Sanitized() => Task.CompletedTask;

    [Fact(Skip = "HTTPS enforcement not configured in test environment")]
    public Task HTTPS_Enforced_RedirectsToHTTPS() => Task.CompletedTask;

    [Fact(Skip = "Rate limiting not yet implemented")]
    public Task RateLimiting_ExcessiveRequests_Blocked() => Task.CompletedTask;
}
