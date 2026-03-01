// <copyright file="AcceptSuggestionResult.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Application.Categorization;

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
