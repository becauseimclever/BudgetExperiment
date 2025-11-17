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
