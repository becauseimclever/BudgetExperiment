// <copyright file="ColumnMapping.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Import;

/// <summary>
/// Represents a mapping from a CSV column to a transaction field.
/// </summary>
public sealed record ColumnMapping
{
    /// <summary>
    /// Gets the zero-based index of the column in the CSV file.
    /// </summary>
    public int ColumnIndex { get; init; }

    /// <summary>
    /// Gets the header name of the column as it appears in the CSV file.
    /// </summary>
    public string ColumnHeader { get; init; } = string.Empty;

    /// <summary>
    /// Gets the target transaction field for this column.
    /// </summary>
    public ImportField TargetField { get; init; }

    /// <summary>
    /// Gets an optional expression for transforming the column value.
    /// Future use for combining or transforming column values.
    /// </summary>
    public string? TransformExpression { get; init; }

    /// <summary>
    /// Gets an optional date format override for this specific column.
    /// If not specified, the mapping's default date format is used.
    /// </summary>
    public string? DateFormat { get; init; }
}
