using Microsoft.EntityFrameworkCore;
using Zebrahoof_EMR.Data;
using Zebrahoof_EMR.Models;

namespace Zebrahoof_EMR.Services;

public class StickyNoteService
{
    private const int MaxNotesPerUser = 5;
    private readonly ApplicationDbContext _context;
    private string _currentUserId = "default";
    private List<StickyNote> _cachedNotes = new();
    
    public event Action? OnNotesChanged;

    public StickyNoteService(ApplicationDbContext context)
    {
        _context = context;
    }

    public string CurrentUserId => _currentUserId;

    public IReadOnlyList<StickyNote> Notes => 
        _cachedNotes.Where(n => n.IsVisible).OrderBy(n => n.NoteNumber).ToList().AsReadOnly();

    public int TotalNoteCount => _cachedNotes.Count;

    public bool CanAddNote => TotalNoteCount < MaxNotesPerUser;

    public async Task SetCurrentUserAsync(string userId)
    {
        _currentUserId = userId;
        await LoadNotesAsync();
        OnNotesChanged?.Invoke();
    }

    public async Task LoadNotesAsync()
    {
        _cachedNotes = await _context.StickyNotes
            .Where(n => n.UserId == _currentUserId)
            .OrderBy(n => n.NoteNumber)
            .ToListAsync();
    }

    public async Task<StickyNote?> AddNoteAsync()
    {
        if (!CanAddNote)
            return null;

        var nextNumber = _cachedNotes.Count > 0 ? _cachedNotes.Max(n => n.NoteNumber) + 1 : 1;
        
        var note = new StickyNote
        {
            UserId = _currentUserId,
            NoteNumber = nextNumber,
            IsVisible = true,
            X = 300 + (_cachedNotes.Count * 30),
            Y = 100 + (_cachedNotes.Count * 30),
            Color = NoteColor.Yellow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.StickyNotes.Add(note);
        await _context.SaveChangesAsync();
        
        _cachedNotes.Add(note);
        OnNotesChanged?.Invoke();
        return note;
    }

    public async Task ToggleNoteVisibilityAsync(Guid id)
    {
        var note = _cachedNotes.FirstOrDefault(n => n.Id == id);
        if (note != null)
        {
            note.IsVisible = !note.IsVisible;
            note.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            OnNotesChanged?.Invoke();
        }
    }

    public async Task ShowNoteAsync(Guid id)
    {
        var note = _cachedNotes.FirstOrDefault(n => n.Id == id);
        if (note != null)
        {
            note.IsVisible = true;
            note.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            OnNotesChanged?.Invoke();
        }
    }

    public async Task HideNoteAsync(Guid id)
    {
        var note = _cachedNotes.FirstOrDefault(n => n.Id == id);
        if (note != null)
        {
            note.IsVisible = false;
            note.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            OnNotesChanged?.Invoke();
        }
    }

    public List<StickyNote> GetAllNotesIncludingHidden()
    {
        return _cachedNotes.OrderBy(n => n.NoteNumber).ToList();
    }

    public async Task UpdateNoteContentAsync(Guid id, string content)
    {
        var note = _cachedNotes.FirstOrDefault(n => n.Id == id);
        if (note != null)
        {
            note.Content = content;
            note.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            OnNotesChanged?.Invoke();
        }
    }

    public async Task UpdateNotePositionAsync(Guid id, double x, double y)
    {
        var note = _cachedNotes.FirstOrDefault(n => n.Id == id);
        if (note != null)
        {
            note.X = x;
            note.Y = y;
            note.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            OnNotesChanged?.Invoke();
        }
    }

    public async Task SetReminderAsync(Guid id, DateTime? reminderDate)
    {
        var note = _cachedNotes.FirstOrDefault(n => n.Id == id);
        if (note != null)
        {
            note.ReminderDate = reminderDate;
            note.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            OnNotesChanged?.Invoke();
        }
    }

    public async Task UpdateNoteColorAsync(Guid id, NoteColor color)
    {
        var note = _cachedNotes.FirstOrDefault(n => n.Id == id);
        if (note != null)
        {
            note.Color = color;
            note.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            OnNotesChanged?.Invoke();
        }
    }

    public async Task DeleteNoteAsync(Guid id)
    {
        var note = _cachedNotes.FirstOrDefault(n => n.Id == id);
        if (note != null)
        {
            _context.StickyNotes.Remove(note);
            await _context.SaveChangesAsync();
            _cachedNotes.Remove(note);
            OnNotesChanged?.Invoke();
        }
    }
}
