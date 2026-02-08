// <copyright file="BarChartData.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Client.Components.Charts;

/// <summary>
/// Data for a single bar group in a bar chart.
/// </summary>
public class BarChartGroup
{
    /// <summary>Gets or sets the label for this bar group (e.g., "Jan 2026").</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Gets or sets the bars in this group.</summary>
    public IReadOnlyList<BarChartValue> Values { get; set; } = [];
}

/// <summary>
/// A single bar value within a group.
/// </summary>
public class BarChartValue
{
    /// <summary>Gets or sets the series name (e.g., "Income", "Spending").</summary>
    public string Series { get; set; } = string.Empty;

    /// <summary>Gets or sets the value.</summary>
    public decimal Value { get; set; }

    /// <summary>Gets or sets the color.</summary>
    public string Color { get; set; } = "#6366f1";
}

/// <summary>
/// Legend item for the bar chart series.
/// </summary>
public class BarChartSeries
{
    /// <summary>Gets or sets the series name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the series color.</summary>
    public string Color { get; set; } = "#6366f1";
}
