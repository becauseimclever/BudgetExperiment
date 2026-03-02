// <copyright file="CreateImportMappingRequest.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// Request to create a new import mapping.
/// </summary>
public sealed record CreateImportMappingRequest
{
    /// <summary>
    /// Gets the mapping name.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets the column mappings.
    /// </summary>
    public IReadOnlyList<ColumnMappingDto> ColumnMappings { get; init; } = [];

    /// <summary>
    /// Gets the optional default date format.
    /// </summary>
    public string? DateFormat { get; init; }

    /// <summary>
    /// Gets the optional default amount parse mode.
    /// </summary>
    public AmountParseMode? AmountMode { get; init; }

    /// <summary>
    /// Gets the optional duplicate detection settings.
    /// </summary>
    public DuplicateDetectionSettingsDto? DuplicateSettings { get; init; }

    /// <summary>
    /// Gets the number of rows to skip at the beginning of the file.
    /// </summary>
    public int RowsToSkip { get; init; }

    /// <summary>
    /// Gets the optional debit/credit indicator settings.
    /// </summary>
    public DebitCreditIndicatorSettingsDto? IndicatorSettings { get; init; }
}
