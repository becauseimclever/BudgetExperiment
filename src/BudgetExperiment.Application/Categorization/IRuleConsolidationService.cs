// <copyright file="IRuleConsolidationService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Application.Categorization;

/// <summary>
/// Service that orchestrates rule consolidation analysis and persists the results as
/// <see cref="RuleSuggestion"/> domain entities.
/// </summary>
public interface IRuleConsolidationService
{
    /// <summary>
    /// Loads all active categorization rules, analyzes them for consolidation opportunities,
    /// persists each identified opportunity as a <see cref="RuleSuggestion"/>, and returns the
    /// persisted entities.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The newly persisted consolidation suggestions, or an empty list when none are found.</returns>
    Task<IReadOnlyList<RuleSuggestion>> AnalyzeAndStoreAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Accepts a consolidation suggestion: creates the merged rule from the suggestion's
    /// optimized pattern, deactivates all source rules, marks the suggestion as accepted,
    /// and persists all changes atomically.
    /// </summary>
    /// <param name="suggestionId">The ID of the <see cref="RuleSuggestion"/> to accept.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The newly created merged <see cref="CategorizationRule"/>.</returns>
    /// <exception cref="DomainException">
    /// Thrown when the suggestion is not found or is not of type
    /// <see cref="SuggestionType.RuleConsolidation"/>.
    /// </exception>
    Task<CategorizationRule> AcceptConsolidationAsync(Guid suggestionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Dismisses a consolidation suggestion so it is no longer shown as a pending action.
    /// </summary>
    /// <param name="suggestionId">The ID of the <see cref="RuleSuggestion"/> to dismiss.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="DomainException">
    /// Thrown when the suggestion is not found.
    /// </exception>
    Task DismissConsolidationAsync(Guid suggestionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Undoes an accepted consolidation: reactivates the original source rules, deactivates the
    /// merged rule, and resets the suggestion back to pending so it can be re-evaluated.
    /// </summary>
    /// <param name="suggestionId">The ID of the accepted <see cref="RuleSuggestion"/> to undo.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="DomainException">
    /// Thrown when the suggestion is not found (<see cref="DomainExceptionType.NotFound"/>) or
    /// is not in the accepted state (<see cref="DomainExceptionType.InvalidState"/>).
    /// </exception>
    Task UndoConsolidationAsync(Guid suggestionId, CancellationToken cancellationToken = default);
}
