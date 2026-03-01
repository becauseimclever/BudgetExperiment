// <copyright file="FindMatchesResult.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// Result of finding matches for transactions.
/// </summary>
public sealed record FindMatchesResult
{
    /// <summary>
    /// Gets the matches found, grouped by transaction ID.
    /// </summary>
    public IReadOnlyDictionary<Guid, IReadOnlyList<ReconciliationMatchDto>> MatchesByTransaction { get; init; }
        = new Dictionary<Guid, IReadOnlyList<ReconciliationMatchDto>>();

    /// <summary>
    /// Gets the total number of matches found.
    /// </summary>
    public int TotalMatchesFound { get; init; }

    /// <summary>
    /// Gets the number of high confidence matches.
    /// </summary>
    public int HighConfidenceCount { get; init; }
}
