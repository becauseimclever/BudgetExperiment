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

    /// <summary>
    /// Learns a merchant-to-category mapping from a manual categorization.
    /// </summary>
    /// <param name="ownerId">The user ID.</param>
    /// <param name="description">The transaction description.</param>
    /// <param name="categoryId">The category ID the user assigned.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task LearnFromCategorizationAsync(
        string ownerId,
        string description,
        Guid categoryId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all learned mappings for a user.
    /// </summary>
    /// <param name="ownerId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The learned mappings.</returns>
    Task<IReadOnlyList<LearnedMerchantMappingInfo>> GetLearnedMappingsAsync(
        string ownerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a learned mapping.
    /// </summary>
    /// <param name="ownerId">The user ID.</param>
    /// <param name="mappingId">The mapping ID to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if deleted, false if not found.</returns>
    Task<bool> DeleteLearnedMappingAsync(
        string ownerId,
        Guid mappingId,
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

/// <summary>
/// Information about a learned merchant mapping.
/// </summary>
public sealed record LearnedMerchantMappingInfo
{
    /// <summary>
    /// Gets the mapping ID.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Gets the merchant pattern.
    /// </summary>
    public required string MerchantPattern { get; init; }

    /// <summary>
    /// Gets the category ID.
    /// </summary>
    public required Guid CategoryId { get; init; }

    /// <summary>
    /// Gets the category name.
    /// </summary>
    public required string CategoryName { get; init; }

    /// <summary>
    /// Gets the number of times this mapping has been learned.
    /// </summary>
    public required int LearnCount { get; init; }

    /// <summary>
    /// Gets the creation timestamp.
    /// </summary>
    public required DateTime CreatedAtUtc { get; init; }

    /// <summary>
    /// Gets the last update timestamp.
    /// </summary>
    public required DateTime UpdatedAtUtc { get; init; }
}
