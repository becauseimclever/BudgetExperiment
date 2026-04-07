// <copyright file="HeatmapDayResponse.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// Spending and intensity data for a single calendar day in the heatmap response.
/// </summary>
public sealed class HeatmapDayResponse
{
    /// <summary>Gets or sets the total spend for this day.</summary>
    public decimal Spend { get; set; }

    /// <summary>
    /// Gets or sets the relative spending intensity.
    /// Valid values: <c>"None"</c>, <c>"Low"</c>, <c>"Moderate"</c>, <c>"High"</c>.
    /// </summary>
    public string Intensity { get; set; } = "None";
}
