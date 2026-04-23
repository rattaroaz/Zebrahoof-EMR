using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;
using Zebrahoof_EMR.Helpers;

namespace Zebrahoof.EMR.UiSmokeTests;

[Collection(PlaywrightServerCollection.CollectionName)]
public sealed class CrossBrowserTests : IAsyncLifetime
{
    private readonly ITestOutputHelper _output;
    private readonly Uri _baseUri;
    private readonly HttpClient _httpClient;
    private readonly NetworkCredential _playwrightCreds;
    private IPlaywright? _playwright;
    private bool _browsersAvailable = true;
    private readonly string _artifactTraceDir;
    private readonly string _artifactVideoDir;
    private readonly int _defaultRetryAttempts;
    private readonly int _defaultRetryDelayMs;

    public CrossBrowserTests(ITestOutputHelper output)
    {
        _output = output;
        var baseUrl = Environment.GetEnvironmentVariable("UI_SMOKE_BASEURL") ?? "https://localhost:7177";
        _baseUri = new Uri(baseUrl);
        _httpClient = new HttpClient(new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        })
        {
            Timeout = TimeSpan.FromSeconds(15)
        };

        _playwrightCreds = new NetworkCredential(
            Environment.GetEnvironmentVariable("PLAYWRIGHT_USERNAME") ?? "playwright",
            Environment.GetEnvironmentVariable("PLAYWRIGHT_PASSWORD") ?? "P@ssw0rd!");

        var artifactRoot = Environment.GetEnvironmentVariable("PLAYWRIGHT_ARTIFACTS_DIR")
                           ?? Path.Combine(AppContext.BaseDirectory, "playwright-artifacts");
        _artifactTraceDir = Path.Combine(artifactRoot, "trace");
        _artifactVideoDir = Path.Combine(artifactRoot, "video");
        Directory.CreateDirectory(_artifactTraceDir);
        Directory.CreateDirectory(_artifactVideoDir);

