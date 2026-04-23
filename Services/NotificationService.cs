using System.Text;

namespace Zebrahoof_EMR.Services;

public class NotificationService
{
    private readonly List<Notification> _notifications = new();
    
    public event Action<Notification>? OnNotification;
    public event Action? OnNotificationDismissed;

    public void ShowSuccess(string message, string? title = null)
    {
        Show(new Notification
        {
            Type = NotificationType.Success,
            Title = title ?? "Success",
            Message = message
        });
    }

    public void ShowError(string message, string? title = null)
    {
        Show(new Notification
        {
            Type = NotificationType.Error,
            Title = title ?? "Error",
            Message = message
        });
    }

    public void ShowWarning(string message, string? title = null)
    {
        Show(new Notification
        {
            Type = NotificationType.Warning,
            Title = title ?? "Warning",
            Message = message
        });
    }

    public void ShowInfo(string message, string? title = null)
    {
        Show(new Notification
        {
            Type = NotificationType.Info,
            Title = title ?? "Information",
            Message = message
        });
    }

    private void Show(Notification notification)
    {
        _notifications.Add(notification);
        OnNotification?.Invoke(notification);
    }

    public void Dismiss(Guid id)
    {
        _notifications.RemoveAll(n => n.Id == id);
        OnNotificationDismissed?.Invoke();
    }

    public IReadOnlyList<Notification> GetActive() => _notifications.AsReadOnly();

    public string BuildTemplate(string templateName, IReadOnlyDictionary<string, string> tokens)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"[{templateName}]");
        builder.AppendLine("================================");
        foreach (var token in tokens)
        {
            builder.AppendLine($"{token.Key}: {token.Value}");
        }
        builder.AppendLine("================================");
        builder.AppendLine("Thank you,");
        builder.AppendLine("Zebrahoof EMR Team");
        return builder.ToString();
    }

    public void ShowTemplatePreview(string templateName, IReadOnlyDictionary<string, string> tokens)
    {
        var content = BuildTemplate(templateName, tokens);
        ShowInfo(content, $"{templateName} Preview");
    }
}

public class Notification
{
    public Guid Id { get; } = Guid.NewGuid();
    public NotificationType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; } = DateTime.Now;
}

public enum NotificationType
{
    Success,
    Error,
    Warning,
    Info
}
