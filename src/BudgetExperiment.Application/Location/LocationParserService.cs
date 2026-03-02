// <copyright file="LocationParserService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Text.RegularExpressions;

namespace BudgetExperiment.Application.Location;

/// <summary>
/// Regex-based parser that extracts geographic location from transaction descriptions.
/// Recognizes US city/state and Canadian city/province patterns commonly found in bank data.
/// </summary>
public sealed partial class LocationParserService : ILocationParserService
{
    /// <summary>
    /// Pattern: CITY, ST 98101 — comma-separated city-state with optional ZIP.
    /// </summary>
    private const string CityCommaStateZipPatternName = "CityCommaStateZip";

    /// <summary>
    /// Pattern: CITY ST 98101 — space-separated city-state with optional ZIP.
    /// </summary>
    private const string CitySpaceStateZipPatternName = "CitySpaceStateZip";

    /// <summary>
    /// Pattern: CITY ST embedded within a longer description (city + 2-letter state boundary).
    /// </summary>
    private const string EmbeddedCityStatePatternName = "EmbeddedCityState";

    /// <summary>
    /// Ordered parse strategies. First match wins.
    /// </summary>
    private static readonly (Regex Regex, string PatternName, decimal Confidence)[] Patterns =
    [
        // Highest confidence: City, ST ZIP — unambiguous pattern
        (CityCommaStateZipRegex(), CityCommaStateZipPatternName, 0.95m),

        // High confidence: CITY ST ZIP — has ZIP to disambiguate
        (CitySpaceStateZipRegex(), CitySpaceStateZipPatternName, 0.90m),

        // Medium-high: City, ST (no ZIP)
        (CityCommaStateRegex(), CityCommaStateZipPatternName, 0.85m),

        // Medium: CITY ST at end or before known delimiters (Date, Card, Merchant Category, etc.)
        (EmbeddedCityStateRegex(), EmbeddedCityStatePatternName, 0.70m),
    ];

    /// <inheritdoc/>
    public TransactionLocationValue? ParseFromDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        foreach (var (regex, _, _) in Patterns)
        {
            int startIndex = 0;
            while (startIndex < description.Length)
            {
                var match = regex.Match(description, startIndex);
                if (!match.Success)
                {
                    break;
                }

                var city = match.Groups["city"].Value.Trim();
                var state = match.Groups["state"].Value.Trim().ToUpperInvariant();
                var zip = match.Groups["zip"].Success ? match.Groups["zip"].Value.Trim() : null;

                // Validate the state abbreviation
                var country = UsStateData.GetCountryCode(state);
                if (country is not null && !string.IsNullOrWhiteSpace(city) && IsPlausibleCity(city))
                {
                    return TransactionLocationValue.Create(
                        city: NormalizeCity(city),
                        stateOrRegion: state,
                        country: country,
                        postalCode: string.IsNullOrWhiteSpace(zip) ? null : zip,
                        coordinates: null,
                        source: LocationSource.Parsed);
                }

                // Advance past this match's start to look for a subsequent valid match
                startIndex = match.Index + 1;
            }
        }

        return null;
    }

    /// <inheritdoc/>
    public IReadOnlyList<LocationParseResult> ParseBatch(IEnumerable<string> descriptions)
    {
        var results = new List<LocationParseResult>();

        foreach (var desc in descriptions)
        {
            var location = ParseFromDescription(desc);
            string? matchedPattern = null;
            decimal confidence = 0m;

            if (location is not null)
            {
                // Determine which pattern matched for reporting
                foreach (var (regex, patternName, patternConfidence) in Patterns)
                {
                    if (regex.IsMatch(desc))
                    {
                        var m = regex.Match(desc);
                        var state = m.Groups["state"].Value.Trim();
                        if (UsStateData.GetCountryCode(state) is not null)
                        {
                            matchedPattern = patternName;
                            confidence = patternConfidence;
                            break;
                        }
                    }
                }
            }

            results.Add(new LocationParseResult
            {
                OriginalText = desc,
                Location = location,
                Confidence = confidence,
                MatchedPattern = matchedPattern,
            });
        }

        return results;
    }

    /// <summary>
    /// Checks whether a candidate city string is plausible (not a URL, number, or very short garbage).
    /// For multi-word cities, each word is checked against the non-city word list.
    /// </summary>
    private static bool IsPlausibleCity(string city)
    {
        // Must be at least 2 characters
        if (city.Length < 2)
        {
            return false;
        }

        // Should not contain URL-like patterns
        if (city.Contains('.') || city.Contains('/') || city.Contains(':'))
        {
            return false;
        }

        // Should not be purely numeric
        if (city.All(c => char.IsDigit(c) || c == '-'))
        {
            return false;
        }

        // Check each word individually against non-city tokens
        var words = city.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        foreach (var word in words)
        {
            if (IsCommonNonCityWord(word))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Returns true for common words that appear before state abbreviations but aren't cities.
    /// </summary>
    private static bool IsCommonNonCityWord(string word)
    {
        var upper = word.ToUpperInvariant();
        return upper is "PURCHASE" or "DIRECT" or "SENT" or "CARD" or "BIL"
            or "BILL" or "MOBILE" or "DEBIT" or "CREDIT" or "PMT"
            or "PAYMENT" or "DEPOSIT" or "COM" or "VISA" or "XXX"
            or "INSIDE" or "ONLINE" or "MKTPL" or "AMZN";
    }

    /// <summary>
    /// Normalizes city capitalization — preserves original casing (bank descriptions are usually upper).
    /// </summary>
    private static string NormalizeCity(string city)
    {
        // Trim trailing non-alpha chars that might leak from regex boundaries
        return city.TrimEnd(' ', '*', '#', '-');
    }

    // City, ST 98101  or  City, ST
    [GeneratedRegex(@"\b(?<city>[A-Za-z][A-Za-z ]{1,30}),\s*(?<state>[A-Z]{2})\s+(?<zip>\d{5}(?:-\d{4})?)\b", RegexOptions.IgnoreCase)]
    private static partial Regex CityCommaStateZipRegex();

    // City, ST (without ZIP)
    [GeneratedRegex(@"\b(?<city>[A-Za-z][A-Za-z ]{1,30}),\s*(?<state>[A-Z]{2})\b", RegexOptions.IgnoreCase)]
    private static partial Regex CityCommaStateRegex();

    // CITY ST 98101
    [GeneratedRegex(@"\b(?<city>[A-Z][A-Z ]{1,30})\s+(?<state>[A-Z]{2})\s+(?<zip>\d{5}(?:-\d{4})?)\b", RegexOptions.IgnoreCase)]
    private static partial Regex CitySpaceStateZipRegex();

    // CITY ST at end or before Date/Card/Merchant/digit boundary
    // Captures 1-4 word city before a 2-letter state at a natural boundary
    [GeneratedRegex(@"\b(?<city>[A-Z][A-Z]{1,}(?:\s+[A-Z]{2,}){0,3})\s+(?<state>[A-Z]{2})(?:\s+Date\b|\s+Card\b|\s+Merchant\b|\s*$)", RegexOptions.IgnoreCase)]
    private static partial Regex EmbeddedCityStateRegex();
}
