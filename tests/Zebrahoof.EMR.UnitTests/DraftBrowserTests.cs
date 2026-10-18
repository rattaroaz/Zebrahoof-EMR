using Microsoft.AspNetCore.Http;
using Zebrahoof_EMR.Helpers;

namespace Zebrahoof.EMR.UnitTests;

public class DraftBrowserTests
{
    [Fact]
    public void Ensure_KeepsTheSameBrowserAcrossRequests_UntilLogoutDeletesIt()
    {
        var first = new DefaultHttpContext();
        var id = DraftBrowser.Ensure(first);
        Assert.Equal(id, DraftBrowser.Read(first));

        var later = new DefaultHttpContext();
        later.Request.Headers.Cookie = $"{DraftBrowser.CookieName}={id}";
        Assert.Equal(id, DraftBrowser.Ensure(later));

        DraftBrowser.Delete(later);
        Assert.Null(DraftBrowser.Read(later));
        Assert.Contains("draft-browser", later.Response.Headers.SetCookie.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Ensure_GivesEachBrowserItsOwnId()
    {
        var deviceA = DraftBrowser.Ensure(new DefaultHttpContext());
        var deviceB = DraftBrowser.Ensure(new DefaultHttpContext());

        Assert.NotEqual(deviceA, deviceB);
    }
}
