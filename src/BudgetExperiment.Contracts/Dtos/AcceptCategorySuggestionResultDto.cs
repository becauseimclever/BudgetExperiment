// <copyright file="AcceptCategorySuggestionResultDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// Result of accepting a category suggestion.
/// </summary>
public sealed record AcceptCategorySuggestionResultDto
{
    /// <summary>
    /// Gets the suggestion ID that was processed.
    /// </summary>
    public required Guid SuggestionId { get; init; }

    /// <summary>
    /// Gets whether the accept was successful.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Gets the created category ID (if successful).
    /// </summary>
    public Guid? CategoryId { get; init; }

    /// <summary>
    /// Gets the created category name (if successful).
    /// </summary>
    public string? CategoryName { get; init; }

    /// <summary>
    /// Gets the error message (if failed).
    /// </summary>
    public string? ErrorMessage { get; init; }
}
