// <copyright file="ImportRowProcessor.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.Import;

/// <summary>
/// Pipeline coordinator for processing individual CSV rows during import.
/// Delegates field extraction to <see cref="ImportFieldExtractor"/>,
/// parsing to <see cref="ImportFieldParser"/>, and orchestrates
/// categorization, duplicate detection, and status determination.
/// </summary>
public sealed class ImportRowProcessor : IImportRowProcessor
{
    private readonly IImportDuplicateDetector _duplicateDetector;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImportRowProcessor"/> class.
    /// </summary>
    /// <param name="duplicateDetector">The duplicate detector.</param>
    public ImportRowProcessor(IImportDuplicateDetector duplicateDetector)
    {
        _duplicateDetector = duplicateDetector;
    }

    /// <inheritdoc />
    public ImportPreviewRow ProcessRow(
        int rowIndex,
        IReadOnlyList<string> row,
        IReadOnlyList<ColumnMappingDto> mappings,
        string dateFormat,
        AmountParseMode amountMode,
        DebitCreditIndicatorSettingsDto? indicatorSettings,
        IReadOnlyList<CategorizationRule> rules,
        Dictionary<string, BudgetCategory> categoryByName,
        IReadOnlyList<Transaction> existingTransactions,
        DuplicateDetectionSettingsDto duplicateSettings)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        var extracted = ImportFieldExtractor.Extract(row, mappings);

        var date = ParseAndValidateDate(extracted.DateStr, dateFormat, errors);

        var amount = ParseAndValidateAmount(
            extracted.AmountStr,
            extracted.DebitStr,
            extracted.CreditStr,
            extracted.IndicatorValue,
            amountMode,
            indicatorSettings,
            errors,
            warnings);

        var description = ValidateDescription(extracted.Description, errors);

        var categorization = DetermineCategory(description, extracted.CategoryName, rules, categoryByName, warnings);

        Guid? duplicateOfId = null;
        if (duplicateSettings.Enabled && date.HasValue && amount.HasValue && errors.Count == 0)
        {
            duplicateOfId = _duplicateDetector.FindDuplicate(
                date.Value, amount.Value, description, existingTransactions, duplicateSettings);
        }

        var (status, statusMessage) = DetermineStatus(errors, warnings, duplicateOfId);

