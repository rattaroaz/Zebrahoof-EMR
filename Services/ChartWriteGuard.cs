namespace Zebrahoof_EMR.Services;

/// <summary>The circuit tried to change a chart it does not currently hold.</summary>
public sealed class ChartNotHeldException : InvalidOperationException
{
    public const string RefusalMessage = "This chart is no longer open on this screen. The change was not saved.";

    public ChartNotHeldException()
        : base(RefusalMessage)
    {
    }
}

/// <summary>
/// The Blazor circuit that is executing the current UI call.
/// Absent for tests, startup, and background work, which are not a user's open chart.
/// </summary>
public static class CircuitCall
{
    private static readonly AsyncLocal<Call?> CurrentCall = new();

    public static Call? Current => CurrentCall.Value;

    public static IDisposable Push(string? userId, string circuitId)
    {
        var prior = CurrentCall.Value;
        CurrentCall.Value = new Call(userId, circuitId);
        return new Popper(prior);
    }

    private sealed class Popper(Call? prior) : IDisposable
    {
        public void Dispose() => CurrentCall.Value = prior;
    }

    public readonly record struct Call(string? UserId, string CircuitId);
}

/// <summary>
/// Refuses a patient write when this circuit is signed in and does not hold that patient's lock.
/// </summary>
public sealed class ChartWriteGuard
{
    private readonly ActivePatientRegistry _registry;

    public ChartWriteGuard(ActivePatientRegistry registry) => _registry = registry;

    public void Ensure(int patientId)
    {
        var call = CircuitCall.Current;
        if (call == null || string.IsNullOrEmpty(call.Value.UserId) || patientId <= 0)
        {
            return;
        }

        _registry.ExpireDisconnected();
        if (_registry.Holds(call.Value.UserId, call.Value.CircuitId, patientId))
        {
            return;
        }

        throw new ChartNotHeldException();
    }
}
