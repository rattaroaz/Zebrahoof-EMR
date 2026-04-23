using System.Reflection;
using Zebrahoof_EMR.Endpoints;

namespace Zebrahoof_EMR.UnitTests;

public class AccountEndpointsTests
{
    [Theory]
    [InlineData(null, "/")]
    [InlineData("", "/")]
    [InlineData("   ", "/")]
    [InlineData("/dashboard", "/dashboard")]
    [InlineData("//evil.com", "/")]
    [InlineData("http://evil.com", "/")]
    [InlineData("https://evil.com", "/")]
    [InlineData("/relative/path", "/relative/path")]
    [InlineData("/relative/path?query=1", "/relative/path?query=1")]
    [InlineData("//", "/")]
    public void NormalizeReturnUrl_EnforcesRelativePaths(string? input, string expected)
    {
        var normalize = GetNormalizeReturnUrl();

        var result = (string)normalize.Invoke(null, new object?[] { input })!;

        Assert.Equal(expected, result);
    }

    [Fact]
    public void ResolveIdleWindow_Returns30MinutesForAdminRoles()
    {
        var resolver = GetResolveIdleWindow();
        var roles = new List<string> { "Physician", "Admin" };

        var result = (TimeSpan)resolver.Invoke(null, new object?[] { roles })!;

        Assert.Equal(TimeSpan.FromMinutes(30), result);
    }

    [Fact]
    public void ResolveIdleWindow_Returns30MinutesForSystemAdministratorRole()
    {
        var resolver = GetResolveIdleWindow();
        var roles = new List<string> { "System Administrator" };

        var result = (TimeSpan)resolver.Invoke(null, new object?[] { roles })!;

        Assert.Equal(TimeSpan.FromMinutes(30), result);
    }

    [Fact]
    public void ResolveIdleWindow_Returns15MinutesForStandardRoles()
    {
        var resolver = GetResolveIdleWindow();
        var roles = new List<string> { "Physician", "Nurse" };

        var result = (TimeSpan)resolver.Invoke(null, new object?[] { roles })!;

        Assert.Equal(TimeSpan.FromMinutes(15), result);
    }

    [Fact]
    public void ResolveIdleWindow_Returns15MinutesForEmptyRoles()
    {
        var resolver = GetResolveIdleWindow();
        var roles = new List<string>();

        var result = (TimeSpan)resolver.Invoke(null, new object?[] { roles })!;

        Assert.Equal(TimeSpan.FromMinutes(15), result);
    }

    [Fact]
    public void ResolveIdleWindow_IsCaseInsensitive()
    {
        var resolver = GetResolveIdleWindow();
        var roles1 = new List<string> { "admin" };
        var roles2 = new List<string> { "ADMIN" };
        var roles3 = new List<string> { "System administrator" };

        var result1 = (TimeSpan)resolver.Invoke(null, new object?[] { roles1 })!;
        var result2 = (TimeSpan)resolver.Invoke(null, new object?[] { roles2 })!;
        var result3 = (TimeSpan)resolver.Invoke(null, new object?[] { roles3 })!;

        Assert.Equal(TimeSpan.FromMinutes(30), result1);
        Assert.Equal(TimeSpan.FromMinutes(30), result2);
        Assert.Equal(TimeSpan.FromMinutes(30), result3);
    }

    [Fact]
    public void ResolveIdleWindow_ReturnsAdminTimeoutForMixedRoles()
    {
        var resolver = GetResolveIdleWindow();
        var roles = new List<string> { "Physician", "Admin", "Nurse" };

        var result = (TimeSpan)resolver.Invoke(null, new object?[] { roles })!;

        Assert.Equal(TimeSpan.FromMinutes(30), result);
    }

    [Theory]
    [InlineData("Manager")]
    [InlineData("Supervisor")]
    [InlineData("Director")]
    [InlineData("Unknown Role")]
    public void ResolveIdleWindow_ReturnsStandardTimeoutForNonAdminRoles(string role)
    {
        var resolver = GetResolveIdleWindow();
        var roles = new List<string> { role };

        var result = (TimeSpan)resolver.Invoke(null, new object?[] { roles })!;

        Assert.Equal(TimeSpan.FromMinutes(15), result);
    }

    [Fact]
    public void NormalizeReturnUrl_HandlesUnicodeCharacters()
    {
        var normalize = GetNormalizeReturnUrl();
        var input = "/dashboard?name=测试";

        var result = (string)normalize.Invoke(null, new object?[] { input })!;

        Assert.Equal(input, result);
    }

    [Fact]
    public void NormalizeReturnUrl_HandlesEncodedCharacters()
    {
        var normalize = GetNormalizeReturnUrl();
        var input = "/dashboard?name=%20test%20";

        var result = (string)normalize.Invoke(null, new object?[] { input })!;

        Assert.Equal(input, result);
    }

    [Fact]
    public void NormalizeReturnUrl_RejectsProtocolRelativeUrls()
    {
        var normalize = GetNormalizeReturnUrl();
        var inputs = new[] { "//example.com", "//evil.com/path", "//localhost:3000" };

        foreach (var input in inputs)
        {
            var result = (string)normalize.Invoke(null, new object?[] { input })!;
            Assert.Equal("/", result);
        }
    }

    private static MethodInfo GetNormalizeReturnUrl() =>
        typeof(AccountEndpoints).GetMethod("NormalizeReturnUrl", BindingFlags.NonPublic | BindingFlags.Static)
        ?? throw new InvalidOperationException("NormalizeReturnUrl not found");

    private static MethodInfo GetResolveIdleWindow() =>
        typeof(AccountEndpoints).GetMethod("ResolveIdleWindow", BindingFlags.NonPublic | BindingFlags.Static)
        ?? throw new InvalidOperationException("ResolveIdleWindow not found");
}
