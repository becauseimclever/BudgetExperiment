// <copyright file="IBankCsvParser.cs" company="Fortinbra">
// Copyright (c) 2025 Fortinbra (becauseimclever.com). All rights reserved.
// </copyright>

using BudgetExperiment.Application.CsvImport.Models;

namespace BudgetExperiment.Application.CsvImport.Parsers;

/// <summary>
/// Parser abstraction for bank-specific CSV formats.
/// </summary>
public interface IBankCsvParser
{
    /// <summary>
    /// Gets the bank type this parser supports.
    /// </summary>
    BankType BankType { get; }

    /// <summary>
    /// Parse a CSV stream into a collection of transactions.
    /// </summary>
    /// <param name="csvStream">The CSV file stream.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of parsed transactions.</returns>
    Task<IReadOnlyList<ParsedTransaction>> ParseAsync(Stream csvStream, CancellationToken cancellationToken = default);
}
