using Microsoft.EntityFrameworkCore;
using Zebrahoof_EMR.Data;
using Zebrahoof_EMR.Models;

namespace Zebrahoof_EMR.Services;

public class AuditLogService
{
    private readonly ApplicationDbContext _dbContext;

    public AuditLogService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<AuditLog>> GetLogsAsync(
        DateTimeOffset? start,
        DateTimeOffset? end,
        string? userId,
        string? action,
        CancellationToken cancellationToken = default)
    {
        IQueryable<AuditLog> query = _dbContext.AuditLogs.OrderByDescending(log => log.Timestamp);

        if (start.HasValue)
        {
            query = query.Where(log => log.Timestamp >= start.Value);
        }

        if (end.HasValue)
        {
            query = query.Where(log => log.Timestamp <= end.Value);
        }

        if (!string.IsNullOrWhiteSpace(userId))
        {
            query = query.Where(log => log.UserId != null && log.UserId.Contains(userId));
        }

        if (!string.IsNullOrWhiteSpace(action))
        {
            query = query.Where(log => log.Action == action);
        }

        return await query.Take(500).ToListAsync(cancellationToken);
    }

    public Task<List<string>> GetRecentActionsAsync(CancellationToken cancellationToken = default) =>
        _dbContext.AuditLogs
            .OrderByDescending(log => log.Timestamp)
            .Select(log => log.Action)
            .Distinct()
            .Take(50)
            .ToListAsync(cancellationToken);
}
