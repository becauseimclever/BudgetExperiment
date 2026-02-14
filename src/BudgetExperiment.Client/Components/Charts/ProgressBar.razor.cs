// <copyright file="ProgressBar.razor.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace BudgetExperiment.Client.Components.Charts;

/// <summary>
/// Horizontal progress bar component with threshold coloring.
/// </summary>
public partial class ProgressBar
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
    /// Gets or sets the optional label.
    /// </summary>
    [Parameter]
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether to show the label area.
    /// </summary>
    [Parameter]
    public bool ShowLabel { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to show the percent value.
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
    public string AriaLabel { get; set; } = "Progress";

    private decimal Percent
    {
        get
        {
            if (MaxValue <= 0)
            {
                return 0m;
            }

            return Math.Clamp((Value / MaxValue) * 100m, 0m, 100m);
        }
    }

    private string PercentText => Percent.ToString("N0", CultureInfo.CurrentCulture) + "%";

    private string LabelText => string.IsNullOrWhiteSpace(Label) ? "Progress" : Label;

    private string FillColor => ResolveThresholdColor();

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

    private static string F(decimal value)
    {
        return value.ToString("0", CultureInfo.InvariantCulture);
    }
}
