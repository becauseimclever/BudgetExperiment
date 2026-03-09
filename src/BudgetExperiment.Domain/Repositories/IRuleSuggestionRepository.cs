// <copyright file="IRuleSuggestionRepository.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Repositories;

/// <summary>
/// Repository interface for AI-generated rule suggestions.
/// </summary>
public interface IRuleSuggestionRepository : IReadRepository<RuleSuggestion>, IWriteRepository<RuleSuggestion>
{
    /// <summary>
    /// Gets all pending suggestions ordered by creation date (newest first).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Pending suggestions.</returns>
    Task<IReadOnlyList<RuleSuggestion>> GetPendingAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets pending suggestions filtered by type.
    /// </summary>
    /// <param name="type">The suggestion type to filter by.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Pending suggestions of the specified type.</returns>
    Task<IReadOnlyList<RuleSuggestion>> GetPendingByTypeAsync(SuggestionType type, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets suggestions by status.
    /// </summary>
    /// <param name="status">The status to filter by.</param>
    /// <param name="skip">Items to skip.</param>
    /// <param name="take">Items to take.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Suggestions with the specified status.</returns>
    Task<IReadOnlyList<RuleSuggestion>> GetByStatusAsync(SuggestionStatus status, int skip, int take, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a suggestion with the same pattern already exists (to avoid duplicates).
    /// </summary>
    /// <param name="pattern">The pattern to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if a pending suggestion with the pattern exists.</returns>
    Task<bool> ExistsPendingWithPatternAsync(string pattern, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a pending suggestion already exists for a specific rule and suggestion type.
    /// </summary>
    /// <param name="ruleId">The target rule ID.</param>
    /// <param name="type">The suggestion type.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if a pending suggestion exists for the rule.</returns>
    Task<bool> ExistsPendingForRuleAsync(Guid ruleId, SuggestionType type, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a pending suggestion already exists for a set of conflicting rules.
    /// </summary>
    /// <param name="ruleIds">The conflicting rule IDs.</param>
    /// <param name="type">The suggestion type.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if a pending suggestion exists for the same set of rules.</returns>
    Task<bool> ExistsPendingForRulesAsync(IReadOnlyList<Guid> ruleIds, SuggestionType type, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds multiple suggestions in a batch.
    /// </summary>
    /// <param name="suggestions">The suggestions to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task AddRangeAsync(IEnumerable<RuleSuggestion> suggestions, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the patterns of dismissed new-rule suggestions for feedback-informed prompting.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Distinct patterns from dismissed new-rule suggestions.</returns>
    Task<IReadOnlyList<string>> GetDismissedNewRulePatternsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets accepted new-rule suggestions with their category IDs for feedback-informed prompting.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Accepted suggestion patterns and their assigned category IDs.</returns>
    Task<IReadOnlyList<(string Pattern, Guid CategoryId)>> GetAcceptedNewRulesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of reviewed suggestions (accepted + dismissed) grouped by type and status.
    /// Used for computing acceptance rates per suggestion type.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Counts keyed by (type, status).</returns>
    Task<IReadOnlyDictionary<(SuggestionType Type, SuggestionStatus Status), int>> GetReviewedCountsByTypeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of pending suggestions grouped by type.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Counts keyed by suggestion type.</returns>
    Task<IReadOnlyDictionary<SuggestionType, int>> GetPendingCountsByTypeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the average confidence score for accepted and dismissed suggestions.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A tuple of (average accepted confidence, average dismissed confidence). Null if no suggestions in that status.</returns>
    Task<(decimal? AcceptedAvgConfidence, decimal? DismissedAvgConfidence)> GetAverageConfidenceByStatusAsync(CancellationToken cancellationToken = default);
}
