// <copyright file="SparkLine.razor.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace BudgetExperiment.Client.Components.Charts;

/// <summary>
/// Inline sparkline chart for trend indication.
/// </summary>
public partial class SparkLine
{
    /// <summary>
    /// Gets or sets the values to plot.
    /// </summary>
    [Parameter]
    public IReadOnlyList<decimal> Values { get; set; } = [];

    /// <summary>
    /// Gets or sets the width of the sparkline.
    /// </summary>
    [Parameter]
    public int Width { get; set; } = 80;

    /// <summary>
    /// Gets or sets the height of the sparkline.
    /// </summary>
    [Parameter]
    public int Height { get; set; } = 20;

    /// <summary>
    /// Gets or sets a value indicating whether to show endpoint dots.
    /// </summary>
    [Parameter]
    public bool ShowEndpoints { get; set; } = true;

    /// <summary>
    /// Gets or sets the color for a positive trend.
    /// </summary>
    [Parameter]
    public string PositiveColor { get; set; } = "#22c55e";

    /// <summary>
    /// Gets or sets the color for a negative trend.
    /// </summary>
    [Parameter]
    public string NegativeColor { get; set; } = "#ef4444";

    /// <summary>
    /// Gets or sets the neutral color.
    /// </summary>
    [Parameter]
    public string NeutralColor { get; set; } = "#6b7280";

    /// <summary>
    /// Gets or sets a value indicating whether to color by trend direction.
    /// </summary>
    [Parameter]
    public bool ColorByTrend { get; set; } = true;

    /// <summary>
    /// Gets or sets the accessibility label.
    /// </summary>
    [Parameter]
    public string AriaLabel { get; set; } = "Sparkline";

    private double Padding => 2d;

    private string Points => BuildPoints();

    private (double StartX, double StartY, double EndX, double EndY)? EndpointPoints => BuildEndpoints();

    private string LineColor => ResolveLineColor();

    private string BuildPoints()
    {
        if (Values.Count == 0)
        {
            return string.Empty;
        }

        var (min, max) = GetMinMax();
        var range = max - min;
        var xStep = Values.Count > 1
            ? (Width - (Padding * 2)) / (Values.Count - 1)
            : 0;

        var points = new List<string>();
        for (var i = 0; i < Values.Count; i++)
        {
            var x = Padding + (i * xStep);
            var y = MapValueToY(Values[i], min, range);
            points.Add($"{F(x)},{F(y)}");
        }

        return string.Join(" ", points);
    }

    private (double StartX, double StartY, double EndX, double EndY)? BuildEndpoints()
    {
        if (Values.Count == 0)
        {
            return null;
        }

        var (min, max) = GetMinMax();
        var range = max - min;
        var xStep = Values.Count > 1
            ? (Width - (Padding * 2)) / (Values.Count - 1)
            : 0;

        var startX = Padding;
        var startY = MapValueToY(Values[0], min, range);
        var endX = Padding + ((Values.Count - 1) * xStep);
        var endY = MapValueToY(Values[^1], min, range);

        return (startX, startY, endX, endY);
    }

    private double MapValueToY(decimal value, decimal min, decimal range)
    {
        if (range == 0)
        {
            return Height / 2d;
        }

        var ratio = (double)((value - min) / range);
        var plotHeight = Height - (Padding * 2);
        return Padding + plotHeight - (ratio * plotHeight);
    }

    private (decimal Min, decimal Max) GetMinMax()
    {
        var min = Values.Min();
        var max = Values.Max();
        return (min, max == min ? min + 1m : max);
    }

    private string ResolveLineColor()
    {
        if (!ColorByTrend || Values.Count < 2)
        {
            return NeutralColor;
        }

        var delta = Values[^1] - Values[0];
        if (delta > 0)
        {
            return PositiveColor;
        }

        if (delta < 0)
        {
            return NegativeColor;
        }

        return NeutralColor;
    }

    private static string F(double value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }
}
