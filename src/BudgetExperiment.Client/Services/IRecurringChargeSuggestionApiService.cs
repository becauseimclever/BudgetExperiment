// <copyright file="IRecurringChargeSuggestionApiService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Client.Services;

/// <summary>
/// Client-side API service for recurring charge suggestion operations.
/// </summary>
public interface IRecurringChargeSuggestionApiService
{
    /// <summary>
    /// Triggers recurring charge detection.
    /// </summary>
    /// <param name="accountId">Optional account filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of new or updated suggestions.</returns>
    Task<int> DetectAsync(Guid? accountId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets suggestions with optional filtering.
    /// </summary>
    /// <param name="accountId">Optional account filter.</param>
    /// <param name="status">Optional status filter.</param>
    /// <param name="skip">Number of items to skip.</param>
    /// <param name="take">Number of items to take.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of recurring charge suggestions.</returns>
    Task<IReadOnlyList<RecurringChargeSuggestionDto>> GetSuggestionsAsync(
        Guid? accountId = null,
        string? status = null,
        int skip = 0,
        int take = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a single suggestion by ID.
    /// </summary>
    /// <param name="id">The suggestion identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The suggestion, or null if not found.</returns>
    Task<RecurringChargeSuggestionDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Accepts a suggestion, creating a RecurringTransaction.
    /// </summary>
    /// <param name="id">The suggestion identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The acceptance result.</returns>
    Task<AcceptRecurringChargeSuggestionResultDto?> AcceptAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Dismisses a suggestion.
    /// </summary>
    /// <param name="id">The suggestion identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if dismissed successfully.</returns>
    Task<bool> DismissAsync(Guid id, CancellationToken cancellationToken = default);
}
