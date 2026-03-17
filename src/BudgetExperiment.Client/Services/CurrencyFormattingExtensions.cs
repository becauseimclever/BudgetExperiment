// <copyright file="CurrencyFormattingExtensions.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Globalization;

namespace BudgetExperiment.Client.Services;

/// <summary>
/// Extension methods for culture-aware currency formatting.
/// </summary>
public static class CurrencyFormattingExtensions
{
    /// <summary>
    /// Formats a decimal value as currency using the specified culture.
    /// </summary>
    /// <param name="value">The decimal value to format.</param>
    /// <param name="culture">The culture to use for formatting.</param>
    /// <returns>The formatted currency string.</returns>
    public static string FormatCurrency(this decimal value, CultureInfo culture)
    {
        return value.ToString("C", culture);
    }

    /// <summary>
    /// Formats a nullable decimal value as currency using the specified culture.
    /// Returns an empty string if the value is null.
    /// </summary>
    /// <param name="value">The nullable decimal value to format.</param>
    /// <param name="culture">The culture to use for formatting.</param>
    /// <returns>The formatted currency string, or empty string if null.</returns>
    public static string FormatCurrency(this decimal? value, CultureInfo culture)
    {
        return value.HasValue ? value.Value.FormatCurrency(culture) : string.Empty;
    }
}
