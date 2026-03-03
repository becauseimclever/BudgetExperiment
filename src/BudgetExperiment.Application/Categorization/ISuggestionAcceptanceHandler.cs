// <copyright file="ISuggestionAcceptanceHandler.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.Categorization;

/// <summary>
/// Handles the lifecycle of rule suggestions: acceptance, dismissal, and feedback.
/// </summary>
public interface ISuggestionAcceptanceHandler
{
    /// <summary>
    /// Accepts a suggestion and creates the corresponding rule.
    /// </summary>
    /// <param name="suggestionId">The suggestion ID to accept.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created or updated categorization rule.</returns>
    Task<CategorizationRule> AcceptSuggestionAsync(
        Guid suggestionId,
        CancellationToken ct = default);

    /// <summary>
    /// Dismisses a suggestion with an optional reason.
    /// </summary>
    /// <param name="suggestionId">The suggestion ID to dismiss.</param>
    /// <param name="reason">Optional reason for dismissal.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    Task DismissSuggestionAsync(
        Guid suggestionId,
        string? reason = null,
        CancellationToken ct = default);

    /// <summary>
    /// Provides feedback on a suggestion.
    /// </summary>
    /// <param name="suggestionId">The suggestion ID.</param>
    /// <param name="isPositive">Whether the feedback is positive.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    Task ProvideFeedbackAsync(
        Guid suggestionId,
        bool isPositive,
        CancellationToken ct = default);
}