        return new ImportPreviewRow
        {
            RowIndex = rowIndex,
            Date = date,
            Description = description,
            Amount = amount,
            Category = categorization.CategoryId.HasValue && categoryByName.Values.FirstOrDefault(c => c.Id == categorization.CategoryId) is { } cat
                ? cat.Name
                : extracted.CategoryName,
            CategoryId = categorization.CategoryId,
            CategorySource = categorization.Source,
            MatchedRuleName = categorization.MatchedRuleName,
            MatchedRuleId = categorization.MatchedRuleId,
            Reference = extracted.Reference,
            Status = status,
            StatusMessage = statusMessage,
            DuplicateOfTransactionId = duplicateOfId,
            IsSelected = status != ImportRowStatus.Error && status != ImportRowStatus.Duplicate,
        };
    }

    /// <inheritdoc />
    public List<DateOnly> ExtractDatesFromRows(
        IReadOnlyList<IReadOnlyList<string>> rows,
        IReadOnlyList<ColumnMappingDto> mappings,
        string dateFormat)
    {
        var dateMapping = mappings.FirstOrDefault(m => m.TargetField == ImportField.Date);
        if (dateMapping is null)
        {
            return [];
        }

        return rows
            .Where(row => dateMapping.ColumnIndex >= 0 && dateMapping.ColumnIndex < row.Count)
            .Select(row => ImportFieldParser.ParseDate(row[dateMapping.ColumnIndex], dateFormat))
            .Where(d => d.HasValue)
            .Select(d => d!.Value)
            .ToList();
    }

    /// <summary>
    /// Parses a date string using the preferred format and common fallbacks.
    /// Exposed internally for legacy call-sites; prefer <see cref="ImportFieldParser.ParseDate"/>.
    /// </summary>
    internal static DateOnly? ParseDate(string dateStr, string preferredFormat) =>
        ImportFieldParser.ParseDate(dateStr, preferredFormat);

    /// <summary>
    /// Parses an amount string applying the specified sign mode.
    /// Exposed internally for legacy call-sites; prefer <see cref="ImportFieldParser.ParseAmount"/>.
    /// </summary>
    internal static decimal? ParseAmount(string amountStr, AmountParseMode mode) =>
        ImportFieldParser.ParseAmount(amountStr, mode);

    /// <summary>
    /// Parses a raw amount string handling currency symbols and parentheses.
    /// Exposed internally for legacy call-sites; prefer <see cref="ImportFieldParser.ParseAmountValue"/>.
    /// </summary>
    internal static decimal? ParseAmountValue(string? amountStr) =>
        ImportFieldParser.ParseAmountValue(amountStr);

    /// <summary>
    /// Determines the sign multiplier for a debit/credit indicator value.
    /// Exposed internally for legacy call-sites; prefer <see cref="ImportFieldParser.GetIndicatorSignMultiplier"/>.
    /// </summary>
    internal static int? GetIndicatorSignMultiplier(string? indicatorValue, DebitCreditIndicatorSettingsDto settings) =>
        ImportFieldParser.GetIndicatorSignMultiplier(indicatorValue, settings);

    private static DateOnly? ParseAndValidateDate(string? dateStr, string dateFormat, List<string> errors)
    {
        if (!string.IsNullOrEmpty(dateStr))
        {
            var date = ImportFieldParser.ParseDate(dateStr, dateFormat);
            if (!date.HasValue)
            {
                errors.Add($"Could not parse date: '{dateStr}'");
            }

            return date;
        }

        errors.Add("Date is required");
        return null;
    }

    private static decimal? ParseAndValidateAmount(
        string? amountStr,
        string? debitStr,
        string? creditStr,
        string? indicatorValue,
        AmountParseMode amountMode,
        DebitCreditIndicatorSettingsDto? indicatorSettings,
        List<string> errors,
        List<string> warnings)
    {
        if (amountMode == AmountParseMode.IndicatorColumn && indicatorSettings != null && indicatorSettings.ColumnIndex >= 0)
        {
            return ParseIndicatorAmount(amountStr, indicatorValue, indicatorSettings, errors, warnings);
        }

        if (!string.IsNullOrEmpty(amountStr))
        {
            return ParseStandardAmount(amountStr, amountMode, errors);
        }

        if (!string.IsNullOrEmpty(debitStr) || !string.IsNullOrEmpty(creditStr))
        {
            return ParseDebitCreditAmount(debitStr, creditStr, errors);
        }

        errors.Add("Amount is required");
        return null;
    }

    private static decimal? ParseIndicatorAmount(
        string? amountStr,
        string? indicatorValue,
        DebitCreditIndicatorSettingsDto indicatorSettings,
        List<string> errors,
        List<string> warnings)
    {
        if (string.IsNullOrEmpty(amountStr))
        {
            errors.Add("Amount is required when using indicator column mode");
            return null;
        }

        var rawAmount = ImportFieldParser.ParseAmountValue(amountStr);
        if (!rawAmount.HasValue)
        {
            errors.Add($"Could not parse amount: '{amountStr}'");
            return null;
        }

        var signMultiplier = ImportFieldParser.GetIndicatorSignMultiplier(indicatorValue, indicatorSettings);
        if (!signMultiplier.HasValue)
        {
            warnings.Add($"Unrecognized indicator value: '{indicatorValue}'");
            return rawAmount.Value;
        }

        return Math.Abs(rawAmount.Value) * signMultiplier.Value;
    }

    private static decimal? ParseStandardAmount(string amountStr, AmountParseMode amountMode, List<string> errors)
    {
        var amount = ImportFieldParser.ParseAmount(amountStr, amountMode);
        if (!amount.HasValue)
        {
            errors.Add($"Could not parse amount: '{amountStr}'");
        }

        return amount;
    }

    private static decimal? ParseDebitCreditAmount(string? debitStr, string? creditStr, List<string> errors)
    {
        var debit = ImportFieldParser.ParseAmountValue(debitStr);
        var credit = ImportFieldParser.ParseAmountValue(creditStr);

        if (debit.HasValue && debit.Value != 0)
        {
            return -Math.Abs(debit.Value);
        }

        if (credit.HasValue && credit.Value != 0)
        {
            return Math.Abs(credit.Value);
        }

        if (!string.IsNullOrEmpty(debitStr) && !debit.HasValue)
        {
            errors.Add($"Could not parse debit amount: '{debitStr}'");
        }
        else if (!string.IsNullOrEmpty(creditStr) && !credit.HasValue)
        {
            errors.Add($"Could not parse credit amount: '{creditStr}'");
        }

        return null;
    }

    private static string ValidateDescription(string? description, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            errors.Add("Description is required");
            return string.Empty;
        }

        return description;
    }

    private static CategorizationResult DetermineCategory(
        string description,
        string? csvCategoryName,
        IReadOnlyList<CategorizationRule> rules,
        Dictionary<string, BudgetCategory> categoryByName,
        List<string> warnings)
    {
        if (!string.IsNullOrWhiteSpace(csvCategoryName) && categoryByName.TryGetValue(csvCategoryName, out var csvCategory))
        {
            return new CategorizationResult(csvCategory.Id, CategorySource.CsvColumn, null, null);
        }

        if (!string.IsNullOrWhiteSpace(csvCategoryName))
        {
            warnings.Add($"Category '{csvCategoryName}' not found");
        }

        var matchedRule = string.IsNullOrWhiteSpace(description)
            ? null
            : rules.FirstOrDefault(r => r.Matches(description));

        if (matchedRule is not null)
        {
            return new CategorizationResult(matchedRule.CategoryId, CategorySource.AutoRule, matchedRule.Name, matchedRule.Id);
        }

        return new CategorizationResult(null, CategorySource.None, null, null);
    }

    private static (ImportRowStatus Status, string? StatusMessage) DetermineStatus(
        List<string> errors,
        List<string> warnings,
        Guid? duplicateOfId)
    {
        if (errors.Count > 0)
        {
            return (ImportRowStatus.Error, string.Join("; ", errors));
        }

        if (duplicateOfId.HasValue)
        {
            return (ImportRowStatus.Duplicate, "Possible duplicate of existing transaction");
        }

        if (warnings.Count > 0)
        {
            return (ImportRowStatus.Warning, string.Join("; ", warnings));
        }

        return (ImportRowStatus.Valid, null);
    }

    private readonly record struct CategorizationResult(
        Guid? CategoryId,
        CategorySource Source,
        string? MatchedRuleName,
        Guid? MatchedRuleId);
}
