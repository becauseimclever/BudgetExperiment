// <copyright file="ITransactionMatcher.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain;

/// <summary>
/// Matches imported transactions with recurring transaction instances.
/// </summary>
public interface ITransactionMatcher
{
    /// <summary>
    /// Finds potential matches between an imported transaction and recurring instances.
    /// </summary>
    /// <param name="transaction">The imported transaction to match.</param>
    /// <param name="candidates">The recurring instances to consider as match candidates.</param>
    /// <param name="tolerances">The matching tolerances to use.</param>
    /// <returns>A collection of potential matches ordered by confidence score descending.</returns>
    IReadOnlyList<TransactionMatchResult> FindMatches(
        Transaction transaction,
        IEnumerable<RecurringInstanceInfo> candidates,
        MatchingTolerances tolerances);

    /// <summary>
    /// Calculates the confidence score for matching a transaction to a recurring instance.
    /// </summary>
    /// <param name="transaction">The imported transaction.</param>
    /// <param name="candidate">The recurring instance candidate.</param>
    /// <param name="tolerances">The matching tolerances to use.</param>
    /// <returns>The match result with confidence score, or null if not a viable match.</returns>
    TransactionMatchResult? CalculateMatch(
        Transaction transaction,
        RecurringInstanceInfo candidate,
        MatchingTolerances tolerances);
}
