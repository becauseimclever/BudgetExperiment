// <copyright file="WaterfallChart.razor.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Globalization;

using BudgetExperiment.Client.Components.Charts.Models;
using Microsoft.AspNetCore.Components;

namespace BudgetExperiment.Client.Components.Charts;

/// <summary>
/// SVG waterfall chart for visualizing incremental budget changes across categories.
/// </summary>
public partial class WaterfallChart
{
    private IReadOnlyList<BarRenderInfo> _computedBars = [];
    private IReadOnlyList<ConnectorRenderInfo> _computedConnectors = [];
    private bool _isEmpty = true;
    private double _axisY;

    /// <summary>
    /// Gets or sets the list of waterfall segments to render.
    /// </summary>
    [Parameter]
    public IReadOnlyList<WaterfallSegment>? Segments { get; set; }

    /// <summary>
    /// Gets or sets the accessibility label for the chart.
    /// </summary>
    [Parameter]
    public string AriaLabel { get; set; } = "Budget waterfall chart";

    private static double ViewBoxHeight => 200;

    private static double MarginLeft => 45;

    private static double MarginRight => 10;

    private static double MarginTop => 10;

    private static double MarginBottom => 24;

    private static double ChartAreaBottom => ViewBoxHeight - MarginBottom;

    private static double ChartAreaHeight => ViewBoxHeight - MarginTop - MarginBottom;

    private static double ChartAreaWidth => 400 - MarginLeft - MarginRight;

    private static double PlotRight => 400 - MarginRight;

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        _isEmpty = Segments == null || Segments.Count == 0;
        if (_isEmpty)
        {
            _computedBars = [];
            _computedConnectors = [];
            _axisY = ChartAreaBottom;
            return;
        }

        var (minVal, maxVal) = ComputeValueRange();
        _axisY = MapY(0, minVal, maxVal);
        _computedBars = BuildBars(minVal, maxVal);
        _computedConnectors = BuildConnectors(_computedBars);
    }

    private static string F(double value) => value.ToString(CultureInfo.InvariantCulture);

    private static Microsoft.AspNetCore.Components.MarkupString RenderSvgText(
        double x, double y, string cssClass, string textAnchor, string fontSize, string content)
    {
        return new Microsoft.AspNetCore.Components.MarkupString(
            $"<text x=\"{F(x)}\" y=\"{F(y)}\" class=\"{cssClass}\" text-anchor=\"{textAnchor}\" font-size=\"{fontSize}\">{content}</text>");
    }

    private static double MapY(double value, double minVal, double maxVal)
    {
        var range = maxVal - minVal;
        var ratio = range > 0 ? (value - minVal) / range : 0;
        return ChartAreaBottom - (ratio * ChartAreaHeight);
    }

    private static IReadOnlyList<ConnectorRenderInfo> BuildConnectors(IReadOnlyList<BarRenderInfo> bars)
    {
        if (bars.Count < 2)
        {
            return [];
        }

        var connectors = new List<ConnectorRenderInfo>(bars.Count - 1);
        for (var i = 0; i < bars.Count - 1; i++)
        {
            connectors.Add(new ConnectorRenderInfo(
                X1: bars[i].BarRightX,
                X2: bars[i + 1].BarLeftX,
                Y: bars[i].ConnectorY));
        }

        return connectors;
    }

    private (double MinVal, double MaxVal) ComputeValueRange()
    {
        var allValues = new List<double> { 0 };
        foreach (var seg in Segments!)
        {
            allValues.Add((double)seg.RunningTotal);
            allValues.Add(seg.IsTotal ? 0.0 : (double)(seg.RunningTotal - seg.Amount));
        }

        var min = allValues.Min();
        var max = allValues.Max();
        if (max <= min)
        {
            max = min + 1;
        }

        return (min, max);
    }

    private IReadOnlyList<BarRenderInfo> BuildBars(double minVal, double maxVal)
    {
        var count = Segments!.Count;
        var slotWidth = ChartAreaWidth / count;
        var barWidth = slotWidth * 0.65;
        var bars = new List<BarRenderInfo>(count);

        for (var i = 0; i < count; i++)
        {
            var seg = Segments[i];
            var startVal = seg.IsTotal ? 0.0 : (double)(seg.RunningTotal - seg.Amount);
            var endVal = (double)seg.RunningTotal;
            var centerX = MarginLeft + ((i + 0.5) * slotWidth);
            var barX = centerX - (barWidth / 2);
            var topY = MapY(Math.Max(startVal, endVal), minVal, maxVal);
            var bottomY = MapY(Math.Min(startVal, endVal), minVal, maxVal);
            var height = Math.Max(1.0, bottomY - topY);

            var cssClass = seg.IsTotal
                ? "waterfall-bar waterfall-total"
                : seg.IsPositive ? "waterfall-bar waterfall-positive" : "waterfall-bar waterfall-negative";

            bars.Add(new BarRenderInfo(
                BarX: barX,
                BarY: topY,
                BarWidth: barWidth,
                BarHeight: height,
                CssClass: cssClass,
                LabelX: centerX,
                LabelY: ChartAreaBottom + 14,
                LabelText: seg.Label,
                ConnectorY: MapY((double)seg.RunningTotal, minVal, maxVal),
                BarRightX: barX + barWidth,
                BarLeftX: barX));
        }

        return bars;
    }

    private sealed record BarRenderInfo(
        double BarX,
        double BarY,
        double BarWidth,
        double BarHeight,
        string CssClass,
        double LabelX,
        double LabelY,
        string LabelText,
        double ConnectorY,
        double BarRightX,
        double BarLeftX);

    private sealed record ConnectorRenderInfo(
        double X1,
        double X2,
        double Y);
}
