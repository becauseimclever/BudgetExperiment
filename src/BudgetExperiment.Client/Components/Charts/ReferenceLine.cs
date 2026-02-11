// <copyright file="ReferenceLine.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Client.Components.Charts;

/// <summary>
/// Reference line definition for charts.
/// </summary>
public sealed record ReferenceLine
{
    /// <summary>
    /// Gets the value for the reference line.
    /// </summary>
    public required decimal Value { get; init; }

    /// <summary>
    /// Gets the label for the reference line.
    /// </summary>
    public required string Label { get; init; }

    /// <summary>
    /// Gets the reference line color.
    /// </summary>
    public string Color { get; init; } = "#9ca3af";
}
