// <copyright file="CsvImportResult.cs" company="Fortinbra">
// Copyright (c) 2025 Fortinbra (becauseimclever.com). All rights reserved.
// </copyright>

namespace BudgetExperiment.Client.Models;

/// <summary>
/// Result of a CSV import operation.
/// </summary>
public sealed class CsvImportResult
{
    /// <summary>
    /// Gets or sets total number of data rows processed.
    /// </summary>
    public int TotalRows { get; set; }

    /// <summary>
    /// Gets or sets number of transactions successfully imported.
    /// </summary>
    public int SuccessfulImports { get; set; }

    /// <summary>
    /// Gets or sets number of transactions that failed to import.
    /// </summary>
    public int FailedImports { get; set; }

    /// <summary>
    /// Gets or sets number of duplicate transactions skipped.
    /// </summary>
    public int DuplicatesSkipped { get; set; }

    /// <summary>
    /// Gets or sets collection of errors encountered during import.
    /// </summary>
    public List<CsvImportError> Errors { get; set; } = new();

    /// <summary>
    /// Gets or sets collection of duplicate transactions that were skipped.
    /// </summary>
    public List<DuplicateTransaction> Duplicates { get; set; } = new();
}

/// <summary>
/// Represents an error encountered during CSV import.
/// </summary>
public sealed class CsvImportError
{
    /// <summary>
    /// Gets or sets the row number where the error occurred.
    /// </summary>
    public int RowNumber { get; set; }

    /// <summary>
    /// Gets or sets the field name that caused the error.
    /// </summary>
    public string Field { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets description of the error.
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;
}

/// <summary>
/// Represents a duplicate transaction that was skipped during import.
/// </summary>
public sealed class DuplicateTransaction
{
    /// <summary>
    /// Gets or sets the row number in the CSV file (1-based, excluding header).
    /// </summary>
    public int RowNumber { get; set; }

    /// <summary>
    /// Gets or sets the transaction date.
    /// </summary>
    public DateOnly Date { get; set; }

    /// <summary>
    /// Gets or sets the transaction description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the transaction amount (absolute value).
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Gets or sets the ID of the existing matching transaction in the database.
    /// </summary>
    public Guid ExistingTransactionId { get; set; }
}
