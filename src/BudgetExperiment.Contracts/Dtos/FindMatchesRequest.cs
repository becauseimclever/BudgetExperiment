// <copyright file="FindMatchesRequest.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// Request to find matches for transactions.
/// </summary>
public sealed record FindMatchesRequest
{
    /// <summary>
    /// Gets the transaction IDs to find matches for.
    /// </summary>
    public IReadOnlyList<Guid> TransactionIds { get; init; } = [];

    /// <summary>
    /// Gets the date range start for recurring instances to consider.
    /// </summary>
    public DateOnly StartDate { get; init; }

    /// <summary>
    /// Gets the date range end for recurring instances to consider.
    /// </summary>
    public DateOnly EndDate { get; init; }

    /// <summary>
    /// Gets custom tolerances (optional, uses defaults if null).
    /// </summary>
    public MatchingTolerancesDto? Tolerances { get; init; }
}
