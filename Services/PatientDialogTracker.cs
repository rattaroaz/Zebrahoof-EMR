using MudBlazor;

namespace Zebrahoof_EMR.Services;

/// <summary>
/// Every dialog opened on this circuit. A chart takeover closes them all,
/// whether they came from the dialog service or an inline chart tab.
/// </summary>
public sealed class PatientDialogTracker : IDisposable
{
    private readonly List<IDialogReference> _open = new();
    private readonly List<Action> _inline = new();
    private readonly IDialogService? _dialogs;

    public PatientDialogTracker()
    {
    }

    public PatientDialogTracker(IDialogService dialogs)
        : this()
    {
        _dialogs = dialogs;
        dialogs.DialogInstanceAddedAsync += OnDialogOpened;
    }

    public void Track(IDialogReference dialog) => _open.Add(dialog);

    public IDisposable TrackInline(Action close)
    {
        _inline.Add(close);
        return new InlineLease(this, close);
    }

    public void CloseAll()
    {
        foreach (var close in _inline.ToArray())
        {
            try
            {
                close();
            }
            catch (ObjectDisposedException)
            {
            }
        }

        foreach (var dialog in _open.ToArray())
        {
            try
            {
                dialog.Close(DialogResult.Cancel());
            }
            catch (ObjectDisposedException)
            {
            }
        }

        _open.Clear();
    }

    public void Dispose()
    {
        if (_dialogs != null)
        {
            _dialogs.DialogInstanceAddedAsync -= OnDialogOpened;
        }
    }

    private Task OnDialogOpened(IDialogReference dialog)
    {
        Track(dialog);
        return Task.CompletedTask;
    }

    private sealed class InlineLease(PatientDialogTracker owner, Action close) : IDisposable
    {
        public void Dispose() => owner._inline.Remove(close);
    }
}
