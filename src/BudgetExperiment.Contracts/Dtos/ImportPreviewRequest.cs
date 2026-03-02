// <copyright file="ImportPreviewRequest.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// Request to preview an import with mapping configuration.
/// </summary>
public sealed record ImportPreviewRequest
{
    /// <summary>
    /// Gets the target account ID for the import.
    /// </summary>
    public Guid AccountId { get; init; }

    /// <summary>
    /// Gets the raw CSV rows (without header if HasHeaderRow was true).
    /// </summary>
    public IReadOnlyList<IReadOnlyList<string>> Rows { get; init; } = [];

    /// <summary>
    /// Gets the column mappings defining how CSV columns map to transaction fields.
    /// </summary>
    public IReadOnlyList<ColumnMappingDto> Mappings { get; init; } = [];

    /// <summary>
    /// Gets the date format to use for parsing dates (e.g., "MM/dd/yyyy").
    /// </summary>
    public string DateFormat { get; init; } = "MM/dd/yyyy";

    /// <summary>
    /// Gets how amounts should be interpreted.
    /// </summary>
    public AmountParseMode AmountMode { get; init; } = AmountParseMode.NegativeIsExpense;

    /// <summary>
    /// Gets the settings for duplicate detection.
    /// </summary>
    public DuplicateDetectionSettingsDto DuplicateSettings { get; init; } = new();

    /// <summary>
    /// Gets a value indicating whether to check for recurring transaction matches.
    /// </summary>
    public bool CheckRecurringMatches { get; init; } = true;

    /// <summary>
    /// Gets the number of rows to skip at the beginning of the file.
    /// </summary>
    public int RowsToSkip { get; init; }

    /// <summary>
    /// Gets the debit/credit indicator settings.
    /// </summary>
    public DebitCreditIndicatorSettingsDto? IndicatorSettings { get; init; }
}
