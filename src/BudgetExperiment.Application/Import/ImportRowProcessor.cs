// <copyright file="ImportRowProcessor.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.Import;

/// <summary>
/// Processes individual CSV rows during import, handling parsing, categorization, and validation.
/// </summary>
public sealed class ImportRowProcessor : IImportRowProcessor
{
    private static readonly string[] CommonDateFormats =
    [
        "MM/dd/yyyy",
        "M/d/yyyy",
        "MM-dd-yyyy",
        "M-d-yyyy",
        "yyyy-MM-dd",
        "yyyy/MM/dd",
        "dd/MM/yyyy",
        "d/M/yyyy",
        "dd-MM-yyyy",
        "d-M-yyyy",
        "MMM dd, yyyy",
        "MMMM dd, yyyy",
        "dd MMM yyyy",
        "MM/dd/yy",
        "M/d/yy",
    ];

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

        // Extract values from row using mappings
        var extracted = ExtractColumnValues(row, mappings);

        // Parse date
        var date = ParseAndValidateDate(extracted.DateStr, dateFormat, errors);

        // Parse amount
        var amount = ParseAndValidateAmount(
            extracted.AmountStr,
            extracted.DebitStr,
            extracted.CreditStr,
            extracted.IndicatorValue,
            amountMode,
            indicatorSettings,
            errors,
            warnings);

        // Validate description
        var description = ValidateDescription(extracted.Description, errors);

        // Determine category
        var categorization = DetermineCategory(description, extracted.CategoryName, rules, categoryByName, warnings);

        // Check for duplicates
        Guid? duplicateOfId = null;
        if (duplicateSettings.Enabled && date.HasValue && amount.HasValue && errors.Count == 0)
        {
            duplicateOfId = _duplicateDetector.FindDuplicate(
                date.Value, amount.Value, description, existingTransactions, duplicateSettings);
        }

