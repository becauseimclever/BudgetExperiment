// <copyright file="BarChartSeries.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Client.Components.Charts;

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
