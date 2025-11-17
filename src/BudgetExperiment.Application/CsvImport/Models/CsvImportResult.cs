// <copyright file="CsvImportResult.cs" company="Fortinbra">
// Copyright (c) 2025 Fortinbra (becauseimclever.com). All rights reserved.
// </copyright>

namespace BudgetExperiment.Application.CsvImport.Models;

/// <summary>
/// Result of a CSV import operation.
/// </summary>
/// <param name="TotalRows">Total number of data rows processed (excluding header).</param>
/// <param name="SuccessfulImports">Number of transactions successfully imported.</param>
/// <param name="FailedImports">Number of transactions that failed to import.</param>
/// <param name="DuplicatesSkipped">Number of duplicate transactions skipped.</param>
/// <param name="Errors">Collection of errors encountered during import.</param>
/// <param name="Duplicates">Collection of duplicate transactions that were skipped.</param>
public sealed record CsvImportResult(
    int TotalRows,
    int SuccessfulImports,
    int FailedImports,
    int DuplicatesSkipped,
    IReadOnlyList<CsvImportError> Errors,
    IReadOnlyList<DuplicateTransaction> Duplicates);
