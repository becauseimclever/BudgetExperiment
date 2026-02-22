// <copyright file="PerformanceThresholdsTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.E2E.Tests.Helpers;

namespace BudgetExperiment.E2E.Tests.Tests;

/// <summary>
/// Unit tests for <see cref="PerformanceThresholds"/> rating methods.
/// These are pure logic tests that do not require Playwright or a running application.
/// </summary>
public class PerformanceThresholdsTests
{
    // --- FCP Rating ---

    /// <summary>
    /// FCP at or below the good threshold should be rated "Good".
    /// </summary>
    /// <param name="fcpMs">FCP value in milliseconds.</param>
    [Theory]
    [InlineData(0)]
    [InlineData(500)]
    [InlineData(1000)]
    public void GetFcpRating_AtOrBelowGoodThreshold_ReturnsGood(double fcpMs)
    {
        var rating = PerformanceThresholds.GetFcpRating(fcpMs);

        Assert.Equal("Good", rating);
    }

    /// <summary>
    /// FCP between good and warning thresholds should be rated "Needs Improvement".
    /// </summary>
    /// <param name="fcpMs">FCP value in milliseconds.</param>
    [Theory]
    [InlineData(1001)]
    [InlineData(1250)]
    [InlineData(1500)]
    public void GetFcpRating_BetweenGoodAndWarning_ReturnsNeedsImprovement(double fcpMs)
    {
        var rating = PerformanceThresholds.GetFcpRating(fcpMs);

        Assert.Equal("Needs Improvement", rating);
    }

    /// <summary>
    /// FCP above the warning threshold should be rated "Poor".
    /// </summary>
    /// <param name="fcpMs">FCP value in milliseconds.</param>
    [Theory]
    [InlineData(1501)]
    [InlineData(3000)]
    public void GetFcpRating_AboveWarningThreshold_ReturnsPoor(double fcpMs)
    {
        var rating = PerformanceThresholds.GetFcpRating(fcpMs);

        Assert.Equal("Poor", rating);
    }

    // --- LCP Rating ---

    /// <summary>
    /// LCP at or below the good threshold should be rated "Good".
    /// </summary>
    /// <param name="lcpMs">LCP value in milliseconds.</param>
    [Theory]
    [InlineData(0)]
    [InlineData(1000)]
    [InlineData(2000)]
    public void GetLcpRating_AtOrBelowGoodThreshold_ReturnsGood(double lcpMs)
    {
        var rating = PerformanceThresholds.GetLcpRating(lcpMs);

        Assert.Equal("Good", rating);
    }

    /// <summary>
    /// LCP between good and warning thresholds should be rated "Needs Improvement".
    /// </summary>
    /// <param name="lcpMs">LCP value in milliseconds.</param>
    [Theory]
    [InlineData(2001)]
    [InlineData(2250)]
    [InlineData(2500)]
    public void GetLcpRating_BetweenGoodAndWarning_ReturnsNeedsImprovement(double lcpMs)
    {
        var rating = PerformanceThresholds.GetLcpRating(lcpMs);

        Assert.Equal("Needs Improvement", rating);
    }

    /// <summary>
    /// LCP above the warning threshold should be rated "Poor".
    /// </summary>
    /// <param name="lcpMs">LCP value in milliseconds.</param>
    [Theory]
    [InlineData(2501)]
    [InlineData(5000)]
    public void GetLcpRating_AboveWarningThreshold_ReturnsPoor(double lcpMs)
    {
        var rating = PerformanceThresholds.GetLcpRating(lcpMs);

        Assert.Equal("Poor", rating);
    }

    // --- TTI Rating ---

    /// <summary>
    /// TTI at or below the good threshold should be rated "Good".
    /// </summary>
    /// <param name="ttiMs">TTI value in milliseconds.</param>
    [Theory]
    [InlineData(0)]
    [InlineData(1500)]
    [InlineData(2500)]
    public void GetTtiRating_AtOrBelowGoodThreshold_ReturnsGood(double ttiMs)
    {
        var rating = PerformanceThresholds.GetTtiRating(ttiMs);

        Assert.Equal("Good", rating);
    }

    /// <summary>
    /// TTI between good and warning thresholds should be rated "Needs Improvement".
    /// </summary>
    /// <param name="ttiMs">TTI value in milliseconds.</param>
    [Theory]
    [InlineData(2501)]
    [InlineData(2750)]
    [InlineData(3000)]
    public void GetTtiRating_BetweenGoodAndWarning_ReturnsNeedsImprovement(double ttiMs)
    {
        var rating = PerformanceThresholds.GetTtiRating(ttiMs);

        Assert.Equal("Needs Improvement", rating);
    }

    /// <summary>
    /// TTI above the warning threshold should be rated "Poor".
    /// </summary>
    /// <param name="ttiMs">TTI value in milliseconds.</param>
    [Theory]
    [InlineData(3001)]
    [InlineData(6000)]
    public void GetTtiRating_AboveWarningThreshold_ReturnsPoor(double ttiMs)
    {
        var rating = PerformanceThresholds.GetTtiRating(ttiMs);

        Assert.Equal("Poor", rating);
    }

    // --- CLS Rating ---

    /// <summary>
    /// CLS at or below the good threshold should be rated "Good".
    /// </summary>
    /// <param name="cls">CLS score.</param>
    [Theory]
    [InlineData(0.0)]
    [InlineData(0.01)]
    [InlineData(0.05)]
    public void GetClsRating_AtOrBelowGoodThreshold_ReturnsGood(double cls)
    {
        var rating = PerformanceThresholds.GetClsRating(cls);

        Assert.Equal("Good", rating);
    }

    /// <summary>
    /// CLS between good and warning thresholds should be rated "Needs Improvement".
    /// </summary>
    /// <param name="cls">CLS score.</param>
    [Theory]
    [InlineData(0.051)]
    [InlineData(0.075)]
    [InlineData(0.1)]
    public void GetClsRating_BetweenGoodAndWarning_ReturnsNeedsImprovement(double cls)
    {
        var rating = PerformanceThresholds.GetClsRating(cls);

        Assert.Equal("Needs Improvement", rating);
    }

    /// <summary>
    /// CLS above the warning threshold should be rated "Poor".
    /// </summary>
    /// <param name="cls">CLS score.</param>
    [Theory]
    [InlineData(0.101)]
    [InlineData(0.5)]
    public void GetClsRating_AboveWarningThreshold_ReturnsPoor(double cls)
    {
        var rating = PerformanceThresholds.GetClsRating(cls);

        Assert.Equal("Poor", rating);
    }
}
