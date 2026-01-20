// <copyright file="DonutSegmentData.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Client.Components.Charts;

/// <summary>
/// Represents data for a single segment in a donut chart.
/// </summary>
public sealed class DonutSegmentData
{
    /// <summary>
    /// Gets or sets the unique identifier for this segment.
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the display label for this segment.
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the monetary value for this segment.
    /// </summary>
    public decimal Value { get; set; }

    /// <summary>
    /// Gets or sets the percentage of the total this segment represents.
    /// </summary>
    public decimal Percentage { get; set; }

    /// <summary>
    /// Gets or sets the color for this segment (hex code).
    /// </summary>
    public string Color { get; set; } = "#6b7280";

    /// <summary>
    /// Gets or sets the number of transactions in this segment.
    /// </summary>
    public int TransactionCount { get; set; }

    /// <summary>
    /// Gets or sets optional additional data associated with this segment.
    /// </summary>
    public object? Data { get; set; }
}
