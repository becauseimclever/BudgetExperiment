// <copyright file="BarSeriesDefinition.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Client.Components.Charts;

/// <summary>
/// Defines a bar series for grouped or stacked charts.
/// </summary>
public sealed record BarSeriesDefinition
{
    /// <summary>
    /// Gets the series identifier.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the series display label.
    /// </summary>
    public required string Label { get; init; }

    /// <summary>
    /// Gets the series color.
    /// </summary>
    public required string Color { get; init; }
}
