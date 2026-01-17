// <copyright file="RecurringTransfer.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain;

/// <summary>
/// Represents a recurring transfer between two accounts that generates paired transactions on a schedule.
/// </summary>
public sealed class RecurringTransfer
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RecurringTransfer"/> class.
    /// </summary>
    /// <remarks>
    /// Private constructor for EF Core and factory method.
    /// </remarks>
    private RecurringTransfer()
    {
    }

    /// <summary>
    /// Gets the unique identifier.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the identifier of the source account (where money is transferred from).
    /// </summary>
    public Guid SourceAccountId { get; private set; }

    /// <summary>
    /// Gets the identifier of the destination account (where money is transferred to).
    /// </summary>
    public Guid DestinationAccountId { get; private set; }

    /// <summary>
    /// Gets the description of the recurring transfer.
    /// </summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the monetary amount of the transfer (always positive, direction determined by accounts).
    /// </summary>
    public MoneyValue Amount { get; private set; } = null!;

    /// <summary>
    /// Gets the recurrence pattern defining the schedule.
    /// </summary>
    public RecurrencePattern RecurrencePattern { get; private set; } = null!;

    /// <summary>
    /// Gets the start date for the recurring transfer.
    /// </summary>
    public DateOnly StartDate { get; private set; }

    /// <summary>
    /// Gets the optional end date for the recurring transfer (null = indefinite).
    /// </summary>
    public DateOnly? EndDate { get; private set; }

    /// <summary>
    /// Gets the date of the next scheduled occurrence.
    /// </summary>
    public DateOnly NextOccurrence { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the recurring transfer is active.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Gets the date of the last generated transfer (null if never generated).
    /// </summary>
    public DateOnly? LastGeneratedDate { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp when the recurring transfer was created.
    /// </summary>
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp when the recurring transfer was last updated.
    /// </summary>
    public DateTime UpdatedAtUtc { get; private set; }

    /// <summary>
    /// Gets the budget scope (Shared or Personal).
    /// </summary>
    public BudgetScope Scope { get; private set; }

    /// <summary>
    /// Gets the owner user ID. NULL for Shared scope, user ID for Personal scope.
    /// </summary>
    public Guid? OwnerUserId { get; private set; }

    /// <summary>
    /// Gets the user ID of who created this recurring transfer.
    /// </summary>
    public Guid CreatedByUserId { get; private set; }

    /// <summary>
    /// Creates a new recurring transfer.
    /// </summary>
    /// <param name="sourceAccountId">The source account identifier.</param>
    /// <param name="destinationAccountId">The destination account identifier.</param>
    /// <param name="description">The description.</param>
    /// <param name="amount">The monetary amount (must be positive).</param>
    /// <param name="recurrencePattern">The recurrence pattern.</param>
    /// <param name="startDate">The start date.</param>
    /// <param name="endDate">Optional end date.</param>
    /// <returns>A new <see cref="RecurringTransfer"/> instance.</returns>
    /// <exception cref="DomainException">Thrown when validation fails.</exception>
    public static RecurringTransfer Create(
        Guid sourceAccountId,
        Guid destinationAccountId,
        string description,
        MoneyValue amount,
        RecurrencePattern recurrencePattern,
        DateOnly startDate,
        DateOnly? endDate = null)
    {
        if (sourceAccountId == Guid.Empty)
        {
            throw new DomainException("Source account ID is required.");
        }

        if (destinationAccountId == Guid.Empty)
        {
            throw new DomainException("Destination account ID is required.");
        }

        if (sourceAccountId == destinationAccountId)
        {
            throw new DomainException("Source and destination accounts must be different.");
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new DomainException("Description is required.");
        }

        if (amount is null)
        {
            throw new DomainException("Amount is required.");
        }

        if (amount.Amount <= 0)
        {
            throw new DomainException("Transfer amount must be positive.");
        }

        if (recurrencePattern is null)
        {
            throw new DomainException("Recurrence pattern is required.");
        }

        if (endDate.HasValue && endDate.Value < startDate)
        {
            throw new DomainException("End date must be on or after start date.");
        }

        var now = DateTime.UtcNow;
        return new RecurringTransfer
        {
            Id = Guid.NewGuid(),
            SourceAccountId = sourceAccountId,
            DestinationAccountId = destinationAccountId,
            Description = description.Trim(),
            Amount = amount,
            RecurrencePattern = recurrencePattern,
            StartDate = startDate,
            EndDate = endDate,
            NextOccurrence = startDate,
            IsActive = true,
            LastGeneratedDate = null,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };
    }

    /// <summary>
    /// Updates the recurring transfer properties.
    /// </summary>
    /// <param name="description">The new description.</param>
    /// <param name="amount">The new amount.</param>
    /// <param name="recurrencePattern">The new recurrence pattern.</param>
    /// <param name="endDate">The new end date (null to remove).</param>
    /// <exception cref="DomainException">Thrown when validation fails.</exception>
    public void Update(
        string description,
        MoneyValue amount,
        RecurrencePattern recurrencePattern,
        DateOnly? endDate)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            throw new DomainException("Description is required.");
        }

        if (amount is null)
        {
            throw new DomainException("Amount is required.");
        }

        if (amount.Amount <= 0)
        {
            throw new DomainException("Transfer amount must be positive.");
        }

        if (recurrencePattern is null)
        {
            throw new DomainException("Recurrence pattern is required.");
        }

        if (endDate.HasValue && endDate.Value < this.StartDate)
        {
            throw new DomainException("End date must be on or after start date.");
        }

        this.Description = description.Trim();
        this.Amount = amount;
        this.RecurrencePattern = recurrencePattern;
        this.EndDate = endDate;
        this.UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Pauses the recurring transfer.
    /// </summary>
    public void Pause()
    {
        this.IsActive = false;
        this.UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Resumes a paused recurring transfer, recalculating the next occurrence from the given date.
    /// </summary>
    /// <param name="fromDate">The date from which to calculate the next occurrence.</param>
    public void Resume(DateOnly fromDate)
    {
        if (this.IsActive)
        {
            return;
        }

        this.IsActive = true;
        this.NextOccurrence = this.RecurrencePattern.CalculateNextOccurrence(fromDate);
        this.UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Advances to the next occurrence after generating the current one.
    /// </summary>
    /// <exception cref="DomainException">Thrown when the recurring transfer is inactive.</exception>
    public void AdvanceToNextOccurrence()
    {
        if (!this.IsActive)
        {
            throw new DomainException("Cannot advance inactive recurring transfer.");
        }

        this.LastGeneratedDate = this.NextOccurrence;
        var nextDate = this.RecurrencePattern.CalculateNextOccurrence(this.NextOccurrence);

        if (this.EndDate.HasValue && nextDate > this.EndDate.Value)
        {
            this.IsActive = false;
        }
        else
        {
            this.NextOccurrence = nextDate;
        }

        this.UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Skips the next occurrence without marking it as generated.
    /// </summary>
    /// <exception cref="DomainException">Thrown when the recurring transfer is inactive.</exception>
    public void SkipNextOccurrence()
    {
        if (!this.IsActive)
        {
            throw new DomainException("Cannot skip occurrence for inactive recurring transfer.");
        }

        var nextDate = this.RecurrencePattern.CalculateNextOccurrence(this.NextOccurrence);

        if (this.EndDate.HasValue && nextDate > this.EndDate.Value)
        {
            this.IsActive = false;
        }
        else
        {
            this.NextOccurrence = nextDate;
        }

        this.UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Advances the next occurrence to the following scheduled date.
    /// </summary>
    /// <exception cref="DomainException">Thrown when the recurring transfer is inactive.</exception>
    public void AdvanceNextOccurrence()
    {
        if (!this.IsActive)
        {
            throw new DomainException("Cannot advance inactive recurring transfer.");
        }

        this.LastGeneratedDate = this.NextOccurrence;
        var nextDate = this.RecurrencePattern.CalculateNextOccurrence(this.NextOccurrence);

        if (this.EndDate.HasValue && nextDate > this.EndDate.Value)
        {
            this.IsActive = false;
        }
        else
        {
            this.NextOccurrence = nextDate;
        }

        this.UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets all occurrences between the specified date range.
    /// </summary>
    /// <param name="from">The start of the date range (inclusive).</param>
    /// <param name="to">The end of the date range (inclusive).</param>
    /// <returns>An enumerable of occurrence dates.</returns>
    public IEnumerable<DateOnly> GetOccurrencesBetween(DateOnly from, DateOnly to)
    {
        if (!this.IsActive)
        {
            yield break;
        }

        var current = this.StartDate;

        // Find the first occurrence that's >= from
        while (current < from && (!this.EndDate.HasValue || current <= this.EndDate.Value))
        {
            current = this.RecurrencePattern.CalculateNextOccurrence(current);
        }

        // If starting point is before StartDate, use StartDate
        if (this.StartDate >= from && this.StartDate <= to)
        {
            current = this.StartDate;
        }

        // Yield all occurrences in range
        while (current <= to && (!this.EndDate.HasValue || current <= this.EndDate.Value))
        {
            if (current >= from)
            {
                yield return current;
            }

            current = this.RecurrencePattern.CalculateNextOccurrence(current);
        }
    }
}
