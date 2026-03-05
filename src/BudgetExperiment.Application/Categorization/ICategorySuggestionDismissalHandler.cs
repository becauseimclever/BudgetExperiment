// <copyright file="ICategorySuggestionDismissalHandler.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Application.Categorization;

/// <summary>
/// Handles dismissing and restoring category suggestions.
/// </summary>
public interface ICategorySuggestionDismissalHandler
{
    /// <summary>
    /// Dismisses a category suggestion and records the dismissed pattern.
    /// </summary>
    /// <param name="id">The suggestion ID to dismiss.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if dismissed successfully.</returns>
    Task<bool> DismissSuggestionAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores a dismissed category suggestion back to pending status.
    /// </summary>
    /// <param name="id">The suggestion ID to restore.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if restored successfully.</returns>
    Task<bool> RestoreSuggestionAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears all dismissed suggestion patterns for the current user.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of patterns cleared.</returns>
    Task<int> ClearDismissedPatternsAsync(CancellationToken cancellationToken = default);
}
