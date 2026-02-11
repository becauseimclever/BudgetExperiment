// <copyright file="LineData.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Client.Components.Charts;

/// <summary>
/// Represents a single line chart data point.
/// </summary>
public sealed record LineData
{
    /// <summary>
    /// Gets the label for the X axis.
    /// </summary>
    public required string Label { get; init; }

    /// <summary>
    /// Gets the value for single-series charts.
    /// </summary>
    public required decimal Value { get; init; }

    /// <summary>
    /// Gets the values for multi-series charts.
    /// </summary>
    public IReadOnlyDictionary<string, decimal>? SeriesValues { get; init; }
}
