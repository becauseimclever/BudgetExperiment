// <copyright file="RecurringChargeSuggestion.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Recurring;

/// <summary>
/// Represents a detected recurring charge pattern that is suggested to the user.
/// The user can accept (creating a RecurringTransaction), dismiss, or restore it.
/// </summary>
public sealed class RecurringChargeSuggestion
{
    /// <summary>
    /// Maximum length for description fields.
    /// </summary>
    public const int MaxDescriptionLength = 500;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecurringChargeSuggestion"/> class.
    /// </summary>
    /// <remarks>Private constructor for EF Core and factory method.</remarks>
    private RecurringChargeSuggestion()
    {
    }

    /// <summary>
    /// Gets the unique identifier.
    /// </summary>
    public Guid Id
    {
        get; private set;
    }

    /// <summary>
    /// Gets the account identifier this suggestion belongs to.
    /// </summary>
    public Guid AccountId
    {
        get; private set;
    }

    /// <summary>
    /// Gets the normalized description used for grouping and duplicate detection.
    /// </summary>
    public string NormalizedDescription { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the original sample description for display purposes.
    /// </summary>
    public string SampleDescription { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the average monetary amount across matching transactions.
    /// </summary>
    public MoneyValue AverageAmount { get; private set; } = null!;

    /// <summary>
    /// Gets the detected recurrence frequency.
    /// </summary>
    public RecurrenceFrequency DetectedFrequency
    {
        get; private set;
    }

    /// <summary>
    /// Gets the detected interval (e.g. 1 for monthly, 2 for every-other-month).
    /// </summary>
    public int DetectedInterval
    {
        get; private set;
    }

    /// <summary>
    /// Gets the confidence score (0.0–1.0).
    /// </summary>
    public decimal Confidence
    {
        get; private set;
    }

    /// <summary>
    /// Gets the number of matching transactions.
    /// </summary>
    public int MatchingTransactionCount
    {
        get; private set;
    }

    /// <summary>
    /// Gets the date of the earliest matching transaction.
    /// </summary>
    public DateOnly FirstOccurrence
    {
        get; private set;
    }

    /// <summary>
    /// Gets the date of the most recent matching transaction.
    /// </summary>
    public DateOnly LastOccurrence
    {
        get; private set;
    }

    /// <summary>
    /// Gets the most-used category ID from matched transactions, if any.
    /// </summary>
    public Guid? CategoryId
    {
        get; private set;
    }

    /// <summary>
    /// Gets the suggestion status (Pending, Accepted, or Dismissed).
    /// </summary>
    public SuggestionStatus Status
    {
        get; private set;
    }

    /// <summary>
    /// Gets the ID of the RecurringTransaction created when the suggestion is accepted.
    /// </summary>
    public Guid? AcceptedRecurringTransactionId
    {
        get; private set;
    }

    /// <summary>
    /// Gets the owner user ID. Null for shared items.
    /// </summary>
    public Guid? OwnerUserId
    {
        get; private set;
    }

    /// <summary>
    /// Gets the user ID of who triggered the detection.
    /// </summary>
    public Guid CreatedByUserId
    {
        get; private set;
    }

    /// <summary>
    /// Gets the UTC timestamp when the suggestion was created.
    /// </summary>
    public DateTime CreatedAtUtc
    {
        get; private set;
    }

    /// <summary>
    /// Gets the UTC timestamp when the suggestion was last updated.
    /// </summary>
    public DateTime UpdatedAtUtc
    {
        get; private set;
    }

    /// <summary>
    /// Creates a new recurring charge suggestion from a detected pattern.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="pattern">The detected pattern to create a suggestion from.</param>
    /// <param name="createdByUserId">The user who triggered detection.</param>
    /// <param name="ownerUserId">The owner user ID (null for shared items).</param>
    /// <returns>A new <see cref="RecurringChargeSuggestion"/> instance.</returns>
    /// <exception cref="DomainException">Thrown when validation fails.</exception>
    public static RecurringChargeSuggestion Create(
        Guid accountId,
        DetectedPattern pattern,
        Guid createdByUserId,
        Guid? ownerUserId = null)
    {
        if (accountId == Guid.Empty)
        {
            throw new DomainException("Account ID is required.");
        }

        if (pattern is null)
        {
            throw new DomainException("Detected pattern is required.");
        }

        if (string.IsNullOrWhiteSpace(pattern.NormalizedDescription))
        {
            throw new DomainException("Normalized description is required.");
        }

        if (pattern.Confidence is < 0m or > 1m)
        {
            throw new DomainException("Confidence must be between 0.0 and 1.0.");
        }

        if (createdByUserId == Guid.Empty)
        {
            throw new DomainException("Created by user ID is required.");
        }

        var now = DateTime.UtcNow;
        return new RecurringChargeSuggestion
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            NormalizedDescription = pattern.NormalizedDescription,
            SampleDescription = pattern.SampleDescription,
            AverageAmount = pattern.AverageAmount,
            DetectedFrequency = pattern.Frequency,
            DetectedInterval = pattern.Interval,
            Confidence = pattern.Confidence,
            MatchingTransactionCount = pattern.MatchingTransactionIds.Count,
            FirstOccurrence = pattern.FirstOccurrence,
            LastOccurrence = pattern.LastOccurrence,
            CategoryId = pattern.MostUsedCategoryId,
            Status = SuggestionStatus.Pending,
            AcceptedRecurringTransactionId = null,
            OwnerUserId = ownerUserId,
            CreatedByUserId = createdByUserId,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };
    }

    /// <summary>
    /// Accepts the suggestion, linking it to the created recurring transaction.
    /// </summary>
    /// <param name="recurringTransactionId">The ID of the created recurring transaction.</param>
    /// <exception cref="DomainException">Thrown if the suggestion is not pending.</exception>
    public void Accept(Guid recurringTransactionId)
    {
        if (this.Status != SuggestionStatus.Pending)
        {
            throw new DomainException("Only pending suggestions can be accepted.");
        }

        if (recurringTransactionId == Guid.Empty)
        {
            throw new DomainException("Recurring transaction ID is required.");
        }

        this.Status = SuggestionStatus.Accepted;
        this.AcceptedRecurringTransactionId = recurringTransactionId;
        this.UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Dismisses the suggestion.
    /// </summary>
    /// <exception cref="DomainException">Thrown if the suggestion is not pending.</exception>
    public void Dismiss()
    {
        if (this.Status != SuggestionStatus.Pending)
        {
            throw new DomainException("Only pending suggestions can be dismissed.");
        }

        this.Status = SuggestionStatus.Dismissed;
        this.UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Restores a dismissed suggestion back to pending.
    /// </summary>
    /// <exception cref="DomainException">Thrown if the suggestion is not dismissed.</exception>
    public void Restore()
    {
        if (this.Status != SuggestionStatus.Dismissed)
        {
            throw new DomainException("Only dismissed suggestions can be restored.");
        }

        this.Status = SuggestionStatus.Pending;
        this.AcceptedRecurringTransactionId = null;
        this.UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the suggestion with refreshed detection data (for re-detection without duplicating).
    /// </summary>
    /// <param name="pattern">The updated detection pattern.</param>
    /// <exception cref="DomainException">Thrown if the suggestion is already accepted.</exception>
    public void UpdateFromDetection(DetectedPattern pattern)
    {
        if (this.Status == SuggestionStatus.Accepted)
        {
            throw new DomainException("Accepted suggestions cannot be updated by detection.");
        }

        if (pattern is null)
        {
            throw new DomainException("Detected pattern is required.");
        }

        this.AverageAmount = pattern.AverageAmount;
        this.DetectedFrequency = pattern.Frequency;
        this.DetectedInterval = pattern.Interval;
        this.Confidence = pattern.Confidence;
        this.MatchingTransactionCount = pattern.MatchingTransactionIds.Count;
        this.FirstOccurrence = pattern.FirstOccurrence;
        this.LastOccurrence = pattern.LastOccurrence;
        this.CategoryId = pattern.MostUsedCategoryId;
        this.UpdatedAtUtc = DateTime.UtcNow;
    }
}
