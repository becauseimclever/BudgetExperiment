// <copyright file="IRecurringChargeSuggestionRepository.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Repositories;

/// <summary>
/// Repository interface for recurring charge suggestions.
/// </summary>
public interface IRecurringChargeSuggestionRepository : IReadRepository<RecurringChargeSuggestion>, IWriteRepository<RecurringChargeSuggestion>
{
    /// <summary>
    /// Gets suggestions filtered by status for an account.
    /// </summary>
    /// <param name="accountId">Optional account filter.</param>
    /// <param name="status">Optional status filter.</param>
    /// <param name="skip">Items to skip.</param>
    /// <param name="take">Items to take.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Filtered suggestions sorted by confidence descending.</returns>
    Task<IReadOnlyList<RecurringChargeSuggestion>> GetByStatusAsync(
        Guid? accountId,
        SuggestionStatus? status,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts suggestions matching the optional filters.
    /// </summary>
    /// <param name="accountId">Optional account filter.</param>
    /// <param name="status">Optional status filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The count of matching suggestions.</returns>
    Task<long> CountByStatusAsync(
        Guid? accountId,
        SuggestionStatus? status,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds an existing suggestion by normalized description and account.
    /// Used for duplicate detection during re-detection.
    /// </summary>
    /// <param name="normalizedDescription">The normalized description.</param>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The existing suggestion or null.</returns>
    Task<RecurringChargeSuggestion?> GetByNormalizedDescriptionAndAccountAsync(
        string normalizedDescription,
        Guid accountId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds multiple suggestions in a batch.
    /// </summary>
    /// <param name="suggestions">The suggestions to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task AddRangeAsync(
        IEnumerable<RecurringChargeSuggestion> suggestions,
        CancellationToken cancellationToken = default);
}
