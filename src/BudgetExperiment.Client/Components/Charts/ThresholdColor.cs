// <copyright file="ThresholdColor.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Client.Components.Charts;

/// <summary>
/// Represents a color threshold based on a percent value.
/// </summary>
public sealed record ThresholdColor
{
    /// <summary>
    /// Gets the minimum percent (0-100) for this threshold.
    /// </summary>
    public required decimal MinPercent { get; init; }

    /// <summary>
    /// Gets the color for this threshold.
    /// </summary>
    public required string Color { get; init; }

    /// <summary>
    /// Gets the optional label for this threshold.
    /// </summary>
    public string? Label { get; init; }
}
