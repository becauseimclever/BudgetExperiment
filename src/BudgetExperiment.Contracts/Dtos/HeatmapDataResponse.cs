// <copyright file="HeatmapDataResponse.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// Monthly spending heatmap data for the calendar overlay.
/// </summary>
public sealed class HeatmapDataResponse
{
    /// <summary>Gets or sets the average daily spend for the month (total spend / days with spend).</summary>
    public decimal DailyAverageSpend { get; set; }

    /// <summary>
    /// Gets or sets the per-day heatmap data, keyed by day-of-month (1-based).
    /// </summary>
    public Dictionary<int, HeatmapDayResponse> Days { get; set; } = new();
}
