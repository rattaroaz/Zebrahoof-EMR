using MudBlazor;

namespace Zebrahoof_EMR.Services;

/// <summary>Dialogs opened for the chart on this circuit, so a closed chart can dismiss them.</summary>
public sealed class PatientDialogTracker
{
    private readonly List<IDialogReference> _open = new();

    public void Track(IDialogReference dialog) => _open.Add(dialog);

    public void CloseAll()
    {
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
}
