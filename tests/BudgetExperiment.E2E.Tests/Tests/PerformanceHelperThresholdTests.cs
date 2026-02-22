// <copyright file="PerformanceHelperThresholdTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.E2E.Tests.Helpers;

namespace BudgetExperiment.E2E.Tests.Tests;

/// <summary>
/// Unit tests for <see cref="PerformanceHelper.AssertThresholds"/> logic.
/// These tests verify that threshold assertions pass or fail correctly
/// without requiring Playwright or a running application.
/// </summary>
public class PerformanceHelperThresholdTests
{
    /// <summary>
    /// Metrics within all thresholds should not throw.
    /// </summary>
    [Fact]
    public void AssertThresholds_AllMetricsWithinLimits_DoesNotThrow()
    {
        var metrics = new PerformanceMetrics(
            FirstContentfulPaintMs: 800,
            LargestContentfulPaintMs: 1800,
            DomContentLoadedMs: 600,
            LoadCompleteMs: 2000,
            CumulativeLayoutShift: 0.03,
            TimeToInteractiveMs: 2000);

        var exception = Record.Exception(() => PerformanceHelper.AssertThresholds(metrics));

        Assert.Null(exception);
    }

    /// <summary>
    /// Metrics exactly at the fail thresholds should not throw (boundary check: uses > not >=).
    /// </summary>
    [Fact]
    public void AssertThresholds_MetricsExactlyAtFailThresholds_DoesNotThrow()
    {
        var metrics = new PerformanceMetrics(
            FirstContentfulPaintMs: PerformanceThresholds.Fcp.FailMs,
            LargestContentfulPaintMs: PerformanceThresholds.Lcp.FailMs,
            DomContentLoadedMs: 1000,
            LoadCompleteMs: 3000,
            CumulativeLayoutShift: PerformanceThresholds.Cls.Fail,
            TimeToInteractiveMs: PerformanceThresholds.Tti.FailMs);

        var exception = Record.Exception(() => PerformanceHelper.AssertThresholds(metrics));

        Assert.Null(exception);
    }

    /// <summary>
    /// FCP exceeding the fail threshold should cause assertion failure.
    /// </summary>
    [Fact]
    public void AssertThresholds_FcpExceedsThreshold_Throws()
    {
        var metrics = new PerformanceMetrics(
            FirstContentfulPaintMs: PerformanceThresholds.Fcp.FailMs + 1,
            LargestContentfulPaintMs: 1000,
            DomContentLoadedMs: 500,
            LoadCompleteMs: 1500,
            CumulativeLayoutShift: 0.01,
            TimeToInteractiveMs: 1000);

        var exception = Assert.ThrowsAny<Exception>(() => PerformanceHelper.AssertThresholds(metrics));

        Assert.Contains("FCP", exception.Message);
    }

    /// <summary>
    /// LCP exceeding the fail threshold should cause assertion failure.
    /// </summary>
    [Fact]
    public void AssertThresholds_LcpExceedsThreshold_Throws()
    {
        var metrics = new PerformanceMetrics(
            FirstContentfulPaintMs: 500,
            LargestContentfulPaintMs: PerformanceThresholds.Lcp.FailMs + 1,
            DomContentLoadedMs: 500,
            LoadCompleteMs: 3000,
            CumulativeLayoutShift: 0.01,
            TimeToInteractiveMs: 1000);

        var exception = Assert.ThrowsAny<Exception>(() => PerformanceHelper.AssertThresholds(metrics));

        Assert.Contains("LCP", exception.Message);
    }

    /// <summary>
    /// TTI exceeding the fail threshold should cause assertion failure.
    /// </summary>
    [Fact]
    public void AssertThresholds_TtiExceedsThreshold_Throws()
    {
        var metrics = new PerformanceMetrics(
            FirstContentfulPaintMs: 500,
            LargestContentfulPaintMs: 1000,
            DomContentLoadedMs: 500,
            LoadCompleteMs: 3500,
            CumulativeLayoutShift: 0.01,
            TimeToInteractiveMs: PerformanceThresholds.Tti.FailMs + 1);

        var exception = Assert.ThrowsAny<Exception>(() => PerformanceHelper.AssertThresholds(metrics));

        Assert.Contains("TTI", exception.Message);
    }

    /// <summary>
    /// CLS exceeding the fail threshold should cause assertion failure.
    /// </summary>
    [Fact]
    public void AssertThresholds_ClsExceedsThreshold_Throws()
    {
        var metrics = new PerformanceMetrics(
            FirstContentfulPaintMs: 500,
            LargestContentfulPaintMs: 1000,
            DomContentLoadedMs: 500,
            LoadCompleteMs: 1500,
            CumulativeLayoutShift: PerformanceThresholds.Cls.Fail + 0.001,
            TimeToInteractiveMs: 1000);

        var exception = Assert.ThrowsAny<Exception>(() => PerformanceHelper.AssertThresholds(metrics));

        Assert.Contains("CLS", exception.Message);
    }

    /// <summary>
    /// Multiple metrics exceeding thresholds should report all failures.
    /// </summary>
    [Fact]
    public void AssertThresholds_MultipleMetricsExceedThreshold_ReportsAllFailures()
    {
        var metrics = new PerformanceMetrics(
            FirstContentfulPaintMs: PerformanceThresholds.Fcp.FailMs + 500,
            LargestContentfulPaintMs: PerformanceThresholds.Lcp.FailMs + 500,
            DomContentLoadedMs: 1000,
            LoadCompleteMs: 5000,
            CumulativeLayoutShift: PerformanceThresholds.Cls.Fail + 0.5,
            TimeToInteractiveMs: PerformanceThresholds.Tti.FailMs + 500);

        var exception = Assert.ThrowsAny<Exception>(() => PerformanceHelper.AssertThresholds(metrics));

        Assert.Contains("FCP", exception.Message);
        Assert.Contains("LCP", exception.Message);
        Assert.Contains("TTI", exception.Message);
        Assert.Contains("CLS", exception.Message);
    }
}
