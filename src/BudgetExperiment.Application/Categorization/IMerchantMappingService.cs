// <copyright file="IMerchantMappingService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Application.Categorization;

/// <summary>
/// Service for resolving merchant-to-category mappings.
/// Combines default knowledge base with user-learned mappings.
/// </summary>
public interface IMerchantMappingService
{
    /// <summary>
    /// Gets a category mapping for a transaction description.
    /// Learned mappings take precedence over default mappings.
    /// </summary>
    /// <param name="ownerId">The user ID.</param>
    /// <param name="description">The transaction description.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The mapping result or null if no mapping found.</returns>
    Task<MerchantMappingResult?> GetMappingAsync(string ownerId, string description, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds all matching merchant patterns from a collection of transaction descriptions.
    /// </summary>
    /// <param name="ownerId">The user ID.</param>
    /// <param name="descriptions">The transaction descriptions to analyze.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of pattern matches grouped by category.</returns>
    Task<IReadOnlyList<PatternMatch>> FindMatchingPatternsAsync(
        string ownerId,
        IEnumerable<string> descriptions,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a merchant mapping lookup.
/// </summary>
public readonly record struct MerchantMappingResult
{
    /// <summary>
    /// Gets the matched pattern.
    /// </summary>
    public required string Pattern { get; init; }

    /// <summary>
    /// Gets the category name.
    /// </summary>
    public required string Category { get; init; }

    /// <summary>
    /// Gets the suggested icon.
    /// </summary>
    public required string Icon { get; init; }

    /// <summary>
    /// Gets a value indicating whether this is a learned mapping.
    /// </summary>
    public required bool IsLearned { get; init; }

    /// <summary>
    /// Gets the category ID if this is a learned mapping.
    /// </summary>
    public Guid? CategoryId { get; init; }
}

/// <summary>
/// Represents a pattern match from transaction analysis.
/// </summary>
public sealed class PatternMatch
{
    /// <summary>
    /// Gets the matched pattern (merchant keyword).
    /// </summary>
    public required string Pattern { get; init; }

    /// <summary>
    /// Gets the suggested category name.
    /// </summary>
    public required string Category { get; init; }

    /// <summary>
    /// Gets the suggested icon.
    /// </summary>
    public required string Icon { get; init; }

    /// <summary>
    /// Gets the count of transactions matching this pattern.
    /// </summary>
    public int TransactionCount { get; set; }

    /// <summary>
    /// Gets the sample transaction descriptions.
    /// </summary>
    public List<string> SampleDescriptions { get; init; } = new();

    /// <summary>
    /// Gets a value indicating whether this is from a learned mapping.
    /// </summary>
    public bool IsLearned { get; init; }
}
