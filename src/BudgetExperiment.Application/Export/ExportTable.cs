// <copyright file="ExportTable.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Application.Export;

/// <summary>
/// Represents a table of data for export.
/// </summary>
public sealed record ExportTable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExportTable"/> class.
    /// </summary>
    /// <param name="title">Optional title.</param>
    /// <param name="columns">Column headers.</param>
    /// <param name="rows">Row values.</param>
    public ExportTable(string? title, IReadOnlyList<string> columns, IReadOnlyList<IReadOnlyList<string>> rows)
    {
        this.Title = title;
        this.Columns = columns;
        this.Rows = rows;
    }

    /// <summary>
    /// Gets the optional title for the export.
    /// </summary>
    public string? Title { get; }

    /// <summary>
    /// Gets the column headers.
    /// </summary>
    public IReadOnlyList<string> Columns { get; }

    /// <summary>
    /// Gets the row values.
    /// </summary>
    public IReadOnlyList<IReadOnlyList<string>> Rows { get; }
}
