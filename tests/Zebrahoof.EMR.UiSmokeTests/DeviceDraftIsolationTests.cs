using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Xunit.Abstractions;

namespace Zebrahoof.EMR.UiSmokeTests;

/// <summary>
/// Same user, two browsers. Logging out of browser A clears A's drafts and
/// leaves A's other tabs, and browser B stays signed in and keeps its draft.
/// </summary>
[Collection(PlaywrightServerCollection.CollectionName)]
public sealed class DeviceDraftIsolationTests : IAsyncLifetime
{
    private readonly ITestOutputHelper _output;
    private readonly string _baseUrl;
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private bool _browsersAvailable = true;

    public DeviceDraftIsolationTests(ITestOutputHelper output)
    {
        _output = output;
        _baseUrl = (Environment.GetEnvironmentVariable("UI_SMOKE_BASEURL") ?? "https://localhost:7177").TrimEnd('/');
    }

    public async Task InitializeAsync()
    {
        try
        {
            _playwright = await Microsoft.Playwright.Playwright.CreateAsync();
            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        }
        catch (PlaywrightException ex) when (ex.Message.Contains("Executable doesn't exist") || ex.Message.Contains("browserType.launch"))
        {
            _output.WriteLine($"Playwright browsers not installed. Skipping. {ex.Message}");
            _browsersAvailable = false;
        }
    }

    public async Task DisposeAsync()
    {
        if (_browser != null)
        {
            await _browser.DisposeAsync();
        }

        _playwright?.Dispose();
    }

    [SkippableFact]
    public async Task SecondDevice_KeepsDraft_WhenTheOtherBrowserLogsOut()
    {
        Skip.If(!_browsersAvailable, "Playwright browsers not installed.");
        Assert.NotNull(_browser);
        await using var deviceB = await _browser.NewContextAsync(new BrowserNewContextOptions { IgnoreHTTPSErrors = true });
        await using var deviceA = await _browser.NewContextAsync(new BrowserNewContextOptions { IgnoreHTTPSErrors = true });
        var pageB = await deviceB.NewPageAsync();
        var pageA = await deviceA.NewPageAsync();
        var otherTab = await deviceA.NewPageAsync();

        await LoginAsync(pageB);
        await OpenChartAsync(pageB, 1);
        var fieldB = await OpenNoteAsync(pageB);
        await fieldB.FillAsync("DEVICEB draft marker");
        await pageB.WaitForTimeoutAsync(1500);

        await LoginAsync(pageA);
        await OpenChartAsync(pageA, 1);
        var fieldA = await OpenNoteAsync(pageA);
        await fieldA.FillAsync("DEVICEA draft marker");
        await pageA.WaitForTimeoutAsync(1500);
        await otherTab.GotoAsync(_baseUrl + "/", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

        var started = DateTime.UtcNow;
        await otherTab.EvaluateAsync("() => sessionStorage.setItem('zebrahoof.signingOut', '1')");
        await otherTab.GotoAsync(_baseUrl + "/logout", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await pageA.WaitForURLAsync(new Regex("/login"), new PageWaitForURLOptions { Timeout = 8000 });
        var elapsedMs = (DateTime.UtcNow - started).TotalMilliseconds;
        _output.WriteLine($"other tab left the chart in {elapsedMs:0} ms");
        Assert.True(elapsedMs < 5000, $"other tab took {elapsedMs:0} ms");

        await pageB.WaitForTimeoutAsync(1000);
        Assert.DoesNotContain("/login", pageB.Url, StringComparison.OrdinalIgnoreCase);
        await fieldB.FillAsync("DEVICEB draft marker still");
        await pageB.WaitForTimeoutAsync(1500);
        await pageB.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Cancel" }).ClickAsync();
        await pageB.GetByRole(AriaRole.Textbox, new PageGetByRoleOptions { Name = "Chief Complaint" })
            .WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Hidden, Timeout = 10000 });

        var restoredB = await OpenNoteAsync(pageB);
        await pageB.WaitForTimeoutAsync(400);
        Assert.Equal("DEVICEB draft marker still", await restoredB.InputValueAsync());
        Assert.True(await pageB.GetByTestId("draft-restored").CountAsync() > 0);

        await LoginAsync(pageA);
        await OpenChartAsync(pageA, 1);
        var after = await OpenNoteAsync(pageA);
        await pageA.WaitForTimeoutAsync(400);
        var value = await after.InputValueAsync();
        Assert.DoesNotContain("DEVICEA", value, StringComparison.Ordinal);
        Assert.Equal(0, await pageA.GetByTestId("draft-restored").CountAsync());
    }

    private async Task LoginAsync(IPage page)
    {
        var form = page.APIRequest.CreateFormData();
        form.Set("Username", "Physician");
        form.Set("Password", "password");
        form.Set("ReturnUrl", "/");
        var response = await page.APIRequest.PostAsync(_baseUrl + "/account/login", new APIRequestContextOptions
        {
            Form = form,
            MaxRedirects = 5
        });
        _output.WriteLine($"login {response.Status} {response.Url}");
        await page.GotoAsync(_baseUrl + "/", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        Assert.DoesNotContain("/login", page.Url, StringComparison.OrdinalIgnoreCase);
    }

    private async Task OpenChartAsync(IPage page, int patientId)
    {
        await page.GotoAsync($"{_baseUrl}/patients/{patientId}/notes", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.GetByTestId("banner-new-note").WaitForAsync(new LocatorWaitForOptions { Timeout = 20000 });
    }

    private async Task<ILocator> OpenNoteAsync(IPage page)
    {
        await page.GetByTestId("banner-new-note").ClickAsync();
        var field = page.GetByRole(AriaRole.Textbox, new PageGetByRoleOptions { Name = "Chief Complaint" });
        await field.WaitForAsync(new LocatorWaitForOptions { Timeout = 15000 });
        return field;
    }
}
