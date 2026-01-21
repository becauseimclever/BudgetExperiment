// <copyright file="ICategorySuggestionRepository.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Repositories;

/// <summary>
/// Repository interface for category suggestions.
/// </summary>
public interface ICategorySuggestionRepository : IReadRepository<CategorySuggestion>, IWriteRepository<CategorySuggestion>
{
    /// <summary>
    /// Gets all pending suggestions for a user ordered by creation date (newest first).
    /// </summary>
    /// <param name="ownerId">The owner user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Pending suggestions.</returns>
    Task<IReadOnlyList<CategorySuggestion>> GetPendingByOwnerAsync(string ownerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets suggestions by status for a user.
    /// </summary>
    /// <param name="ownerId">The owner user ID.</param>
    /// <param name="status">The status to filter by.</param>
    /// <param name="skip">Items to skip.</param>
    /// <param name="take">Items to take.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Suggestions with the specified status.</returns>
    Task<IReadOnlyList<CategorySuggestion>> GetByStatusAsync(
        string ownerId,
        SuggestionStatus status,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a pending suggestion with the same name already exists for a user.
    /// </summary>
    /// <param name="ownerId">The owner user ID.</param>
    /// <param name="suggestedName">The suggested category name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if a pending suggestion with the name exists.</returns>
    Task<bool> ExistsPendingWithNameAsync(string ownerId, string suggestedName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds multiple suggestions in a batch.
    /// </summary>
    /// <param name="suggestions">The suggestions to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task AddRangeAsync(IEnumerable<CategorySuggestion> suggestions, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all pending suggestions for a user (for re-analysis).
    /// </summary>
    /// <param name="ownerId">The owner user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DeletePendingByOwnerAsync(string ownerId, CancellationToken cancellationToken = default);
}
