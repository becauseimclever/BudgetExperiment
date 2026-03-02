// <copyright file="CsvParseResultModel.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Client.Models;

/// <summary>
/// Model for CSV parse result from the API.
/// </summary>
public sealed record CsvParseResultModel
{
    /// <summary>
    /// Gets the column headers.
    /// </summary>
    public IReadOnlyList<string> Headers { get; init; } = [];

    /// <summary>
    /// Gets the data rows.
    /// </summary>
    public IReadOnlyList<IReadOnlyList<string>> Rows { get; init; } = [];

    /// <summary>
    /// Gets the detected delimiter as a string.
    /// </summary>
    public string DetectedDelimiter { get; init; } = ",";

    /// <summary>
    /// Gets a value indicating whether a header row was detected.
    /// </summary>
    public bool HasHeaderRow { get; init; }

    /// <summary>
    /// Gets the total row count.
    /// </summary>
    public int RowCount { get; init; }

    /// <summary>
    /// Gets the number of rows that were skipped before the header row.
    /// </summary>
    public int RowsSkipped { get; init; }
}
