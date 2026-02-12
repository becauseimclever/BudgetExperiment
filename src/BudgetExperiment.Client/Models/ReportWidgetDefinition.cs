// <copyright file="ReportWidgetDefinition.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Client.Models;

/// <summary>
/// Represents a widget placed in a custom report layout.
/// </summary>
public sealed class ReportWidgetDefinition
{
    /// <summary>
    /// Gets or sets the widget identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the widget type.
    /// </summary>
    public string Type { get; set; } = "summary";

    /// <summary>
    /// Gets or sets the widget title.
    /// </summary>
    public string Title { get; set; } = "Widget";

    /// <summary>
    /// Gets or sets the grid layouts per breakpoint.
    /// </summary>
    public Dictionary<string, ReportWidgetLayoutPosition> Layouts { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets or sets sizing constraints for the widget.
    /// </summary>
    public ReportWidgetConstraints? Constraints { get; set; }

    /// <summary>
    /// Gets or sets the widget configuration.
    /// </summary>
    public ReportWidgetConfigDefinition? Config { get; set; }
}
