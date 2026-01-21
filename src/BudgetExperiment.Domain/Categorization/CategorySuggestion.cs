// <copyright file="CategorySuggestion.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Categorization;

/// <summary>
/// Represents an AI-generated suggestion for a new budget category based on transaction analysis.
/// </summary>
public sealed class CategorySuggestion
{
    /// <summary>
    /// Maximum length for category name.
    /// </summary>
    public const int MaxNameLength = 100;

    /// <summary>
    /// Maximum length for merchant pattern.
    /// </summary>
    public const int MaxPatternLength = 200;

    private List<string> _merchantPatterns = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="CategorySuggestion"/> class.
    /// </summary>
    /// <remarks>
    /// Private constructor for EF Core and factory method.
    /// </remarks>
    private CategorySuggestion()
    {
    }

    /// <summary>
    /// Gets the unique identifier.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the suggested category name.
    /// </summary>
    public string SuggestedName { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the suggested icon identifier.
    /// </summary>
    public string? SuggestedIcon { get; private set; }

    /// <summary>
    /// Gets the suggested color (hex code).
    /// </summary>
    public string? SuggestedColor { get; private set; }

    /// <summary>
    /// Gets the suggested category type.
    /// </summary>
    public CategoryType SuggestedType { get; private set; }

    /// <summary>
    /// Gets the confidence score (0.0 to 1.0).
    /// </summary>
    public decimal Confidence { get; private set; }

    /// <summary>
    /// Gets the merchant patterns that triggered this suggestion.
    /// </summary>
    public IReadOnlyList<string> MerchantPatterns => _merchantPatterns.AsReadOnly();

    /// <summary>
    /// Gets the count of transactions that would match this category.
    /// </summary>
    public int MatchingTransactionCount { get; private set; }

    /// <summary>
    /// Gets the suggestion status.
    /// </summary>
    public SuggestionStatus Status { get; private set; }

    /// <summary>
    /// Gets the owner user ID.
    /// </summary>
    public string OwnerId { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the UTC timestamp when the suggestion was created.
    /// </summary>
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>
    /// Creates a new category suggestion.
    /// </summary>
    /// <param name="suggestedName">The suggested category name.</param>
    /// <param name="suggestedType">The suggested category type.</param>
    /// <param name="merchantPatterns">The merchant patterns that triggered this suggestion.</param>
    /// <param name="matchingTransactionCount">Count of transactions that would match.</param>
    /// <param name="confidence">The confidence score (0.0 to 1.0).</param>
    /// <param name="ownerId">The owner user ID.</param>
    /// <param name="suggestedIcon">Optional suggested icon identifier.</param>
    /// <param name="suggestedColor">Optional suggested color (hex code).</param>
    /// <returns>A new <see cref="CategorySuggestion"/> instance.</returns>
    /// <exception cref="DomainException">Thrown when validation fails.</exception>
    public static CategorySuggestion Create(
        string suggestedName,
        CategoryType suggestedType,
        IEnumerable<string> merchantPatterns,
        int matchingTransactionCount,
        decimal confidence,
        string ownerId,
        string? suggestedIcon = null,
        string? suggestedColor = null)
    {
        ValidateName(suggestedName);
        ValidateConfidence(confidence);
        ValidateOwnerId(ownerId);

        var patterns = merchantPatterns?.ToList() ?? new List<string>();
        ValidateMerchantPatterns(patterns);
        ValidateTransactionCount(matchingTransactionCount);

        return new CategorySuggestion
        {
            Id = Guid.NewGuid(),
            SuggestedName = suggestedName.Trim(),
            SuggestedType = suggestedType,
            SuggestedIcon = suggestedIcon?.Trim(),
            SuggestedColor = suggestedColor?.Trim(),
            Confidence = confidence,
            _merchantPatterns = patterns,
            MatchingTransactionCount = matchingTransactionCount,
            Status = SuggestionStatus.Pending,
            OwnerId = ownerId,
            CreatedAtUtc = DateTime.UtcNow,
        };
    }

    /// <summary>
    /// Accepts the suggestion.
    /// </summary>
    /// <exception cref="DomainException">Thrown if the suggestion is not pending.</exception>
    public void Accept()
    {
        if (Status != SuggestionStatus.Pending)
        {
            throw new DomainException("Only pending suggestions can be accepted.");
        }

        Status = SuggestionStatus.Accepted;
    }

    /// <summary>
    /// Dismisses the suggestion.
    /// </summary>
    /// <exception cref="DomainException">Thrown if the suggestion is not pending.</exception>
    public void Dismiss()
    {
        if (Status != SuggestionStatus.Pending)
        {
            throw new DomainException("Only pending suggestions can be dismissed.");
        }

        Status = SuggestionStatus.Dismissed;
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Suggested name is required.");
        }

        if (name.Length > MaxNameLength)
        {
            throw new DomainException($"Suggested name cannot exceed {MaxNameLength} characters.");
        }
    }

    private static void ValidateConfidence(decimal confidence)
    {
        if (confidence < 0 || confidence > 1)
        {
            throw new DomainException("Confidence must be between 0.0 and 1.0.");
        }
    }

    private static void ValidateOwnerId(string ownerId)
    {
        if (string.IsNullOrWhiteSpace(ownerId))
        {
            throw new DomainException("Owner ID is required.");
        }
    }

    private static void ValidateMerchantPatterns(List<string> patterns)
    {
        if (patterns.Count == 0)
        {
            throw new DomainException("At least one merchant pattern is required.");
        }
    }

    private static void ValidateTransactionCount(int count)
    {
        if (count < 0)
        {
            throw new DomainException("Matching transaction count cannot be negative.");
        }
    }
}
