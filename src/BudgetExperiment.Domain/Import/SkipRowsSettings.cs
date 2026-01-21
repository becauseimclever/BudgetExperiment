// <copyright file="SkipRowsSettings.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Import;

/// <summary>
/// Settings for skipping rows at the beginning of a CSV file.
/// </summary>
public sealed record SkipRowsSettings
{
    /// <summary>
    /// Maximum number of rows that can be skipped.
    /// </summary>
    public const int MaxSkipRows = 100;

    /// <summary>
    /// Gets the number of rows to skip before the data/header begins.
    /// </summary>
    public int RowsToSkip { get; init; }

    /// <summary>
    /// Creates skip rows settings.
    /// </summary>
    /// <param name="rowsToSkip">Number of rows to skip (0-100).</param>
    /// <returns>A new <see cref="SkipRowsSettings"/> instance.</returns>
    /// <exception cref="DomainException">Thrown when rowsToSkip is out of range.</exception>
    public static SkipRowsSettings Create(int rowsToSkip)
    {
        if (rowsToSkip < 0 || rowsToSkip > MaxSkipRows)
        {
            throw new DomainException($"Rows to skip must be between 0 and {MaxSkipRows}.");
        }

        return new SkipRowsSettings { RowsToSkip = rowsToSkip };
    }

    /// <summary>
    /// Gets default settings (no rows skipped).
    /// </summary>
    public static SkipRowsSettings Default => new() { RowsToSkip = 0 };
}
