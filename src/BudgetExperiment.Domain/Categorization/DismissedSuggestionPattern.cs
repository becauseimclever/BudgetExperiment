// <copyright file="DismissedSuggestionPattern.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Categorization;

/// <summary>
/// Represents a dismissed suggestion pattern to prevent re-suggesting the same category.
/// </summary>
public sealed class DismissedSuggestionPattern
{
    /// <summary>
    /// Maximum length for pattern.
    /// </summary>
    public const int MaxPatternLength = 200;

    /// <summary>
    /// Initializes a new instance of the <see cref="DismissedSuggestionPattern"/> class.
    /// </summary>
    /// <remarks>
    /// Private constructor for EF Core and factory method.
    /// </remarks>
    private DismissedSuggestionPattern()
    {
    }

    /// <summary>
    /// Gets the unique identifier.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the normalized pattern (uppercase).
    /// </summary>
    public string Pattern { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the owner user ID.
    /// </summary>
    public string OwnerId { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the UTC timestamp when the pattern was dismissed.
    /// </summary>
    public DateTime DismissedAtUtc { get; private set; }

    /// <summary>
    /// Creates a new dismissed suggestion pattern.
    /// </summary>
    /// <param name="pattern">The pattern that was dismissed.</param>
    /// <param name="ownerId">The owner user ID.</param>
    /// <returns>A new <see cref="DismissedSuggestionPattern"/> instance.</returns>
    /// <exception cref="DomainException">Thrown when validation fails.</exception>
    public static DismissedSuggestionPattern Create(string pattern, string ownerId)
    {
        ValidatePattern(pattern);
        ValidateOwnerId(ownerId);

        return new DismissedSuggestionPattern
        {
            Id = Guid.NewGuid(),
            Pattern = NormalizePattern(pattern),
            OwnerId = ownerId,
            DismissedAtUtc = DateTime.UtcNow,
        };
    }

    private static string NormalizePattern(string pattern)
    {
        return pattern.Trim().ToUpperInvariant();
    }

    private static void ValidatePattern(string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            throw new DomainException("Pattern is required.");
        }

        if (pattern.Trim().Length > MaxPatternLength)
        {
            throw new DomainException($"Pattern cannot exceed {MaxPatternLength} characters.");
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
