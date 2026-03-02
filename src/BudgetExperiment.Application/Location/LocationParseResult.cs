// <copyright file="LocationParseResult.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Application.Location;

/// <summary>
/// Result of attempting to parse a location from a transaction description.
/// </summary>
public sealed class LocationParseResult
{
    /// <summary>
    /// Gets or sets the original transaction description text.
    /// </summary>
    public required string OriginalText { get; init; }

    /// <summary>
    /// Gets or sets the parsed location, or <see langword="null"/> if no location was found.
    /// </summary>
    public TransactionLocationValue? Location { get; init; }

    /// <summary>
    /// Gets or sets the confidence score (0.0 – 1.0) of the parse result.
    /// </summary>
    public decimal Confidence { get; init; }

    /// <summary>
    /// Gets or sets the regex pattern name that matched, if any.
    /// </summary>
    public string? MatchedPattern { get; init; }
}
