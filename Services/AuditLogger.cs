using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Zebrahoof_EMR.Data;
using Zebrahoof_EMR.Models;

namespace Zebrahoof_EMR.Services;

public class AuditLogger : IAuditLogger
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<AuditLogger> _logger;

    public AuditLogger(IServiceScopeFactory scopeFactory, TimeProvider timeProvider, ILogger<AuditLogger> logger)
    {
        _scopeFactory = scopeFactory;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task LogAsync(string action, string scopeContext, string? metadata = null, string? userId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var serviceScope = _scopeFactory.CreateAsyncScope();
            var dbContext = serviceScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var log = new AuditLog
            {
                Action = action,
                Scope = scopeContext,
                Metadata = metadata,
                UserId = userId,
                Timestamp = _timeProvider.GetUtcNow()
            };

            dbContext.AuditLogs.Add(log);
            await dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogDebug("Audit {Action} scope {Scope} user {UserId}", action, scopeContext, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write audit log {Action} scope {Scope} user {UserId}", action, scopeContext, userId);
            throw;
        }
    }
}
