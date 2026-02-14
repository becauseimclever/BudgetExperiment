// <copyright file="ReportWidgetLayoutPosition.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Client.Models;

/// <summary>
/// Represents a widget position in the grid layout.
/// </summary>
public sealed class ReportWidgetLayoutPosition
{
    /// <summary>
    /// Gets or sets the column start (1-based).
    /// </summary>
    public int X { get; set; }

    /// <summary>
    /// Gets or sets the row start (1-based).
    /// </summary>
    public int Y { get; set; }

    /// <summary>
    /// Gets or sets the column span.
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    /// Gets or sets the row span.
    /// </summary>
    public int Height { get; set; }
}
