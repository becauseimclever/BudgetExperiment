// <copyright file="ImportPreviewRow.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// A single row from the import preview.
/// </summary>
public sealed record ImportPreviewRow
{
    /// <summary>
    /// Gets the row index from the original CSV (1-based for display).
    /// </summary>
    public int RowIndex { get; init; }

    /// <summary>
    /// Gets the parsed transaction date.
    /// </summary>
    public DateOnly? Date { get; init; }

    /// <summary>
    /// Gets the transaction description.
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Gets the parsed amount.
    /// </summary>
    public decimal? Amount { get; init; }

    /// <summary>
    /// Gets the category name (from CSV or auto-categorization).
    /// </summary>
    public string? Category { get; init; }

    /// <summary>
    /// Gets the category ID if matched.
    /// </summary>
    public Guid? CategoryId { get; init; }

    /// <summary>
    /// Gets the source of the category assignment.
    /// </summary>
    public CategorySource CategorySource { get; init; }

    /// <summary>
    /// Gets the name of the matched auto-categorization rule (if any).
    /// </summary>
    public string? MatchedRuleName { get; init; }

    /// <summary>
    /// Gets the matched rule ID for tracking.
    /// </summary>
    public Guid? MatchedRuleId { get; init; }

    /// <summary>
    /// Gets the external reference/ID from the CSV.
    /// </summary>
    public string? Reference { get; init; }

    /// <summary>
    /// Gets the validation status of this row.
    /// </summary>
    public ImportRowStatus Status { get; init; }

    /// <summary>
    /// Gets a message describing the status (error message, warning, etc.).
    /// </summary>
    public string? StatusMessage { get; init; }

    /// <summary>
    /// Gets the ID of the existing transaction this row duplicates.
    /// </summary>
    public Guid? DuplicateOfTransactionId { get; init; }

    /// <summary>
    /// Gets a value indicating whether this row is selected for import.
    /// </summary>
    public bool IsSelected { get; init; } = true;

    /// <summary>
    /// Gets the potential recurring transaction match for this row (if any).
    /// </summary>
    public ImportRecurringMatchPreview? RecurringMatch { get; init; }

    /// <summary>
    /// Gets the parsed location preview for this row (if location parsing found a match).
    /// </summary>
    public ImportLocationPreview? ParsedLocation { get; init; }
}
