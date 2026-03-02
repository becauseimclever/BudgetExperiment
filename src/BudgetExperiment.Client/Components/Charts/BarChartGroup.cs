// <copyright file="BarChartGroup.cs" company="BecauseImClever">
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
