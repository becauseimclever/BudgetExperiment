// <copyright file="ReportWidgetConstraints.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Client.Models;

/// <summary>
/// Represents size constraints for a widget in the grid.
/// </summary>
public sealed class ReportWidgetConstraints
{
    /// <summary>
    /// Gets or sets the minimum width in grid columns.
    /// </summary>
    public int MinWidth { get; set; }

    /// <summary>
    /// Gets or sets the minimum height in grid rows.
    /// </summary>
    public int MinHeight { get; set; }

    /// <summary>
    /// Gets or sets the maximum width in grid columns.
    /// </summary>
    public int MaxWidth { get; set; }

    /// <summary>
    /// Gets or sets the maximum height in grid rows.
    /// </summary>
    public int MaxHeight { get; set; }
}
