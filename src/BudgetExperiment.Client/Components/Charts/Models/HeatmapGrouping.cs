// <copyright file="HeatmapGrouping.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Client.Components.Charts.Models;

/// <summary>
/// Controls how columns are aggregated in a spending heatmap.
/// </summary>
public enum HeatmapGrouping
{
    /// <summary>Columns represent individual calendar weeks.</summary>
    DayOfWeekByWeek = 0,

    /// <summary>Columns represent individual calendar months.</summary>
    DayOfWeekByMonth,
}
