// <copyright file="RecurringTransaction.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain;

/// <summary>
/// Represents a recurring transaction that generates transaction entries on a schedule.
/// </summary>
public sealed class RecurringTransaction
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RecurringTransaction"/> class.
    /// </summary>
    /// <remarks>
    /// Private constructor for EF Core and factory method.
    /// </remarks>
    private RecurringTransaction()
    {
    }

    /// <summary>
    /// Gets the unique identifier.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the identifier of the account this recurring transaction belongs to.
    /// </summary>
    public Guid AccountId { get; private set; }

    /// <summary>
    /// Gets the description of the recurring transaction.
    /// </summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the monetary amount of the recurring transaction.
    /// </summary>
    public MoneyValue Amount { get; private set; } = null!;

    /// <summary>
    /// Gets the recurrence pattern defining the schedule.
    /// </summary>
    public RecurrencePattern RecurrencePattern { get; private set; } = null!;

    /// <summary>
    /// Gets the start date for the recurring transaction.
    /// </summary>
    public DateOnly StartDate { get; private set; }

    /// <summary>
    /// Gets the optional end date for the recurring transaction (null = indefinite).
    /// </summary>
    public DateOnly? EndDate { get; private set; }

    /// <summary>
    /// Gets the optional category identifier linking to a BudgetCategory.
    /// </summary>
    public Guid? CategoryId { get; private set; }

    /// <summary>
    /// Gets the date of the next scheduled occurrence.
    /// </summary>
    public DateOnly NextOccurrence { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the recurring transaction is active.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Gets the date of the last generated transaction (null if never generated).
    /// </summary>
    public DateOnly? LastGeneratedDate { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp when the recurring transaction was created.
    /// </summary>
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp when the recurring transaction was last updated.
    /// </summary>
    public DateTime UpdatedAtUtc { get; private set; }

    /// <summary>
    /// Creates a new recurring transaction.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="description">The description.</param>
    /// <param name="amount">The monetary amount.</param>
    /// <param name="recurrencePattern">The recurrence pattern.</param>
    /// <param name="startDate">The start date.</param>
    /// <param name="endDate">Optional end date.</param>
    /// <param name="categoryId">Optional category identifier.</param>
    /// <returns>A new <see cref="RecurringTransaction"/> instance.</returns>
    /// <exception cref="DomainException">Thrown when validation fails.</exception>
    public static RecurringTransaction Create(
        Guid accountId,
        string description,
        MoneyValue amount,
        RecurrencePattern recurrencePattern,
        DateOnly startDate,
        DateOnly? endDate = null,
        Guid? categoryId = null)
    {
        if (accountId == Guid.Empty)
        {
            throw new DomainException("Account ID is required.");
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new DomainException("Description is required.");
        }

        if (amount is null)
        {
            throw new DomainException("Amount is required.");
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
        return new RecurringTransaction
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            Description = description.Trim(),
            Amount = amount,
            RecurrencePattern = recurrencePattern,
            StartDate = startDate,
            EndDate = endDate,
            CategoryId = categoryId,
            NextOccurrence = startDate,
            IsActive = true,
            LastGeneratedDate = null,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };
    }

    /// <summary>
    /// Updates the recurring transaction properties.
    /// </summary>
    /// <param name="description">The new description.</param>
    /// <param name="amount">The new amount.</param>
    /// <param name="recurrencePattern">The new recurrence pattern.</param>
    /// <param name="endDate">The new end date (null to remove).</param>
    /// <param name="categoryId">The new category identifier (null to remove).</param>
    /// <exception cref="DomainException">Thrown when validation fails.</exception>
    public void Update(
        string description,
        MoneyValue amount,
        RecurrencePattern recurrencePattern,
        DateOnly? endDate,
        Guid? categoryId)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            throw new DomainException("Description is required.");
        }

        if (amount is null)
        {
            throw new DomainException("Amount is required.");
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
        this.CategoryId = categoryId;
        this.UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Pauses the recurring transaction, preventing new occurrences from generating.
    /// </summary>
    public void Pause()
    {
        this.IsActive = false;
        this.UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Resumes a paused recurring transaction.
    /// </summary>
    public void Resume()
    {
        this.IsActive = true;
        this.UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Advances the next occurrence to the following scheduled date.
    /// </summary>
    public void AdvanceNextOccurrence()
    {
        this.LastGeneratedDate = this.NextOccurrence;
        this.NextOccurrence = this.RecurrencePattern.CalculateNextOccurrence(this.NextOccurrence);
        this.UpdatedAtUtc = DateTime.UtcNow;

        // If the new next occurrence is past the end date, deactivate
        if (this.EndDate.HasValue && this.NextOccurrence > this.EndDate.Value)
        {
            this.IsActive = false;
        }
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
