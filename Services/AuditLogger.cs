using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Zebrahoof_EMR.Data;
using Zebrahoof_EMR.Models;

namespace Zebrahoof_EMR.Services;

public class AuditLogger : IAuditLogger
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeProvider _timeProvider;

    public AuditLogger(IServiceScopeFactory scopeFactory, TimeProvider timeProvider)
    {
        _scopeFactory = scopeFactory;
        _timeProvider = timeProvider;
    }

    public async Task LogAsync(string action, string scopeContext, string? metadata = null, string? userId = null, CancellationToken cancellationToken = default)
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
    }
}
