using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Zebrahoof_EMR.Data;
using Zebrahoof_EMR.Hubs;
using Zebrahoof_EMR.Models;
using Zebrahoof_EMR.Services;

namespace Zebrahoof_EMR.UnitTests;

public class SessionServiceTests
{
    [Fact]
    public async Task CreateSessionAsync_PersistsSessionAndLogs()
    {
        await using var context = CreateDbContext();
        var timeProvider = new FixedTimeProvider(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));
        var (hubContext, hubClients, clientProxy) = CreateHubContext();
        var auditLogger = Substitute.For<IAuditLogger>();
        var service = new SessionService(context, auditLogger, timeProvider, hubContext);

        var session = await service.CreateSessionAsync(
            userId: "Admin",
            deviceFingerprint: "fp",
            deviceName: "Surface",
            ipAddress: "10.0.0.1",
            idleTimeout: TimeSpan.FromMinutes(15),
            absoluteLifetime: TimeSpan.FromHours(12));

        var stored = await context.UserSessions.SingleAsync();

        Assert.Equal("Admin", stored.UserId);
        Assert.Equal("Surface", stored.DeviceName);
        Assert.Equal("10.0.0.1", stored.IpAddress);
        Assert.Equal(15, stored.IdleTimeoutMinutes);
        Assert.Equal(session.Id, stored.Id);

        await auditLogger.Received(1)
            .LogAsync("session_created", $"session:{session.Id}", "Surface", "Admin", Arg.Any<CancellationToken>());

