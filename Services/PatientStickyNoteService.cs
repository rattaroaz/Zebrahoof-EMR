using Microsoft.EntityFrameworkCore;
using Zebrahoof_EMR.Data;
using Zebrahoof_EMR.Models;

namespace Zebrahoof_EMR.Services;

public class PatientStickyNoteService
{
    private readonly ApplicationDbContext _context;
    
    public event Action? OnNoteChanged;

    public PatientStickyNoteService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PatientStickyNote?> GetNoteAsync(string userId, int patientId)
    {
        return await _context.PatientStickyNotes
            .FirstOrDefaultAsync(n => n.UserId == userId && n.PatientId == patientId);
    }

    public async Task<PatientStickyNote> CreateOrUpdateNoteAsync(string userId, int patientId, string content)
    {
        var note = await GetNoteAsync(userId, patientId);
        
        if (note == null)
        {
            note = new PatientStickyNote
            {
                UserId = userId,
                PatientId = patientId,
                Content = content,
                IsVisible = true,
                Color = NoteColor.Pink,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.PatientStickyNotes.Add(note);
        }
        else
        {
            note.Content = content;
            note.UpdatedAt = DateTime.UtcNow;
        }
        
        await _context.SaveChangesAsync();
        OnNoteChanged?.Invoke();
        return note;
    }

    public async Task UpdatePositionAsync(string userId, int patientId, double x, double y)
    {
        var note = await GetNoteAsync(userId, patientId);
        if (note != null)
        {
            note.X = x;
            note.Y = y;
            note.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task UpdateColorAsync(string userId, int patientId, NoteColor color)
    {
        var note = await GetNoteAsync(userId, patientId);
        if (note != null)
        {
            note.Color = color;
            note.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            OnNoteChanged?.Invoke();
        }
    }

    public async Task ToggleVisibilityAsync(string userId, int patientId)
    {
        var note = await GetNoteAsync(userId, patientId);
        if (note != null)
        {
            note.IsVisible = !note.IsVisible;
            note.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            OnNoteChanged?.Invoke();
        }
    }

    public async Task<PatientStickyNote?> ShowNoteAsync(string userId, int patientId)
    {
        var note = await GetNoteAsync(userId, patientId);
        if (note != null)
        {
            note.IsVisible = true;
            note.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            OnNoteChanged?.Invoke();
        }
        return note;
    }

    public async Task HideNoteAsync(string userId, int patientId)
    {
        var note = await GetNoteAsync(userId, patientId);
        if (note != null)
        {
            note.IsVisible = false;
            note.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            OnNoteChanged?.Invoke();
        }
    }

    public async Task DeleteNoteAsync(string userId, int patientId)
    {
        var note = await GetNoteAsync(userId, patientId);
        if (note != null)
        {
            _context.PatientStickyNotes.Remove(note);
            await _context.SaveChangesAsync();
            OnNoteChanged?.Invoke();
        }
    }
}
