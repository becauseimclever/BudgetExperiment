// <copyright file="IDismissedSuggestionPatternRepository.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Repositories;

/// <summary>
/// Repository interface for dismissed suggestion patterns.
/// </summary>
public interface IDismissedSuggestionPatternRepository : IReadRepository<DismissedSuggestionPattern>, IWriteRepository<DismissedSuggestionPattern>
{
    /// <summary>
    /// Checks if a pattern has been dismissed by a user.
    /// </summary>
    /// <param name="ownerId">The owner user ID.</param>
    /// <param name="pattern">The normalized pattern.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the pattern was dismissed.</returns>
    Task<bool> IsDismissedAsync(string ownerId, string pattern, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all dismissed patterns for a user.
    /// </summary>
    /// <param name="ownerId">The owner user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Dismissed patterns for the user.</returns>
    Task<IReadOnlyList<DismissedSuggestionPattern>> GetByOwnerAsync(string ownerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a dismissed pattern by pattern and owner.
    /// </summary>
    /// <param name="ownerId">The owner user ID.</param>
    /// <param name="pattern">The normalized pattern.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The dismissed pattern or null if not found.</returns>
    Task<DismissedSuggestionPattern?> GetByPatternAsync(string ownerId, string pattern, CancellationToken cancellationToken = default);
}
