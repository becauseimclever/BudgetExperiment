// <copyright file="GroupedBarData.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Client.Components.Charts;

/// <summary>
/// Represents grouped bar chart data for a single group.
/// </summary>
public sealed record GroupedBarData
{
    /// <summary>
    /// Gets the group identifier.
    /// </summary>
    public required string GroupId { get; init; }

    /// <summary>
    /// Gets the group label for the X axis.
    /// </summary>
    public required string GroupLabel { get; init; }

    /// <summary>
    /// Gets the series values for the group (SeriesId -> value).
    /// </summary>
    public required IReadOnlyDictionary<string, decimal> Values { get; init; }
}
