using Zebrahoof_EMR.Services;

namespace Zebrahoof.EMR.UnitTests;

public class AuthCookieIdleTests
{
    private static readonly TimeSpan Idle = TimeSpan.FromMinutes(15);

    [Fact]
    public void MissingStamp_Renews()
    {
        var items = new Dictionary<string, string?>();
        var now = DateTimeOffset.Parse("2026-10-17T12:00:00Z");

        var decision = AuthCookieIdle.Evaluate(items, now, Idle);

        Assert.Equal(AuthCookieIdleDecision.Renew, decision);
        Assert.Equal(now.Ticks.ToString(), items[AuthCookieIdle.LastActivityKey]);
    }

    [Fact]
    public void ActivityInsideTheThrottle_DoesNotRewriteTheStamp()
    {
        var login = DateTimeOffset.Parse("2026-10-17T12:00:00Z");
        var items = Stamp(login);

        var decision = AuthCookieIdle.Evaluate(items, login.AddSeconds(30), Idle);

        Assert.Equal(AuthCookieIdleDecision.Keep, decision);
        Assert.Equal(login.Ticks.ToString(), items[AuthCookieIdle.LastActivityKey]);
    }

    [Fact]
    public void ActivityAfterTheThrottle_SlidesTheWindow()
    {
        var login = DateTimeOffset.Parse("2026-10-17T12:00:00Z");
        var items = Stamp(login);
        var activeAt = login.AddMinutes(14);

        var decision = AuthCookieIdle.Evaluate(items, activeAt, Idle);

        Assert.Equal(AuthCookieIdleDecision.Renew, decision);
        Assert.Equal(activeAt.Ticks.ToString(), items[AuthCookieIdle.LastActivityKey]);

        var stillInside = new Dictionary<string, string?>(items);
        Assert.NotEqual(AuthCookieIdleDecision.Expired, AuthCookieIdle.Evaluate(stillInside, activeAt.AddMinutes(15), Idle));

        var pastWindow = new Dictionary<string, string?>(items);
        Assert.Equal(AuthCookieIdleDecision.Expired, AuthCookieIdle.Evaluate(pastWindow, activeAt.AddMinutes(15).AddSeconds(1), Idle));
    }

    [Fact]
    public void IdlePastTheWindow_ExpiresAndLeavesTheStamp()
    {
        var login = DateTimeOffset.Parse("2026-10-17T12:00:00Z");
        var items = Stamp(login);

        var decision = AuthCookieIdle.Evaluate(items, login.AddMinutes(16), Idle);

        Assert.Equal(AuthCookieIdleDecision.Expired, decision);
        Assert.Equal(login.Ticks.ToString(), items[AuthCookieIdle.LastActivityKey]);
    }

    [Fact]
    public void CircuitTraffic_DoesNotCountAsActivity()
    {
        Assert.False(AuthCookieIdle.CountsAsActivity("/_blazor"));
        Assert.False(AuthCookieIdle.CountsAsActivity("/_blazor/negotiate"));
        Assert.False(AuthCookieIdle.CountsAsActivity("/_framework/blazor.web.js"));
        Assert.False(AuthCookieIdle.CountsAsActivity("/_content/MudBlazor/MudBlazor.min.js"));
        Assert.False(AuthCookieIdle.CountsAsActivity("/hubs/session"));
        Assert.True(AuthCookieIdle.CountsAsActivity("/account/activity"));
        Assert.True(AuthCookieIdle.CountsAsActivity("/dashboard"));
        Assert.True(AuthCookieIdle.CountsAsActivity("/patients"));
    }

    [Fact]
    public void AbsoluteCap_ExpiresAfterTwelveHours()
    {
        var issued = DateTimeOffset.Parse("2026-10-17T12:00:00Z");
        var items = new Dictionary<string, string?>
        {
            [AuthCookieIdle.AbsoluteIssuedKey] = issued.Ticks.ToString(),
            [AuthCookieIdle.LastActivityKey] = issued.AddHours(11).Ticks.ToString()
        };

        Assert.False(AuthCookieIdle.IsAbsoluteExpired(items, issued.AddHours(12)));
        Assert.True(AuthCookieIdle.IsAbsoluteExpired(items, issued.AddHours(12).AddSeconds(1)));

        var slid = new Dictionary<string, string?>(items);
        var decision = AuthCookieIdle.Evaluate(slid, issued.AddHours(11).AddMinutes(14), TimeSpan.FromMinutes(15));
        Assert.NotEqual(AuthCookieIdleDecision.Expired, decision);
        Assert.True(AuthCookieIdle.IsAbsoluteExpired(slid, issued.AddHours(12).AddSeconds(1)));
    }

    [Fact]
    public void ActiveSession_StaysSignedInAcrossTwentyMinutes()
    {
        var now = DateTimeOffset.Parse("2026-10-17T12:00:00Z");
        var items = Stamp(now);

        for (var minute = 2; minute <= 20; minute += 2)
        {
            now = now.AddMinutes(2);
            var decision = AuthCookieIdle.Evaluate(items, now, Idle);
            Assert.NotEqual(AuthCookieIdleDecision.Expired, decision);
        }

        Assert.Equal(AuthCookieIdleDecision.Keep, AuthCookieIdle.Evaluate(items, now.AddSeconds(30), Idle));
    }

    private static Dictionary<string, string?> Stamp(DateTimeOffset at) =>
        new() { [AuthCookieIdle.LastActivityKey] = at.Ticks.ToString() };
}
