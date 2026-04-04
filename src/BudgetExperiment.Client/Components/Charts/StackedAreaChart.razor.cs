// <copyright file="StackedAreaChart.razor.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Globalization;
using System.Text;

using BudgetExperiment.Client.Components.Charts.Models;
using Microsoft.AspNetCore.Components;

namespace BudgetExperiment.Client.Components.Charts;

/// <summary>
/// SVG stacked area chart that visualizes cumulative multi-series values over time.
/// </summary>
public partial class StackedAreaChart
{
    /// <summary>
    /// Gets or sets the list of series to render as stacked filled areas.
    /// </summary>
    [Parameter]
    public IReadOnlyList<StackedAreaSeries>? Series { get; set; }

    /// <summary>
    /// Gets or sets the accessibility label for the chart.
    /// </summary>
    [Parameter]
    public string AriaLabel { get; set; } = "Stacked area chart";

    private static double ViewBoxWidth => 400;

    private static double ViewBoxHeight => 200;

    private static double MarginLeft => 45;

    private static double MarginRight => 10;

    private static double MarginTop => 10;

    private static double MarginBottom => 24;

    private static double ChartWidth => ViewBoxWidth - MarginLeft - MarginRight;

    private static double ChartHeight => ViewBoxHeight - MarginTop - MarginBottom;

    private static double ChartAreaBottom => ViewBoxHeight - MarginBottom;

    private static double PlotRight => ViewBoxWidth - MarginRight;

    private bool IsEmpty =>
        Series == null || Series.Count == 0 || Series.All(s => s.Points.Count == 0);

    private IReadOnlyList<DateOnly> AllDates
    {
        get
        {
            if (Series == null)
            {
                return [];
            }

            return Series
                .SelectMany(s => s.Points.Select(p => p.Date))
                .Distinct()
                .OrderBy(d => d)
                .ToList();
        }
    }

    private static double MapX(int index, int count)
    {
        if (count <= 1)
        {
            return MarginLeft + (ChartWidth / 2);
        }

        return MarginLeft + (((double)index / (count - 1)) * ChartWidth);
    }

    private static double MapY(double stackedValue, double maxTotal)
    {
        return ChartAreaBottom - ((stackedValue / maxTotal) * ChartHeight);
    }

    private static string F(double value) => value.ToString(CultureInfo.InvariantCulture);

    private double ComputedMaxTotalValue()
    {
        var dates = AllDates;
        if (dates.Count == 0 || Series == null || Series.Count == 0)
        {
            return 1.0;
        }

        double max = 0;
        foreach (var date in dates)
        {
            double total = 0;
            foreach (var s in Series)
            {
                var pt = s.Points.FirstOrDefault(p => p.Date == date);
                if (pt != null)
                {
                    total += (double)pt.Value;
                }
            }

            if (total > max)
            {
                max = total;
            }
        }

        return max > 0 ? max : 1.0;
    }

    private IReadOnlyList<StackedPathInfo> ComputeStackedPaths()
    {
        var dates = AllDates;
        if (dates.Count == 0 || Series == null || Series.Count == 0)
        {
            return [];
        }

        var count = dates.Count;
        var maxTotal = ComputedMaxTotalValue();
        var cumulative = new double[count];
        var results = new List<StackedPathInfo>(Series.Count);

        foreach (var series in Series)
        {
            var values = new double[count];
            for (var j = 0; j < count; j++)
            {
                var pt = series.Points.FirstOrDefault(p => p.Date == dates[j]);
                values[j] = pt != null ? (double)pt.Value : 0.0;
            }

            var baselines = (double[])cumulative.Clone();
            var tops = new double[count];
            for (var j = 0; j < count; j++)
            {
                tops[j] = baselines[j] + values[j];
                cumulative[j] = tops[j];
            }

            var sb = new StringBuilder();
            sb.Append($"M {F(MapX(0, count))},{F(MapY(tops[0], maxTotal))}");
            for (var j = 1; j < count; j++)
            {
                sb.Append($" L {F(MapX(j, count))},{F(MapY(tops[j], maxTotal))}");
            }

            for (var j = count - 1; j >= 0; j--)
            {
                sb.Append($" L {F(MapX(j, count))},{F(MapY(baselines[j], maxTotal))}");
            }

            sb.Append(" Z");

            results.Add(new StackedPathInfo(series.Color, sb.ToString()));
        }

        return results;
    }

    private sealed record StackedPathInfo(string Color, string PathData);
}
