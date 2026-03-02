// <copyright file="ColumnMappingDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// DTO for column mapping configuration.
/// </summary>
public sealed record ColumnMappingDto
{
    /// <summary>
    /// Gets the zero-based column index in the CSV.
    /// </summary>
    public int ColumnIndex { get; init; }

    /// <summary>
    /// Gets the column header name (if available).
    /// </summary>
    public string? ColumnHeader { get; init; }

    /// <summary>
    /// Gets the target field this column maps to.
    /// </summary>
    public ImportField TargetField { get; init; }

    /// <summary>
    /// Gets the optional date format override for this column.
    /// </summary>
    public string? DateFormat { get; init; }
}
