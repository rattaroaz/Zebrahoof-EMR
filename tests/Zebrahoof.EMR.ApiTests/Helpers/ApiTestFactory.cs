using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Zebrahoof_EMR.Data;
using Zebrahoof_EMR.Models;

namespace Zebrahoof.EMR.ApiTests.Helpers;

public class ApiTestFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"zebrahoof-aptests-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseSqlite($"Data Source={_dbPath}");
            });

            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();
            SeedTestDataAsync(
                scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(),
                db).GetAwaiter().GetResult();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (File.Exists(_dbPath))
        {
            try
            {
                File.Delete(_dbPath);
            }
            catch (IOException)
            {
                // Ignore, file may be locked
            }
        }
    }

    private static async Task SeedTestDataAsync(UserManager<ApplicationUser> userManager, ApplicationDbContext dbContext)
    {
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

        dbContext.Patients.AddRange(
            new Patient { MRN = "API001", FirstName = "API", LastName = "TestPatient1", DateOfBirth = DateTime.Today.AddYears(-30), Sex = "M" },
            new Patient { MRN = "API002", FirstName = "API", LastName = "TestPatient2", DateOfBirth = DateTime.Today.AddYears(-45), Sex = "F" },
            new Patient { MRN = "API003", FirstName = "API", LastName = "TestPatient3", DateOfBirth = DateTime.Today.AddYears(-25), Sex = "M" });

        await dbContext.SaveChangesAsync();
    }

    public HttpClient CreateAuthenticatedClient(string username = "api_test_user")
    {
        return CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    public async Task<string> GetAuthTokenAsync(string username = "api_test_user")
    {
        using var client = CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Username"] = username,
            ["Password"] = "TestPassword123!"
        });

        var response = await client.PostAsync("/account/login", content);
        if (!response.IsSuccessStatusCode)
        {
            return string.Empty;
        }

        var cookies = response.Headers.GetValues("Set-Cookie");
        return cookies.FirstOrDefault(c => c.Contains("zebrahoof.session", StringComparison.Ordinal)) ?? string.Empty;
    }
}
