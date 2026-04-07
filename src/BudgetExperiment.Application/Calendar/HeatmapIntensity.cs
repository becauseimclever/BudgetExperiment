// <copyright file="HeatmapIntensity.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Application.Calendar;

/// <summary>
/// Relative spending intensity for a single calendar day's heatmap overlay.
/// </summary>
public enum HeatmapIntensity
{
    /// <summary>No spending on this day.</summary>
    None = 0,

    /// <summary>Spending is 0–50% of the daily average.</summary>
    Low = 1,

    /// <summary>Spending is 50–100% of the daily average.</summary>
    Moderate = 2,

    /// <summary>Spending exceeds the daily average.</summary>
    High = 3,
}
