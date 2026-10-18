using Microsoft.EntityFrameworkCore;
using Zebrahoof_EMR.Data;
using Zebrahoof_EMR.Models;
using Zebrahoof_EMR.Services;

namespace Zebrahoof_EMR.UnitTests;

public class AuditLogServiceTests
{
    [Fact]
    public async Task GetLogs_SortsNewestFirst_OnSqlite()
    {
        await using var context = CreateContext();
        await AddUserAsync(context, "audit-physician");
        await AddUserAsync(context, "audit-nurse");
        var older = new DateTimeOffset(2026, 1, 1, 8, 0, 0, TimeSpan.Zero);
        var newer = new DateTimeOffset(2026, 10, 18, 8, 0, 0, TimeSpan.Zero);
        context.AuditLogs.AddRange(
            new AuditLog { Action = "login", Scope = "account", UserId = "audit-physician", Timestamp = older },
            new AuditLog { Action = "chart.open", Scope = "patient", UserId = "audit-physician", Timestamp = newer },
            new AuditLog { Action = "login", Scope = "account", UserId = "audit-nurse", Timestamp = older.AddHours(1) });
        await context.SaveChangesAsync();

        var service = new AuditLogService(context);
        var logs = await service.GetLogsAsync(null, null, null, null);
        var actions = await service.GetRecentActionsAsync();

        Assert.Equal(new[] { "chart.open", "login", "login" }, logs.Select(log => log.Action).ToArray());
        Assert.Equal(newer, logs[0].Timestamp);
        Assert.Equal(new[] { "chart.open", "login" }, actions);
    }

    [Fact]
    public async Task GetLogs_AppliesFiltersBeforeTheInMemorySort()
    {
        await using var context = CreateContext();
        await AddUserAsync(context, "audit-physician");
        await AddUserAsync(context, "audit-nurse");
        context.AuditLogs.AddRange(
            new AuditLog { Action = "login", Scope = "account", UserId = "audit-physician", Timestamp = new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero) },
            new AuditLog { Action = "logout", Scope = "account", UserId = "audit-nurse", Timestamp = new DateTimeOffset(2026, 1, 3, 0, 0, 0, TimeSpan.Zero) });
        await context.SaveChangesAsync();

        var service = new AuditLogService(context);
        var logs = await service.GetLogsAsync(null, null, "audit-physician", "login");

        Assert.Single(logs);
        Assert.Equal("audit-physician", logs[0].UserId);
    }

    [Fact]
    public async Task GetLogs_CorruptMetadata_KeepsTheRowAndTheRestOfThePage()
    {
        await using var context = CreateContext();
        await AddUserAsync(context, "audit-physician");
        var good = new AuditLog
        {
            Action = "login",
            Scope = "account",
            UserId = "audit-physician",
            Timestamp = new DateTimeOffset(2026, 10, 18, 8, 0, 0, TimeSpan.Zero),
            Metadata = "{\"ok\":true}"
        };
        var bad = new AuditLog
        {
            Action = "chart.open",
            Scope = "patient",
            UserId = "audit-physician",
            Timestamp = new DateTimeOffset(2026, 10, 18, 9, 0, 0, TimeSpan.Zero),
            Metadata = "will be replaced"
        };
        context.AuditLogs.AddRange(good, bad);
        await context.SaveChangesAsync();
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE AuditLogs SET Metadata = {"%%%not-ciphertext%%%"} WHERE Id = {bad.Id}");
        context.ChangeTracker.Clear();

        var logs = await new AuditLogService(context).GetLogsAsync(null, null, null, null);

        Assert.Equal(2, logs.Count);
        Assert.Equal(FieldEncryptionHelper.Unreadable, logs.Single(log => log.Action == "chart.open").Metadata);
        Assert.Contains("ok", logs.Single(log => log.Action == "login").Metadata);
    }

    private static async Task AddUserAsync(ApplicationDbContext context, string userId)
    {
        context.Users.Add(new ApplicationUser
        {
            Id = userId,
            UserName = userId,
            NormalizedUserName = userId.ToUpperInvariant(),
            Email = $"{userId}@test.com",
            NormalizedEmail = $"{userId}@TEST.COM",
            EmailConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString(),
            IsActive = true
        });
        await context.SaveChangesAsync();
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite($"Data Source={Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())}.db")
            .Options;
        var context = new ApplicationDbContext(options);
        context.Database.EnsureCreated();
        context.AuditLogs.RemoveRange(context.AuditLogs);
        context.SaveChanges();
        return context;
    }
}
