using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using Zebrahoof_EMR.Data;
using Zebrahoof_EMR.Models;
using Zebrahoof_EMR.Services;

namespace Zebrahoof.EMR.ApiTests.Helpers;

public class ApiTestFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly IHost _host;
    private readonly string _databaseName = $"TestDb_{Guid.NewGuid():N}";
    private readonly Action<IServiceCollection>? _configureServices;
    private readonly Action<WebHostBuilder>? _configureWebHost;

    public ApiTestFactory()
    {
        _configureWebHost = builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                // Replace the database with an in-memory SQLite database
                services.Remove(services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>)));
                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseSqlite($"Data Source={_databaseName}.db"));
                
                // Configure test-specific services
                services.RemoveAll<SessionService>();
                services.AddScoped<SessionService>();
            });
        };
    }

    public ApiTestFactory(Action<IServiceCollection> configureServices) : this()
    {
        _configureServices = configureServices;
    }

    public ApiTestFactory(Action<WebHostBuilder> configureWebHost) : this()
    {
        _configureWebHost = configureWebHost;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _configureWebHost?.Invoke(builder);
        base.ConfigureWebHost(builder);
    }

    protected override void ConfigureHost(IHostBuilder builder)
    {
        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Warning);
        });

        builder.ConfigureServices(services =>
        {
            _configureServices?.Invoke(services);
        });

        base.ConfigureHost(builder);
    }

    public async Task InitializeAsync()
    {
        // Create and seed the database
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        
        await dbContext.Database.EnsureCreatedAsync();
        await SeedTestDataAsync(userManager, dbContext);
    }

    public async Task DisposeAsync()
    {
        // Clean up the database
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        await dbContext.Database.EnsureDeletedAsync();
        
        // Dispose the host
        await _host?.StopAsync();
        _host?.Dispose();
    }

    private async Task SeedTestDataAsync(UserManager<ApplicationUser> userManager, ApplicationDbContext dbContext)
    {
        // Seed test users
        var testUsers = new[]
        {
            new { Username = "api_test_user", Email = "api_test@example.com", Role = "Physician" },
            new { Username = "api_test_admin", Email = "api_admin@example.com", Role = "Admin" },
            new { Username = "api_test_nurse", Email = "api_nurse@example.com", Role = "Nurse" }
        };

        foreach (var testUser in testUsers)
        {
            var user = new ApplicationUser
            {
                UserName = testUser.Username,
                Email = testUser.Email,
                DisplayName = testUser.Username.Replace('_', ' ').ToUpperInvariant(),
                EmailConfirmed = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(user, "TestPassword123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, testUser.Role);
            }
        }

        // Seed test patients
        var patients = new[]
        {
            new Patient { MRN = "API001", FirstName = "API", LastName = "TestPatient1", DateOfBirth = DateTime.Today.AddYears(-30), Sex = "M" },
            new Patient { MRN = "API002", FirstName = "API", LastName = "TestPatient2", DateOfBirth = DateTime.Today.AddYears(-45), Sex = "F" },
            new Patient { MRN = "API003", FirstName = "API", LastName = "TestPatient3", DateOfBirth = DateTime.Today.AddYears(-25), Sex = "M" }
        };

        foreach (var patient in patients)
        {
            dbContext.Patients.Add(patient);
        }

        await dbContext.SaveChangesAsync();
    }

    public HttpClient CreateAuthenticatedClient(string username = "api_test_user")
    {
        var client = CreateClient();
        
        // Authenticate the user
        using var scope = Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = userManager.FindByNameAsync(username).Result;
        
        if (user != null)
        {
            // Create authentication claims
            var claims = new[]
            {
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, user.Id),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, user.UserName!),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Email, user.Email!)
            };

            var identity = new System.Security.Claims.ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new System.Security.Claims.ClaimsPrincipal(identity);

            // Create authenticated client
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Test", username);
        }

        return client;
    }

    public async Task<string> GetAuthTokenAsync(string username = "api_test_user")
    {
        using var client = CreateClient();
        using var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Username", username),
            new KeyValuePair<string, string>("Password", "TestPassword123!")
        });

        var response = await client.PostAsync("/account/login", content);
        
        if (response.IsSuccessStatusCode)
        {
            var cookies = response.Headers.GetValues("Set-Cookie");
            var sessionCookie = cookies?.FirstOrDefault(c => c.Contains("zebrahoof.session"));
            return sessionCookie ?? string.Empty;
        }

        return string.Empty;
    }
}
