namespace Zebrahoof_EMR.Models;

public class Document
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string UploadedBy { get; set; } = string.Empty;
    public string? FileName { get; set; }
    public byte[]? Content { get; set; }
    public string? ExtractedText { get; set; }

    public virtual Patient Patient { get; set; } = null!;
}
