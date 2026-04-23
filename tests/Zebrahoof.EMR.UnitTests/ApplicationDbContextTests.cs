using Microsoft.EntityFrameworkCore;
using Zebrahoof_EMR.Data;
using Zebrahoof_EMR.Models;

namespace Zebrahoof_EMR.UnitTests;

public class ApplicationDbContextTests
{
    [Fact]
    public void ApplicationDbContext_CanBeCreated_WithValidOptions()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite($"Data Source={Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())}.db")
            .Options;

        using var context = new ApplicationDbContext(options);

        Assert.NotNull(context);
        Assert.NotNull(context.UserSessions);
        Assert.NotNull(context.Users);
        Assert.NotNull(context.AuditLogs);
    }

    [Fact]
    public async Task UserSessions_CanAddAndRetrieveSession()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite($"Data Source={Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())}.db")
            .Options;

        await using var context = new ApplicationDbContext(options);
        context.Database.EnsureCreated();
        
        // Create user first to satisfy FK constraint
        await CreateTestUserAsync(context, "test-user");
        
        var session = new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = "test-user",
            RefreshToken = "refresh-token",
            CreatedAt = DateTimeOffset.UtcNow,
            LastSeenAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(12),
            DeviceFingerprint = "fingerprint",
            DeviceName = "Test Device",
            IpAddress = "127.0.0.1",
            IdleTimeoutMinutes = 15,
            IsRevoked = false
        };

        context.UserSessions.Add(session);
        await context.SaveChangesAsync();

        var retrieved = await context.UserSessions.FindAsync(session.Id);

        Assert.NotNull(retrieved);
        Assert.Equal(session.UserId, retrieved!.UserId);
        Assert.Equal(session.RefreshToken, retrieved.RefreshToken);
        Assert.Equal(session.DeviceFingerprint, retrieved.DeviceFingerprint);
        Assert.False(retrieved.IsRevoked);
    }

    [Fact]
    public async Task UserSessions_CanUpdateSession()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite($"Data Source={Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())}.db")
            .Options;

        await using var context = new ApplicationDbContext(options);
        context.Database.EnsureCreated();
        
        // Create user first to satisfy FK constraint
        await CreateTestUserAsync(context, "test-user");
        
        var session = new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = "test-user",
            RefreshToken = "refresh-token",
            CreatedAt = DateTimeOffset.UtcNow,
            LastSeenAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(12),
            DeviceFingerprint = "fingerprint",
            DeviceName = "Test Device",
            IpAddress = "127.0.0.1",
            IdleTimeoutMinutes = 15,
            IsRevoked = false
        };

        context.UserSessions.Add(session);
        await context.SaveChangesAsync();

        session.IsRevoked = true;
        session.LastSeenAt = DateTimeOffset.UtcNow.AddMinutes(5);
        await context.SaveChangesAsync();

        var updated = await context.UserSessions.FindAsync(session.Id);

        Assert.NotNull(updated);
        Assert.True(updated!.IsRevoked);
        Assert.Equal(session.LastSeenAt, updated.LastSeenAt);
    }

    [Fact]
    public async Task UserSessions_CanDeleteSession()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite($"Data Source={Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())}.db")
            .Options;

        await using var context = new ApplicationDbContext(options);
        context.Database.EnsureCreated();
        
        // Create user first to satisfy FK constraint
        await CreateTestUserAsync(context, "test-user");
        
        var session = new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = "test-user",
            RefreshToken = "refresh-token",
            CreatedAt = DateTimeOffset.UtcNow,
            LastSeenAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(12),
            DeviceFingerprint = "fingerprint",
            DeviceName = "Test Device",
            IpAddress = "127.0.0.1",
            IdleTimeoutMinutes = 15,
            IsRevoked = false
        };

        context.UserSessions.Add(session);
        await context.SaveChangesAsync();

        context.UserSessions.Remove(session);
        await context.SaveChangesAsync();

        var deleted = await context.UserSessions.FindAsync(session.Id);

        Assert.Null(deleted);
    }

    [Fact]
    public async Task AuditLogs_CanAddAndRetrieveLog()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite($"Data Source={Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())}.db")
            .Options;

        await using var context = new ApplicationDbContext(options);
        context.Database.EnsureCreated();
        
        // Create user first to satisfy FK constraint
        await CreateTestUserAsync(context, "test-user");
        
        var auditLog = new AuditLog
        {
            Id = Guid.NewGuid(),
            Action = "test-event",
            Scope = "resource-123",
            UserId = "test-user",
            Timestamp = DateTimeOffset.UtcNow,
            Metadata = "{\"ip\":\"127.0.0.1\",\"userAgent\":\"Test Agent\"}"
        };

        context.AuditLogs.Add(auditLog);
        await context.SaveChangesAsync();

        var retrieved = await context.AuditLogs.FindAsync(auditLog.Id);

        Assert.NotNull(retrieved);
        Assert.Equal(auditLog.Action, retrieved!.Action);
        Assert.Equal(auditLog.Scope, retrieved.Scope);
        Assert.Equal(auditLog.UserId, retrieved.UserId);
    }

    [Fact]
    public async Task AuditLogs_CanQueryByAction()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite($"Data Source={Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())}.db")
            .Options;

        await using var context = new ApplicationDbContext(options);
        context.Database.EnsureCreated();
        
        // Create users first to satisfy FK constraints
        await CreateTestUserAsync(context, "user-1");
        await CreateTestUserAsync(context, "user-2");
        await CreateTestUserAsync(context, "user-3");
        
        var logs = new[]
        {
            new AuditLog
            {
                Id = Guid.NewGuid(),
                Action = "login",
                Scope = "resource-1",
                UserId = "user-1",
                Timestamp = DateTimeOffset.UtcNow
            },
            new AuditLog
            {
                Id = Guid.NewGuid(),
                Action = "logout",
                Scope = "resource-2",
                UserId = "user-2",
                Timestamp = DateTimeOffset.UtcNow
            },
            new AuditLog
            {
                Id = Guid.NewGuid(),
                Action = "login",
                Scope = "resource-3",
                UserId = "user-3",
                Timestamp = DateTimeOffset.UtcNow
            }
        };

        context.AuditLogs.AddRange(logs);
        await context.SaveChangesAsync();

        var loginLogs = await context.AuditLogs
            .Where(log => log.Action == "login")
            .ToListAsync();

        Assert.Equal(2, loginLogs.Count);
        Assert.All(loginLogs, log => Assert.Equal("login", log.Action));
    }

    [Fact]
    public async Task Users_CanAddAndRetrieveUser()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite($"Data Source={Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())}.db")
            .Options;

        await using var context = new ApplicationDbContext(options);
        context.Database.EnsureCreated();
        
        var user = new ApplicationUser
        {
            Id = "test-user-id",
            UserName = "testuser",
            Email = "test@example.com",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        var retrieved = await context.Users.FindAsync(user.Id);

        Assert.NotNull(retrieved);
        Assert.Equal(user.UserName, retrieved!.UserName);
        Assert.Equal(user.Email, retrieved.Email);
        Assert.True(retrieved.IsActive);
    }

    [Fact]
    public async Task Users_CanUpdateUser()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite($"Data Source={Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())}.db")
            .Options;

        await using var context = new ApplicationDbContext(options);
        context.Database.EnsureCreated();
        
        var user = new ApplicationUser
        {
            Id = "test-user-id",
            UserName = "testuser",
            Email = "test@example.com",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        user.IsActive = false;
        await context.SaveChangesAsync();

        var updated = await context.Users.FindAsync(user.Id);

        Assert.NotNull(updated);
        Assert.False(updated!.IsActive);
    }

    [Fact]
    public async Task SaveChanges_UpdatesTimestamps()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite($"Data Source={Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())}.db")
            .Options;

        await using var context = new ApplicationDbContext(options);
        context.Database.EnsureCreated();
        
        // Create user first to satisfy FK constraint
        await CreateTestUserAsync(context, "test-user");
        
        var session = new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = "test-user",
            RefreshToken = "refresh-token",
            CreatedAt = DateTimeOffset.UtcNow,
            LastSeenAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(12),
            DeviceFingerprint = "fingerprint",
            DeviceName = "Test Device",
            IpAddress = "127.0.0.1",
            IdleTimeoutMinutes = 15,
            IsRevoked = false
        };

        var initialTime = session.CreatedAt;

        context.UserSessions.Add(session);
        await context.SaveChangesAsync();

        var savedTime = session.CreatedAt;

        Assert.Equal(initialTime, savedTime);
    }

    [Fact]
    public async Task QueryOperations_CanFilterActiveSessions()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite($"Data Source={Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())}.db")
            .Options;

        await using var context = new ApplicationDbContext(options);
        context.Database.EnsureCreated();
        
        // Create users first to satisfy FK constraints
        await CreateTestUserAsync(context, "user-1");
        await CreateTestUserAsync(context, "user-2");
        
        var sessions = new[]
        {
            new UserSession
            {
                Id = Guid.NewGuid(),
                UserId = "user-1",
                RefreshToken = "token-1",
                CreatedAt = DateTimeOffset.UtcNow,
                LastSeenAt = DateTimeOffset.UtcNow,
                ExpiresAt = DateTimeOffset.UtcNow.AddHours(12),
                DeviceFingerprint = "fp-1",
                DeviceName = "Device 1",
                IpAddress = "127.0.0.1",
                IdleTimeoutMinutes = 15,
                IsRevoked = false
            },
            new UserSession
            {
                Id = Guid.NewGuid(),
                UserId = "user-2",
                RefreshToken = "token-2",
                CreatedAt = DateTimeOffset.UtcNow,
                LastSeenAt = DateTimeOffset.UtcNow,
                ExpiresAt = DateTimeOffset.UtcNow.AddHours(12),
                DeviceFingerprint = "fp-2",
                DeviceName = "Device 2",
                IpAddress = "127.0.0.1",
                IdleTimeoutMinutes = 15,
                IsRevoked = true
            }
        };

        context.UserSessions.AddRange(sessions);
        await context.SaveChangesAsync();

        var activeSessions = await context.UserSessions
            .Where(s => !s.IsRevoked)
            .ToListAsync();

        Assert.Single(activeSessions);
        Assert.False(activeSessions[0].IsRevoked);
    }

    private static async Task<ApplicationUser> CreateTestUserAsync(ApplicationDbContext context, string userId = "test-user")
    {
        var user = new ApplicationUser
        {
            Id = userId,
            UserName = userId,
            NormalizedUserName = userId.ToUpperInvariant(),
            Email = $"{userId}@test.com",
            NormalizedEmail = $"{userId}@TEST.COM",
            EmailConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString(),
            IsActive = true
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user;
    }
}
