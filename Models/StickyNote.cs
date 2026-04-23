using System.ComponentModel.DataAnnotations;

namespace Zebrahoof_EMR.Models;

public class StickyNote
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [MaxLength(256)]
    public string UserId { get; set; } = string.Empty;
    
    public int NoteNumber { get; set; } = 1;
    
    [MaxLength(4000)]
    public string Content { get; set; } = string.Empty;
    
    public double X { get; set; }
    public double Y { get; set; }
    public bool IsVisible { get; set; } = true;
    public DateTime? ReminderDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public NoteColor Color { get; set; } = NoteColor.Yellow;
}

public enum NoteColor
{
    Yellow,
    Pink,
    Blue,
    Green,
    Orange
}
