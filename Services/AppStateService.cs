using Zebrahoof_EMR.Models;

namespace Zebrahoof_EMR.Services;

public class AppStateService
{
    public Patient? SelectedPatient { get; private set; }
    public int? SelectedEncounterId { get; private set; }
    public string? CurrentBreadcrumb { get; private set; }

    public event Action? OnStateChanged;

    public void SelectPatient(Patient? patient)
    {
        SelectedPatient = patient;
        if (patient == null) SelectedEncounterId = null;
        OnStateChanged?.Invoke();
    }

    public void SelectEncounter(int? encounterId)
    {
        SelectedEncounterId = encounterId;
        OnStateChanged?.Invoke();
    }

    public void SetBreadcrumb(string? breadcrumb)
    {
        CurrentBreadcrumb = breadcrumb;
        OnStateChanged?.Invoke();
    }

    public void ClearContext()
    {
        SelectedPatient = null;
        SelectedEncounterId = null;
        CurrentBreadcrumb = null;
        OnStateChanged?.Invoke();
    }
}
