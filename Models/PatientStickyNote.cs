using System.ComponentModel.DataAnnotations;

namespace Zebrahoof_EMR.Models;

public class PatientStickyNote
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [MaxLength(256)]
    public string UserId { get; set; } = string.Empty;
    
    public int PatientId { get; set; }
    
    [MaxLength(4000)]
    public string Content { get; set; } = string.Empty;
    
    public double X { get; set; } = 300;
    public double Y { get; set; } = 100;
    public bool IsVisible { get; set; } = true;
    public NoteColor Color { get; set; } = NoteColor.Pink;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
