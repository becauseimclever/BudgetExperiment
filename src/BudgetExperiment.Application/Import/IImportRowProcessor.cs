// <copyright file="IImportRowProcessor.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.Import;

/// <summary>
/// Processes individual CSV rows during import, handling parsing, categorization, and validation.
/// </summary>
public interface IImportRowProcessor
{
    /// <summary>
    /// Processes a single CSV row into an <see cref="ImportPreviewRow"/>.
    /// </summary>
    /// <param name="rowIndex">The 1-based display row index.</param>
    /// <param name="row">The raw column values from the CSV row.</param>
    /// <param name="mappings">Column-to-field mappings.</param>
    /// <param name="dateFormat">The preferred date format string.</param>
    /// <param name="amountMode">How to interpret the amount sign.</param>
    /// <param name="indicatorSettings">Debit/credit indicator column settings, if any.</param>
    /// <param name="rules">Active categorization rules ordered by priority.</param>
    /// <param name="categoryByName">Budget categories indexed by name.</param>
    /// <param name="existingTransactions">Existing transactions for duplicate detection.</param>
    /// <param name="duplicateSettings">Duplicate detection configuration.</param>
    /// <returns>A preview row with parsed data, categorization, and status.</returns>
    ImportPreviewRow ProcessRow(
        int rowIndex,
        IReadOnlyList<string> row,
        IReadOnlyList<ColumnMappingDto> mappings,
        string dateFormat,
        AmountParseMode amountMode,
        DebitCreditIndicatorSettingsDto? indicatorSettings,
        IReadOnlyList<CategorizationRule> rules,
        Dictionary<string, BudgetCategory> categoryByName,
        IReadOnlyList<Transaction> existingTransactions,
        DuplicateDetectionSettingsDto duplicateSettings);

    /// <summary>
    /// Extracts parseable dates from raw CSV rows using the date column mapping.
    /// </summary>
    /// <param name="rows">The raw CSV rows.</param>
    /// <param name="mappings">Column-to-field mappings.</param>
    /// <param name="dateFormat">The preferred date format string.</param>
    /// <returns>A list of successfully parsed dates.</returns>
    List<DateOnly> ExtractDatesFromRows(
        IReadOnlyList<IReadOnlyList<string>> rows,
        IReadOnlyList<ColumnMappingDto> mappings,
        string dateFormat);
}
