// <copyright file="ReportWidgetPaletteItem.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Client.Models;

/// <summary>
/// Represents a palette item for report widgets.
/// </summary>
public sealed record ReportWidgetPaletteItem
{
    /// <summary>
    /// Gets the widget type.
    /// </summary>
    public required string Type { get; init; }

    /// <summary>
    /// Gets the display title.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Gets the description.
    /// </summary>
    public string? Description { get; init; }
}
