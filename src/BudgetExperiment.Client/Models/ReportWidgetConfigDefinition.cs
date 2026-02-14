// <copyright file="ReportWidgetConfigDefinition.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Client.Models;

/// <summary>
/// Represents configuration options for a report widget.
/// </summary>
public sealed class ReportWidgetConfigDefinition
{
    /// <summary>
    /// Gets or sets the report type identifier.
    /// </summary>
    public string ReportType { get; set; } = "summary";

    /// <summary>
    /// Gets or sets the date range preset.
    /// </summary>
    public string DateRangePreset { get; set; } = "last-30-days";

    /// <summary>
    /// Gets or sets the metric name (summary widgets).
    /// </summary>
    public string Metric { get; set; } = "net";

    /// <summary>
    /// Gets or sets the comparison mode (summary widgets).
    /// </summary>
    public string Comparison { get; set; } = "previous-period";

    /// <summary>
    /// Gets or sets the chart orientation.
    /// </summary>
    public string Orientation { get; set; } = "vertical";

    /// <summary>
    /// Gets or sets whether to show values in charts.
    /// </summary>
    public bool ShowValues { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to show labels in charts.
    /// </summary>
    public bool ShowLabels { get; set; } = true;

    /// <summary>
    /// Gets or sets the series identifiers for multi-series charts.
    /// </summary>
    public List<string> Series { get; set; } = [];

    /// <summary>
    /// Gets or sets the columns for table widgets.
    /// </summary>
    public List<string> Columns { get; set; } = [];

    /// <summary>
    /// Creates default configuration for a widget type.
    /// </summary>
    /// <param name="widgetType">Widget type.</param>
    /// <returns>Default configuration.</returns>
    public static ReportWidgetConfigDefinition CreateDefault(string widgetType)
    {
        var normalized = widgetType?.Trim().ToLowerInvariant() ?? "summary";

        return normalized switch
        {
            "chart" => new ReportWidgetConfigDefinition
            {
                ReportType = "spending-by-category",
                DateRangePreset = "this-month",
                Orientation = "vertical",
                ShowValues = true,
                ShowLabels = true,
                Series = new List<string>(),
            },
            "table" => new ReportWidgetConfigDefinition
            {
                ReportType = "transactions",
                DateRangePreset = "this-month",
                Columns = new List<string> { "date", "merchant", "category", "amount" },
            },
            _ => new ReportWidgetConfigDefinition
            {
                ReportType = "summary",
                DateRangePreset = "this-month",
                Metric = "net",
                Comparison = "previous-period",
            },
        };
    }
}
