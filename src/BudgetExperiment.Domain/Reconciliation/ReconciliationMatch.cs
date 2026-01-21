// <copyright file="ReconciliationMatch.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Reconciliation;

/// <summary>
/// Represents a potential match between an imported transaction and a recurring transaction instance.
/// </summary>
public sealed class ReconciliationMatch
{
    /// <summary>
    /// The confidence score threshold for high confidence matches.
    /// </summary>
    private const decimal HighConfidenceThreshold = 0.85m;

    /// <summary>
    /// The confidence score threshold for medium confidence matches.
    /// </summary>
    private const decimal MediumConfidenceThreshold = 0.60m;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReconciliationMatch"/> class.
    /// Private constructor for factory method.
    /// </summary>
    private ReconciliationMatch()
    {
    }

    /// <summary>
    /// Gets the unique identifier.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the identifier of the imported transaction.
    /// </summary>
    public Guid ImportedTransactionId { get; private set; }

    /// <summary>
    /// Gets the identifier of the recurring transaction.
    /// </summary>
    public Guid RecurringTransactionId { get; private set; }

    /// <summary>
    /// Gets the scheduled date this match is for from the recurring transaction.
    /// </summary>
    public DateOnly RecurringInstanceDate { get; private set; }

    /// <summary>
    /// Gets the confidence score (0.0 to 1.0) indicating how likely this is a correct match.
    /// </summary>
    public decimal ConfidenceScore { get; private set; }

    /// <summary>
    /// Gets the confidence level derived from the confidence score.
    /// </summary>
    public MatchConfidenceLevel ConfidenceLevel { get; private set; }

    /// <summary>
    /// Gets the current status of this match.
    /// </summary>
    public ReconciliationMatchStatus Status { get; private set; }

    /// <summary>
    /// Gets the variance between expected and actual amount (expected - actual).
    /// Positive means paid less than expected, negative means paid more.
    /// </summary>
    public decimal AmountVariance { get; private set; }

    /// <summary>
    /// Gets the offset in days between actual transaction date and scheduled date.
    /// Positive means transaction occurred after scheduled date, negative means before.
    /// </summary>
    public int DateOffsetDays { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp when this match was created.
    /// </summary>
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp when this match was resolved (accepted/rejected/auto-matched).
    /// </summary>
    public DateTime? ResolvedAtUtc { get; private set; }

    /// <summary>
    /// Gets the budget scope (Shared or Personal).
    /// </summary>
    public BudgetScope Scope { get; private set; }

    /// <summary>
    /// Gets the owner user ID. NULL for Shared scope, user ID for Personal scope.
    /// </summary>
    public Guid? OwnerUserId { get; private set; }

    /// <summary>
    /// Creates a new reconciliation match.
    /// </summary>
    /// <param name="importedTransactionId">The imported transaction identifier.</param>
    /// <param name="recurringTransactionId">The recurring transaction identifier.</param>
    /// <param name="recurringInstanceDate">The scheduled instance date.</param>
    /// <param name="confidenceScore">The match confidence score (0.0 to 1.0).</param>
    /// <param name="amountVariance">The variance between expected and actual amount.</param>
    /// <param name="dateOffsetDays">The offset in days from scheduled date.</param>
    /// <param name="scope">The budget scope.</param>
    /// <param name="ownerUserId">The owner user ID (required for Personal scope).</param>
    /// <returns>A new <see cref="ReconciliationMatch"/> instance.</returns>
    /// <exception cref="DomainException">Thrown when validation fails.</exception>
    public static ReconciliationMatch Create(
        Guid importedTransactionId,
        Guid recurringTransactionId,
        DateOnly recurringInstanceDate,
        decimal confidenceScore,
        decimal amountVariance,
        int dateOffsetDays,
        BudgetScope scope,
        Guid? ownerUserId)
    {
        if (importedTransactionId == Guid.Empty)
        {
            throw new DomainException("Imported transaction ID is required.");
        }

        if (recurringTransactionId == Guid.Empty)
        {
            throw new DomainException("Recurring transaction ID is required.");
        }

        if (confidenceScore < 0 || confidenceScore > 1)
        {
            throw new DomainException("Confidence score must be between 0 and 1.");
        }

        if (scope == BudgetScope.Personal && ownerUserId is null)
        {
            throw new DomainException("Owner user ID is required for Personal scope.");
        }

        var confidenceLevel = DetermineConfidenceLevel(confidenceScore);

        return new ReconciliationMatch
        {
            Id = Guid.NewGuid(),
            ImportedTransactionId = importedTransactionId,
            RecurringTransactionId = recurringTransactionId,
            RecurringInstanceDate = recurringInstanceDate,
            ConfidenceScore = confidenceScore,
            ConfidenceLevel = confidenceLevel,
            Status = ReconciliationMatchStatus.Suggested,
            AmountVariance = amountVariance,
            DateOffsetDays = dateOffsetDays,
            CreatedAtUtc = DateTime.UtcNow,
            ResolvedAtUtc = null,
            Scope = scope,
            OwnerUserId = ownerUserId,
        };
    }

    /// <summary>
    /// Accepts this match, linking the transaction to the recurring instance.
    /// </summary>
    /// <exception cref="DomainException">Thrown when match is already resolved.</exception>
    public void Accept()
    {
        this.EnsureNotResolved();
        this.Status = ReconciliationMatchStatus.Accepted;
        this.ResolvedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Rejects this match, leaving the transaction unlinked.
    /// </summary>
    /// <exception cref="DomainException">Thrown when match is already resolved.</exception>
    public void Reject()
    {
        this.EnsureNotResolved();
        this.Status = ReconciliationMatchStatus.Rejected;
        this.ResolvedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Auto-matches this match due to high confidence score.
    /// </summary>
    /// <exception cref="DomainException">Thrown when match is already resolved.</exception>
    public void AutoMatch()
    {
        this.EnsureNotResolved();
        this.Status = ReconciliationMatchStatus.AutoMatched;
        this.ResolvedAtUtc = DateTime.UtcNow;
    }

    private static MatchConfidenceLevel DetermineConfidenceLevel(decimal confidenceScore)
    {
        if (confidenceScore >= HighConfidenceThreshold)
        {
            return MatchConfidenceLevel.High;
        }

        if (confidenceScore >= MediumConfidenceThreshold)
        {
            return MatchConfidenceLevel.Medium;
        }

        return MatchConfidenceLevel.Low;
    }

    private void EnsureNotResolved()
    {
        if (this.Status != ReconciliationMatchStatus.Suggested)
        {
            throw new DomainException("Match is already resolved and cannot be modified.");
        }
    }
}
