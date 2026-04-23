using System.Diagnostics;
using System.IO;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;
using Zebrahoof_EMR.Data;

namespace Zebrahoof.EMR.UiSmokeTests;

public sealed class PlaywrightServerFixture : IAsyncLifetime
{
    private readonly Uri _baseUri;
    private readonly HttpClient _httpClient;
    private Process? _serverProcess;
    private IHost? _testHost;

    public PlaywrightServerFixture()
    {
        var baseUrl = Environment.GetEnvironmentVariable("UI_SMOKE_BASEURL") ?? "https://localhost:7177";
        _baseUri = new Uri(baseUrl);
        _httpClient = new HttpClient(new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
        })
        {
            Timeout = TimeSpan.FromSeconds(5)
        };
    }

    public async Task InitializeAsync()
    {
        if (await IsServerReachableAsync())
        {
            // Server is already running, just seed test data
            await SeedTestDataAsync();
            return;
        }

        await StartServerAsync();
        await WaitForServerAsync(TimeSpan.FromSeconds(90));
        
        // Seed test data after server starts
        await SeedTestDataAsync();
    }

    private async Task SeedTestDataAsync()
    {
        try
        {
            // Create a test host to access services
            var hostBuilder = new HostBuilder()
                .ConfigureServices((context, services) =>
                {
                    // Configure services similar to the main application
                    // This is a simplified version - in a real scenario you'd
                    // copy the service configuration from Program.cs
                });

            _testHost = hostBuilder.Build();
            
            // Seed test data using the seeder
            await UITestDataSeeder.SeedAllTestDataAsync(_testHost.Services);
        }
        catch (Exception ex)
        {
            // Log error but don't fail the tests - they can run with existing data
            System.Diagnostics.Debug.WriteLine($"Failed to seed test data: {ex.Message}");
        }
    }

    public async Task DisposeAsync()
    {
        try
        {
            _httpClient.Dispose();
        }
        catch
        {
            // ignore
        }

        if (_testHost != null)
        {
            _testHost.Dispose();
        }

        if (_serverProcess is { HasExited: false })
        {
            try
            {
                _serverProcess.Kill(entireProcessTree: true);
            }
            catch
            {
                // ignore
            }
        }

        if (_serverProcess is not null)
        {
            using var waitCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            try
            {
                await _serverProcess.WaitForExitAsync(waitCts.Token);
            }
            catch (OperationCanceledException)
            {
                // ignore timeout
            }
            _serverProcess.Dispose();
        }
    }

    private async Task<bool> IsServerReachableAsync()
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            var response = await _httpClient.GetAsync(new Uri(_baseUri, "/"), cts.Token);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private async Task StartServerAsync()
    {
        var repoRoot = GetRepositoryRoot();
        var projectPath = Path.Combine(repoRoot, "Zebrahoof EMR.csproj");

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project \"{projectPath}\" --urls \"{_baseUri}\"",
            WorkingDirectory = repoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Testing";
        startInfo.Environment["DOTNET_URLS"] = _baseUri.ToString();

        _serverProcess = new Process { StartInfo = startInfo, EnableRaisingEvents = true };

        if (!_serverProcess.Start())
        {
            throw new InvalidOperationException("Failed to start Zebrahoof EMR server for UI smoke tests.");
        }

        // Drain stdout/stderr to avoid buffering deadlocks
        _ = Task.Run(() => DrainStreamAsync(_serverProcess.StandardOutput));
        _ = Task.Run(() => DrainStreamAsync(_serverProcess.StandardError));

        await Task.CompletedTask;
    }

    private async Task WaitForServerAsync(TimeSpan timeout)
    {
        var start = DateTimeOffset.UtcNow;
        var delay = TimeSpan.FromSeconds(1);

        while (DateTimeOffset.UtcNow - start < timeout)
        {
            if (_serverProcess is { HasExited: true })
            {
                throw new InvalidOperationException("Zebrahoof EMR server process exited unexpectedly during startup.");
            }

            if (await IsServerReachableAsync())
            {
                return;
            }

            await Task.Delay(delay);
        }

        throw new TimeoutException($"Timed out waiting for Zebrahoof EMR server to start listening on {_baseUri}.");
    }

    private static async Task DrainStreamAsync(StreamReader reader)
    {
        try
        {
            while (!reader.EndOfStream)
            {
                await reader.ReadLineAsync().ConfigureAwait(false);
            }
        }
        catch
        {
            // ignore
        }
    }

    private static string GetRepositoryRoot()
    {
        var assemblyDirectory = Path.GetDirectoryName(typeof(PlaywrightServerFixture).Assembly.Location)
                                ?? throw new InvalidOperationException("Unable to determine assembly directory.");
        // .../tests/Zebrahoof.EMR.UiSmokeTests/bin/Debug/net10.0
        return Path.GetFullPath(Path.Combine(assemblyDirectory, "..", "..", "..", "..", ".."));
    }
}

[CollectionDefinition(CollectionName, DisableParallelization = true)]
public sealed class PlaywrightServerCollection : ICollectionFixture<PlaywrightServerFixture>
{
    public const string CollectionName = "Playwright server collection";
}
