// <copyright file="PerformanceThresholds.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.E2E.Tests.Helpers;

/// <summary>
/// Defines performance thresholds for Core Web Vitals based on Google's recommendations.
/// </summary>
public static class PerformanceThresholds
{
    /// <summary>
    /// First Contentful Paint thresholds in milliseconds.
    /// Good: &lt; 1000ms, Warning: 1000-1500ms, Fail: &gt; 1500ms.
    /// </summary>
    public static class Fcp
    {
        /// <summary>
        /// Good threshold (under this is excellent).
        /// </summary>
        public const double GoodMs = 1000;

        /// <summary>
        /// Warning threshold (above this needs attention).
        /// </summary>
        public const double WarningMs = 1500;

        /// <summary>
        /// Fail threshold (above this fails the test).
        /// </summary>
        public const double FailMs = 1500;
    }

    /// <summary>
    /// Largest Contentful Paint thresholds in milliseconds.
    /// Good: &lt; 2000ms, Warning: 2000-2500ms, Fail: &gt; 2500ms.
    /// </summary>
    public static class Lcp
    {
        /// <summary>
        /// Good threshold (under this is excellent).
        /// </summary>
        public const double GoodMs = 2000;

        /// <summary>
        /// Warning threshold (above this needs attention).
        /// </summary>
        public const double WarningMs = 2500;

        /// <summary>
        /// Fail threshold (above this fails the test).
        /// </summary>
        public const double FailMs = 2500;
    }

    /// <summary>
    /// Time to Interactive thresholds in milliseconds.
    /// Good: &lt; 2500ms, Warning: 2500-3000ms, Fail: &gt; 3000ms.
    /// </summary>
    public static class Tti
    {
        /// <summary>
        /// Good threshold (under this is excellent).
        /// </summary>
        public const double GoodMs = 2500;

        /// <summary>
        /// Warning threshold (above this needs attention).
        /// </summary>
        public const double WarningMs = 3000;

        /// <summary>
        /// Fail threshold (above this fails the test).
        /// </summary>
        public const double FailMs = 3000;
    }

    /// <summary>
    /// Cumulative Layout Shift thresholds (unitless score).
    /// Good: &lt; 0.05, Warning: 0.05-0.1, Fail: &gt; 0.1.
    /// </summary>
    public static class Cls
    {
        /// <summary>
        /// Good threshold (under this is excellent).
        /// </summary>
        public const double Good = 0.05;

        /// <summary>
        /// Warning threshold (above this needs attention).
        /// </summary>
        public const double Warning = 0.1;

        /// <summary>
        /// Fail threshold (above this fails the test).
        /// </summary>
        public const double Fail = 0.1;
    }

    /// <summary>
    /// Gets a rating string for a given FCP value.
    /// </summary>
    /// <param name="fcpMs">FCP in milliseconds.</param>
    /// <returns>Rating: "Good", "Needs Improvement", or "Poor".</returns>
    public static string GetFcpRating(double fcpMs) =>
        fcpMs <= Fcp.GoodMs ? "Good" :
        fcpMs <= Fcp.WarningMs ? "Needs Improvement" :
        "Poor";

    /// <summary>
    /// Gets a rating string for a given LCP value.
    /// </summary>
    /// <param name="lcpMs">LCP in milliseconds.</param>
    /// <returns>Rating: "Good", "Needs Improvement", or "Poor".</returns>
    public static string GetLcpRating(double lcpMs) =>
        lcpMs <= Lcp.GoodMs ? "Good" :
        lcpMs <= Lcp.WarningMs ? "Needs Improvement" :
        "Poor";

    /// <summary>
    /// Gets a rating string for a given TTI value.
    /// </summary>
    /// <param name="ttiMs">TTI in milliseconds.</param>
    /// <returns>Rating: "Good", "Needs Improvement", or "Poor".</returns>
    public static string GetTtiRating(double ttiMs) =>
        ttiMs <= Tti.GoodMs ? "Good" :
        ttiMs <= Tti.WarningMs ? "Needs Improvement" :
        "Poor";

    /// <summary>
    /// Gets a rating string for a given CLS value.
    /// </summary>
    /// <param name="cls">CLS score.</param>
    /// <returns>Rating: "Good", "Needs Improvement", or "Poor".</returns>
    public static string GetClsRating(double cls) =>
        cls <= Cls.Good ? "Good" :
        cls <= Cls.Warning ? "Needs Improvement" :
        "Poor";
}
