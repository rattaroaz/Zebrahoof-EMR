using Microsoft.AspNetCore.Http;
using Zebrahoof_EMR.Logging;

namespace Zebrahoof.EMR.UnitTests;

public class StatusCodePagePathsTests
{
    [Theory]
    [InlineData("/not-found")]
    [InlineData("/Not-Found")]
    [InlineData("/api/patients/999")]
    [InlineData("/_blazor/negotiate")]
    [InlineData("/_framework/blazor.web.js")]
    [InlineData("/_content/MudBlazor/MudBlazor.min.js")]
    [InlineData("/hubs/session")]
    [InlineData("/health")]
    public void LeavesPipeline404sAlone(string path)
    {
        Assert.True(StatusCodePagePaths.ShouldLeaveNotFoundAsIs(path));
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/missing-page")]
    [InlineData("/favicon.ico")]
    public void RewritesUnknownDocument404s(string path)
    {
        Assert.False(StatusCodePagePaths.ShouldLeaveNotFoundAsIs(path));
    }

    [Fact]
    public void EmptyPathIsRewritable()
    {
        Assert.False(StatusCodePagePaths.ShouldLeaveNotFoundAsIs(PathString.Empty));
    }
}
