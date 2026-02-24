// <copyright file="ICsvParserService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Models;

namespace BudgetExperiment.Client.Services;

/// <summary>
/// Client-side service for parsing CSV files in the browser.
/// </summary>
public interface ICsvParserService
{
    /// <summary>
    /// Parses a CSV file stream and returns the raw data with detected settings.
    /// </summary>
    /// <param name="fileStream">The file stream to parse.</param>
    /// <param name="fileName">The name of the file (for error messages).</param>
    /// <param name="rowsToSkip">Number of rows to skip before the header row (default 0).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The parse result containing headers and rows.</returns>
    Task<CsvParseResult> ParseAsync(Stream fileStream, string fileName, int rowsToSkip = 0, CancellationToken ct = default);
}
