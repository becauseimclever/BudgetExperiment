// <copyright file="StackedBarChart.razor.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Globalization;
using BudgetExperiment.Client.Components.Charts.Shared;
using Microsoft.AspNetCore.Components;

namespace BudgetExperiment.Client.Components.Charts;

/// <summary>
/// Pure SVG stacked bar chart component for rendering multi-series bar data.
/// </summary>
public partial class StackedBarChart
{
    /// <summary>
    /// Gets or sets the grouped bar data.
    /// </summary>
    [Parameter]
    public IReadOnlyList<GroupedBarData> Data { get; set; } = [];

    /// <summary>
    /// Gets or sets the series definitions.
    /// </summary>
    [Parameter]
    public IReadOnlyList<BarSeriesDefinition> Series { get; set; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether to show group labels.
    /// </summary>
    [Parameter]
    public bool ShowLabels { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to show the legend.
    /// </summary>
    [Parameter]
    public bool ShowLegend { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to show grid lines.
    /// </summary>
    [Parameter]
    public bool ShowGrid { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to show Y axis labels.
    /// </summary>
    [Parameter]
    public bool ShowYAxis { get; set; } = true;

    /// <summary>
    /// Gets or sets the accessibility label.
    /// </summary>
    [Parameter]
    public string AriaLabel { get; set; } = "Stacked bar chart";

    /// <summary>
    /// Gets or sets the callback for segment clicks.
    /// </summary>
    [Parameter]
    public EventCallback<(GroupedBarData Group, string SeriesId)> OnSegmentClick { get; set; }

    private static double MarginLeft => 48;

    private static double MarginRight => 12;

    private static double MarginTop => 12;

    private static double MarginBottom => 28;

    private static double ViewBoxHeight => 190;

    private double ChartAreaBottom => ViewBoxHeight - MarginBottom;

    private double ChartAreaHeight => ViewBoxHeight - MarginTop - MarginBottom;

    private double ChartAreaWidth => ComputedViewBoxWidth - MarginLeft - MarginRight;

    private double ComputedViewBoxWidth => Math.Max(MarginLeft + MarginRight + (Data.Count * 64), 320);

    private IReadOnlyList<string> SeriesOrder => ResolveSeriesOrder();

    private decimal ComputedMaxTotal
    {
        get
        {
            if (Data.Count == 0)
            {
                return 100m;
            }

            var max = Data.Max(group => group.Values.Values.Sum());
            return max > 0 ? max * 1.1m : 100m;
        }
    }

    private IReadOnlyList<ChartTick> ComputedYTicks => BuildYTicks();

    private IReadOnlyList<ChartTick> ComputedXTicks => BuildXTicks();

    private IReadOnlyList<GroupInfo> ComputedGroups => BuildGroups();

    private HoveredSegmentInfo? HoveredSegment { get; set; }

    private IReadOnlyList<string> ResolveSeriesOrder()
    {
        if (Series.Count > 0)
        {
            return Series.Select(s => s.Id).ToList();
        }

        return Data.SelectMany(group => group.Values.Keys).Distinct(StringComparer.Ordinal).ToList();
    }

    private IReadOnlyList<GroupInfo> BuildGroups()
    {
        var result = new List<GroupInfo>();
        var groupWidth = Data.Count > 0 ? ChartAreaWidth / Data.Count : ChartAreaWidth;
        var groupPadding = groupWidth * 0.2;
        var usableWidth = groupWidth - (groupPadding * 2);
        var barWidth = Math.Max(usableWidth, 6);
        var maxTotal = ComputedMaxTotal;
        var seriesIds = SeriesOrder;

        for (var gi = 0; gi < Data.Count; gi++)
        {
            var group = Data[gi];
            var x = MarginLeft + (gi * groupWidth) + groupPadding;
            var currentY = MarginTop + ChartAreaHeight;
            var segments = new List<StackedSegmentInfo>();

            foreach (var seriesId in seriesIds)
            {
                if (!group.Values.TryGetValue(seriesId, out var value))
                {
                    continue;
                }

                var ratio = maxTotal > 0 ? (double)(value / maxTotal) : 0;
                var height = ratio * ChartAreaHeight;
                var clampedHeight = Math.Max(height, value > 0 ? 1 : 0);
                currentY -= clampedHeight;

                var seriesLabel = GetSeriesLabel(seriesId);
                var formattedValue = FormatValue(value);

                segments.Add(new StackedSegmentInfo
                {
                    X = x,
                    Y = currentY,
                    Width = barWidth,
                    Height = clampedHeight,
                    Color = GetSeriesColor(seriesId),
                    SeriesId = seriesId,
                    SeriesLabel = seriesLabel,
                    GroupLabel = group.GroupLabel,
                    Group = group,
                    FormattedValue = formattedValue,
                    Value = value,
                    AriaLabel = $"{seriesLabel}: {formattedValue} for {group.GroupLabel}",
                });
            }

            result.Add(new GroupInfo
            {
                GroupLabel = group.GroupLabel,
                LabelX = x + (usableWidth / 2),
                Segments = segments,
            });
        }

        return result;
    }

    private IReadOnlyList<ChartTick> BuildYTicks()
    {
        var maxVal = ComputedMaxTotal;
        var step = CalculateNiceStep(maxVal);
        var ticks = new List<ChartTick>();

        for (decimal tick = 0; tick <= maxVal; tick += step)
        {
            ticks.Add(new ChartTick
            {
                Position = MapValueToY(tick),
                Label = FormatAxisValue(tick),
            });
        }

        return ticks;
    }

    private IReadOnlyList<ChartTick> BuildXTicks()
    {
        var ticks = new List<ChartTick>();
        if (Data.Count == 0)
        {
            return ticks;
        }

        var step = Data.Count <= 8 ? 1 : (int)Math.Ceiling(Data.Count / 8d);
        for (var i = 0; i < Data.Count; i += step)
        {
            var x = MarginLeft + (i * (ChartAreaWidth / Data.Count)) + ((ChartAreaWidth / Data.Count) / 2);
            ticks.Add(new ChartTick
            {
                Position = x,
                Label = Data[i].GroupLabel,
            });
        }

        return ticks;
    }

    private double MapValueToY(decimal value)
    {
        var ratio = ComputedMaxTotal > 0 ? (double)(value / ComputedMaxTotal) : 0;
        return MarginTop + ChartAreaHeight - (ratio * ChartAreaHeight);
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
            _ => value.ToString("N0", CultureInfo.CurrentCulture),
        };
    }

    private static string FormatValue(decimal value)
    {
        return value.ToString("C2", CultureInfo.CurrentCulture);
    }

    private string GetSeriesLabel(string seriesId)
    {
        return Series.FirstOrDefault(s => s.Id == seriesId)?.Label ?? seriesId;
    }

    private string GetSeriesColor(string seriesId)
    {
        return Series.FirstOrDefault(s => s.Id == seriesId)?.Color ?? "#6366f1";
    }

    private static string F(double value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    private MarkupString RenderSvgText(double x, double y, string cssClass, string textAnchor, string fontSize, string content)
    {
        return new MarkupString(
            $"<text x=\"{F(x)}\" y=\"{F(y)}\" class=\"{cssClass}\" text-anchor=\"{textAnchor}\" font-size=\"{fontSize}\">{content}</text>");
    }

    private void HandleSegmentHover(StackedSegmentInfo segment)
    {
        HoveredSegment = new HoveredSegmentInfo
        {
            GroupLabel = segment.GroupLabel,
            SeriesLabel = segment.SeriesLabel,
            FormattedValue = segment.FormattedValue,
        };
    }

    private void HandleSegmentHoverEnd()
    {
        HoveredSegment = null;
    }

    private void HandleSegmentClick(StackedSegmentInfo segment)
    {
        if (OnSegmentClick.HasDelegate)
        {
            OnSegmentClick.InvokeAsync((segment.Group, segment.SeriesId));
        }
    }

    private sealed class GroupInfo
    {
        public string GroupLabel { get; set; } = string.Empty;

        public double LabelX { get; set; }

        public List<StackedSegmentInfo> Segments { get; set; } = [];
    }

    private sealed class StackedSegmentInfo
    {
        public double X { get; set; }

        public double Y { get; set; }

        public double Width { get; set; }

        public double Height { get; set; }

        public string Color { get; set; } = string.Empty;

        public string SeriesId { get; set; } = string.Empty;

        public string SeriesLabel { get; set; } = string.Empty;

        public string GroupLabel { get; set; } = string.Empty;

        public string FormattedValue { get; set; } = string.Empty;

        public decimal Value { get; set; }

        public GroupedBarData Group { get; set; } = null!;

        public string AriaLabel { get; set; } = string.Empty;
    }

    private sealed class HoveredSegmentInfo
    {
        public string GroupLabel { get; set; } = string.Empty;

        public string SeriesLabel { get; set; } = string.Empty;

        public string FormattedValue { get; set; } = string.Empty;
    }
}
