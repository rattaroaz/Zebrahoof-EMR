namespace Zebrahoof_EMR.Services;

/// <summary>
/// The idle warning is a fixed two-minute countdown. A heartbeat that arrives
/// after the window has already opened must not become the first number the
/// user sees; the clock is armed from the earlier remaining time and opens at 2:00.
/// </summary>
public static class IdleWarningClock
{
    public static readonly TimeSpan WarningLead = TimeSpan.FromMinutes(2);

    public static DateTimeOffset SignOutAt(DateTimeOffset now, double idleSecondsRemaining) =>
        now.AddSeconds(Math.Max(0, idleSecondsRemaining));

    public static bool ShouldWarn(TimeSpan untilSignOut) =>
        untilSignOut > TimeSpan.Zero && untilSignOut <= WarningLead;

    public static bool ShouldSignOut(TimeSpan untilSignOut) =>
        untilSignOut <= TimeSpan.Zero;

    public static string Format(TimeSpan untilSignOut)
    {
        if (untilSignOut <= TimeSpan.Zero)
        {
            return "0:00";
        }

        var seconds = (int)Math.Ceiling(untilSignOut.TotalSeconds - 1e-9);
        return $"{seconds / 60}:{seconds % 60:00}";
    }
}
