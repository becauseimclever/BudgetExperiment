// <copyright file="ICsvImportService.cs" company="Fortinbra">
// Copyright (c) 2025 Fortinbra (becauseimclever.com). All rights reserved.
// </copyright>

using BudgetExperiment.Application.CsvImport.Models;

namespace BudgetExperiment.Application.CsvImport;

/// <summary>
/// Service for importing bank transactions from CSV files.
/// </summary>
public interface ICsvImportService
{
    /// <summary>
    /// Import transactions from a CSV file.
    /// </summary>
    /// <param name="csvStream">The CSV file stream.</param>
    /// <param name="bankType">The bank type that generated the CSV.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Import result with success/failure counts and errors.</returns>
    Task<CsvImportResult> ImportAsync(Stream csvStream, BankType bankType, CancellationToken cancellationToken = default);
}