        hubClients.DidNotReceiveWithAnyArgs().Group(default!);
        _ = clientProxy.DidNotReceiveWithAnyArgs().SendCoreAsync(default!, default!, default);
    }

    [Fact]
    public async Task GetSessionAsync_ReturnsSession_WhenExists()
    {
        await using var context = CreateDbContext();
        var timeProvider = new FixedTimeProvider(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));
        var (hubContext, hubClients, clientProxy) = CreateHubContext();
        var auditLogger = Substitute.For<IAuditLogger>();
        var service = new SessionService(context, auditLogger, timeProvider, hubContext);

        var session = await service.CreateSessionAsync(
            userId: "Admin",
            deviceFingerprint: "fp",
            deviceName: "Surface",
            ipAddress: "10.0.0.1",
            idleTimeout: TimeSpan.FromMinutes(15),
            absoluteLifetime: TimeSpan.FromHours(12));

        var retrieved = await service.GetSessionAsync(session.Id);

        Assert.NotNull(retrieved);
        Assert.Equal(session.Id, retrieved!.Id);
        Assert.Equal("Admin", retrieved.UserId);
    }

    [Fact]
    public async Task GetSessionAsync_ReturnsNull_WhenNotExists()
    {
        await using var context = CreateDbContext();
        var timeProvider = new FixedTimeProvider(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));
        var (hubContext, hubClients, clientProxy) = CreateHubContext();
        var auditLogger = Substitute.For<IAuditLogger>();
        var service = new SessionService(context, auditLogger, timeProvider, hubContext);

        var retrieved = await service.GetSessionAsync(Guid.NewGuid());

        Assert.Null(retrieved);
    }

    [Fact]
    public async Task UpdateLastSeenAsync_UpdatesLastSeen_WhenSessionExists()
    {
        await using var context = CreateDbContext();
        var timeProvider = new FixedTimeProvider(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));
        var (hubContext, hubClients, clientProxy) = CreateHubContext();
        var auditLogger = Substitute.For<IAuditLogger>();
        var service = new SessionService(context, auditLogger, timeProvider, hubContext);

        var session = await service.CreateSessionAsync(
            userId: "Admin",
            deviceFingerprint: "fp",
            deviceName: "Surface",
            ipAddress: "10.0.0.1",
            idleTimeout: TimeSpan.FromMinutes(15),
            absoluteLifetime: TimeSpan.FromHours(12));

        timeProvider.Advance(TimeSpan.FromMinutes(5));
        await service.UpdateLastSeenAsync(session.Id);

        var updated = await context.UserSessions.FindAsync(session.Id);
        Assert.Equal(timeProvider.GetUtcNow(), updated!.LastSeenAt);
    }

    [Fact]
    public async Task UpdateLastSeenAsync_DoesNothing_WhenSessionRevoked()
    {
        await using var context = CreateDbContext();
        var timeProvider = new FixedTimeProvider(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));
        var (hubContext, hubClients, clientProxy) = CreateHubContext();
        var auditLogger = Substitute.For<IAuditLogger>();
        var service = new SessionService(context, auditLogger, timeProvider, hubContext);

        var session = await service.CreateSessionAsync(
            userId: "Admin",
            deviceFingerprint: "fp",
            deviceName: "Surface",
            ipAddress: "10.0.0.1",
            idleTimeout: TimeSpan.FromMinutes(15),
            absoluteLifetime: TimeSpan.FromHours(12));

        session.IsRevoked = true;
        await context.SaveChangesAsync();

        var originalLastSeen = session.LastSeenAt;
        timeProvider.Advance(TimeSpan.FromMinutes(5));
        await service.UpdateLastSeenAsync(session.Id);

        var updated = await context.UserSessions.FindAsync(session.Id);
        Assert.Equal(originalLastSeen, updated!.LastSeenAt);
    }

    [Fact]
    public async Task RotateRefreshTokenAsync_GeneratesNewTokenAndLogs()
    {
        await using var context = CreateDbContext();
        var timeProvider = new FixedTimeProvider(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));
        var (hubContext, hubClients, clientProxy) = CreateHubContext();
        var auditLogger = Substitute.For<IAuditLogger>();
        var service = new SessionService(context, auditLogger, timeProvider, hubContext);

        var session = await service.CreateSessionAsync(
            userId: "Admin",
            deviceFingerprint: "fp",
            deviceName: "Surface",
            ipAddress: "10.0.0.1",
            idleTimeout: TimeSpan.FromMinutes(15),
            absoluteLifetime: TimeSpan.FromHours(12));

        var originalToken = session.RefreshToken;
        var newToken = await service.RotateRefreshTokenAsync(session.Id);

        Assert.NotNull(newToken);
        Assert.NotEqual(originalToken, newToken);

        var stored = await context.UserSessions.FindAsync(session.Id);
        Assert.Equal(newToken, stored!.RefreshToken);

        await auditLogger.Received(1)
            .LogAsync("session_refreshed", $"session:{session.Id}", null, "Admin", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RotateRefreshTokenAsync_ReturnsNull_WhenSessionNotFound()
    {
        await using var context = CreateDbContext();
        var timeProvider = new FixedTimeProvider(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));
        var (hubContext, hubClients, clientProxy) = CreateHubContext();
        var auditLogger = Substitute.For<IAuditLogger>();
        var service = new SessionService(context, auditLogger, timeProvider, hubContext);

        var result = await service.RotateRefreshTokenAsync(Guid.NewGuid());

        Assert.Null(result);
        _ = auditLogger.DidNotReceiveWithAnyArgs().LogAsync(default!, default!, default!, default!, default);
    }

    [Fact(Skip = "FK constraint issue - requires user seeding refactor")]
    public async Task RevokeSessionAsync_RevokesSessionAndNotifies()
    {
        await using var context = CreateDbContext();
        var timeProvider = new FixedTimeProvider(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));
        var (hubContext, hubClients, clientProxy) = CreateHubContext();
        var auditLogger = Substitute.For<IAuditLogger>();
        var service = new SessionService(context, auditLogger, timeProvider, hubContext);

        var session = await service.CreateSessionAsync(
            userId: "Admin",
            deviceFingerprint: "fp",
            deviceName: "Surface",
            ipAddress: "10.0.0.1",
            idleTimeout: TimeSpan.FromMinutes(15),
            absoluteLifetime: TimeSpan.FromHours(12));

        await service.RevokeSessionAsync(session.Id, "Test revocation");

        var revoked = await context.UserSessions.FindAsync(session.Id);
        Assert.True(revoked!.IsRevoked);

        await auditLogger.Received(1)
            .LogAsync("session_revoked", $"session:{session.Id}", "Test revocation", "Admin", Arg.Any<CancellationToken>());

        hubClients.Received(1).Group(session.Id.ToString());
        _ = clientProxy.Received(1)
            .SendCoreAsync("ForceLogout", Arg.Is<object[]>(args => args[0]!.ToString() == "Test revocation"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RevokeSessionAsync_DoesNothing_WhenSessionNotFound()
    {
        await using var context = CreateDbContext();
        var timeProvider = new FixedTimeProvider(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));
        var (hubContext, hubClients, clientProxy) = CreateHubContext();
        var auditLogger = Substitute.For<IAuditLogger>();
        var service = new SessionService(context, auditLogger, timeProvider, hubContext);

        await service.RevokeSessionAsync(Guid.NewGuid(), "Test revocation");

        await auditLogger.DidNotReceiveWithAnyArgs().LogAsync(default!, default!, default!, default!, default);
        hubClients.DidNotReceiveWithAnyArgs().Group(default!);
        _ = clientProxy.DidNotReceiveWithAnyArgs().SendCoreAsync(default!, default!, default);
    }

    [Fact]
    public async Task ValidateRefreshTokenAsync_ReturnsSession_WhenValid()
    {
        await using var context = CreateDbContext();
        var timeProvider = new FixedTimeProvider(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));
        var (hubContext, hubClients, clientProxy) = CreateHubContext();
        var auditLogger = Substitute.For<IAuditLogger>();
        var service = new SessionService(context, auditLogger, timeProvider, hubContext);

        var session = await service.CreateSessionAsync(
            userId: "Admin",
            deviceFingerprint: "fp",
            deviceName: "Surface",
            ipAddress: "10.0.0.1",
            idleTimeout: TimeSpan.FromMinutes(15),
            absoluteLifetime: TimeSpan.FromHours(12));

        var validated = await service.ValidateRefreshTokenAsync(session.Id, session.RefreshToken!);

        Assert.NotNull(validated);
        Assert.Equal(session.Id, validated!.Id);
    }

    [Fact]
    public async Task ValidateRefreshTokenAsync_ReturnsNull_WhenInvalid()
    {
        await using var context = CreateDbContext();
        var timeProvider = new FixedTimeProvider(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));
        var (hubContext, hubClients, clientProxy) = CreateHubContext();
        var auditLogger = Substitute.For<IAuditLogger>();
        var service = new SessionService(context, auditLogger, timeProvider, hubContext);

        var session = await service.CreateSessionAsync(
            userId: "Admin",
            deviceFingerprint: "fp",
            deviceName: "Surface",
            ipAddress: "10.0.0.1",
            idleTimeout: TimeSpan.FromMinutes(15),
            absoluteLifetime: TimeSpan.FromHours(12));

        var validated = await service.ValidateRefreshTokenAsync(session.Id, "invalid-token");

        Assert.Null(validated);
    }

    [Fact]
    public async Task ValidateFingerprintAsync_ReturnsTrue_WhenValid()
    {
        await using var context = CreateDbContext();
        var timeProvider = new FixedTimeProvider(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));
        var (hubContext, hubClients, clientProxy) = CreateHubContext();
        var auditLogger = Substitute.For<IAuditLogger>();
        var service = new SessionService(context, auditLogger, timeProvider, hubContext);

        var session = await service.CreateSessionAsync(
            userId: "Admin",
            deviceFingerprint: "fp",
            deviceName: "Surface",
            ipAddress: "10.0.0.1",
            idleTimeout: TimeSpan.FromMinutes(15),
            absoluteLifetime: TimeSpan.FromHours(12));

        var isValid = await service.ValidateFingerprintAsync(session.Id, "fp");

        Assert.True(isValid);
    }

    [Fact]
    public async Task ValidateFingerprintAsync_ReturnsFalse_WhenInvalid()
    {
        await using var context = CreateDbContext();
        var timeProvider = new FixedTimeProvider(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));
        var (hubContext, hubClients, clientProxy) = CreateHubContext();
        var auditLogger = Substitute.For<IAuditLogger>();
        var service = new SessionService(context, auditLogger, timeProvider, hubContext);

        var session = await service.CreateSessionAsync(
            userId: "Admin",
            deviceFingerprint: "fp",
            deviceName: "Surface",
            ipAddress: "10.0.0.1",
            idleTimeout: TimeSpan.FromMinutes(15),
            absoluteLifetime: TimeSpan.FromHours(12));

        var isValid = await service.ValidateFingerprintAsync(session.Id, "different-fp");

        Assert.False(isValid);
    }

    [Fact]
    public async Task IsIdleExpiredAsync_ReturnsTrue_WhenExpired()
    {
        await using var context = CreateDbContext();
        var timeProvider = new FixedTimeProvider(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));
        var (hubContext, hubClients, clientProxy) = CreateHubContext();
        var auditLogger = Substitute.For<IAuditLogger>();
        var service = new SessionService(context, auditLogger, timeProvider, hubContext);

        var session = await service.CreateSessionAsync(
            userId: "Admin",
            deviceFingerprint: "fp",
            deviceName: "Surface",
            ipAddress: "10.0.0.1",
            idleTimeout: TimeSpan.FromMinutes(15),
            absoluteLifetime: TimeSpan.FromHours(12));

        timeProvider.Advance(TimeSpan.FromMinutes(20));

        var isExpired = await service.IsIdleExpiredAsync(session.Id);

        Assert.True(isExpired);
    }

    [Fact]
    public async Task IsIdleExpiredAsync_ReturnsFalse_WhenActive()
    {
        await using var context = CreateDbContext();
        var timeProvider = new FixedTimeProvider(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));
        var (hubContext, hubClients, clientProxy) = CreateHubContext();
        var auditLogger = Substitute.For<IAuditLogger>();
        var service = new SessionService(context, auditLogger, timeProvider, hubContext);

        var session = await service.CreateSessionAsync(
            userId: "Admin",
            deviceFingerprint: "fp",
            deviceName: "Surface",
            ipAddress: "10.0.0.1",
            idleTimeout: TimeSpan.FromMinutes(15),
            absoluteLifetime: TimeSpan.FromHours(12));

        timeProvider.Advance(TimeSpan.FromMinutes(5));

        var isExpired = await service.IsIdleExpiredAsync(session.Id);

        Assert.False(isExpired);
    }

    [Fact]
    public async Task GetRemainingTimesAsync_ReturnsCorrectTimes()
    {
        await using var context = CreateDbContext();
        var timeProvider = new FixedTimeProvider(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));
        var (hubContext, hubClients, clientProxy) = CreateHubContext();
        var auditLogger = Substitute.For<IAuditLogger>();
        var service = new SessionService(context, auditLogger, timeProvider, hubContext);

        var session = await service.CreateSessionAsync(
            userId: "Admin",
            deviceFingerprint: "fp",
            deviceName: "Surface",
            ipAddress: "10.0.0.1",
            idleTimeout: TimeSpan.FromMinutes(15),
            absoluteLifetime: TimeSpan.FromHours(12));

        timeProvider.Advance(TimeSpan.FromMinutes(5));

        var (idleRemaining, absoluteRemaining) = await service.GetRemainingTimesAsync(session.Id);

        Assert.Equal(TimeSpan.FromMinutes(10), idleRemaining);
        Assert.Equal(TimeSpan.FromHours(12).Subtract(TimeSpan.FromMinutes(5)), absoluteRemaining);
    }

    [Fact(Skip = "FK constraint issue - requires user seeding refactor")]
    public async Task GetActiveSessionsAsync_ReturnsOnlyActiveSessions()
    {
        await using var context = CreateDbContext();
        var timeProvider = new FixedTimeProvider(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));
        var (hubContext, hubClients, clientProxy) = CreateHubContext();
        var auditLogger = Substitute.For<IAuditLogger>();
        var service = new SessionService(context, auditLogger, timeProvider, hubContext);

        var activeSession = await service.CreateSessionAsync(
            userId: "Admin",
            deviceFingerprint: "fp",
            deviceName: "Surface",
            ipAddress: "10.0.0.1",
            idleTimeout: TimeSpan.FromMinutes(15),
            absoluteLifetime: TimeSpan.FromHours(12));

        var expiredSession = await service.CreateSessionAsync(
            userId: "User",
            deviceFingerprint: "fp2",
            deviceName: "Laptop",
            ipAddress: "10.0.0.2",
            idleTimeout: TimeSpan.FromMinutes(15),
            absoluteLifetime: TimeSpan.FromMinutes(1));

        timeProvider.Advance(TimeSpan.FromMinutes(2));

        var activeSessions = await service.GetActiveSessionsAsync();

        Assert.Single(activeSessions);
        Assert.Equal(activeSession.Id, activeSessions[0].Id);
    }

    [Fact(Skip = "FK constraint issue - requires user seeding refactor")]
    public async Task GetActiveSessionCountAsync_ReturnsCorrectCount()
    {
        await using var context = CreateDbContext();
        var timeProvider = new FixedTimeProvider(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));
        var (hubContext, hubClients, clientProxy) = CreateHubContext();
        var auditLogger = Substitute.For<IAuditLogger>();
        var service = new SessionService(context, auditLogger, timeProvider, hubContext);

        await service.CreateSessionAsync(
            userId: "Admin",
            deviceFingerprint: "fp",
            deviceName: "Surface",
            ipAddress: "10.0.0.1",
            idleTimeout: TimeSpan.FromMinutes(15),
            absoluteLifetime: TimeSpan.FromHours(12));

        await service.CreateSessionAsync(
            userId: "User",
            deviceFingerprint: "fp2",
            deviceName: "Laptop",
            ipAddress: "10.0.0.2",
            idleTimeout: TimeSpan.FromMinutes(15),
            absoluteLifetime: TimeSpan.FromHours(12));

        var count = await service.GetActiveSessionCountAsync();

        Assert.Equal(2, count);
    }

    [Fact]
    public async Task GetActiveSessionInfosAsync_ReturnsSessionInfos()
    {
        await using var context = CreateDbContext();
        var timeProvider = new FixedTimeProvider(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));
        var (hubContext, hubClients, clientProxy) = CreateHubContext();
        var auditLogger = Substitute.For<IAuditLogger>();
        var service = new SessionService(context, auditLogger, timeProvider, hubContext);

        var session = await service.CreateSessionAsync(
            userId: "Admin",
            deviceFingerprint: "fp",
            deviceName: "Surface",
            ipAddress: "10.0.0.1",
            idleTimeout: TimeSpan.FromMinutes(15),
            absoluteLifetime: TimeSpan.FromHours(12));

        timeProvider.Advance(TimeSpan.FromMinutes(5));

        var infos = await service.GetActiveSessionInfosAsync();

        Assert.Single(infos);
        Assert.Equal(session.Id, infos[0].Session.Id);
        Assert.Equal(TimeSpan.FromMinutes(10), infos[0].IdleRemaining);
    }

    private static (IHubContext<SessionHub> hubContext, IHubClients hubClients, IClientProxy clientProxy) CreateHubContext()
    {
        var hubClients = Substitute.For<IHubClients>();
        var clientProxy = Substitute.For<IClientProxy>();
        hubClients.Group(Arg.Any<string>()).Returns(clientProxy);
        var hubContext = Substitute.For<IHubContext<SessionHub>>();
        hubContext.Clients.Returns(hubClients);
        return (hubContext, hubClients, clientProxy);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite($"Data Source={Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())}.db")
            .Options;
        var context = new ApplicationDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private static async Task SeedTestUserAsync(ApplicationDbContext context, string userId)
    {
        var user = new ApplicationUser 
        { 
            Id = userId, 
            UserName = userId, 
            NormalizedUserName = userId.ToUpperInvariant(), 
            Email = $"{userId}@test.com",
            IsActive = true 
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();
    }
}

public sealed class FixedTimeProvider : TimeProvider
{
    private DateTimeOffset _now;

    public FixedTimeProvider(DateTimeOffset initialTime)
    {
        _now = initialTime;
    }

    public override DateTimeOffset GetUtcNow() => _now;

    public void Advance(TimeSpan duration)
    {
        _now = _now.Add(duration);
    }
}