        _defaultRetryAttempts = int.TryParse(Environment.GetEnvironmentVariable("PLAYWRIGHT_RETRIES"), out var retries) && retries > 0
            ? retries
            : 3;
        _defaultRetryDelayMs = int.TryParse(Environment.GetEnvironmentVariable("PLAYWRIGHT_RETRY_DELAY_MS"), out var retryDelay) && retryDelay > 0
            ? retryDelay
            : 500;
    }

    public async Task InitializeAsync()
    {
        try
        {
            _playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        }
        catch (PlaywrightException ex) when (ex.Message.Contains("Executable doesn't exist") || ex.Message.Contains("browserType.launch"))
        {
            _output.WriteLine($"Playwright browsers not installed. Skipping tests. Error: {ex.Message}");
            _browsersAvailable = false;
        }
    }

    private void SkipIfBrowsersUnavailable()
    {
        Skip.If(!_browsersAvailable, "Playwright browsers not installed. Run 'playwright install' to enable UI tests.");
    }

    public async Task DisposeAsync()
    {
        _httpClient.Dispose();
        _playwright?.Dispose();
    }

    [Theory(DisplayName = "Login works on {browser}")]
    [InlineData("chromium")]
    [InlineData("firefox")]
    [InlineData("webkit")]
    public async Task Login_WorksOnAllBrowsers(string browserType)
    {
        await EnsureServerReachableAsync();
        
        IBrowser browser = null;
        try
        {
            browser = browserType.ToLower() switch
            {
                "chromium" => await _playwright!.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true }),
                "firefox" => await _playwright!.Firefox.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true }),
                "webkit" => await _playwright!.Webkit.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true }),
                _ => throw new ArgumentException($"Unsupported browser: {browserType}")
            };

            await RunScenarioAsync(browser, $"{browserType}_login", async context =>
            {
                var page = await context.NewPageAsync();
                await LoginAsync(page);

                await WithRetriesAsync(async () =>
                {
                    var cookies = await context.CookiesAsync();
                    Assert.Contains(cookies, c => c.Name == SessionCookieHelper.RefreshCookieName);
                }, $"verify session cookie exists on {browserType}");
            });
        }
        finally
        {
            if (browser != null)
            {
                await browser.DisposeAsync();
            }
        }
    }

    [Theory(DisplayName = "Patient search works on {browser}")]
    [InlineData("chromium")]
    [InlineData("firefox")]
    public async Task PatientSearch_WorksOnBrowsers(string browserType)
    {
        await EnsureServerReachableAsync();
        
        IBrowser browser = null;
        try
        {
            browser = browserType.ToLower() switch
            {
                "chromium" => await _playwright!.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true }),
                "firefox" => await _playwright!.Firefox.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true }),
                _ => throw new ArgumentException($"Unsupported browser: {browserType}")
            };

            await RunScenarioAsync(browser, $"{browserType}_patient_search", async context =>
            {
                var page = await context.NewPageAsync();
                await LoginAsync(page);
                await page.GotoAsync(new Uri(_baseUri, "/patients").ToString(), new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

                await WithRetriesAsync(async () =>
                {
                    var tableVisible = await page.Locator("table, .mud-table").IsVisibleAsync();
                    Assert.True(tableVisible, "Patient table should be visible");
                }, $"patient table visible on {browserType}");

                var searchInput = page.Locator("input[placeholder*='Search'], input[type='search'], .mud-input input").First;
                if (await searchInput.IsVisibleAsync())
                {
                    await searchInput.FillAsync("test");
                    await page.WaitForTimeoutAsync(500);
                }
            });
        }
        finally
        {
            if (browser != null)
            {
                await browser.DisposeAsync();
            }
        }
    }

    private async Task LoginAsync(IPage page)
    {
        await page.GotoAsync(new Uri(_baseUri, "/login").ToString(), new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await page.FillAsync("input[name=Username]", _playwrightCreds.UserName);
        await page.FillAsync("input[name=Password]", _playwrightCreds.Password);
        await page.ClickAsync("button[type=submit]");
        await page.WaitForURLAsync(new Regex(".*(?<!login)$"), new PageWaitForURLOptions { Timeout = 10000 });
    }

    private async Task RunScenarioAsync(
        IBrowser browser,
        string scenarioName,
        Func<IBrowserContext, Task> scenarioAction,
        int? maxAttempts = null)
    {
        ArgumentNullException.ThrowIfNull(browser);
        var attemptLimit = maxAttempts ?? _defaultRetryAttempts;

        for (var attempt = 1; attempt <= attemptLimit; attempt++)
        {
            var context = await browser.NewContextAsync(new BrowserNewContextOptions
            {
                IgnoreHTTPSErrors = true,
                RecordVideoDir = _artifactVideoDir,
                RecordVideoSize = new RecordVideoSize { Height = 720, Width = 1280 }
            });

            var attemptStamp = $"{scenarioName}-{DateTime.UtcNow:yyyyMMddHHmmss}-attempt{attempt}";
            var tracePath = Path.Combine(_artifactTraceDir, $"{attemptStamp}.zip");
            var success = false;

            try
            {
                await context.Tracing.StartAsync(new TracingStartOptions
                {
                    Title = scenarioName,
                    Screenshots = true,
                    Snapshots = true,
                    Sources = true
                });

                await scenarioAction(context);
                success = true;
            }
            catch (Exception ex)
            {
                _output.WriteLine($"Scenario '{scenarioName}' failed on attempt {attempt}: {ex}");
                if (attempt == attemptLimit)
                {
                    throw;
                }
            }
            finally
            {
                await context.Tracing.StopAsync(new TracingStopOptions { Path = tracePath });
                var pages = context.Pages.ToList();
                await context.CloseAsync();

                if (success)
                {
                    TryDeleteFile(tracePath);
                    await PersistVideosAsync(pages, attemptStamp, keepArtifacts: false);
                }
                else
                {
                    _output.WriteLine($"Trace saved to {tracePath}");
                    await PersistVideosAsync(pages, attemptStamp, keepArtifacts: true);
                }
            }

            if (success)
            {
                return;
            }

            await Task.Delay(_defaultRetryDelayMs);
        }

        throw new XunitException($"Scenario '{scenarioName}' failed after {maxAttempts ?? _defaultRetryAttempts} attempts.");
    }

    private async Task PersistVideosAsync(IEnumerable<IPage> pages, string attemptStamp, bool keepArtifacts)
    {
        foreach (var page in pages)
        {
            if (page.Video is null)
            {
                continue;
            }

            try
            {
                var tempPath = await page.Video.PathAsync();
                if (!keepArtifacts)
                {
                    TryDeleteFile(tempPath);
                    continue;
                }

                var destination = Path.Combine(_artifactVideoDir, $"{attemptStamp}-{Path.GetFileName(tempPath)}");
                File.Copy(tempPath, destination, true);
                _output.WriteLine($"Video saved to {destination}");
            }
            catch (Exception ex)
            {
                _output.WriteLine($"Failed to capture video for {attemptStamp}: {ex.Message}");
            }
        }
    }

    private async Task WithRetriesAsync(Func<Task> action, string description)
    {
        Exception? lastException = null;
        for (var attempt = 1; attempt <= _defaultRetryAttempts; attempt++)
        {
            try
            {
                await action();
                return;
            }
            catch (Exception ex)
            {
                lastException = ex;
                _output.WriteLine($"[{description}] attempt {attempt} failed: {ex.Message}");
                if (attempt == _defaultRetryAttempts)
                {
                    throw;
                }
            }

            await Task.Delay(_defaultRetryDelayMs);
        }

        throw lastException ?? new XunitException($"[{description}] failed after retries.");
    }

    private static void TryDeleteFile(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // best effort cleanup
        }
    }

    private async Task EnsureServerReachableAsync()
    {
        var timeout = TimeSpan.FromSeconds(90);
        var start = DateTimeOffset.UtcNow;
        Exception? lastException = null;

        while (DateTimeOffset.UtcNow - start < timeout)
        {
            try
            {
                using var response = await _httpClient.GetAsync(new Uri(_baseUri, "/"), HttpCompletionOption.ResponseHeadersRead);
                if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.Redirect)
                {
                    return;
                }
                lastException = new HttpRequestException($"The app returned {(int)response.StatusCode} when probed.");
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                lastException = ex;
            }
            await Task.Delay(1000);
        }

        Assert.Fail(BuildSkipMessage(lastException?.Message ?? "Server did not become reachable within timeout."));
    }

    private static string BuildSkipMessage(string details) =>
        $"UI smoke tests require the Zebrahoof EMR app to be running. Start the app (e.g. `dotnet run --launch-profile https`) " +
        $"and optionally set UI_SMOKE_BASEURL. Details: {details}";
}
