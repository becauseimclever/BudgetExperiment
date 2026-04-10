// <copyright file="ImportFieldParser.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.Import;

/// <summary>
/// Provides pure parsing methods for individual import field values (date, amount).
/// Contains no validation side-effects; callers are responsible for error handling.
/// </summary>
internal static class ImportFieldParser
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

    /// <summary>
    /// Tries to parse a date string using the preferred format, common formats, and generic parse.
    /// </summary>
    /// <param name="dateStr">The raw date string to parse.</param>
    /// <param name="preferredFormat">The caller-preferred date format string.</param>
    /// <returns>A parsed <see cref="DateOnly"/>, or <c>null</c> if parsing fails.</returns>
    internal static DateOnly? ParseDate(string dateStr, string preferredFormat)
    {
        if (DateOnly.TryParseExact(dateStr, preferredFormat, null, System.Globalization.DateTimeStyles.None, out var date))
        {
            return date;
        }

        foreach (var format in CommonDateFormats)
        {
            if (DateOnly.TryParseExact(dateStr, format, null, System.Globalization.DateTimeStyles.None, out date))
            {
                return date;
            }
        }

        return DateOnly.TryParse(dateStr, out date) ? date : null;
    }

    /// <summary>
    /// Parses an amount string applying the specified sign mode.
    /// </summary>
    /// <param name="amountStr">The raw amount string.</param>
    /// <param name="mode">Controls how the sign of the amount is interpreted.</param>
    /// <returns>The parsed decimal value, or <c>null</c> if parsing fails.</returns>
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

    /// <summary>
    /// Parses a raw amount string, handling currency symbols, parentheses, and apostrophe prefixes.
    /// </summary>
    /// <param name="amountStr">The raw amount string.</param>
    /// <returns>The parsed decimal value, or <c>null</c> if parsing fails.</returns>
    internal static decimal? ParseAmountValue(string? amountStr)
    {
        if (string.IsNullOrWhiteSpace(amountStr))
        {
            return null;
        }

        var cleaned = amountStr.Trim();

        // Strip leading apostrophe used by CSV sanitization for display safety
        // (e.g., "'-10.05" → "-10.05")
        if (cleaned.Length >= 2 && cleaned[0] == '\'' &&
            cleaned[1] is '=' or '@' or '+' or '-' or '\t' or '\r')
        {
            cleaned = cleaned[1..];
        }

        bool isNegative = cleaned.StartsWith('(') && cleaned.EndsWith(')');
        if (isNegative)
        {
            cleaned = cleaned[1..^1];
        }

        if (cleaned.StartsWith('-'))
        {
            isNegative = !isNegative;
            cleaned = cleaned[1..];
        }

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

    /// <summary>
    /// Determines the sign multiplier for a debit/credit indicator value.
    /// </summary>
    /// <param name="indicatorValue">The raw indicator value from the CSV row.</param>
    /// <param name="settings">Debit/credit indicator configuration.</param>
    /// <returns>-1 for debit, +1 for credit, or <c>null</c> if unrecognized.</returns>
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

        var debitIndicators = settings.DebitIndicators
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var creditIndicators = settings.CreditIndicators
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (debitIndicators.Any(d => string.Equals(d, trimmedValue, comparison)))
        {
            return -1;
        }

        if (creditIndicators.Any(c => string.Equals(c, trimmedValue, comparison)))
        {
            return 1;
        }

        return null;
    }
}
