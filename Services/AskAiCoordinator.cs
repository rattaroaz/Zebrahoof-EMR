namespace Zebrahoof_EMR.Services;

/// <summary>Opens Ask AI from the app bar so the launcher is not a floating control over chart buttons.</summary>
public sealed class AskAiCoordinator
{
    public event Action? OpenRequested;

    public void RequestOpen() => OpenRequested?.Invoke();
}
