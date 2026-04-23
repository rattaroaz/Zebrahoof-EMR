using Microsoft.EntityFrameworkCore;
using Zebrahoof_EMR.Data;
using Zebrahoof_EMR.Models;

namespace Zebrahoof_EMR.Services;

/// <summary>
/// Stores and retrieves the per-patient Grok conversation that powers the
/// Encounter tab. Backed by the local database so the discussion survives
/// page reloads and can be replayed back to Grok as context on follow-ups.
/// </summary>
public class EncounterMessageService
{
    private readonly ApplicationDbContext _db;

    public EncounterMessageService(ApplicationDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Returns every saved turn for a patient in chronological order.
    /// </summary>
    public async Task<List<EncounterMessage>> GetMessagesAsync(int patientId, CancellationToken ct = default)
    {
        return await _db.EncounterMessages
            .AsNoTracking()
            .Where(m => m.PatientId == patientId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<EncounterMessage> AddMessageAsync(
        int patientId,
        string userId,
        string userInput,
        string grokResponse,
        bool includedHistory,
        CancellationToken ct = default)
    {
        var message = new EncounterMessage
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            UserId = string.IsNullOrWhiteSpace(userId) ? "anonymous" : userId,
            UserInput = userInput ?? string.Empty,
            GrokResponse = grokResponse ?? string.Empty,
            IncludedHistory = includedHistory,
            CreatedAt = DateTime.UtcNow
        };

        _db.EncounterMessages.Add(message);
        await _db.SaveChangesAsync(ct);
        return message;
    }

    public async Task<int> ClearMessagesAsync(int patientId, CancellationToken ct = default)
    {
        var existing = await _db.EncounterMessages
            .Where(m => m.PatientId == patientId)
            .ToListAsync(ct);

        if (existing.Count == 0) return 0;

        _db.EncounterMessages.RemoveRange(existing);
        await _db.SaveChangesAsync(ct);
        return existing.Count;
    }
}
