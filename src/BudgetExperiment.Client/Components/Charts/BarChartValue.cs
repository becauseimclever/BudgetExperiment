// <copyright file="BarChartValue.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Client.Components.Charts;

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
