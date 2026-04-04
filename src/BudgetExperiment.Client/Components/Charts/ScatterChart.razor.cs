// <copyright file="ScatterChart.razor.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Globalization;

using BudgetExperiment.Client.Components.Charts.Models;
using Microsoft.AspNetCore.Components;

namespace BudgetExperiment.Client.Components.Charts;

/// <summary>
/// SVG scatter chart that plots transactions as dots with date on the X-axis and amount on the Y-axis.
/// </summary>
public partial class ScatterChart
{
    /// <summary>
    /// Gets or sets the list of data points to plot.
    /// </summary>
    [Parameter]
    public IReadOnlyList<ScatterDataPoint> Points { get; set; } = [];

    /// <summary>
    /// Gets or sets the accessibility label for the chart.
    /// </summary>
    [Parameter]
    public string AriaLabel { get; set; } = "Transaction scatter plot";

    /// <summary>
    /// Gets or sets the optional map of category names to fill colors used to color data points by category.
    /// </summary>
    [Parameter]
    public IReadOnlyDictionary<string, string>? CategoryColors { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to apply a distinct visual marker to outlier points.
    /// </summary>
    [Parameter]
    public bool ShowOutlierMarkers { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether CSS animations and transitions are enabled on the chart.
    /// Set to <c>false</c> to disable all animations (e.g. for reduced-motion scenarios or testing).
    /// </summary>
    [Parameter]
    public bool AnimationsEnabled { get; set; } = true;

    private static double ViewBoxWidth => 400;

    private static double ViewBoxHeight => 200;

    private static double MarginLeft => 45;

    private static double MarginRight => 10;

    private static double MarginTop => 10;

    private static double MarginBottom => 24;

    private string ContainerClass =>
        AnimationsEnabled ? "scatter-chart" : "scatter-chart chart-no-animation";

    private double ChartWidth => ViewBoxWidth - MarginLeft - MarginRight;

    private double ChartHeight => ViewBoxHeight - MarginTop - MarginBottom;

    private double ChartAreaBottom => ViewBoxHeight - MarginBottom;

    private long MinDateTicks => Points.Count > 0
        ? Points.Min(p => p.Date.ToDateTime(TimeOnly.MinValue).Ticks)
        : 0L;

    private long MaxDateTicks => Points.Count > 0
        ? Points.Max(p => p.Date.ToDateTime(TimeOnly.MinValue).Ticks)
        : 1L;

    private decimal ComputedMinAmount => Points.Count > 0 ? Points.Min(p => p.Amount) : 0m;

    private decimal ComputedMaxAmount => Points.Count > 0 ? Points.Max(p => p.Amount) : 100m;

    private static string F(double value) => value.ToString(CultureInfo.InvariantCulture);

    private double MapDateToX(DateOnly date)
    {
        var ticks = date.ToDateTime(TimeOnly.MinValue).Ticks;
        var range = MaxDateTicks - MinDateTicks;
        var ratio = range > 0 ? (double)(ticks - MinDateTicks) / range : 0.5;
        return MarginLeft + (ratio * ChartWidth);
    }

    private double MapAmountToY(decimal amount)
    {
        var amountRange = ComputedMaxAmount - ComputedMinAmount;
        var ratio = amountRange > 0 ? (double)((amount - ComputedMinAmount) / amountRange) : 0.5;
        return ChartAreaBottom - (ratio * ChartHeight);
    }

    private string GetCircleClass(ScatterDataPoint point)
    {
        return point.IsOutlier && ShowOutlierMarkers
            ? "scatter-point scatter-point-outlier"
            : "scatter-point";
    }

    private string GetPointFill(ScatterDataPoint point)
    {
        if (CategoryColors != null && CategoryColors.TryGetValue(point.Category, out var color))
        {
            return color;
        }

        return "var(--color-brand-primary, #6366F1)";
    }
}
