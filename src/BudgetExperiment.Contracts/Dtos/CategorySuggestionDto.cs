// <copyright file="CategorySuggestionDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// DTO representing an AI-generated category suggestion.
/// </summary>
public sealed record CategorySuggestionDto
{
    /// <summary>
    /// Gets the unique identifier.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Gets the suggested category name.
    /// </summary>
    public required string SuggestedName { get; init; }

    /// <summary>
    /// Gets the suggested icon.
    /// </summary>
    public string? SuggestedIcon { get; init; }

    /// <summary>
    /// Gets the suggested color.
    /// </summary>
    public string? SuggestedColor { get; init; }

    /// <summary>
    /// Gets the suggested category type.
    /// </summary>
    public required string SuggestedType { get; init; }

    /// <summary>
    /// Gets the confidence score (0-1).
    /// </summary>
    public required decimal Confidence { get; init; }

    /// <summary>
    /// Gets the merchant patterns that triggered this suggestion.
    /// </summary>
    public required IReadOnlyList<string> MerchantPatterns { get; init; }

    /// <summary>
    /// Gets the number of matching uncategorized transactions.
    /// </summary>
    public required int MatchingTransactionCount { get; init; }

    /// <summary>
    /// Gets the suggestion status.
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// Gets the source of this suggestion (PatternMatch or AiDiscovered).
    /// </summary>
    public required string Source { get; init; }

    /// <summary>
    /// Gets the AI reasoning for this suggestion (only set for AI-discovered suggestions).
    /// </summary>
    public string? Reasoning { get; init; }

    /// <summary>
    /// Gets when the suggestion was created.
    /// </summary>
    public required DateTime CreatedAtUtc { get; init; }
}
