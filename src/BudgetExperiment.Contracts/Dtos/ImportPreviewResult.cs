// <copyright file="ImportPreviewResult.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// Result of an import preview operation.
/// </summary>
public sealed record ImportPreviewResult
{
    /// <summary>
    /// Gets the preview rows with validation results.
    /// </summary>
    public IReadOnlyList<ImportPreviewRow> Rows { get; init; } = [];

    /// <summary>
    /// Gets the count of valid rows ready for import.
    /// </summary>
    public int ValidCount
    {
        get; init;
    }

    /// <summary>
    /// Gets the count of rows with warnings.
    /// </summary>
    public int WarningCount
    {
        get; init;
    }

    /// <summary>
    /// Gets the count of rows with errors.
    /// </summary>
    public int ErrorCount
    {
        get; init;
    }

    /// <summary>
    /// Gets the count of duplicate rows.
    /// </summary>
    public int DuplicateCount
    {
        get; init;
    }

    /// <summary>
    /// Gets the total amount of valid transactions.
    /// </summary>
    public decimal TotalAmount
    {
        get; init;
    }

    /// <summary>
    /// Gets the count of rows that would be auto-categorized by rules.
    /// </summary>
    public int AutoCategorizedCount
    {
        get; init;
    }

    /// <summary>
    /// Gets the count of rows enriched with parsed location data.
    /// </summary>
    public int LocationEnrichedCount
    {
        get; init;
    }
}
