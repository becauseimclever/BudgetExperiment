// <copyright file="HeatmapChart.razor.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Globalization;

using BudgetExperiment.Client.Components.Charts.Models;
using Microsoft.AspNetCore.Components;

namespace BudgetExperiment.Client.Components.Charts;

/// <summary>
/// SVG heatmap chart that visualizes spending intensity across days of the week and weekly columns.
/// </summary>
public partial class HeatmapChart
{
    /// <summary>
    /// Gets or sets the heatmap data as a jagged array where each outer element represents a day row
    /// (index 0 = Monday, index 6 = Sunday) and each inner array contains the week cells for that row.
    /// </summary>
    [Parameter]
    public HeatmapDataPoint[][]? Data { get; set; }

    /// <summary>
    /// Gets or sets the accessibility label for the chart.
    /// </summary>
    [Parameter]
    public string AriaLabel { get; set; } = "Spending heatmap";

    /// <summary>
    /// Gets or sets the row labels displayed on the left side of the heatmap. Defaults to Mon–Sun.
    /// </summary>
    [Parameter]
    public string[] RowLabels { get; set; } = ["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"];

    /// <summary>
    /// Gets or sets the optional maximum amount used as the color-scale ceiling.
    /// When null, the maximum is computed from the provided data.
    /// </summary>
    [Parameter]
    public decimal? MaxAmount { get; set; }

    private static double CellSize => 16;

    private static double CellGap => 2;

    private static double LabelAreaWidth => 32;

    private bool IsEmpty => Data == null || Data.All(row => row.Length == 0);

    private int ComputedWeekCount
    {
        get
        {
            if (Data == null)
            {
                return 0;
            }

            var maxWeek = Data.SelectMany(r => r).Select(p => p.WeekIndex).DefaultIfEmpty(-1).Max();
            return maxWeek + 1;
        }
    }

    private double ComputedViewBoxWidth => LabelAreaWidth + (ComputedWeekCount * (CellSize + CellGap));

    private double ComputedViewBoxHeight => 7 * (CellSize + CellGap);

    private decimal ComputedMaxAmount
    {
        get
        {
            if (MaxAmount.HasValue)
            {
                return MaxAmount.Value;
            }

            if (Data == null)
            {
                return 1m;
            }

            var max = Data.SelectMany(r => r).Select(p => p.TotalAmount).DefaultIfEmpty(0m).Max();
            return max > 0 ? max : 1m;
        }
    }

    private static double CellX(int weekIndex) => LabelAreaWidth + (weekIndex * (CellSize + CellGap));

    private static double CellY(int dayIndex) => dayIndex * (CellSize + CellGap);

    private static double RowLabelY(int dayIndex) => (dayIndex * (CellSize + CellGap)) + (CellSize * 0.75);

    private static string F(double value) => value.ToString(CultureInfo.InvariantCulture);

    private string ComputeCellFill(decimal amount)
    {
        var opacity = Math.Clamp((double)(amount / ComputedMaxAmount), 0.1, 1.0);
        return $"rgba(16, 124, 16, {F(opacity)})";
    }

    private MarkupString RenderSvgText(double x, double y, string cssClass, string textAnchor, string fontSize, string content)
    {
        var xStr = F(x);
        var yStr = F(y);
        return new MarkupString(
            $"<text x=\"{xStr}\" y=\"{yStr}\" class=\"{cssClass}\" text-anchor=\"{textAnchor}\" font-size=\"{fontSize}\">{content}</text>");
    }
}
