using System.Security.Cryptography;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Zebrahoof_EMR.Data;
using Zebrahoof_EMR.Hubs;
using Zebrahoof_EMR.Models;

namespace Zebrahoof_EMR.Services;

public class SessionService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IAuditLogger _auditLogger;
    private readonly TimeProvider _timeProvider;
    private readonly IHubContext<SessionHub> _hubContext;

    public SessionService(
        ApplicationDbContext dbContext,
        IAuditLogger auditLogger,
        TimeProvider timeProvider,
        IHubContext<SessionHub> hubContext)
    {
        _dbContext = dbContext;
        _auditLogger = auditLogger;
        _timeProvider = timeProvider;
        _hubContext = hubContext;
    }

    public async Task<UserSession> CreateSessionAsync(
        string userId,
        string deviceFingerprint,
        string? deviceName,
        string? ipAddress,
        TimeSpan idleTimeout,
        TimeSpan absoluteLifetime,
        CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();

        await _dbContext.UserSessions
            .Where(s => s.UserId == userId &&
                        s.DeviceFingerprint == deviceFingerprint &&
                        !s.IsRevoked)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(s => s.IsRevoked, true)
                .SetProperty(s => s.LastSeenAt, now),
                cancellationToken);

        var session = new UserSession
        {
            UserId = userId,
            RefreshToken = GenerateRefreshToken(),
            CreatedAt = now,
            LastSeenAt = now,
            ExpiresAt = now.Add(absoluteLifetime),
            DeviceFingerprint = deviceFingerprint,
            DeviceName = deviceName,
            IpAddress = ipAddress,
            IdleTimeoutMinutes = (int)idleTimeout.TotalMinutes
        };

        _dbContext.UserSessions.Add(session);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogger.LogAsync("session_created", $"session:{session.Id}", deviceName, userId, cancellationToken);

        return session;
    }

    public Task<UserSession?> GetSessionAsync(Guid sessionId, CancellationToken cancellationToken = default) =>
        _dbContext.UserSessions.FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);

    public async Task UpdateLastSeenAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var session = await GetSessionAsync(sessionId, cancellationToken);
        if (session == null || session.IsRevoked)
        {
            return;
        }

        session.LastSeenAt = _timeProvider.GetUtcNow();
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<string?> RotateRefreshTokenAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var session = await GetSessionAsync(sessionId, cancellationToken);
        if (session == null || session.IsRevoked)
        {
            return null;
        }

        session.RefreshToken = GenerateRefreshToken();
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogger.LogAsync("session_refreshed", $"session:{session.Id}", null, session.UserId, cancellationToken);

        return session.RefreshToken;
    }

    public async Task<List<UserSession>> GetActiveSessionsAsync(CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        var allSessions = await _dbContext.UserSessions
            .Include(s => s.User)
            .Where(s => !s.IsRevoked)
            .ToListAsync(cancellationToken);
        
        return allSessions
            .Where(s => s.ExpiresAt > now)
            .OrderByDescending(s => s.LastSeenAt)
            .ToList();
    }

    public async Task<int> GetActiveSessionCountAsync(CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        var allSessions = await _dbContext.UserSessions
            .Where(s => !s.IsRevoked)
            .ToListAsync(cancellationToken);
        
        return allSessions.Count(s => s.ExpiresAt > now);
    }

    public async Task<List<ActiveSessionInfo>> GetActiveSessionInfosAsync(CancellationToken cancellationToken = default)
    {
        var sessions = await GetActiveSessionsAsync(cancellationToken);
        var results = new List<ActiveSessionInfo>(sessions.Count);
        foreach (var session in sessions)
        {
            var (idle, absolute) = await GetRemainingTimesAsync(session.Id, cancellationToken);
            results.Add(new ActiveSessionInfo(session, idle, absolute));
        }

        return results;
    }

    public async Task RevokeSessionAsync(Guid sessionId, string reason, CancellationToken cancellationToken = default)
    {
        var session = await GetSessionAsync(sessionId, cancellationToken);
        if (session == null || session.IsRevoked)
        {
            return;
        }

        session.IsRevoked = true;
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogger.LogAsync("session_revoked", $"session:{sessionId}", reason, session.UserId, cancellationToken);
    }

    public async Task<UserSession?> ValidateRefreshTokenAsync(Guid sessionId, string refreshToken, CancellationToken cancellationToken = default)
    {
        var session = await _dbContext.UserSessions.FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);
        if (session == null || session.IsRevoked || session.RefreshToken != refreshToken)
        {
            return null;
        }

        return session;
    }

    public async Task<bool> ValidateFingerprintAsync(Guid sessionId, string fingerprint, CancellationToken cancellationToken = default)
    {
        var session = await _dbContext.UserSessions
            .Where(s => s.Id == sessionId && !s.IsRevoked)
            .Select(s => new { s.DeviceFingerprint })
            .FirstOrDefaultAsync(cancellationToken);

        return session != null && string.Equals(session.DeviceFingerprint, fingerprint, StringComparison.Ordinal);
    }

    public async Task<bool> IsIdleExpiredAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var session = await GetSessionAsync(sessionId, cancellationToken);
        if (session == null || session.IsRevoked)
        {
            return true;
        }

        var now = _timeProvider.GetUtcNow();
        var idleWindow = TimeSpan.FromMinutes(session.IdleTimeoutMinutes);
        return now - session.LastSeenAt > idleWindow || session.ExpiresAt <= now;
    }

    public async Task<(TimeSpan idleRemaining, TimeSpan absoluteRemaining)> GetRemainingTimesAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var session = await GetSessionAsync(sessionId, cancellationToken);
        if (session == null)
        {
            return (TimeSpan.Zero, TimeSpan.Zero);
        }

        var now = _timeProvider.GetUtcNow();
        var idleWindow = TimeSpan.FromMinutes(session.IdleTimeoutMinutes);
        var idleRemaining = idleWindow - (now - session.LastSeenAt);
        var absoluteRemaining = session.ExpiresAt - now;

        return (idleRemaining, absoluteRemaining);
    }

    private static string GenerateRefreshToken()
    {
        Span<byte> buffer = stackalloc byte[32];
        RandomNumberGenerator.Fill(buffer);
        return Convert.ToBase64String(buffer);
    }
}

public record ActiveSessionInfo(UserSession Session, TimeSpan IdleRemaining, TimeSpan AbsoluteRemaining);
