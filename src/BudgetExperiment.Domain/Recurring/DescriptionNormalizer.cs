// <copyright file="DescriptionNormalizer.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Text.RegularExpressions;

namespace BudgetExperiment.Domain.Recurring;

/// <summary>
/// Normalizes transaction descriptions for grouping by stripping bank prefixes,
/// trailing reference numbers, trailing dates, and collapsing whitespace.
/// </summary>
public static partial class DescriptionNormalizer
{
    private static readonly string[] BankPrefixes =
    [
        "POS PURCHASE",
        "POS DEBIT",
        "POS REFUND",
        "POS",
        "PURCHASE",
        "CHECKCARD",
        "CHECK CARD",
        "DEBIT CARD",
        "DEBIT",
        "ACH DEBIT",
        "ACH CREDIT",
        "ACH",
        "VISA DEBIT",
        "VISA",
        "MASTERCARD",
        "RECURRING PAYMENT",
        "RECURRING",
        "ELECTRONIC",
        "PREAUTHORIZED",
        "PRE-AUTHORIZED",
        "PENDING",
        "POINT OF SALE",
    ];

    /// <summary>
    /// Normalizes a transaction description for grouping purposes.
    /// Strips bank prefixes, trailing reference numbers, trailing dates,
    /// and collapses whitespace. Returns uppercase for consistent grouping.
    /// </summary>
    /// <param name="description">The raw transaction description.</param>
    /// <returns>The normalized description in uppercase, or empty string if input is null/whitespace.</returns>
    public static string Normalize(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return string.Empty;
        }

        var result = description.Trim().ToUpperInvariant();

        // Strip bank prefixes (longest first to avoid partial matches)
        foreach (var prefix in BankPrefixes.OrderByDescending(p => p.Length))
        {
            if (result.StartsWith(prefix, StringComparison.Ordinal))
            {
                result = result[prefix.Length..].TrimStart();
            }
        }

        // Strip trailing date patterns (MM/DD/YYYY, MM/DD/YY, MM-DD-YYYY, YYYY-MM-DD, etc.)
        result = TrailingDatePattern().Replace(result, string.Empty).TrimEnd();

        // Strip trailing reference/confirmation numbers (sequences of 4+ digits, possibly with dashes)
        result = TrailingReferencePattern().Replace(result, string.Empty).TrimEnd();

        // Strip trailing pound sign with numbers (e.g., #1234)
        result = TrailingHashNumberPattern().Replace(result, string.Empty).TrimEnd();

        // Collapse multiple spaces into one
        result = MultipleSpacesPattern().Replace(result, " ");

        return result.Trim();
    }

    [GeneratedRegex(@"\s+\d{1,2}[/\-]\d{1,2}[/\-]\d{2,4}\s*$")]
    private static partial Regex TrailingDatePattern();

    [GeneratedRegex(@"\s+[\d\-]{4,}\s*$")]
    private static partial Regex TrailingReferencePattern();

    [GeneratedRegex(@"\s*#\d+\s*$")]
    private static partial Regex TrailingHashNumberPattern();

    [GeneratedRegex(@"\s{2,}")]
    private static partial Regex MultipleSpacesPattern();
}
