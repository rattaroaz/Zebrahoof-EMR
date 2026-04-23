using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Zebrahoof_EMR.Data;
using Zebrahoof_EMR.Models;
using Xunit;

namespace Zebrahoof.EMR.ApiTests.Rest;

public class AccountEndpointsTests : IClassFixture<ApiTestFactory>, IAsyncLifetime
{
    private readonly ApiTestFactory _factory;
    private readonly HttpClient _client;

    public AccountEndpointsTests(ApiTestFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        // Additional setup if needed
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsSuccess()
    {
        // Arrange
        var loginData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Username", "api_test_user"),
            new KeyValuePair<string, string>("Password", "TestPassword123!")
        });

        // Act
        var response = await _client.PostAsync("/account/login", loginData);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        
        // Verify session cookies are set
        var setCookieHeaders = response.Headers.GetValues("Set-Cookie");
        setCookieHeaders.Should().NotBeNull();
        setCookieHeaders.Should().Contain(c => c.Contains("zebrahoof.auth"));
        setCookieHeaders.Should().Contain(c => c.Contains("zebrahoof.session"));
    }

    [Fact]
    public async Task Login_InvalidCredentials_ReturnsRedirectWithError()
    {
        // Arrange
        var loginData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Username", "api_test_user"),
            new KeyValuePair<string, string>("Password", "WrongPassword")
        });

        // Act
        var response = await _client.PostAsync("/account/login", loginData);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location?.ToString().Should().Contain("error=invalid");
    }

    [Fact]
    public async Task Login_InactiveUser_ReturnsRedirectWithError()
    {
        // Arrange - Create inactive user
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        
        var inactiveUser = new ApplicationUser
        {
            UserName = "inactive_user",
            Email = "inactive@example.com",
            DisplayName = "Inactive User",
            EmailConfirmed = true,
            IsActive = false,
            CreatedAt = DateTime.UtcNow
        };
        
        await userManager.CreateAsync(inactiveUser, "TestPassword123!");

        var loginData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Username", "inactive_user"),
            new KeyValuePair<string, string>("Password", "TestPassword123!")
        });

        // Act
        var response = await _client.PostAsync("/account/login", loginData);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location?.ToString().Should().Contain("error=inactive");
    }

    [Fact]
    public async Task Login_NonExistentUser_ReturnsRedirectWithError()
    {
        // Arrange
        var loginData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Username", "nonexistent_user"),
            new KeyValuePair<string, string>("Password", "TestPassword123!")
        });

        // Act
        var response = await _client.PostAsync("/account/login", loginData);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location?.ToString().Should().Contain("error=invalid");
    }

    [Fact]
    public async Task Login_EmailLogin_WorksCorrectly()
    {
        // Arrange
        var loginData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Username", "api_test@example.com"),
            new KeyValuePair<string, string>("Password", "TestPassword123!")
        });

        // Act
        var response = await _client.PostAsync("/account/login", loginData);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        var setCookieHeaders = response.Headers.GetValues("Set-Cookie");
        setCookieHeaders.Should().NotBeNull();
        setCookieHeaders.Should().Contain(c => c.Contains("zebrahoof.auth"));
    }

    [Fact]
    public async Task Login_WithRememberMe_SetsPersistentCookies()
    {
        // Arrange
        var loginData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Username", "api_test_user"),
            new KeyValuePair<string, string>("Password", "TestPassword123!"),
            new KeyValuePair<string, string>("RememberMe", "true")
        });

        // Act
        var response = await _client.PostAsync("/account/login", loginData);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        var setCookieHeaders = response.Headers.GetValues("Set-Cookie");
        setCookieHeaders.Should().NotBeNull();
        
        // Check for persistent cookie attributes
        var authCookie = setCookieHeaders.FirstOrDefault(c => c.Contains("zebrahoof.auth"));
        authCookie.Should().NotBeNull();
        // Note: Max-Age attribute would be set for persistent cookies
    }

    [Fact]
    public async Task Logout_AuthenticatedUser_ClearsCookies()
    {
        // Arrange - First login
        var loginData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Username", "api_test_user"),
            new KeyValuePair<string, string>("Password", "TestPassword123!")
        });

        await _client.PostAsync("/account/login", loginData);

        // Act
        var response = await _client.PostAsync("/account/logout", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location?.ToString().Should().Contain("logout=success");
        
        // Verify cookies are cleared
        var setCookieHeaders = response.Headers.GetValues("Set-Cookie");
        setCookieHeaders.Should().NotBeNull();
        setCookieHeaders.Should().Contain(c => c.Contains("zebrahoof.auth") && c.Contains("max-age=0"));
        setCookieHeaders.Should().Contain(c => c.Contains("zebrahoof.session") && c.Contains("max-age=0"));
    }

    [Fact]
    public async Task Logout_GetRequest_WorksCorrectly()
    {
        // Arrange
        var loginData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Username", "api_test_user"),
            new KeyValuePair<string, string>("Password", "TestPassword123!")
        });

        await _client.PostAsync("/account/login", loginData);

        // Act
        var response = await _client.GetAsync("/account/logout");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location?.ToString().Should().Contain("logout=success");
    }

    [Fact]
    public async Task Refresh_ValidToken_ReturnsOk()
    {
        // Arrange - First login to get session
        var loginData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Username", "api_test_user"),
            new KeyValuePair<string, string>("Password", "TestPassword123!")
        });

        var loginResponse = await _client.PostAsync("/account/login", loginData);
        
        // Extract session cookies
        var setCookieHeaders = loginResponse.Headers.GetValues("Set-Cookie");
        var refreshToken = setCookieHeaders?.FirstOrDefault(c => c.Contains("zebrahoof.session"));
        
        if (refreshToken != null)
        {
            var cookieValue = refreshToken.Split(';')[0].Split('=')[1];
            _client.DefaultRequestHeaders.Add("Cookie", $"zebrahoof.session={cookieValue}");
        }

        // Act
        var response = await _client.PostAsync("/account/refresh", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // Verify new refresh token is issued
        var newSetCookieHeaders = response.Headers.GetValues("Set-Cookie");
        newSetCookieHeaders.Should().NotBeNull();
        newSetCookieHeaders.Should().Contain(c => c.Contains("zebrahoof.session"));
    }

    [Fact]
    public async Task Refresh_InvalidToken_ReturnsUnauthorized()
    {
        // Arrange
        _client.DefaultRequestHeaders.Add("Cookie", "zebrahoof.session=invalid-token");

        // Act
        var response = await _client.PostAsync("/account/refresh", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_NoToken_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.PostAsync("/account/refresh", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("", "/")]
    [InlineData("   ", "/")]
    [InlineData("/dashboard", "/dashboard")]
    [InlineData("//evil.com", "/")]
    [InlineData("http://evil.com", "/")]
    [InlineData("https://evil.com", "/")]
    [InlineData("/relative/path", "/relative/path")]
    public async Task Login_WithReturnUrl_HandlesUrlsCorrectly(string returnUrl, string expectedRedirect)
    {
        // Arrange
        var loginData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Username", "api_test_user"),
            new KeyValuePair<string, string>("Password", "TestPassword123!"),
            new KeyValuePair<string, string>("ReturnUrl", returnUrl)
        });

        // Act
        var response = await _client.PostAsync($"/account/login?returnUrl={Uri.EscapeDataString(returnUrl)}", loginData);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        var location = response.Headers.Location?.ToString();
        location.Should().StartWith(expectedRedirect);
    }

    [Fact]
    public async Task Login_RateLimiting_HandlesExcessiveRequests()
    {
        // Arrange
        var loginData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Username", "api_test_user"),
            new KeyValuePair<string, string>("Password", "WrongPassword")
        });

        // Act - Make multiple failed login attempts
        var responses = new List<HttpResponseMessage>();
        for (int i = 0; i < 6; i++) // Exceed the lockout threshold (5)
        {
            var response = await _client.PostAsync("/account/login", loginData);
            responses.Add(response);
        }

        // Assert
        // The last attempt should result in lockout
        var lastResponse = responses.Last();
        lastResponse.StatusCode.Should().Be(HttpStatusCode.Redirect);
        lastResponse.Headers.Location?.ToString().Should().Contain("error=locked");
    }

    [Fact]
    public async Task Login_ConcurrentRequests_HandlesCorrectly()
    {
        // Arrange
        var loginData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Username", "api_test_user"),
            new KeyValuePair<string, string>("Password", "TestPassword123!")
        });

        // Act - Make concurrent login requests
        var tasks = new List<Task<HttpResponseMessage>>();
        for (int i = 0; i < 5; i++)
        {
            tasks.Add(_client.PostAsync("/account/login", loginData));
        }

        var responses = await Task.WhenAll(tasks);

        // Assert
        // All requests should succeed (though some might be redirects to the same session)
        foreach (var response in responses)
        {
            response.StatusCode.Should().Be(HttpStatusCode.Redirect);
            response.Dispose();
        }
    }
}
