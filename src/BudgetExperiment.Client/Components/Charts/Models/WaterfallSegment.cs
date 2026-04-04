// <copyright file="WaterfallSegment.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Client.Components.Charts.Models;

/// <summary>
/// Represents one step in a waterfall chart.
/// </summary>
/// <param name="Label">The display label for this segment.</param>
/// <param name="Amount">The value change contributed by this segment.</param>
/// <param name="RunningTotal">The cumulative total after including this segment.</param>
/// <param name="IsTotal">Indicates whether this is a totals / summary bar rather than an incremental segment.</param>
public sealed record WaterfallSegment(string Label, decimal Amount, decimal RunningTotal, bool IsTotal)
{
    /// <summary>
    /// Gets a value indicating whether this segment represents a positive change (Amount &gt;= 0).
    /// </summary>
    public bool IsPositive => Amount >= 0;
}
