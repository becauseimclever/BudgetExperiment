// <copyright file="PerformanceMetrics.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.E2E.Tests.Helpers;

/// <summary>
/// Represents Core Web Vitals and performance metrics captured during page load.
/// </summary>
/// <param name="FirstContentfulPaintMs">Time to First Contentful Paint in milliseconds.</param>
/// <param name="LargestContentfulPaintMs">Time to Largest Contentful Paint in milliseconds.</param>
/// <param name="DomContentLoadedMs">Time when DOM content is fully loaded in milliseconds.</param>
/// <param name="LoadCompleteMs">Time when the page load event completes in milliseconds.</param>
/// <param name="CumulativeLayoutShift">Cumulative Layout Shift score (lower is better, target &lt; 0.1).</param>
/// <param name="TimeToInteractiveMs">Estimated Time to Interactive in milliseconds.</param>
public record PerformanceMetrics(
    double FirstContentfulPaintMs,
    double LargestContentfulPaintMs,
    double DomContentLoadedMs,
    double LoadCompleteMs,
    double CumulativeLayoutShift,
    double TimeToInteractiveMs)
{
    /// <summary>
    /// Gets the First Contentful Paint in seconds.
    /// </summary>
    public double FirstContentfulPaintSeconds => FirstContentfulPaintMs / 1000.0;

    /// <summary>
    /// Gets the Largest Contentful Paint in seconds.
    /// </summary>
    public double LargestContentfulPaintSeconds => LargestContentfulPaintMs / 1000.0;

    /// <summary>
    /// Gets the Time to Interactive in seconds.
    /// </summary>
    public double TimeToInteractiveSeconds => TimeToInteractiveMs / 1000.0;

    /// <summary>
    /// Returns a formatted summary of the performance metrics.
    /// </summary>
    /// <returns>A multi-line string summarizing all metrics.</returns>
    public string ToSummary()
    {
        return $"""
            === Core Web Vitals ===
            FCP:  {FirstContentfulPaintSeconds:F2}s ({FirstContentfulPaintMs:F0}ms)
            LCP:  {LargestContentfulPaintSeconds:F2}s ({LargestContentfulPaintMs:F0}ms)
            TTI:  {TimeToInteractiveSeconds:F2}s ({TimeToInteractiveMs:F0}ms)
            CLS:  {CumulativeLayoutShift:F4}
            DOM:  {DomContentLoadedMs:F0}ms
            Load: {LoadCompleteMs:F0}ms
            """;
    }
}
