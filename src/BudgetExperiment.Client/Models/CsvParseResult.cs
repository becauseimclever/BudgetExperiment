// <copyright file="CsvParseResult.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Client.Models;

/// <summary>
/// Result of parsing a CSV file on the client side.
/// </summary>
public sealed record CsvParseResult
{
    /// <summary>
    /// Gets a value indicating whether the parse was successful.
    /// </summary>
    public bool Success
    {
        get; init;
    }

    /// <summary>
    /// Gets the error message if parsing failed.
    /// </summary>
    public string? ErrorMessage
    {
        get; init;
    }

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
    public char DetectedDelimiter
    {
        get; init;
    }

    /// <summary>
    /// Gets a value indicating whether the file has a header row.
    /// </summary>
    public bool HasHeaderRow
    {
        get; init;
    }

    /// <summary>
    /// Gets the total number of data rows (excluding header).
    /// </summary>
    public int RowCount
    {
        get; init;
    }

    /// <summary>
    /// Gets the number of rows that were skipped before the header row.
    /// </summary>
    public int RowsSkipped
    {
        get; init;
    }

    /// <summary>
    /// Creates a successful parse result.
    /// </summary>
    /// <param name="headers">The parsed column headers.</param>
    /// <param name="rows">The parsed data rows.</param>
    /// <param name="delimiter">The detected delimiter character.</param>
    /// <param name="hasHeaderRow">Whether a header row was detected.</param>
    /// <param name="rowsSkipped">The number of rows skipped before the header.</param>
    /// <returns>A successful <see cref="CsvParseResult"/>.</returns>
    public static CsvParseResult CreateSuccess(
        IReadOnlyList<string> headers,
        IReadOnlyList<IReadOnlyList<string>> rows,
        char delimiter,
        bool hasHeaderRow,
        int rowsSkipped = 0)
    {
        return new CsvParseResult
        {
            Success = true,
            Headers = headers,
            Rows = rows,
            DetectedDelimiter = delimiter,
            HasHeaderRow = hasHeaderRow,
            RowCount = rows.Count,
            RowsSkipped = rowsSkipped,
        };
    }

    /// <summary>
    /// Creates a failed parse result.
    /// </summary>
    /// <param name="errorMessage">The error message describing the failure.</param>
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
