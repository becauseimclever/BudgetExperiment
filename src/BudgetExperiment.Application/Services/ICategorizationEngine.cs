// <copyright file="ICategorizationEngine.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.Services;

/// <summary>
/// Service for automatic transaction categorization based on rules.
/// </summary>
public interface ICategorizationEngine
{
    /// <summary>
    /// Finds the best matching category for a transaction description.
    /// Rules are evaluated in priority order (lower priority number = evaluated first).
    /// </summary>
    /// <param name="description">The transaction description to match.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The category ID if a rule matches, null otherwise.</returns>
    Task<Guid?> FindMatchingCategoryAsync(string description, CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies categorization rules to multiple transactions.
    /// </summary>
    /// <param name="transactionIds">The transactions to categorize. If null, all uncategorized transactions are processed.</param>
    /// <param name="overwriteExisting">If true, replaces existing categories; otherwise skips already-categorized transactions.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result with counts of categorized transactions.</returns>
    Task<CategorizationResult> ApplyRulesAsync(
        IEnumerable<Guid>? transactionIds,
        bool overwriteExisting = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests a rule pattern against existing transactions without applying.
    /// </summary>
    /// <param name="matchType">The match type to test.</param>
    /// <param name="pattern">The pattern to test.</param>
    /// <param name="caseSensitive">Whether matching is case-sensitive.</param>
    /// <param name="limit">Maximum transactions to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of matching transaction descriptions.</returns>
    Task<IReadOnlyList<string>> TestPatternAsync(
        RuleMatchType matchType,
        string pattern,
        bool caseSensitive,
        int limit = 10,
        CancellationToken cancellationToken = default);
}
