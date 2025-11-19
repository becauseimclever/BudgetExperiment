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

    /// <summary>
    /// Parse a CSV and detect duplicates without persisting any data.
    /// </summary>
    /// <param name="csvStream">The CSV file stream.</param>
    /// <param name="bankType">The bank type that generated the CSV.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of preview rows with duplicate flags.</returns>
    Task<IReadOnlyList<CsvImportPreviewRow>> PreviewAsync(Stream csvStream, BankType bankType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Commit a set of transactions (possibly edited) to the database. Duplicate rows are imported only if ForceImport=true.
    /// </summary>
    /// <param name="transactions">Transactions to commit.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Import result mirroring CsvImport flow.</returns>
    Task<CsvImportResult> CommitAsync(IEnumerable<CommitTransaction> transactions, CancellationToken cancellationToken = default);
}
