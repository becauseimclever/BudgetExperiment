// <copyright file="CurrencyList.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Client.Models;

/// <summary>
/// Provides a static list of common ISO 4217 currencies for UI selection.
/// </summary>
public static class CurrencyList
{
    /// <summary>
    /// Gets the list of common currencies.
    /// </summary>
    public static IReadOnlyList<CurrencyOption> Currencies { get; } = new CurrencyOption[]
    {
        new("USD", "US Dollar", "$"),
        new("EUR", "Euro", "\u20ac"),
        new("GBP", "British Pound", "\u00a3"),
        new("CAD", "Canadian Dollar", "CA$"),
        new("AUD", "Australian Dollar", "A$"),
        new("JPY", "Japanese Yen", "\u00a5"),
        new("CHF", "Swiss Franc", "CHF"),
        new("SEK", "Swedish Krona", "kr"),
        new("NOK", "Norwegian Krone", "kr"),
        new("DKK", "Danish Krone", "kr"),
        new("NZD", "New Zealand Dollar", "NZ$"),
        new("MXN", "Mexican Peso", "MX$"),
        new("BRL", "Brazilian Real", "R$"),
        new("INR", "Indian Rupee", "\u20b9"),
        new("ZAR", "South African Rand", "R"),
        new("SGD", "Singapore Dollar", "S$"),
        new("HKD", "Hong Kong Dollar", "HK$"),
        new("KRW", "South Korean Won", "\u20a9"),
        new("PLN", "Polish Zloty", "z\u0142"),
        new("CZK", "Czech Koruna", "K\u010d"),
    };
}
