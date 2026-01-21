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
    /// Dismisses a category suggestion.
    /// </summary>
    /// <param name="id">The suggestion ID to dismiss.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if dismissed successfully.</returns>
    Task<bool> DismissSuggestionAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets suggested categorization rules for an accepted suggestion.
    /// </summary>
    /// <param name="id">The suggestion ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Suggested rules for the category.</returns>
    Task<IReadOnlyList<SuggestedRule>> GetSuggestedRulesAsync(Guid id, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of accepting a category suggestion.
/// </summary>
public sealed class AcceptSuggestionResult
{
    /// <summary>
    /// Gets a value indicating whether the operation was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Gets the ID of the suggestion that was accepted.
    /// </summary>
    public Guid SuggestionId { get; init; }

    /// <summary>
    /// Gets the ID of the created category (if successful).
    /// </summary>
    public Guid? CreatedCategoryId { get; init; }

    /// <summary>
    /// Gets the name of the created category.
    /// </summary>
    public string? CategoryName { get; init; }

    /// <summary>
    /// Gets the error message (if failed).
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="suggestionId">The suggestion ID.</param>
    /// <param name="categoryId">The created category ID.</param>
    /// <param name="categoryName">The category name.</param>
    /// <returns>A success result.</returns>
    public static AcceptSuggestionResult Succeeded(Guid suggestionId, Guid categoryId, string categoryName)
    {
        return new AcceptSuggestionResult
        {
            Success = true,
            SuggestionId = suggestionId,
            CreatedCategoryId = categoryId,
            CategoryName = categoryName,
        };
    }

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <param name="suggestionId">The suggestion ID.</param>
    /// <param name="errorMessage">The error message.</param>
    /// <returns>A failure result.</returns>
    public static AcceptSuggestionResult Failed(Guid suggestionId, string errorMessage)
    {
        return new AcceptSuggestionResult
        {
            Success = false,
            SuggestionId = suggestionId,
            ErrorMessage = errorMessage,
        };
    }
}

/// <summary>
/// Represents a suggested categorization rule.
/// </summary>
public sealed class SuggestedRule
{
    /// <summary>
    /// Gets the pattern to match against transaction descriptions.
    /// </summary>
    public required string Pattern { get; init; }

    /// <summary>
    /// Gets the suggested match type.
    /// </summary>
    public RuleMatchType MatchType { get; init; }

    /// <summary>
    /// Gets the count of uncategorized transactions that would match.
    /// </summary>
    public int MatchingTransactionCount { get; init; }

    /// <summary>
    /// Gets sample transaction descriptions.
    /// </summary>
    public IReadOnlyList<string> SampleDescriptions { get; init; } = Array.Empty<string>();
}
