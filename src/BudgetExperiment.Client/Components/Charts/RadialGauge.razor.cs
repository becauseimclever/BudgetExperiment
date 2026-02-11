// <copyright file="RadialGauge.razor.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace BudgetExperiment.Client.Components.Charts;

/// <summary>
/// Circular progress gauge component with threshold coloring.
/// </summary>
public partial class RadialGauge
{
    /// <summary>
    /// Gets or sets the current value.
    /// </summary>
    [Parameter]
    public decimal Value { get; set; }

    /// <summary>
    /// Gets or sets the maximum value.
    /// </summary>
    [Parameter]
    public decimal MaxValue { get; set; } = 100m;

    /// <summary>
    /// Gets or sets the size of the gauge.
    /// </summary>
    [Parameter]
    public int Size { get; set; } = 120;

    /// <summary>
    /// Gets or sets the stroke width.
    /// </summary>
    [Parameter]
    public int StrokeWidth { get; set; } = 10;

    /// <summary>
    /// Gets or sets a value indicating whether to show the label.
    /// </summary>
    [Parameter]
    public bool ShowLabel { get; set; } = true;

    /// <summary>
    /// Gets or sets the label.
    /// </summary>
    [Parameter]
    public string Label { get; set; } = "Progress";

    /// <summary>
    /// Gets or sets a value indicating whether to show percent text.
    /// </summary>
    [Parameter]
    public bool ShowPercent { get; set; } = true;

    /// <summary>
    /// Gets or sets the threshold colors.
    /// </summary>
    [Parameter]
    public IReadOnlyList<ThresholdColor>? Thresholds { get; set; }

    /// <summary>
    /// Gets or sets the accessibility label.
    /// </summary>
    [Parameter]
    public string AriaLabel { get; set; } = "Radial gauge";

    private bool HasData => MaxValue > 0;

    private decimal Percent => MaxValue > 0 ? Math.Clamp((Value / MaxValue) * 100m, 0m, 100m) : 0m;

    private double Center => Size / 2d;

    private double Radius => (Size - StrokeWidth) / 2d - 2d;

    private double Circumference => 2 * Math.PI * Radius;

    private double DashOffset => Circumference * (1 - (double)(Percent / 100m));

    private string StrokeColor => ResolveThresholdColor();

    private string RotationTransform => $"rotate(-90 {F(Center)} {F(Center)})";

    private string ValueText => ShowPercent ? Percent.ToString("N0", CultureInfo.CurrentCulture) + "%" : Value.ToString("N0", CultureInfo.CurrentCulture);

    private string LabelText => ShowLabel ? Label : string.Empty;

    private string ResolveThresholdColor()
    {
        var thresholds = Thresholds ?? DefaultThresholds;
        var selected = thresholds
            .OrderBy(t => t.MinPercent)
            .LastOrDefault(t => Percent >= t.MinPercent);

        return selected?.Color ?? "#6b7280";
    }

    private static IReadOnlyList<ThresholdColor> DefaultThresholds =>
    [
        new ThresholdColor { MinPercent = 0m, Color = "#22c55e", Label = "On track" },
        new ThresholdColor { MinPercent = 70m, Color = "#f59e0b", Label = "Warning" },
        new ThresholdColor { MinPercent = 90m, Color = "#ef4444", Label = "Over" },
    ];

    private static string F(double value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    private MarkupString RenderSvgText(double x, double y, string cssClass, string textAnchor, string fontSize, string content)
    {
        return new MarkupString(
            $"<text x=\"{F(x)}\" y=\"{F(y)}\" class=\"{cssClass}\" text-anchor=\"{textAnchor}\" font-size=\"{fontSize}\">{content}</text>");
    }
}
