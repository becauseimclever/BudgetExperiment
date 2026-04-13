// <copyright file="RadialBarChart.razor.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Globalization;

using BudgetExperiment.Client.Components.Charts.Models;

using Microsoft.AspNetCore.Components;

namespace BudgetExperiment.Client.Components.Charts;

/// <summary>
/// SVG radial bar chart that visualizes budget category utilization as concentric ring arcs.
/// </summary>
public partial class RadialBarChart
{
    /// <summary>
    /// Gets or sets the list of segments to render as concentric arcs.
    /// </summary>
    [Parameter]
    public IReadOnlyList<RadialBarSegment>? Segments
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the accessibility label for the chart.
    /// </summary>
    [Parameter]
    public string AriaLabel { get; set; } = "Budget utilization";

    /// <summary>
    /// Gets or sets the overall width and height of the SVG in pixels.
    /// </summary>
    [Parameter]
    public int Size { get; set; } = 200;

    private static double RingThickness => 16;

    private static double RingRadiusStep => 22;

    private static double OutermostRingOffset => 10;

    private bool IsEmpty => Segments == null || Segments.Count == 0;

    private double Center => Size / 2d;

    private static string F(double value) => value.ToString(CultureInfo.InvariantCulture);

    private IReadOnlyList<RingInfo> ComputeRingInfos()
    {
        if (Segments == null || Segments.Count == 0)
        {
            return [];
        }

        var center = Center;
        var infos = new List<RingInfo>(Segments.Count);

        for (var i = 0; i < Segments.Count; i++)
        {
            var seg = Segments[i];
            var r = (Size / 2d) - OutermostRingOffset - (i * RingRadiusStep);
            var circ = 2 * Math.PI * r;
            var offset = Math.Max(0d, circ * (1d - (double)(seg.Percentage / 100m)));
            var labelY = center - (i * RingRadiusStep) + 4;

            infos.Add(new RingInfo(
                Label: seg.Label,
                Color: seg.Color,
                CenterX: center,
                CenterY: center,
                Radius: r,
                Circumference: circ,
                DashOffset: offset,
                LabelX: center,
                LabelY: labelY));
        }

        return infos;
    }

    private MarkupString RenderSvgText(double x, double y, string cssClass, string textAnchor, string fontSize, string content)
    {
        return new MarkupString(
            $"<text x=\"{F(x)}\" y=\"{F(y)}\" class=\"{cssClass}\" text-anchor=\"{textAnchor}\" font-size=\"{fontSize}\">{content}</text>");
    }

    private sealed record RingInfo(
        string Label,
        string Color,
        double CenterX,
        double CenterY,
        double Radius,
        double Circumference,
        double DashOffset,
        double LabelX,
        double LabelY);
}
