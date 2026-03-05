// <copyright file="ImportMappingDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// DTO for saved import mapping.
/// </summary>
public sealed record ImportMappingDto
{
    /// <summary>
    /// Gets the mapping ID.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Gets the mapping name.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets the column mappings.
    /// </summary>
    public IReadOnlyList<ColumnMappingDto> ColumnMappings { get; init; } = [];

    /// <summary>
    /// Gets the default date format for this mapping.
    /// </summary>
    public string? DateFormat { get; init; }

    /// <summary>
    /// Gets the default amount parse mode.
    /// </summary>
    public AmountParseMode? AmountMode { get; init; }

    /// <summary>
    /// Gets the duplicate detection settings.
    /// </summary>
    public DuplicateDetectionSettingsDto? DuplicateSettings { get; init; }

    /// <summary>
    /// Gets the number of rows to skip at the beginning of the file.
    /// </summary>
    public int RowsToSkip { get; init; }

    /// <summary>
    /// Gets the debit/credit indicator settings.
    /// </summary>
    public DebitCreditIndicatorSettingsDto? IndicatorSettings { get; init; }

    /// <summary>
    /// Gets when the mapping was created.
    /// </summary>
    public DateTime CreatedAtUtc { get; init; }

    /// <summary>
    /// Gets when the mapping was last updated.
    /// </summary>
    public DateTime? UpdatedAtUtc { get; init; }

    /// <summary>
    /// Gets the concurrency version token for optimistic concurrency.
    /// </summary>
    public string? Version { get; init; }
}