        // Determine final status
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
            .Select(row => ParseDate(row[dateMapping.ColumnIndex], dateFormat))
            .Where(d => d.HasValue)
            .Select(d => d!.Value)
            .ToList();
    }

    internal static DateOnly? ParseDate(string dateStr, string preferredFormat)
    {
        // Try the preferred format first
        if (DateOnly.TryParseExact(dateStr, preferredFormat, null, System.Globalization.DateTimeStyles.None, out var date))
        {
            return date;
        }

        // Try common formats
        foreach (var format in CommonDateFormats)
        {
            if (DateOnly.TryParseExact(dateStr, format, null, System.Globalization.DateTimeStyles.None, out date))
            {
                return date;
            }
        }

        // Try generic parse as last resort
        if (DateOnly.TryParse(dateStr, out date))
        {
            return date;
        }

        return null;
    }

    internal static decimal? ParseAmount(string amountStr, AmountParseMode mode)
    {
        var value = ParseAmountValue(amountStr);
        if (!value.HasValue)
        {
            return null;
        }

        return mode switch
        {
            AmountParseMode.NegativeIsExpense => value.Value,
            AmountParseMode.PositiveIsExpense => -value.Value,
            AmountParseMode.AbsoluteExpense => -Math.Abs(value.Value),
            AmountParseMode.AbsoluteIncome => Math.Abs(value.Value),
            _ => value.Value,
        };
    }

    internal static decimal? ParseAmountValue(string? amountStr)
    {
        if (string.IsNullOrWhiteSpace(amountStr))
        {
            return null;
        }

        // Clean up the amount string
        var cleaned = amountStr.Trim();

        // Strip leading apostrophe used by CSV sanitization for display safety
        // (e.g., "'-10.05" → "-10.05")
        if (cleaned.Length >= 2 && cleaned[0] == '\'' &&
            cleaned[1] is '=' or '@' or '+' or '-' or '\t' or '\r')
        {
            cleaned = cleaned[1..];
        }

        // Handle parentheses as negative (accounting format)
        bool isNegative = cleaned.StartsWith('(') && cleaned.EndsWith(')');
        if (isNegative)
        {
            cleaned = cleaned[1..^1];
        }

        // Handle leading minus
        if (cleaned.StartsWith('-'))
        {
            isNegative = !isNegative; // Toggle if already negative from parentheses
            cleaned = cleaned[1..];
        }

        // Remove currency symbols and thousands separators
        cleaned = cleaned.Replace("$", string.Empty)
                        .Replace("£", string.Empty)
                        .Replace("€", string.Empty)
                        .Replace(",", string.Empty)
                        .Trim();

        if (decimal.TryParse(cleaned, out var amount))
        {
            return isNegative ? -amount : amount;
        }

        return null;
    }

    internal static int? GetIndicatorSignMultiplier(string? indicatorValue, DebitCreditIndicatorSettingsDto settings)
    {
        if (string.IsNullOrWhiteSpace(indicatorValue))
        {
            return null;
        }

        var trimmedValue = indicatorValue.Trim();
        var comparison = settings.CaseSensitive
            ? StringComparison.Ordinal
            : StringComparison.OrdinalIgnoreCase;

        // Parse comma-separated indicator values
        var debitIndicators = settings.DebitIndicators
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var creditIndicators = settings.CreditIndicators
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (debitIndicators.Any(d => string.Equals(d, trimmedValue, comparison)))
        {
            return -1; // Debit = expense = negative
        }

        if (creditIndicators.Any(c => string.Equals(c, trimmedValue, comparison)))
        {
            return 1; // Credit = income = positive
        }

        return null; // Unrecognized indicator
    }

    private static ExtractedColumnValues ExtractColumnValues(IReadOnlyList<string> row, IReadOnlyList<ColumnMappingDto> mappings)
    {
        string? dateStr = null;
        string? description = null;
        string? amountStr = null;
        string? debitStr = null;
        string? creditStr = null;
        string? categoryName = null;
        string? reference = null;
        string? indicatorValue = null;

        foreach (var mapping in mappings)
        {
            if (mapping.ColumnIndex < 0 || mapping.ColumnIndex >= row.Count)
            {
                continue;
            }

            var value = row[mapping.ColumnIndex]?.Trim();
            if (string.IsNullOrEmpty(value))
            {
                continue;
            }

            switch (mapping.TargetField)
            {
                case ImportField.Date:
                    dateStr = value;
                    break;
                case ImportField.Description:
                    description = string.IsNullOrEmpty(description) ? value : $"{description} {value}";
                    break;
                case ImportField.Amount:
                    amountStr = value;
                    break;
                case ImportField.DebitAmount:
                    debitStr = value;
                    break;
                case ImportField.CreditAmount:
                    creditStr = value;
                    break;
                case ImportField.Category:
                    categoryName = value;
                    break;
                case ImportField.Reference:
                    reference = value;
                    break;
                case ImportField.DebitCreditIndicator:
                    indicatorValue = value;
                    break;
            }
        }

        return new ExtractedColumnValues(dateStr, description, amountStr, debitStr, creditStr, categoryName, reference, indicatorValue);
    }

    private static DateOnly? ParseAndValidateDate(string? dateStr, string dateFormat, List<string> errors)
    {
        if (!string.IsNullOrEmpty(dateStr))
        {
            var date = ParseDate(dateStr, dateFormat);
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

        var rawAmount = ParseAmountValue(amountStr);
        if (!rawAmount.HasValue)
        {
            errors.Add($"Could not parse amount: '{amountStr}'");
            return null;
        }

        var signMultiplier = GetIndicatorSignMultiplier(indicatorValue, indicatorSettings);
        if (!signMultiplier.HasValue)
        {
            warnings.Add($"Unrecognized indicator value: '{indicatorValue}'");
            return rawAmount.Value;
        }

        return Math.Abs(rawAmount.Value) * signMultiplier.Value;
    }

    private static decimal? ParseStandardAmount(string amountStr, AmountParseMode amountMode, List<string> errors)
    {
        var amount = ParseAmount(amountStr, amountMode);
        if (!amount.HasValue)
        {
            errors.Add($"Could not parse amount: '{amountStr}'");
        }

        return amount;
    }

    private static decimal? ParseDebitCreditAmount(string? debitStr, string? creditStr, List<string> errors)
    {
        var debit = ParseAmountValue(debitStr);
        var credit = ParseAmountValue(creditStr);

        if (debit.HasValue && debit.Value != 0)
        {
            return -Math.Abs(debit.Value); // Debits are expenses (negative)
        }

        if (credit.HasValue && credit.Value != 0)
        {
            return Math.Abs(credit.Value); // Credits are income (positive)
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
        // Priority 1: CSV column category
        if (!string.IsNullOrWhiteSpace(csvCategoryName) && categoryByName.TryGetValue(csvCategoryName, out var csvCategory))
        {
            return new CategorizationResult(csvCategory.Id, CategorySource.CsvColumn, null, null);
        }

        if (!string.IsNullOrWhiteSpace(csvCategoryName))
        {
            warnings.Add($"Category '{csvCategoryName}' not found");
        }

        // Priority 2: Auto-categorization rules (only if no CSV category matched)
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

    private readonly record struct ExtractedColumnValues(
        string? DateStr,
        string? Description,
        string? AmountStr,
        string? DebitStr,
        string? CreditStr,
        string? CategoryName,
        string? Reference,
        string? IndicatorValue);

    private readonly record struct CategorizationResult(
        Guid? CategoryId,
        CategorySource Source,
        string? MatchedRuleName,
        Guid? MatchedRuleId);
}
