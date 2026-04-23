namespace Zebrahoof_EMR.Models;

public class Message
{
    public int Id { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public int? PatientId { get; set; }
    public string? PatientName { get; set; }
    public MessageType Type { get; set; }
    public DateTime SentAt { get; set; }
    public bool IsRead { get; set; }
    public bool IsFlagged { get; set; }
    public int? ParentMessageId { get; set; }
}

public enum MessageType
{
    General,
    Results,
    Refill,
    Administrative,
    PatientPortal
}
