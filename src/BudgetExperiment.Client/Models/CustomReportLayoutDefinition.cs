// <copyright file="CustomReportLayoutDefinition.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Client.Models;

/// <summary>
/// Represents the client-side layout definition for a custom report.
/// </summary>
public sealed class CustomReportLayoutDefinition
{
    private const int DefaultWidgetWidth = 4;
    private const int DefaultWidgetHeight = 4;
    private const int DefaultMinSize = 2;
    private const int DefaultMaxSize = 12;

    /// <summary>
    /// Gets or sets the grid definition.
    /// </summary>
    public ReportGridDefinition Grid { get; set; } = ReportGridDefinition.CreateDefault();

    /// <summary>
    /// Gets or sets the widgets in the layout.
    /// </summary>
    public List<ReportWidgetDefinition> Widgets { get; set; } = [];

    /// <summary>
    /// Ensures the layout has required defaults applied.
    /// </summary>
    public void Normalize()
    {
        Grid ??= ReportGridDefinition.CreateDefault();
        Grid.Normalize();

        var nextY = GetNextRow("lg");
        foreach (var widget in Widgets)
        {
            ApplyWidgetDefaults(widget, nextY);
            if (widget.Layouts.TryGetValue("lg", out var layout))
            {
                nextY = Math.Max(nextY, layout.Y + layout.Height);
            }
        }
    }

    /// <summary>
    /// Adds a widget to the layout, applying grid defaults.
    /// </summary>
    /// <param name="widget">Widget definition.</param>
    public void AddWidget(ReportWidgetDefinition widget)
    {
        var nextY = GetNextRow("lg");
        ApplyWidgetDefaults(widget, nextY);
        Widgets.Add(widget);
    }

    /// <summary>
    /// Updates a widget layout position for a breakpoint.
    /// </summary>
    /// <param name="widgetId">Widget identifier.</param>
    /// <param name="breakpoint">Breakpoint key.</param>
    /// <param name="layout">New layout position.</param>
    public void UpdateWidgetLayout(Guid widgetId, string breakpoint, ReportWidgetLayoutPosition layout)
    {
        var widget = Widgets.FirstOrDefault(item => item.Id == widgetId);
        if (widget == null)
        {
            return;
        }

        ApplyWidgetDefaults(widget, GetNextRow("lg"));

        var columns = Grid.GetColumns(breakpoint);
        var clamped = Clamp(layout, columns, widget.Constraints);
        widget.Layouts[breakpoint] = clamped;
    }

    private int GetNextRow(string breakpoint)
    {
        var nextY = 1;
        foreach (var widget in Widgets)
        {
            if (widget.Layouts.TryGetValue(breakpoint, out var layout))
            {
                nextY = Math.Max(nextY, layout.Y + layout.Height);
            }
        }

        return nextY;
    }

    private void ApplyWidgetDefaults(ReportWidgetDefinition widget, int nextY)
    {
        widget.Constraints ??= new ReportWidgetConstraints
        {
            MinWidth = DefaultMinSize,
            MinHeight = DefaultMinSize,
            MaxWidth = DefaultMaxSize,
            MaxHeight = DefaultMaxSize,
        };

        widget.Config ??= ReportWidgetConfigDefinition.CreateDefault(widget.Type);

        widget.Layouts ??= new Dictionary<string, ReportWidgetLayoutPosition>(StringComparer.OrdinalIgnoreCase);
        EnsureLayout(widget, "lg", Grid.GetColumns("lg"), nextY);
        EnsureLayout(widget, "md", Grid.GetColumns("md"), nextY);
        EnsureLayout(widget, "sm", Grid.GetColumns("sm"), nextY);
    }

    private void EnsureLayout(ReportWidgetDefinition widget, string breakpoint, int columns, int nextY)
    {
        if (widget.Layouts.ContainsKey(breakpoint))
        {
            widget.Layouts[breakpoint] = Clamp(widget.Layouts[breakpoint], columns, widget.Constraints);
            return;
        }

        var width = Math.Min(DefaultWidgetWidth, columns);
        widget.Layouts[breakpoint] = new ReportWidgetLayoutPosition
        {
            X = 1,
            Y = nextY,
            Width = width,
            Height = DefaultWidgetHeight,
        };
    }

    private static ReportWidgetLayoutPosition Clamp(
        ReportWidgetLayoutPosition layout,
        int columns,
        ReportWidgetConstraints? constraints)
    {
        var minWidth = constraints?.MinWidth ?? DefaultMinSize;
        var minHeight = constraints?.MinHeight ?? DefaultMinSize;
        var maxWidth = constraints?.MaxWidth ?? DefaultMaxSize;
        var maxHeight = constraints?.MaxHeight ?? DefaultMaxSize;

        var width = Math.Clamp(layout.Width, minWidth, Math.Min(maxWidth, columns));
        var height = Math.Clamp(layout.Height, minHeight, maxHeight);
        var x = Math.Clamp(layout.X, 1, Math.Max(1, columns - width + 1));
        var y = Math.Max(1, layout.Y);

        return new ReportWidgetLayoutPosition
        {
            X = x,
            Y = y,
            Width = width,
            Height = height,
        };
    }
}
