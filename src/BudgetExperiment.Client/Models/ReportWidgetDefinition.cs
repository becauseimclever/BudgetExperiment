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
}
