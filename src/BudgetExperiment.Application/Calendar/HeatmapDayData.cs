// <copyright file="HeatmapDayData.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Application.Calendar;

/// <summary>
/// Spending and intensity data for a single calendar day's heatmap overlay.
/// </summary>
public sealed class HeatmapDayData
{
    /// <summary>Gets or sets the total spend for this day.</summary>
    public decimal Spend { get; set; }

    /// <summary>Gets or sets the relative spending intensity.</summary>
    public HeatmapIntensity Intensity { get; set; }
}
