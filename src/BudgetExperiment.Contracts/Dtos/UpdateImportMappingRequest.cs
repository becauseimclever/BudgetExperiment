// <copyright file="UpdateImportMappingRequest.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// Request to update an import mapping.
/// </summary>
public sealed record UpdateImportMappingRequest
{
    /// <summary>
    /// Gets the new name (if changing).
    /// </summary>
    public string? Name
    {
        get; init;
    }

    /// <summary>
    /// Gets the new column mappings (if changing).
    /// </summary>
    public IReadOnlyList<ColumnMappingDto>? ColumnMappings
    {
        get; init;
    }

    /// <summary>
    /// Gets the new date format (if changing).
    /// </summary>
    public string? DateFormat
    {
        get; init;
    }

    /// <summary>
    /// Gets the new amount parse mode (if changing).
    /// </summary>
    public AmountParseMode? AmountMode
    {
        get; init;
    }

    /// <summary>
    /// Gets the new duplicate detection settings (if changing).
    /// </summary>
    public DuplicateDetectionSettingsDto? DuplicateSettings
    {
        get; init;
    }

    /// <summary>
    /// Gets the number of rows to skip at the beginning of the file (if changing).
    /// </summary>
    public int? RowsToSkip
    {
        get; init;
    }

    /// <summary>
    /// Gets the new debit/credit indicator settings (if changing).
    /// </summary>
    public DebitCreditIndicatorSettingsDto? IndicatorSettings
    {
        get; init;
    }
}
