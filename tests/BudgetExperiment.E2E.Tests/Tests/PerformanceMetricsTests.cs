// <copyright file="PerformanceMetricsTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.E2E.Tests.Helpers;

namespace BudgetExperiment.E2E.Tests.Tests;

/// <summary>
/// Unit tests for <see cref="PerformanceMetrics"/> computed properties and formatting.
/// These are pure logic tests that do not require Playwright or a running application.
/// </summary>
public class PerformanceMetricsTests
{
    /// <summary>
    /// Seconds properties should convert milliseconds correctly.
    /// </summary>
    [Fact]
    public void SecondsProperties_ConvertFromMilliseconds_Correctly()
    {
        var metrics = new PerformanceMetrics(
            FirstContentfulPaintMs: 1500,
            LargestContentfulPaintMs: 2500,
            DomContentLoadedMs: 1000,
            LoadCompleteMs: 3000,
            CumulativeLayoutShift: 0.05,
            TimeToInteractiveMs: 2000);

        Assert.Equal(1.5, metrics.FirstContentfulPaintSeconds);
        Assert.Equal(2.5, metrics.LargestContentfulPaintSeconds);
        Assert.Equal(2.0, metrics.TimeToInteractiveSeconds);
    }

    /// <summary>
    /// Zero millisecond values should produce zero seconds.
    /// </summary>
    [Fact]
    public void SecondsProperties_WithZeroMs_ReturnZero()
    {
        var metrics = new PerformanceMetrics(0, 0, 0, 0, 0, 0);

        Assert.Equal(0.0, metrics.FirstContentfulPaintSeconds);
        Assert.Equal(0.0, metrics.LargestContentfulPaintSeconds);
        Assert.Equal(0.0, metrics.TimeToInteractiveSeconds);
    }

    /// <summary>
    /// ToSummary should include all metric values in the output.
    /// </summary>
    [Fact]
    public void ToSummary_IncludesAllMetricValues()
    {
        var metrics = new PerformanceMetrics(
            FirstContentfulPaintMs: 800,
            LargestContentfulPaintMs: 1900,
            DomContentLoadedMs: 700,
            LoadCompleteMs: 2500,
            CumulativeLayoutShift: 0.03,
            TimeToInteractiveMs: 2200);

        var summary = metrics.ToSummary();

        Assert.Contains("FCP:", summary);
        Assert.Contains("LCP:", summary);
        Assert.Contains("TTI:", summary);
        Assert.Contains("CLS:", summary);
        Assert.Contains("DOM:", summary);
        Assert.Contains("Load:", summary);
        Assert.Contains("800ms", summary);
        Assert.Contains("1900ms", summary);
        Assert.Contains("2200ms", summary);
        Assert.Contains("0.0300", summary);
    }
}
