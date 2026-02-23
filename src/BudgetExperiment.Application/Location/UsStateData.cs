// <copyright file="UsStateData.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Application.Location;

/// <summary>
/// Lookup data for US state and Canadian province abbreviations.
/// </summary>
internal static class UsStateData
{
    /// <summary>
    /// US state/territory two-letter abbreviations (50 states + DC + territories).
    /// </summary>
    internal static readonly HashSet<string> UsStates = new(StringComparer.OrdinalIgnoreCase)
    {
        "AL", "AK", "AZ", "AR", "CA", "CO", "CT", "DE", "FL", "GA",
        "HI", "ID", "IL", "IN", "IA", "KS", "KY", "LA", "ME", "MD",
        "MA", "MI", "MN", "MS", "MO", "MT", "NE", "NV", "NH", "NJ",
        "NM", "NY", "NC", "ND", "OH", "OK", "OR", "PA", "RI", "SC",
        "SD", "TN", "TX", "UT", "VT", "VA", "WA", "WV", "WI", "WY",
        "DC", "PR", "VI", "GU", "AS", "MP",
    };

    /// <summary>
    /// Canadian province/territory two-letter abbreviations.
    /// </summary>
    internal static readonly HashSet<string> CanadianProvinces = new(StringComparer.OrdinalIgnoreCase)
    {
        "AB", "BC", "MB", "NB", "NL", "NS", "NT", "NU", "ON", "PE", "QC", "SK", "YT",
    };

    /// <summary>
    /// Returns the country code for a matched state/province abbreviation.
    /// </summary>
    /// <param name="abbreviation">Two-letter state or province code.</param>
    /// <returns>"US" for US states, "CA" for Canadian provinces, or <see langword="null"/> if not recognized.</returns>
    internal static string? GetCountryCode(string abbreviation)
    {
        if (UsStates.Contains(abbreviation))
        {
            return "US";
        }

        if (CanadianProvinces.Contains(abbreviation))
        {
            return "CA";
        }

        return null;
    }
}
