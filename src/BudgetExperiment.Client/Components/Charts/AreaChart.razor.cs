// <copyright file="AreaChart.razor.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using Microsoft.AspNetCore.Components;

namespace BudgetExperiment.Client.Components.Charts;

/// <summary>
/// Filled area chart component for trend visualization.
/// </summary>
public partial class AreaChart
{
    /// <summary>
    /// Gets or sets the data points to render.
    /// </summary>
    [Parameter]
    public IReadOnlyList<LineData> Data { get; set; } = [];

    /// <summary>
    /// Gets or sets the series definitions for multi-line charts.
    /// </summary>
    [Parameter]
    public IReadOnlyList<LineSeriesDefinition>? Series { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to show points on the line.
    /// </summary>
    [Parameter]
    public bool ShowPoints { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to use gradient fills for the area.
    /// </summary>
    [Parameter]
    public bool UseGradientFill { get; set; } = true;

    /// <summary>
    /// Gets or sets the interpolation mode. Supported values: linear, smooth.
    /// </summary>
    [Parameter]
    public string Interpolation { get; set; } = "linear";

    /// <summary>
    /// Gets or sets a value indicating whether to render grid lines.
    /// </summary>
    [Parameter]
    public bool ShowGrid { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to render the X axis labels.
    /// </summary>
    [Parameter]
    public bool ShowXAxis { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to render the Y axis labels.
    /// </summary>
    [Parameter]
    public bool ShowYAxis { get; set; } = true;

    /// <summary>
    /// Gets or sets the X axis label.
    /// </summary>
    [Parameter]
    public string XAxisLabel { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Y axis label.
    /// </summary>
    [Parameter]
    public string YAxisLabel { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional minimum Y value.
    /// </summary>
    [Parameter]
    public decimal? MinY { get; set; }

    /// <summary>
    /// Gets or sets the optional maximum Y value.
    /// </summary>
    [Parameter]
    public decimal? MaxY { get; set; }

    /// <summary>
    /// Gets or sets the optional reference lines.
    /// </summary>
    [Parameter]
    public IReadOnlyList<ReferenceLine>? ReferenceLines { get; set; }

    /// <summary>
    /// Gets or sets the accessibility label.
    /// </summary>
    [Parameter]
    public string AriaLabel { get; set; } = "Area chart";

    /// <summary>
    /// Gets or sets the event callback when a point is clicked.
    /// </summary>
    [Parameter]
    public EventCallback<LineData> OnPointClick { get; set; }
}
