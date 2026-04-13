// <copyright file="BoxPlotChart.razor.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Globalization;

using BudgetExperiment.Client.Components.Charts.Models;

using Microsoft.AspNetCore.Components;

namespace BudgetExperiment.Client.Components.Charts;

/// <summary>
/// SVG box plot chart for visualizing the statistical distribution of spending per category.
/// </summary>
public partial class BoxPlotChart
{
    private IReadOnlyList<DistributionRenderInfo> _computedDistributions = [];
    private bool _isEmpty = true;

    /// <summary>
    /// Gets or sets the list of spending distributions to render.
    /// </summary>
    [Parameter]
    public IReadOnlyList<BoxPlotSummary>? Distributions
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the accessibility label for the chart.
    /// </summary>
    [Parameter]
    public string AriaLabel { get; set; } = "Category spending distribution";

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
        _isEmpty = Distributions == null || Distributions.Count == 0;
        _computedDistributions = _isEmpty ? [] : BuildDistributions();
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

    private (double MinVal, double MaxVal) ComputeValueRange()
    {
        var allValues = new List<double> { 0 };
        foreach (var d in Distributions!)
        {
            allValues.Add((double)d.Minimum);
            allValues.Add((double)d.Maximum);
            foreach (var o in d.Outliers)
            {
                allValues.Add((double)o);
            }
        }

        var min = allValues.Min();
        var max = allValues.Max();
        if (max <= min)
        {
            max = min + 1;
        }

        return (min, max);
    }

    private IReadOnlyList<DistributionRenderInfo> BuildDistributions()
    {
        var (minVal, maxVal) = ComputeValueRange();
        var count = Distributions!.Count;
        var slotWidth = ChartAreaWidth / count;
        var boxWidth = slotWidth * 0.5;
        var result = new List<DistributionRenderInfo>(count);

        for (var i = 0; i < count; i++)
        {
            var d = Distributions[i];
            var centerX = MarginLeft + ((i + 0.5) * slotWidth);
            var boxX = centerX - (boxWidth / 2);

            var q1Y = MapY((double)d.Q1, minVal, maxVal);
            var q3Y = MapY((double)d.Q3, minVal, maxVal);
            var medY = MapY((double)d.Median, minVal, maxVal);
            var minY = MapY((double)d.Minimum, minVal, maxVal);
            var maxY = MapY((double)d.Maximum, minVal, maxVal);

            var boxHeight = Math.Max(1.0, q1Y - q3Y);
            var outlierYs = d.Outliers.Select(o => MapY((double)o, minVal, maxVal)).ToList();

            result.Add(new DistributionRenderInfo(
                BoxX: boxX,
                BoxY: q3Y,
                BoxWidth: boxWidth,
                BoxHeight: boxHeight,
                MedianY: medY,
                CenterX: centerX,
                LowerWhiskerTop: q1Y,
                LowerWhiskerBottom: minY,
                UpperWhiskerTop: maxY,
                UpperWhiskerBottom: q3Y,
                OutlierYs: outlierYs,
                LabelX: centerX,
                LabelY: ChartAreaBottom + 14,
                LabelText: d.CategoryName));
        }

        return result;
    }

    private sealed record DistributionRenderInfo(
        double BoxX,
        double BoxY,
        double BoxWidth,
        double BoxHeight,
        double MedianY,
        double CenterX,
        double LowerWhiskerTop,
        double LowerWhiskerBottom,
        double UpperWhiskerTop,
        double UpperWhiskerBottom,
        IReadOnlyList<double> OutlierYs,
        double LabelX,
        double LabelY,
        string LabelText);
}
