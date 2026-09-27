namespace Zebrahoof_EMR.Services;

/// <summary>Keeps ask/order turns for the current chart session so switching tabs does not wipe them.</summary>
public sealed class PatientChartAskSession
{
    private readonly Dictionary<int, List<ChartAskInteraction>> _byPatient = new();
    private readonly HashSet<int> _open = [];

    public bool IsOpen(int patientId) => _open.Contains(patientId);

    public void SetOpen(int patientId, bool open)
    {
        if (open)
        {
            _open.Add(patientId);
        }
        else
        {
            _open.Remove(patientId);
        }
    }

    public List<ChartAskInteraction> For(int patientId)
    {
        if (!_byPatient.TryGetValue(patientId, out var turns))
        {
            turns = [];
            _byPatient[patientId] = turns;
        }

        return turns;
    }

    public void Clear(int patientId) => _byPatient.Remove(patientId);
}

public sealed class ChartAskInteraction
{
    public string UserInput { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public List<string> Applied { get; set; } = [];
    public bool WarnMix { get; set; }
    public bool IsProcessing { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
