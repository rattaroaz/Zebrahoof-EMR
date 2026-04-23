using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Zebrahoof_EMR.Data;
using Zebrahoof_EMR.Models;
using Zebrahoof_EMR.Services;

namespace Zebrahoof_EMR.IntegrationTests;

public static class TestDataSeeder
{
    public static async Task SeedTestUserAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Check if test user already exists
        var existingUser = await userManager.FindByNameAsync("testuser");
        if (existingUser != null)
        {
            // User already exists, just ensure they have the right role
            if (!await userManager.IsInRoleAsync(existingUser, "Physician"))
            {
                await userManager.AddToRoleAsync(existingUser, "Physician");
            }
            return;
        }

        // Create test user
        var user = new ApplicationUser
        {
            UserName = "testuser",
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            DisplayName = "Test User",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(user, "TestPassword123!");
        if (!result.Succeeded)
        {
            // If user creation fails due to existing user, try to find and update
            existingUser = await userManager.FindByNameAsync("testuser");
            if (existingUser != null)
            {
                // Ensure user has the right role
                if (!await userManager.IsInRoleAsync(existingUser, "Physician"))
                {
                    await userManager.AddToRoleAsync(existingUser, "Physician");
                }
                return;
            }
            throw new InvalidOperationException($"Failed to create test user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        // Add user role
        await userManager.AddToRoleAsync(user, "Physician");

        // Create audit log entry
        dbContext.AuditLogs.Add(new AuditLog
        {
            Action = "user_created",
            Scope = "test_setup",
            UserId = user.Id,
            Timestamp = DateTimeOffset.UtcNow,
            Metadata = "{\"purpose\":\"integration_test\"}"
        });

        await dbContext.SaveChangesAsync();
    }

    public static async Task SeedInactiveUserAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        // Check if user already exists
        var existingUser = await userManager.FindByNameAsync("inactiveuser");
        if (existingUser != null)
        {
            return;
        }

        var user = new ApplicationUser
        {
            UserName = "inactiveuser",
            Email = "inactive@example.com",
            FirstName = "Inactive",
            LastName = "User",
            DisplayName = "Inactive User",
            IsActive = false,
            CreatedAt = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(user, "TestPassword123!");
        // Silently ignore duplicate errors - another test may have created this user
        if (!result.Succeeded && !result.Errors.Any(e => e.Code == "DuplicateUserName" || e.Description.Contains("already taken")))
        {
            // Only throw for non-duplicate errors
            throw new InvalidOperationException($"Failed to create inactive test user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }
    }

    public static async Task SeedLockedUserAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        // Check if user already exists
        var existingUser = await userManager.FindByNameAsync("lockeduser");
        if (existingUser != null)
        {
            // Ensure user is locked
            await userManager.SetLockoutEnabledAsync(existingUser, true);
            await userManager.SetLockoutEndDateAsync(existingUser, DateTimeOffset.UtcNow.AddDays(1));
            return;
        }

        var user = new ApplicationUser
        {
            UserName = "lockeduser",
            Email = "locked@example.com",
            FirstName = "Locked",
            LastName = "User",
            DisplayName = "Locked User",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(user, "TestPassword123!");
        // Silently ignore duplicate errors
        if (!result.Succeeded && !result.Errors.Any(e => e.Code == "DuplicateUserName" || e.Description.Contains("already taken")))
        {
            throw new InvalidOperationException($"Failed to create locked test user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        // Lock the user (only if we created them)
        if (result.Succeeded)
        {
            await userManager.SetLockoutEnabledAsync(user, true);
            await userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddDays(1));
        }
    }

    public static async Task SeedAdminUserAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Check if user already exists
        var existingUser = await userManager.FindByNameAsync("admin");
        if (existingUser != null)
        {
            // Ensure user has Admin role
            if (!await userManager.IsInRoleAsync(existingUser, "Admin"))
            {
                await userManager.AddToRoleAsync(existingUser, "Admin");
            }
            return;
        }

        var user = new ApplicationUser
        {
            UserName = "admin",
            Email = "admin@example.com",
            FirstName = "Admin",
            LastName = "User",
            DisplayName = "Admin User",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(user, "AdminPassword123!");
        // Silently ignore duplicate errors
        if (!result.Succeeded && !result.Errors.Any(e => e.Code == "DuplicateUserName" || e.Description.Contains("already taken")))
        {
            throw new InvalidOperationException($"Failed to create admin test user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(user, "Admin");

            dbContext.AuditLogs.Add(new AuditLog
            {
                Action = "admin_user_created",
                Scope = "test_setup",
                UserId = user.Id,
                Timestamp = DateTimeOffset.UtcNow,
                Metadata = "{\"purpose\":\"integration_test\"}"
            });

            await dbContext.SaveChangesAsync();
        }
    }

    public static async Task ClearAllDataAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Clear data using raw SQL to avoid locking issues
        await dbContext.Database.ExecuteSqlRawAsync("DELETE FROM \"AuditLogs\"; DELETE FROM \"UserSessions\"; DELETE FROM \"AspNetUsers\";");
    }
}
