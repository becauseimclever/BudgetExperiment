// <copyright file="ICsvParserService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Application.Import;

/// <summary>
/// Service for parsing CSV files.
/// </summary>
public interface ICsvParserService
{
    /// <summary>
    /// Parses a CSV file and returns the raw data with detected settings.
    /// </summary>
    /// <param name="fileStream">The file stream to parse.</param>
    /// <param name="fileName">The name of the file (for error messages).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The parse result containing headers and rows.</returns>
    Task<CsvParseResult> ParseAsync(Stream fileStream, string fileName, CancellationToken ct = default);
}

/// <summary>
/// Result of parsing a CSV file.
/// </summary>
public sealed record CsvParseResult
{
    /// <summary>
    /// Gets a value indicating whether the parse was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Gets the error message if parsing failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets the column headers from the CSV file.
    /// </summary>
    public IReadOnlyList<string> Headers { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets the data rows from the CSV file.
    /// </summary>
    public IReadOnlyList<IReadOnlyList<string>> Rows { get; init; } = Array.Empty<IReadOnlyList<string>>();

    /// <summary>
    /// Gets the detected delimiter character.
    /// </summary>
    public char DetectedDelimiter { get; init; }

    /// <summary>
    /// Gets a value indicating whether the file has a header row.
    /// </summary>
    public bool HasHeaderRow { get; init; }

    /// <summary>
    /// Gets the total number of data rows (excluding header).
    /// </summary>
    public int RowCount { get; init; }

    /// <summary>
    /// Creates a successful parse result.
    /// </summary>
    /// <param name="headers">The column headers.</param>
    /// <param name="rows">The data rows.</param>
    /// <param name="delimiter">The detected delimiter.</param>
    /// <param name="hasHeaderRow">Whether the file has a header row.</param>
    /// <returns>A successful <see cref="CsvParseResult"/>.</returns>
    public static CsvParseResult CreateSuccess(
        IReadOnlyList<string> headers,
        IReadOnlyList<IReadOnlyList<string>> rows,
        char delimiter,
        bool hasHeaderRow)
    {
        return new CsvParseResult
        {
            Success = true,
            Headers = headers,
            Rows = rows,
            DetectedDelimiter = delimiter,
            HasHeaderRow = hasHeaderRow,
            RowCount = rows.Count,
        };
    }

    /// <summary>
    /// Creates a failed parse result.
    /// </summary>
    /// <param name="errorMessage">The error message.</param>
    /// <returns>A failed <see cref="CsvParseResult"/>.</returns>
    public static CsvParseResult CreateFailure(string errorMessage)
    {
        return new CsvParseResult
        {
            Success = false,
            ErrorMessage = errorMessage,
        };
    }
}
