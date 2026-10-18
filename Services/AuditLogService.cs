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
        IQueryable<AuditLog> query = _dbContext.AuditLogs;

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

        // SQLite refuses ORDER BY DateTimeOffset, so the filtered rows are
        // sorted here. Server-side paging needs a sortable column this provider
        // does not translate.
        var rows = await query.ToListAsync(cancellationToken);
        return rows
            .OrderByDescending(log => log.Timestamp)
            .Take(500)
            .ToList();
    }

    public async Task<List<string>> GetRecentActionsAsync(CancellationToken cancellationToken = default)
    {
        var rows = await _dbContext.AuditLogs
            .Select(log => new { log.Action, log.Timestamp })
            .ToListAsync(cancellationToken);

        return rows
            .OrderByDescending(log => log.Timestamp)
            .Select(log => log.Action)
            .Distinct()
            .Take(50)
            .ToList();
    }
}
