// <copyright file="ICategorySuggestionService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.Categorization;

/// <summary>
/// Service for generating and managing AI-powered category suggestions.
/// </summary>
public interface ICategorySuggestionService
{
    /// <summary>
    /// Analyzes uncategorized transactions and generates category suggestions.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of generated category suggestions.</returns>
    Task<IReadOnlyList<CategorySuggestion>> AnalyzeTransactionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all pending category suggestions for the current user.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Pending suggestions.</returns>
    Task<IReadOnlyList<CategorySuggestion>> GetPendingSuggestionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific category suggestion by ID.
    /// </summary>
    /// <param name="id">The suggestion ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The suggestion or null if not found.</returns>
    Task<CategorySuggestion?> GetSuggestionAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Accepts a category suggestion and creates the corresponding budget category.
    /// </summary>
    /// <param name="id">The suggestion ID to accept.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result of the accept operation.</returns>
    Task<AcceptSuggestionResult> AcceptSuggestionAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Accepts a category suggestion with customized category details.
    /// </summary>
    /// <param name="id">The suggestion ID to accept.</param>
    /// <param name="customName">Optional custom name for the category.</param>
    /// <param name="customIcon">Optional custom icon for the category.</param>
    /// <param name="customColor">Optional custom color for the category.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result of the accept operation.</returns>
    Task<AcceptSuggestionResult> AcceptSuggestionAsync(
        Guid id,
        string? customName,
        string? customIcon,
        string? customColor,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Accepts multiple suggestions at once.
    /// </summary>
    /// <param name="ids">The suggestion IDs to accept.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Results for each suggestion.</returns>
    Task<IReadOnlyList<AcceptSuggestionResult>> AcceptSuggestionsAsync(
        IEnumerable<Guid> ids,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets dismissed category suggestions for the current user.
    /// </summary>
    /// <param name="skip">Number of items to skip.</param>
    /// <param name="take">Number of items to take.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Dismissed suggestions.</returns>
    Task<IReadOnlyList<CategorySuggestion>> GetDismissedSuggestionsAsync(int skip, int take, CancellationToken cancellationToken = default);

    /// <summary>
    /// Dismisses a category suggestion.
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
    /// This does not change the status of existing dismissed suggestions;
    /// it only removes the pattern memory so future analysis can re-suggest those categories.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of patterns cleared.</returns>
    Task<int> ClearDismissedPatternsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets suggested categorization rules for an accepted suggestion.
    /// </summary>
    /// <param name="id">The suggestion ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Suggested rules for the category.</returns>
    Task<IReadOnlyList<SuggestedRule>> GetSuggestedRulesAsync(Guid id, CancellationToken cancellationToken = default);
}
