// <copyright file="ChartTick.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Client.Components.Charts.Shared;

/// <summary>
/// Represents a tick on a chart axis or grid.
/// </summary>
public sealed class ChartTick
{
    /// <summary>
    /// Gets or sets the tick position.
    /// </summary>
    public double Position { get; set; }

    /// <summary>
    /// Gets or sets the tick label.
    /// </summary>
    public string Label { get; set; } = string.Empty;
}
