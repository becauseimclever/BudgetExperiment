// <copyright file="MatchingTolerances.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain;

/// <summary>
/// Configuration for how strictly to match imported transactions to recurring instances.
/// </summary>
public sealed class MatchingTolerances : IEquatable<MatchingTolerances>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MatchingTolerances"/> class.
    /// Private constructor for factory method.
    /// </summary>
    private MatchingTolerances()
    {
    }

    /// <summary>
    /// Gets the maximum days before/after scheduled date to consider a match.
    /// </summary>
    public int DateToleranceDays { get; private set; }

    /// <summary>
    /// Gets the maximum percentage variance in amount to consider a match (0.0 to 1.0).
    /// </summary>
    public decimal AmountTolerancePercent { get; private set; }

    /// <summary>
    /// Gets the maximum absolute amount variance to consider a match.
    /// </summary>
    public decimal AmountToleranceAbsolute { get; private set; }

    /// <summary>
    /// Gets the minimum description similarity score to consider a match (0.0 to 1.0).
    /// </summary>
    public decimal DescriptionSimilarityThreshold { get; private set; }

    /// <summary>
    /// Gets the minimum confidence score for automatic matching without review.
    /// </summary>
    public decimal AutoMatchThreshold { get; private set; }

    /// <summary>
    /// Gets the default matching tolerances with sensible out-of-box behavior.
    /// </summary>
    public static MatchingTolerances Default => new()
    {
        DateToleranceDays = 7,
        AmountTolerancePercent = 0.10m,
        AmountToleranceAbsolute = 10.00m,
        DescriptionSimilarityThreshold = 0.6m,
        AutoMatchThreshold = 0.85m,
    };

    /// <summary>
    /// Creates a new instance of matching tolerances with the specified values.
    /// </summary>
    /// <param name="dateToleranceDays">Maximum days before/after scheduled date.</param>
    /// <param name="amountTolerancePercent">Maximum percentage variance (0.0 to 1.0).</param>
    /// <param name="amountToleranceAbsolute">Maximum absolute amount variance.</param>
    /// <param name="descriptionSimilarityThreshold">Minimum description similarity (0.0 to 1.0).</param>
    /// <param name="autoMatchThreshold">Minimum confidence for auto-matching (0.0 to 1.0).</param>
    /// <returns>A new <see cref="MatchingTolerances"/> instance.</returns>
    /// <exception cref="DomainException">Thrown when validation fails.</exception>
    public static MatchingTolerances Create(
        int dateToleranceDays,
        decimal amountTolerancePercent,
        decimal amountToleranceAbsolute,
        decimal descriptionSimilarityThreshold,
        decimal autoMatchThreshold)
    {
        if (dateToleranceDays < 0)
        {
            throw new DomainException("Date tolerance days cannot be negative.");
        }

        if (amountTolerancePercent < 0 || amountTolerancePercent > 1)
        {
            throw new DomainException("Amount tolerance percent must be between 0 and 1.");
        }

        if (amountToleranceAbsolute < 0)
        {
            throw new DomainException("Amount tolerance absolute cannot be negative.");
        }

        if (descriptionSimilarityThreshold < 0 || descriptionSimilarityThreshold > 1)
        {
            throw new DomainException("Description similarity threshold must be between 0 and 1.");
        }

        if (autoMatchThreshold < 0 || autoMatchThreshold > 1)
        {
            throw new DomainException("Auto match threshold must be between 0 and 1.");
        }

        return new MatchingTolerances
        {
            DateToleranceDays = dateToleranceDays,
            AmountTolerancePercent = amountTolerancePercent,
            AmountToleranceAbsolute = amountToleranceAbsolute,
            DescriptionSimilarityThreshold = descriptionSimilarityThreshold,
            AutoMatchThreshold = autoMatchThreshold,
        };
    }

    /// <inheritdoc/>
    public bool Equals(MatchingTolerances? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return this.DateToleranceDays == other.DateToleranceDays
            && this.AmountTolerancePercent == other.AmountTolerancePercent
            && this.AmountToleranceAbsolute == other.AmountToleranceAbsolute
            && this.DescriptionSimilarityThreshold == other.DescriptionSimilarityThreshold
            && this.AutoMatchThreshold == other.AutoMatchThreshold;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return this.Equals(obj as MatchingTolerances);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HashCode.Combine(
            this.DateToleranceDays,
            this.AmountTolerancePercent,
            this.AmountToleranceAbsolute,
            this.DescriptionSimilarityThreshold,
            this.AutoMatchThreshold);
    }
}
