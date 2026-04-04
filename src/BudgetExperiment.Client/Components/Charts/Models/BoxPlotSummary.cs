// <copyright file="BoxPlotSummary.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Client.Components.Charts.Models;

/// <summary>
/// Represents the statistical distribution of spending for a single category, used in box plot charts.
/// </summary>
/// <param name="CategoryName">The name of the spending category.</param>
/// <param name="Minimum">The minimum non-outlier value.</param>
/// <param name="Q1">The first quartile (25th percentile).</param>
/// <param name="Median">The median value (50th percentile).</param>
/// <param name="Q3">The third quartile (75th percentile).</param>
/// <param name="Maximum">The maximum non-outlier value.</param>
/// <param name="Outliers">Outlier values detected by the 1.5×IQR method.</param>
public sealed record BoxPlotSummary(
    string CategoryName,
    decimal Minimum,
    decimal Q1,
    decimal Median,
    decimal Q3,
    decimal Maximum,
    IReadOnlyList<decimal> Outliers);
