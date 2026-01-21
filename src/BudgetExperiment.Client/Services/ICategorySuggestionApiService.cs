// <copyright file="ICategorySuggestionApiService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Client.Services;

/// <summary>
/// Client service for category suggestion API operations.
/// </summary>
public interface ICategorySuggestionApiService
{
    /// <summary>
    /// Analyzes uncategorized transactions and generates category suggestions.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The generated suggestions.</returns>
    Task<IReadOnlyList<CategorySuggestionDto>> AnalyzeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all pending category suggestions.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The pending suggestions.</returns>
    Task<IReadOnlyList<CategorySuggestionDto>> GetPendingAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific suggestion by ID.
    /// </summary>
    /// <param name="id">The suggestion ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The suggestion if found.</returns>
    Task<CategorySuggestionDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Accepts a category suggestion and creates the category.
    /// </summary>
    /// <param name="id">The suggestion ID.</param>
    /// <param name="request">Optional customization.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The accept result.</returns>
    Task<AcceptCategorySuggestionResultDto> AcceptAsync(
        Guid id,
        AcceptCategorySuggestionRequest? request = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Dismisses a category suggestion.
    /// </summary>
    /// <param name="id">The suggestion ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if dismissed.</returns>
    Task<bool> DismissAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Accepts multiple suggestions in bulk.
    /// </summary>
    /// <param name="suggestionIds">The suggestion IDs to accept.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The results for each suggestion.</returns>
    Task<IReadOnlyList<AcceptCategorySuggestionResultDto>> BulkAcceptAsync(
        IEnumerable<Guid> suggestionIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets suggested rules for a suggestion.
    /// </summary>
    /// <param name="id">The suggestion ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The suggested rules.</returns>
    Task<IReadOnlyList<SuggestedCategoryRuleDto>> PreviewRulesAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates rules from a suggestion.
    /// </summary>
    /// <param name="id">The suggestion ID.</param>
    /// <param name="request">The create rules request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The create rules result.</returns>
    Task<CreateRulesFromSuggestionResult> CreateRulesAsync(
        Guid id,
        CreateRulesFromSuggestionRequest request,
        CancellationToken cancellationToken = default);
}
