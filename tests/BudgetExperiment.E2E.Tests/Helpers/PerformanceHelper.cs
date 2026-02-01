// <copyright file="PerformanceHelper.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.E2E.Tests.Helpers;

/// <summary>
/// Helper class for capturing Core Web Vitals and performance metrics using Playwright.
/// </summary>
public static class PerformanceHelper
{
    /// <summary>
    /// Captures Core Web Vitals and performance metrics from the current page.
    /// Must be called after the page has loaded.
    /// </summary>
    /// <param name="page">The Playwright page instance.</param>
    /// <returns>Performance metrics captured from the browser.</returns>
    public static async Task<PerformanceMetrics> CaptureMetricsAsync(IPage page)
    {
        // Use Performance Observer API to capture Core Web Vitals
        var metricsJson = await page.EvaluateAsync<Dictionary<string, double>>(@"() => {
            return new Promise((resolve) => {
                const metrics = {
                    fcp: 0,
                    lcp: 0,
                    domContentLoaded: 0,
                    loadComplete: 0,
                    cls: 0,
                    tti: 0
                };

                // Get navigation timing
                const nav = performance.getEntriesByType('navigation')[0];
                if (nav) {
                    metrics.domContentLoaded = nav.domContentLoadedEventEnd;
                    metrics.loadComplete = nav.loadEventEnd;
                    // Estimate TTI as domInteractive + time for long tasks to clear
                    // This is a simplified approximation
                    metrics.tti = nav.domInteractive;
                }

                // Get paint timing
                const paint = performance.getEntriesByType('paint');
                const fcp = paint.find(p => p.name === 'first-contentful-paint');
                if (fcp) {
                    metrics.fcp = fcp.startTime;
                }

                // Get LCP from PerformanceObserver if available
                // For immediate capture, use largest-contentful-paint entries
                const lcpEntries = performance.getEntriesByType('largest-contentful-paint');
                if (lcpEntries.length > 0) {
                    metrics.lcp = lcpEntries[lcpEntries.length - 1].startTime;
                } else {
                    // Fallback: approximate LCP as load complete
                    metrics.lcp = metrics.loadComplete;
                }

                // Get CLS from layout-shift entries
                const layoutShifts = performance.getEntriesByType('layout-shift');
                let clsValue = 0;
                for (const entry of layoutShifts) {
                    if (!entry.hadRecentInput) {
                        clsValue += entry.value;
                    }
                }
                metrics.cls = clsValue;

                // Refine TTI estimate: longest task after domInteractive
                const longTasks = performance.getEntriesByType('longtask');
                if (longTasks.length > 0) {
                    const lastLongTask = longTasks[longTasks.length - 1];
                    metrics.tti = Math.max(metrics.tti, lastLongTask.startTime + lastLongTask.duration);
                }

                resolve(metrics);
            });
        }");

        return new PerformanceMetrics(
            FirstContentfulPaintMs: metricsJson.GetValueOrDefault("fcp", 0),
            LargestContentfulPaintMs: metricsJson.GetValueOrDefault("lcp", 0),
            DomContentLoadedMs: metricsJson.GetValueOrDefault("domContentLoaded", 0),
            LoadCompleteMs: metricsJson.GetValueOrDefault("loadComplete", 0),
            CumulativeLayoutShift: metricsJson.GetValueOrDefault("cls", 0),
            TimeToInteractiveMs: metricsJson.GetValueOrDefault("tti", 0));
    }

    /// <summary>
    /// Logs performance metrics to the console output for CI visibility.
    /// </summary>
    /// <param name="metrics">The captured performance metrics.</param>
    /// <param name="testName">Optional test name to include in output.</param>
    public static void LogMetrics(PerformanceMetrics metrics, string? testName = null)
    {
        var header = string.IsNullOrEmpty(testName)
            ? "Performance Metrics"
            : $"Performance Metrics - {testName}";

        Console.WriteLine();
        Console.WriteLine($"╔══════════════════════════════════════════════════╗");
        Console.WriteLine($"║ {header,-48} ║");
        Console.WriteLine($"╠══════════════════════════════════════════════════╣");
        Console.WriteLine($"║ FCP:  {metrics.FirstContentfulPaintMs,8:F0}ms  ({PerformanceThresholds.GetFcpRating(metrics.FirstContentfulPaintMs),-17}) ║");
        Console.WriteLine($"║ LCP:  {metrics.LargestContentfulPaintMs,8:F0}ms  ({PerformanceThresholds.GetLcpRating(metrics.LargestContentfulPaintMs),-17}) ║");
        Console.WriteLine($"║ TTI:  {metrics.TimeToInteractiveMs,8:F0}ms  ({PerformanceThresholds.GetTtiRating(metrics.TimeToInteractiveMs),-17}) ║");
        Console.WriteLine($"║ CLS:  {metrics.CumulativeLayoutShift,8:F4}    ({PerformanceThresholds.GetClsRating(metrics.CumulativeLayoutShift),-17}) ║");
        Console.WriteLine($"╚══════════════════════════════════════════════════╝");
        Console.WriteLine();
    }

    /// <summary>
    /// Asserts that all Core Web Vitals meet the defined thresholds.
    /// </summary>
    /// <param name="metrics">The captured performance metrics.</param>
    /// <exception cref="Xunit.Sdk.XunitException">Thrown when any metric fails its threshold.</exception>
    public static void AssertThresholds(PerformanceMetrics metrics)
    {
        var failures = new List<string>();

        if (metrics.FirstContentfulPaintMs > PerformanceThresholds.Fcp.FailMs)
        {
            failures.Add($"FCP {metrics.FirstContentfulPaintMs:F0}ms exceeds threshold of {PerformanceThresholds.Fcp.FailMs}ms");
        }

        if (metrics.LargestContentfulPaintMs > PerformanceThresholds.Lcp.FailMs)
        {
            failures.Add($"LCP {metrics.LargestContentfulPaintMs:F0}ms exceeds threshold of {PerformanceThresholds.Lcp.FailMs}ms");
        }

        if (metrics.TimeToInteractiveMs > PerformanceThresholds.Tti.FailMs)
        {
            failures.Add($"TTI {metrics.TimeToInteractiveMs:F0}ms exceeds threshold of {PerformanceThresholds.Tti.FailMs}ms");
        }

        if (metrics.CumulativeLayoutShift > PerformanceThresholds.Cls.Fail)
        {
            failures.Add($"CLS {metrics.CumulativeLayoutShift:F4} exceeds threshold of {PerformanceThresholds.Cls.Fail}");
        }

        if (failures.Count > 0)
        {
            Assert.Fail($"Performance thresholds exceeded:\n- {string.Join("\n- ", failures)}");
        }
    }
}
