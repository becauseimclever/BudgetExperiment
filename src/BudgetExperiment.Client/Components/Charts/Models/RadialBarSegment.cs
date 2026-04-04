// <copyright file="RadialBarSegment.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Client.Components.Charts.Models;

/// <summary>
/// Represents one ring segment in a radial bar chart (e.g. one budget category's utilization).
/// </summary>
/// <param name="Label">The display label for this segment.</param>
/// <param name="Value">The current value (e.g. amount spent).</param>
/// <param name="MaxValue">The maximum value (e.g. budget limit).</param>
/// <param name="Color">The stroke color (CSS color string) for the arc.</param>
public sealed record RadialBarSegment(string Label, decimal Value, decimal MaxValue, string Color)
{
    /// <summary>
    /// Gets the utilization percentage, capped at 150 % to prevent UI overflow.
    /// Returns 0 when <see cref="MaxValue"/> is zero or negative.
    /// </summary>
    public decimal Percentage => MaxValue > 0 ? Math.Min(Value / MaxValue * 100m, 150m) : 0m;
}
