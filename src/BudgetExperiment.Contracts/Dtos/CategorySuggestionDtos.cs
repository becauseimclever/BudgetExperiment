// <copyright file="CategorySuggestionDtos.cs" company="BecauseImClever">
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
    /// Gets when the suggestion was created.
    /// </summary>
    public required DateTime CreatedAtUtc { get; init; }
}

/// <summary>
/// Request to accept a category suggestion with optional customizations.
/// </summary>
public sealed record AcceptCategorySuggestionRequest
{
    /// <summary>
    /// Gets or sets the custom name to use instead of the suggested name.
    /// </summary>
    public string? CustomName { get; init; }

    /// <summary>
    /// Gets or sets the custom icon to use.
    /// </summary>
    public string? CustomIcon { get; init; }

    /// <summary>
    /// Gets or sets the custom color to use.
    /// </summary>
    public string? CustomColor { get; init; }

    /// <summary>
    /// Gets or sets whether to auto-create categorization rules.
    /// </summary>
    public bool CreateRules { get; init; } = true;
}

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

/// <summary>
/// Request for bulk accepting category suggestions.
/// </summary>
public sealed record BulkAcceptCategorySuggestionsRequest
{
    /// <summary>
    /// Gets the list of suggestion IDs to accept.
    /// </summary>
    public required IReadOnlyList<Guid> SuggestionIds { get; init; }
}

/// <summary>
/// DTO for a suggested categorization rule.
/// </summary>
public sealed record SuggestedCategoryRuleDto
{
    /// <summary>
    /// Gets the pattern to match.
    /// </summary>
    public required string Pattern { get; init; }

    /// <summary>
    /// Gets the match type (Contains, StartsWith, etc.).
    /// </summary>
    public required string MatchType { get; init; }

    /// <summary>
    /// Gets the number of transactions that would match this rule.
    /// </summary>
    public required int MatchingTransactionCount { get; init; }

    /// <summary>
    /// Gets sample transaction descriptions that would match.
    /// </summary>
    public required IReadOnlyList<string> SampleDescriptions { get; init; }
}

/// <summary>
/// Request to create rules from a category suggestion.
/// </summary>
public sealed record CreateRulesFromSuggestionRequest
{
    /// <summary>
    /// Gets the category ID to assign the rules to.
    /// </summary>
    public required Guid CategoryId { get; init; }

    /// <summary>
    /// Gets the patterns to create rules for. If null or empty, all patterns from the suggestion are used.
    /// </summary>
    public IReadOnlyList<string>? Patterns { get; init; }
}
