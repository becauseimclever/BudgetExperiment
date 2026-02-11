// <copyright file="LineSeriesDefinition.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Client.Components.Charts;

/// <summary>
/// Defines a line series for multi-series charts.
/// </summary>
public sealed record LineSeriesDefinition
{
    /// <summary>
    /// Gets the series identifier.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the label for the series.
    /// </summary>
    public required string Label { get; init; }

    /// <summary>
    /// Gets the series color.
    /// </summary>
    public required string Color { get; init; }

    /// <summary>
    /// Gets a value indicating whether the line is dashed.
    /// </summary>
    public bool Dashed { get; init; }
}
