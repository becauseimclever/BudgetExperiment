// <copyright file="BarChart.razor.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace BudgetExperiment.Client.Components.Charts;

/// <summary>
/// Pure SVG bar chart component for rendering grouped bars.
/// </summary>
public partial class BarChart
{
    /// <summary>
    /// Gets or sets the bar groups to display.
    /// </summary>
    [Parameter]
    public IReadOnlyList<BarChartGroup> Groups { get; set; } = [];

    /// <summary>
    /// Gets or sets the series definitions for the legend.
    /// </summary>
    [Parameter]
    public IReadOnlyList<BarChartSeries> Series { get; set; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether to show the legend.
    /// </summary>
    [Parameter]
    public bool ShowLegend { get; set; } = true;

    /// <summary>
    /// Gets or sets the accessibility label for the chart.
    /// </summary>
    [Parameter]
    public string AriaLabel { get; set; } = "Bar chart";

    private string? HoveredSeriesName { get; set; }

    private string? HoveredGroupLabel { get; set; }

    private decimal HoveredValue { get; set; }

    private static double MarginLeft => 45;

    private static double MarginRight => 10;

    private static double MarginTop => 10;

    private static double MarginBottom => 24;

    private static double ViewBoxHeight => 180;

    private double ChartAreaBottom => ViewBoxHeight - MarginBottom;

    private double ChartAreaHeight => ViewBoxHeight - MarginTop - MarginBottom;

    private double ComputedViewBoxWidth => Math.Max(MarginLeft + MarginRight + (Groups.Count * 50), 200);

    private double ComputedGroupWidth => Groups.Count > 0
        ? (ComputedViewBoxWidth - MarginLeft - MarginRight) / Groups.Count
        : 50;

    private decimal ComputedMaxValue
    {
        get
        {
            var max = Groups.SelectMany(g => g.Values).Select(v => v.Value).DefaultIfEmpty(0).Max();
            return max > 0 ? max * 1.1m : 100m;
        }
    }

    private List<TickInfo> ComputedYTicks
    {
        get
        {
            var maxVal = ComputedMaxValue;
            if (maxVal <= 0)
            {
                return [new TickInfo { Y = ChartAreaBottom, Label = "0" }];
            }

            var step = CalculateNiceStep(maxVal);
            var ticks = new List<TickInfo>();
            for (decimal tick = 0; tick <= maxVal; tick += step)
            {
                var ratio = maxVal > 0 ? (double)(tick / maxVal) : 0;
                var y = MarginTop + ChartAreaHeight - (ratio * ChartAreaHeight);
                ticks.Add(new TickInfo { Y = y, Label = FormatAxisValue(tick) });
            }

            return ticks;
        }
    }

    private List<BarGroupInfo> ComputedBars
    {
        get
        {
            var result = new List<BarGroupInfo>();
            var groupWidth = ComputedGroupWidth;
            var groupPadding = groupWidth * 0.1;
            var maxVal = ComputedMaxValue;

            for (var gi = 0; gi < Groups.Count; gi++)
            {
                var group = Groups[gi];
                var groupX = MarginLeft + (gi * groupWidth) + groupPadding;
                var usableWidth = groupWidth - (groupPadding * 2);
                var barCount = group.Values.Count;
                var barWidth = barCount > 0 ? usableWidth / barCount : 0;
                var labelX = groupX + (usableWidth / 2);

                var rects = new List<BarRectInfo>();
                for (var bi = 0; bi < group.Values.Count; bi++)
                {
                    var bar = group.Values[bi];
                    var barX = groupX + (bi * barWidth) + 1;
                    var ratio = maxVal > 0 ? (double)(bar.Value / maxVal) : 0;
                    var barHeight = ratio * ChartAreaHeight;
                    var clampedHeight = Math.Max(barHeight, bar.Value > 0 ? 1 : 0);
                    var barY = MarginTop + ChartAreaHeight - clampedHeight;
                    var w = Math.Max(barWidth - 2, 1);

                    rects.Add(new BarRectInfo
                    {
                        X = barX,
                        Y = barY,
                        Width = w,
                        Height = clampedHeight,
                        Color = bar.Color,
                        GroupLabel = group.Label,
                        SeriesName = bar.Series,
                        Value = bar.Value,
                        AriaLabel = $"{bar.Series}: {FormatMoney(bar.Value)} for {group.Label}",
                    });
                }

                result.Add(new BarGroupInfo { GroupLabel = group.Label, LabelX = labelX, Rects = rects });
            }

            return result;
        }
    }

    private static decimal CalculateNiceStep(decimal max)
    {
        if (max <= 0)
        {
            return 1m;
        }

        var rough = max / 5m;
        var magnitude = (decimal)Math.Pow(10, Math.Floor(Math.Log10((double)rough)));
        var residual = rough / magnitude;

        return residual switch
        {
            < 1.5m => 1m * magnitude,
            < 3m => 2m * magnitude,
            < 7m => 5m * magnitude,
            _ => 10m * magnitude,
        };
    }

    private static string FormatAxisValue(decimal value)
    {
        return value switch
        {
            >= 1000000m => $"{value / 1000000m:N1}M",
            >= 1000m => $"{value / 1000m:N0}k",
            _ => value.ToString("N0"),
        };
    }

    private static string FormatMoney(decimal value)
    {
        return value.ToString("C2", CultureInfo.CurrentCulture);
    }

    private static string F(double value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    private MarkupString RenderSvgText(double x, double y, string cssClass, string textAnchor, string fontSize, string content)
    {
        var xStr = F(x);
        var yStr = F(y);
        return new MarkupString(
            $"<text x=\"{xStr}\" y=\"{yStr}\" class=\"{cssClass}\" text-anchor=\"{textAnchor}\" font-size=\"{fontSize}\">{content}</text>");
    }

    private void HandleBarHover(string groupLabel, string seriesName, decimal value)
    {
        HoveredGroupLabel = groupLabel;
        HoveredSeriesName = seriesName;
        HoveredValue = value;
    }

    private void HandleBarHoverEnd()
    {
        HoveredSeriesName = null;
        HoveredGroupLabel = null;
    }

    private sealed class TickInfo
    {
        public double Y { get; set; }

        public string Label { get; set; } = string.Empty;
    }

    private sealed class BarGroupInfo
    {
        public string GroupLabel { get; set; } = string.Empty;

        public double LabelX { get; set; }

        public List<BarRectInfo> Rects { get; set; } = [];
    }

    private sealed class BarRectInfo
    {
        public double X { get; set; }

        public double Y { get; set; }

        public double Width { get; set; }

        public double Height { get; set; }

        public string Color { get; set; } = string.Empty;

        public string GroupLabel { get; set; } = string.Empty;

        public string SeriesName { get; set; } = string.Empty;

        public decimal Value { get; set; }

        public string AriaLabel { get; set; } = string.Empty;
    }
}
