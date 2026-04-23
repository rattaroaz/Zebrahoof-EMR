using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using Xunit.Abstractions;
using FluentAssertions;

namespace Zebrahoof.EMR.PerformanceTests;

public class PerformanceMonitoring : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly ITestOutputHelper _output;
    private readonly string _baselineDataPath;
    private readonly PerformanceBaseline _baseline;

    public PerformanceMonitoring(WebApplicationFactory<Program> factory, ITestOutputHelper output)
    {
        _factory = factory;
        _output = output;
        _baselineDataPath = Path.Combine("PerformanceData", "baseline.json");
        _baseline = LoadOrCreateBaseline();
    }

    [Fact(Skip = "Requires baseline data setup", DisplayName = "Performance regression detection")]
    public async Task PerformanceRegression_ShouldDetectRegressions()
    {
        // Arrange
        var currentMetrics = await CollectPerformanceMetrics();

        // Act & Assert
        foreach (var metric in currentMetrics)
        {
            var baselineMetric = _baseline.Metrics.FirstOrDefault(m => m.Endpoint == metric.Endpoint);
            if (baselineMetric != null)
            {
                // Check for regression (10% threshold)
                var regressionThreshold = baselineMetric.AverageResponseTime * 1.1;
                metric.AverageResponseTime.Should().BeLessThan((long)regressionThreshold, 
                    $"Performance regression detected for {metric.Endpoint}. Current: {metric.AverageResponseTime}ms, Baseline: {baselineMetric.AverageResponseTime}ms, Threshold: {regressionThreshold}ms");

                // Check for significant degradation (25% threshold)
                var degradationThreshold = baselineMetric.AverageResponseTime * 1.25;
                if (metric.AverageResponseTime > degradationThreshold)
                {
                    _output.WriteLine($"⚠️  Significant performance degradation for {metric.Endpoint}: {(metric.AverageResponseTime / baselineMetric.AverageResponseTime - 1) * 100:F1}% slower than baseline");
                }
            }
            else
            {
                _output.WriteLine($"ℹ️  New metric for {metric.Endpoint} - no baseline comparison available");
            }
        }

        // Save current metrics as new baseline if they're better
        await SaveBaselineIfImproved(currentMetrics);
    }

    [Fact(Skip = "Requires historical data setup", DisplayName = "Performance trends analysis")]
    public async Task PerformanceTrends_ShouldAnalyzeTrends()
    {
        // Arrange
        var currentMetrics = await CollectPerformanceMetrics();
        var historyPath = Path.Combine("PerformanceData", "history.json");
        var history = LoadPerformanceHistory(historyPath);

        // Act
        history.Add(new PerformanceSnapshot
        {
            Timestamp = DateTime.UtcNow,
            Metrics = currentMetrics
        });

        // Keep only last 30 days of history
        var cutoffDate = DateTime.UtcNow.AddDays(-30);
        history.RemoveAll(h => h.Timestamp < cutoffDate);

        // Save updated history
        await File.WriteAllTextAsync(historyPath, JsonSerializer.Serialize(history, new JsonSerializerOptions { WriteIndented = true }));

        // Assert - Analyze trends
        if (history.Count >= 2)
        {
            var recentSnapshots = history.TakeLast(5).ToList();
            var olderSnapshots = history.SkipLast(5).TakeLast(5).ToList();

            foreach (var endpoint in currentMetrics.Select(m => m.Endpoint).Distinct())
            {
                var recentAvg = recentSnapshots
                    .Where(s => s.Metrics.Any(m => m.Endpoint == endpoint))
                    .SelectMany(s => s.Metrics.Where(m => m.Endpoint == endpoint))
                    .Average(m => m.AverageResponseTime);

                var olderAvg = olderSnapshots
                    .Where(s => s.Metrics.Any(m => m.Endpoint == endpoint))
                    .SelectMany(s => s.Metrics.Where(m => m.Endpoint == endpoint))
                    .DefaultIfEmpty()
                    .Average(m => m.AverageResponseTime);

                if (olderAvg > 0)
                {
                    var trend = (recentAvg - olderAvg) / olderAvg * 100;
                    
                    if (trend > 5)
                    {
                        _output.WriteLine($"📈 Performance improving for {endpoint}: {trend:F1}% faster");
                    }
                    else if (trend < -5)
                    {
                        _output.WriteLine($"📉 Performance degrading for {endpoint}: {Math.Abs(trend):F1}% slower");
                    }
                    else
                    {
                        _output.WriteLine($"➡️  Performance stable for {endpoint}: {Math.Abs(trend):F1}% change");
                    }
                }
            }
        }

        _output.WriteLine($"Performance history contains {history.Count} snapshots");
    }

    [Fact(DisplayName = "Performance benchmarking")]
    public async Task PerformanceBenchmarking_ShouldMeetTargets()
    {
        // Arrange
        var currentMetrics = await CollectPerformanceMetrics();
        var targets = GetPerformanceTargets();

        // Act & Assert
        foreach (var metric in currentMetrics)
        {
            if (targets.TryGetValue(metric.Endpoint, out var target))
            {
                metric.AverageResponseTime.Should().BeLessThan(target.MaxResponseTime, 
                    $"{metric.Endpoint} should respond in under {target.MaxResponseTime}ms. Current: {metric.AverageResponseTime}ms");

                metric.SuccessRate.Should().BeGreaterOrEqualTo(target.MinSuccessRate, 
                    $"{metric.Endpoint} should have success rate >= {target.MinSuccessRate:F1}%. Current: {metric.SuccessRate:F1}%");

                metric.P95ResponseTime.Should().BeLessThan(target.MaxP95ResponseTime, 
                    $"{metric.Endpoint} P95 should be under {target.MaxP95ResponseTime}ms. Current: {metric.P95ResponseTime}ms");
            }
        }

        // Report compliance
        var compliantMetrics = currentMetrics.Count(m => 
            targets.ContainsKey(m.Endpoint) && 
            m.AverageResponseTime <= targets[m.Endpoint].MaxResponseTime &&
            m.SuccessRate >= targets[m.Endpoint].MinSuccessRate &&
            m.P95ResponseTime <= targets[m.Endpoint].MaxP95ResponseTime);

        var complianceRate = (double)compliantMetrics / currentMetrics.Count * 100;
        _output.WriteLine($"Performance compliance: {complianceRate:F1}% ({compliantMetrics}/{currentMetrics.Count} metrics meet targets)");

        complianceRate.Should().BeGreaterOrEqualTo(80, "At least 80% of metrics should meet performance targets");
    }

    [Fact(DisplayName = "Resource usage monitoring")]
    public async Task ResourceUsage_ShouldStayWithinLimits()
    {
        // Arrange
        var initialMemory = GC.GetTotalMemory(true);
        var initialThreadCount = System.Diagnostics.Process.GetCurrentProcess().Threads.Count;

        // Act - Simulate load
        var tasks = new List<Task>();
        for (int i = 0; i < 100; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                var client = _factory.CreateClient();
                await client.GetAsync("/api/patients");
                client.Dispose();
            }));
        }

        await Task.WhenAll(tasks);

        // Force garbage collection
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var finalMemory = GC.GetTotalMemory(true);
        var finalThreadCount = System.Diagnostics.Process.GetCurrentProcess().Threads.Count;

        // Assert
        var memoryIncrease = (finalMemory - initialMemory) / (1024 * 1024); // MB
        var threadIncrease = finalThreadCount - initialThreadCount;

        memoryIncrease.Should().BeLessThan(100, $"Memory increase should be less than 100MB. Current: {memoryIncrease}MB");
        threadIncrease.Should().BeLessThan(50, $"Thread increase should be less than 50. Current: {threadIncrease}");

        _output.WriteLine($"Resource usage after load:");
        _output.WriteLine($"  Memory increase: {memoryIncrease}MB");
        _output.WriteLine($"  Thread increase: {threadIncrease}");
        _output.WriteLine($"  Final memory: {finalMemory / (1024 * 1024)}MB");
        _output.WriteLine($"  Final threads: {finalThreadCount}");
    }

    private async Task<List<PerformanceMetric>> CollectPerformanceMetrics()
    {
        var metrics = new List<PerformanceMetric>();
        var endpoints = new[]
        {
            "/",
            "/login",
            "/patients",
            "/api/patients",
            "/api/patients/search?q=test",
            "/api/patients/1",
            "/appointments"
        };

        foreach (var endpoint in endpoints)
        {
            var responseTimes = new List<long>();
            var successCount = 0;
            var totalRequests = 10;

            for (int i = 0; i < totalRequests; i++)
            {
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                var client = _factory.CreateClient();
                
                try
                {
                    var response = await client.GetAsync(endpoint);
                    stopwatch.Stop();
                    
                    if (response.IsSuccessStatusCode)
                    {
                        responseTimes.Add(stopwatch.ElapsedMilliseconds);
                        successCount++;
                    }
                }
                catch
                {
                    stopwatch.Stop();
                }
                finally
                {
                    client.Dispose();
                }
            }

            if (responseTimes.Count > 0)
            {
                responseTimes.Sort();
                var metric = new PerformanceMetric
                {
                    Endpoint = endpoint,
                    AverageResponseTime = (long)responseTimes.Average(),
                    MinResponseTime = responseTimes.Min(),
                    MaxResponseTime = responseTimes.Max(),
                    P95ResponseTime = responseTimes[(int)(responseTimes.Count * 0.95)],
                    SuccessRate = (double)successCount / totalRequests * 100,
                    SampleSize = responseTimes.Count,
                    Timestamp = DateTime.UtcNow
                };

                metrics.Add(metric);
            }
        }

        return metrics;
    }

    private PerformanceBaseline LoadOrCreateBaseline()
    {
        if (File.Exists(_baselineDataPath))
        {
            var json = File.ReadAllText(_baselineDataPath);
            return JsonSerializer.Deserialize<PerformanceBaseline>(json) ?? new PerformanceBaseline();
        }
        
        return new PerformanceBaseline();
    }

    private async Task SaveBaselineIfImproved(List<PerformanceMetric> currentMetrics)
    {
        var shouldUpdate = false;

        foreach (var currentMetric in currentMetrics)
        {
            var baselineMetric = _baseline.Metrics.FirstOrDefault(m => m.Endpoint == currentMetric.Endpoint);
            
            if (baselineMetric == null || currentMetric.AverageResponseTime < baselineMetric.AverageResponseTime)
            {
                shouldUpdate = true;
                break;
            }
        }

        if (shouldUpdate)
        {
            _baseline.Metrics = currentMetrics;
            _baseline.Timestamp = DateTime.UtcNow;
            
            Directory.CreateDirectory(Path.GetDirectoryName(_baselineDataPath)!);
            await File.WriteAllTextAsync(_baselineDataPath, JsonSerializer.Serialize(_baseline, new JsonSerializerOptions { WriteIndented = true }));
            
            _output.WriteLine("Performance baseline updated with improved metrics");
        }
    }

    private List<PerformanceSnapshot> LoadPerformanceHistory(string historyPath)
    {
        if (File.Exists(historyPath))
        {
            var json = File.ReadAllText(historyPath);
            return JsonSerializer.Deserialize<List<PerformanceSnapshot>>(json) ?? new List<PerformanceSnapshot>();
        }
        
        return new List<PerformanceSnapshot>();
    }

    private Dictionary<string, PerformanceTarget> GetPerformanceTargets()
    {
        return new Dictionary<string, PerformanceTarget>
        {
            ["/"] = new PerformanceTarget { MaxResponseTime = 1500, MaxP95ResponseTime = 2000, MinSuccessRate = 95.0 },
            ["/login"] = new PerformanceTarget { MaxResponseTime = 1000, MaxP95ResponseTime = 1500, MinSuccessRate = 95.0 },
            ["/patients"] = new PerformanceTarget { MaxResponseTime = 2000, MaxP95ResponseTime = 3000, MinSuccessRate = 90.0 },
            ["/api/patients"] = new PerformanceTarget { MaxResponseTime = 500, MaxP95ResponseTime = 1000, MinSuccessRate = 98.0 },
            ["/api/patients/search?q=test"] = new PerformanceTarget { MaxResponseTime = 800, MaxP95ResponseTime = 1500, MinSuccessRate = 95.0 },
            ["/api/patients/1"] = new PerformanceTarget { MaxResponseTime = 300, MaxP95ResponseTime = 600, MinSuccessRate = 98.0 },
            ["/appointments"] = new PerformanceTarget { MaxResponseTime = 1800, MaxP95ResponseTime = 2500, MinSuccessRate = 90.0 }
        };
    }
}

public class PerformanceBaseline
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public List<PerformanceMetric> Metrics { get; set; } = new();
}

public class PerformanceSnapshot
{
    public DateTime Timestamp { get; set; }
    public List<PerformanceMetric> Metrics { get; set; } = new();
}

public class PerformanceMetric
{
    public string Endpoint { get; set; } = string.Empty;
    public long AverageResponseTime { get; set; }
    public long MinResponseTime { get; set; }
    public long MaxResponseTime { get; set; }
    public long P95ResponseTime { get; set; }
    public double SuccessRate { get; set; }
    public int SampleSize { get; set; }
    public DateTime Timestamp { get; set; }
}

public class PerformanceTarget
{
    public long MaxResponseTime { get; set; }
    public long MaxP95ResponseTime { get; set; }
    public double MinSuccessRate { get; set; }
}
