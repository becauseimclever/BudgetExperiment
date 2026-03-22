// <copyright file="IRecurringChargeDetectionService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;
using BudgetExperiment.Domain.Recurring;

namespace BudgetExperiment.Application.Recurring;

/// <summary>
/// Orchestrates recurring charge detection, suggestion listing, and acceptance/dismissal workflows.
/// </summary>
public interface IRecurringChargeDetectionService
{
    /// <summary>
    /// Runs recurrence detection against transactions for the specified account (or all accounts).
    /// New suggestions are created; existing pending suggestions are updated if the pattern changed.
    /// </summary>
    /// <param name="accountId">Optional account to scope detection. Null runs across all accounts.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of new or updated suggestions.</returns>
    Task<int> DetectAsync(Guid? accountId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a paged list of recurring charge suggestions with optional filters.
    /// </summary>
    /// <param name="accountId">Optional account filter.</param>
    /// <param name="status">Optional status filter.</param>
    /// <param name="skip">Number of items to skip.</param>
    /// <param name="take">Number of items to take.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A tuple of suggestions and total count.</returns>
    Task<(IReadOnlyList<RecurringChargeSuggestion> Items, long TotalCount)> GetSuggestionsAsync(
        Guid? accountId = null,
        SuggestionStatus? status = null,
        int skip = 0,
        int take = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a single suggestion by ID.
    /// </summary>
    /// <param name="id">The suggestion identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The suggestion, or null if not found.</returns>
    Task<RecurringChargeSuggestion?> GetSuggestionByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Accepts a suggestion: creates a <see cref="RecurringTransaction"/>, generates an import pattern,
    /// links matching transactions, and marks the suggestion as accepted.
    /// </summary>
    /// <param name="id">The suggestion identifier to accept.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result containing the created recurring transaction ID and number of linked transactions.</returns>
    /// <exception cref="DomainException">Thrown when the suggestion is not found or not in Pending status.</exception>
    Task<AcceptRecurringChargeSuggestionResult> AcceptAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Dismisses a suggestion, hiding it from the default view.
    /// </summary>
    /// <param name="id">The suggestion identifier to dismiss.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="DomainException">Thrown when the suggestion is not found.</exception>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task DismissAsync(Guid id, CancellationToken cancellationToken = default);
}
