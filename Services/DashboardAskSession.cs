namespace Zebrahoof_EMR.Services;

public sealed class DashboardAskSession
{
    public bool IsOpen { get; set; }
    public List<ClinicAskInteraction> Turns { get; } = [];

    public void Clear() => Turns.Clear();
}

public sealed class ClinicAskInteraction
{
    public string UserInput { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public List<ClinicAskPatientRef> Patients { get; set; } = [];
    public List<string> Applied { get; set; } = [];
    public bool WarnMix { get; set; }
    public bool IsProcessing { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
