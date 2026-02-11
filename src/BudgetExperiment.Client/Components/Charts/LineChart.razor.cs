// <copyright file="LineChart.razor.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Globalization;
using System.Text;
using BudgetExperiment.Client.Components.Charts.Shared;
using Microsoft.AspNetCore.Components;

namespace BudgetExperiment.Client.Components.Charts;

/// <summary>
/// Pure SVG line chart component for rendering time series data.
/// </summary>
public partial class LineChart
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
    /// Gets or sets a value indicating whether to fill area under the line.
    /// </summary>
    [Parameter]
    public bool ShowArea { get; set; } = false;

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
    public string AriaLabel { get; set; } = "Line chart";

    /// <summary>
    /// Gets or sets the event callback when a point is clicked.
    /// </summary>
    [Parameter]
    public EventCallback<LineData> OnPointClick { get; set; }

    private ChartPoint? HoveredPoint { get; set; }

    private static double MarginLeft => 46;

    private static double MarginRight => 12;

    private static double MarginTop => 12;

    private static double MarginBottom => 30;

    private static double ViewBoxHeight => 200;

    private double ChartAreaBottom => ViewBoxHeight - MarginBottom;

    private double ChartAreaHeight => ViewBoxHeight - MarginTop - MarginBottom;

    private double ChartAreaWidth => ComputedViewBoxWidth - MarginLeft - MarginRight;

    private double ComputedViewBoxWidth => Math.Max(MarginLeft + MarginRight + (Data.Count * 48), 280);

    private double XStep => Data.Count > 1 ? ChartAreaWidth / (Data.Count - 1) : 0;

    private IReadOnlyList<LineSeriesDefinition> SeriesDefinitions
    {
        get
        {
            if (Series is { Count: > 0 })
            {
                return Series;
            }

            return
            [
                new LineSeriesDefinition
                {
                    Id = "default",
                    Label = "Value",
                    Color = "#2563eb",
                },
            ];
        }
    }

    private decimal ComputedMinY
    {
        get
        {
            var values = GetAllValues();
            if (!values.Any())
            {
                return 0m;
            }

            var min = values.Min();
            return MinY ?? min;
        }
    }

    private decimal ComputedMaxY
    {
        get
        {
            var values = GetAllValues();
            if (!values.Any())
            {
                return 1m;
            }

            var max = values.Max();
            return MaxY ?? max;
        }
    }

    private IReadOnlyList<ChartTick> ComputedYTicks
    {
        get
        {
            var min = ComputedMinY;
            var max = ComputedMaxY;
            if (max <= min)
            {
                max = min + 1m;
            }

            var range = max - min;
            var step = CalculateNiceStep(range);
            var ticks = new List<ChartTick>();

            for (decimal tick = min; tick <= max; tick += step)
            {
                ticks.Add(new ChartTick
                {
                    Position = MapValueToY(tick),
                    Label = FormatAxisValue(tick),
                });
            }

            return ticks;
        }
    }

    private IReadOnlyList<ChartTick> ComputedXTicks
    {
        get
        {
            var ticks = new List<ChartTick>();
            if (Data.Count == 0)
            {
                return ticks;
            }

            var maxLabels = 8;
            var step = Data.Count <= maxLabels ? 1 : (int)Math.Ceiling(Data.Count / (double)maxLabels);

            for (var i = 0; i < Data.Count; i += step)
            {
                ticks.Add(new ChartTick
                {
                    Position = GetXForIndex(i),
                    Label = Data[i].Label,
                });
            }

            if (Data.Count > 1 && ticks.Last().Label != Data[^1].Label)
            {
                ticks.Add(new ChartTick
                {
                    Position = GetXForIndex(Data.Count - 1),
                    Label = Data[^1].Label,
                });
            }

            return ticks;
        }
    }

    private IReadOnlyList<ReferenceLineInfo> ComputedReferenceLines
    {
        get
        {
            if (ReferenceLines is null || ReferenceLines.Count == 0)
            {
                return [];
            }

            return ReferenceLines.Select(reference => new ReferenceLineInfo
            {
                Y = MapValueToY(reference.Value),
                Label = reference.Label,
                Color = reference.Color,
            }).ToList();
        }
    }

    private IReadOnlyList<LineSeriesInfo> ComputedSeries
    {
        get
        {
            var results = new List<LineSeriesInfo>();
            var min = ComputedMinY;
            var max = ComputedMaxY;
            if (max <= min)
            {
                max = min + 1m;
            }

            foreach (var definition in SeriesDefinitions)
            {
                var points = new List<ChartPoint>();

                for (var index = 0; index < Data.Count; index++)
                {
                    if (!TryGetValue(Data[index], definition.Id, out var value))
                    {
                        continue;
                    }

                    points.Add(new ChartPoint
                    {
                        Index = index,
                        Label = Data[index].Label,
                        Value = value,
                        X = GetXForIndex(index),
                        Y = MapValueToY(value),
                        SeriesLabel = definition.Label,
                        FormattedValue = FormatValue(value),
                        AriaLabel = $"{definition.Label}: {FormatValue(value)} at {Data[index].Label}",
                        Data = Data[index],
                    });
                }

                if (points.Count == 0)
                {
                    continue;
                }

                var path = BuildPath(points);
                var areaPath = ShowArea ? BuildAreaPath(points, path) : null;

                results.Add(new LineSeriesInfo
                {
                    Definition = definition,
                    Points = points,
                    Path = path,
                    AreaPath = areaPath,
                });
            }

            return results;
        }
    }

    private bool TryGetValue(LineData data, string seriesId, out decimal value)
    {
        if (Series is null || Series.Count == 0)
        {
            value = data.Value;
            return true;
        }

        if (data.SeriesValues is null)
        {
            value = 0m;
            return false;
        }

        return data.SeriesValues.TryGetValue(seriesId, out value);
    }

    private List<decimal> GetAllValues()
    {
        var values = new List<decimal>();
        foreach (var point in Data)
        {
            if (Series is null || Series.Count == 0)
            {
                values.Add(point.Value);
                continue;
            }

            if (point.SeriesValues is null)
            {
                continue;
            }

            values.AddRange(point.SeriesValues.Values);
        }

        return values;
    }

    private double GetXForIndex(int index)
    {
        if (Data.Count <= 1)
        {
            return MarginLeft + (ChartAreaWidth / 2);
        }

        return MarginLeft + (index * XStep);
    }

    private double MapValueToY(decimal value)
    {
        var min = ComputedMinY;
        var max = ComputedMaxY;
        var range = max - min;
        if (range <= 0)
        {
            return MarginTop + (ChartAreaHeight / 2);
        }

        var ratio = (double)((value - min) / range);
        return MarginTop + ChartAreaHeight - (ratio * ChartAreaHeight);
    }

    private static decimal CalculateNiceStep(decimal range)
    {
        if (range <= 0)
        {
            return 1m;
        }

        var rough = range / 4m;
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


    private string BuildPath(IReadOnlyList<ChartPoint> points)
    {
        if (points.Count == 0)
        {
            return string.Empty;
        }

        if (string.Equals(Interpolation, "smooth", StringComparison.OrdinalIgnoreCase))
        {
            return BuildSmoothPath(points);
        }

        var sb = new StringBuilder();
        sb.Append($"M {F(points[0].X)} {F(points[0].Y)}");

        for (var i = 1; i < points.Count; i++)
        {
            sb.Append($" L {F(points[i].X)} {F(points[i].Y)}");
        }

        return sb.ToString();
    }

    private string BuildSmoothPath(IReadOnlyList<ChartPoint> points)
    {
        if (points.Count < 2)
        {
            return BuildPath(points);
        }

        var sb = new StringBuilder();
        sb.Append($"M {F(points[0].X)} {F(points[0].Y)}");

        for (var i = 0; i < points.Count - 1; i++)
        {
            var p0 = i > 0 ? points[i - 1] : points[i];
            var p1 = points[i];
            var p2 = points[i + 1];
            var p3 = i + 2 < points.Count ? points[i + 2] : p2;

            var cp1x = p1.X + (p2.X - p0.X) / 6;
            var cp1y = p1.Y + (p2.Y - p0.Y) / 6;
            var cp2x = p2.X - (p3.X - p1.X) / 6;
            var cp2y = p2.Y - (p3.Y - p1.Y) / 6;

            sb.Append($" C {F(cp1x)} {F(cp1y)}, {F(cp2x)} {F(cp2y)}, {F(p2.X)} {F(p2.Y)}");
        }

        return sb.ToString();
    }

    private string BuildAreaPath(IReadOnlyList<ChartPoint> points, string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return string.Empty;
        }

        var bottomY = ChartAreaBottom;
        var sb = new StringBuilder();
        sb.Append(path);
        sb.Append($" L {F(points[^1].X)} {F(bottomY)}");
        sb.Append($" L {F(points[0].X)} {F(bottomY)} Z");
        return sb.ToString();
    }

    private void HandlePointHover(ChartPoint point)
    {
        point.FormattedValue = FormatValue(point.Value);
        point.AriaLabel = $"{point.SeriesLabel}: {point.FormattedValue} at {point.Label}";
        HoveredPoint = point;
    }

    private void HandlePointHoverEnd()
    {
        HoveredPoint = null;
    }

    private void HandlePointClick(ChartPoint point)
    {
        if (OnPointClick.HasDelegate)
        {
            OnPointClick.InvokeAsync(point.Data);
        }
    }

    private sealed class LineSeriesInfo
    {
        public LineSeriesDefinition Definition { get; set; } = null!;

        public List<ChartPoint> Points { get; set; } = [];

        public string Path { get; set; } = string.Empty;

        public string? AreaPath { get; set; }
    }

    private sealed class ChartPoint
    {
        public int Index { get; set; }

        public string Label { get; set; } = string.Empty;

        public decimal Value { get; set; }

        public double X { get; set; }

        public double Y { get; set; }

        public string SeriesLabel { get; set; } = string.Empty;

        public string FormattedValue { get; set; } = string.Empty;

        public string AriaLabel { get; set; } = string.Empty;

        public LineData Data { get; set; } = null!;
    }

    private sealed class ReferenceLineInfo
    {
        public double Y { get; set; }

        public string Label { get; set; } = string.Empty;

        public string Color { get; set; } = "#9ca3af";
    }
}
