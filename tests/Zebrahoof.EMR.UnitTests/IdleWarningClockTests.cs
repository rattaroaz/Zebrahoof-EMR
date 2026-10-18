using Zebrahoof_EMR.Services;

namespace Zebrahoof.EMR.UnitTests;

public class IdleWarningClockTests
{
    [Fact]
    public void Warning_StartsAtTwoMinutes()
    {
        var now = new DateTimeOffset(2026, 10, 18, 12, 0, 0, TimeSpan.Zero);
        var signOut = IdleWarningClock.SignOutAt(now, idleSecondsRemaining: 180);
        var openedAt = signOut - IdleWarningClock.WarningLead;

        Assert.Equal("2:00", IdleWarningClock.Format(IdleWarningClock.WarningLead));
        Assert.Equal("2:00", IdleWarningClock.Format(signOut - openedAt));
        Assert.Equal("2:00", IdleWarningClock.Format(TimeSpan.FromSeconds(119.2)));
        Assert.True(IdleWarningClock.ShouldWarn(IdleWarningClock.WarningLead));
        Assert.False(IdleWarningClock.ShouldWarn(TimeSpan.FromSeconds(121)));
        Assert.False(IdleWarningClock.ShouldSignOut(TimeSpan.FromMilliseconds(200)));
        Assert.True(IdleWarningClock.ShouldSignOut(TimeSpan.Zero));
        Assert.Equal("0:00", IdleWarningClock.Format(TimeSpan.Zero));
    }
}
