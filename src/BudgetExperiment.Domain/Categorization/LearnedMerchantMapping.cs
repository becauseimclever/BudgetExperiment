// <copyright file="LearnedMerchantMapping.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Categorization;

/// <summary>
/// Represents a user-learned mapping from a merchant pattern to a category.
/// These mappings are created when users manually categorize transactions.
/// </summary>
public sealed class LearnedMerchantMapping
{
    /// <summary>
    /// Maximum length for merchant pattern.
    /// </summary>
    public const int MaxPatternLength = 200;

    /// <summary>
    /// Initializes a new instance of the <see cref="LearnedMerchantMapping"/> class.
    /// </summary>
    /// <remarks>
    /// Private constructor for EF Core and factory method.
    /// </remarks>
    private LearnedMerchantMapping()
    {
    }

    /// <summary>
    /// Gets the unique identifier.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the normalized merchant pattern (uppercase).
    /// </summary>
    public string MerchantPattern { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the category ID this pattern maps to.
    /// </summary>
    public Guid CategoryId { get; private set; }

    /// <summary>
    /// Gets the owner user ID.
    /// </summary>
    public string OwnerId { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the number of times this mapping has been learned (reinforced).
    /// </summary>
    public int LearnCount { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp when the mapping was created.
    /// </summary>
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp when the mapping was last updated.
    /// </summary>
    public DateTime UpdatedAtUtc { get; private set; }

    /// <summary>
    /// Creates a new learned merchant mapping.
    /// </summary>
    /// <param name="merchantPattern">The merchant pattern to map.</param>
    /// <param name="categoryId">The category ID to map to.</param>
    /// <param name="ownerId">The owner user ID.</param>
    /// <returns>A new <see cref="LearnedMerchantMapping"/> instance.</returns>
    /// <exception cref="DomainException">Thrown when validation fails.</exception>
    public static LearnedMerchantMapping Create(
        string merchantPattern,
        Guid categoryId,
        string ownerId)
    {
        ValidatePattern(merchantPattern);
        ValidateCategoryId(categoryId);
        ValidateOwnerId(ownerId);

        var now = DateTime.UtcNow;
        return new LearnedMerchantMapping
        {
            Id = Guid.NewGuid(),
            MerchantPattern = NormalizePattern(merchantPattern),
            CategoryId = categoryId,
            OwnerId = ownerId,
            LearnCount = 1,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };
    }

    /// <summary>
    /// Increments the learn count when the same mapping is reinforced.
    /// </summary>
    public void IncrementLearnCount()
    {
        LearnCount++;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the category this pattern maps to.
    /// </summary>
    /// <param name="newCategoryId">The new category ID.</param>
    /// <exception cref="DomainException">Thrown when the category ID is empty.</exception>
    public void UpdateCategory(Guid newCategoryId)
    {
        ValidateCategoryId(newCategoryId);
        CategoryId = newCategoryId;
        LearnCount = 1; // Reset count when category changes
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private static string NormalizePattern(string pattern)
    {
        return pattern.Trim().ToUpperInvariant();
    }

    private static void ValidatePattern(string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            throw new DomainException("Merchant pattern is required.");
        }

        if (pattern.Trim().Length > MaxPatternLength)
        {
            throw new DomainException($"Merchant pattern cannot exceed {MaxPatternLength} characters.");
        }
    }

    private static void ValidateCategoryId(Guid categoryId)
    {
        if (categoryId == Guid.Empty)
        {
            throw new DomainException("Category ID is required.");
        }
    }

    private static void ValidateOwnerId(string ownerId)
    {
        if (string.IsNullOrWhiteSpace(ownerId))
        {
            throw new DomainException("Owner ID is required.");
        }
    }
}
