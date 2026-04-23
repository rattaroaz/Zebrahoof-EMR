using Zebrahoof_EMR.Models;

namespace Zebrahoof_EMR.Services;

public class MockMessageService
{
    private readonly List<Message> _messages;

    public MockMessageService()
    {
        _messages = GenerateMockMessages();
    }

    public Task<List<Message>> GetAllMessagesAsync() => Task.FromResult(_messages.ToList());

    public Task<List<Message>> GetUnreadMessagesAsync() =>
        Task.FromResult(_messages.Where(m => !m.IsRead).ToList());

    public Task<List<Message>> GetMessagesByTypeAsync(MessageType type) =>
        Task.FromResult(_messages.Where(m => m.Type == type).ToList());

    public Task<List<Message>> GetMessagesForUserAsync(string username) =>
        Task.FromResult(_messages.Where(m => m.To == username).ToList());

    public Task<Message?> GetMessageByIdAsync(int id) =>
        Task.FromResult(_messages.FirstOrDefault(m => m.Id == id));

    public Task MarkAsReadAsync(int id)
    {
        var msg = _messages.FirstOrDefault(m => m.Id == id);
        if (msg != null) msg.IsRead = true;
        return Task.CompletedTask;
    }

    public Task ToggleFlagAsync(int id)
    {
        var msg = _messages.FirstOrDefault(m => m.Id == id);
        if (msg != null) msg.IsFlagged = !msg.IsFlagged;
        return Task.CompletedTask;
    }

    public int GetUnreadCount() => _messages.Count(m => !m.IsRead);

    private static List<Message> GenerateMockMessages()
    {
        var now = DateTime.Now;
        return
        [
            new() { Id = 1, Subject = "Lab Results Available", Body = "CBC results are now available for review. WBC: 7.2, RBC: 4.8, Hgb: 14.2", From = "Lab System", To = "Dr. Sarah Smith", PatientId = 3, PatientName = "Robert Johnson", Type = MessageType.Results, SentAt = now.AddHours(-2), IsRead = false, IsFlagged = false },
            new() { Id = 2, Subject = "Refill Request: Lisinopril", Body = "Patient requests refill for Lisinopril 10mg. Last fill was 28 days ago.", From = "CVS Pharmacy", To = "Dr. Sarah Smith", PatientId = 1, PatientName = "John Doe", Type = MessageType.Refill, SentAt = now.AddHours(-5), IsRead = false, IsFlagged = true },
            new() { Id = 3, Subject = "Question about medication", Body = "Hi Dr. Smith, I wanted to ask about the side effects of my new medication. I've been experiencing some dizziness.", From = "Jane Smith", To = "Dr. Sarah Smith", PatientId = 2, PatientName = "Jane Smith", Type = MessageType.PatientPortal, SentAt = now.AddDays(-1), IsRead = true, IsFlagged = false },
            new() { Id = 4, Subject = "Staff Meeting Reminder", Body = "Reminder: Monthly staff meeting tomorrow at 12pm in Conference Room A.", From = "Admin", To = "Dr. Sarah Smith", Type = MessageType.Administrative, SentAt = now.AddDays(-1), IsRead = true, IsFlagged = false },
            new() { Id = 5, Subject = "Imaging Results", Body = "Chest X-ray results: No acute cardiopulmonary disease. Heart size normal.", From = "Radiology", To = "Dr. Sarah Smith", PatientId = 1, PatientName = "John Doe", Type = MessageType.Results, SentAt = now.AddHours(-8), IsRead = false, IsFlagged = false },
            new() { Id = 6, Subject = "Prior Authorization Approved", Body = "Prior auth for MRI lumbar spine has been approved by insurance.", From = "Auth Department", To = "Dr. Sarah Smith", PatientId = 4, PatientName = "Maria Garcia", Type = MessageType.Administrative, SentAt = now.AddHours(-12), IsRead = false, IsFlagged = false }
        ];
    }
}
