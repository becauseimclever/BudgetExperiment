// <copyright file="ILocationParserService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Application.Location;

/// <summary>
/// Parses geographic location data from transaction description text.
/// </summary>
public interface ILocationParserService
{
    /// <summary>
    /// Attempts to parse a location from a single transaction description.
    /// </summary>
    /// <param name="description">The raw transaction description text.</param>
    /// <returns>A parsed <see cref="TransactionLocationValue"/> if found; otherwise <see langword="null"/>.</returns>
    TransactionLocationValue? ParseFromDescription(string description);

    /// <summary>
    /// Parses locations from a batch of transaction descriptions.
    /// </summary>
    /// <param name="descriptions">The raw transaction descriptions.</param>
    /// <returns>A list of parse results, one per input description.</returns>
    IReadOnlyList<LocationParseResult> ParseBatch(IEnumerable<string> descriptions);
}
