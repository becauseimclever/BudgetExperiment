// <copyright file="RecurringTransactionException.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Recurring;

/// <summary>
/// Represents an exception (modification or skip) for a specific instance of a recurring transaction.
/// </summary>
public sealed class RecurringTransactionException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RecurringTransactionException"/> class.
    /// </summary>
    /// <remarks>
    /// Private constructor for EF Core and factory method.
    /// </remarks>
    private RecurringTransactionException()
    {
    }

    /// <summary>
    /// Gets the unique identifier.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the identifier of the recurring transaction this exception belongs to.
    /// </summary>
    public Guid RecurringTransactionId { get; private set; }

    /// <summary>
    /// Gets the original scheduled date being modified or skipped.
    /// </summary>
    public DateOnly OriginalDate { get; private set; }

    /// <summary>
    /// Gets the type of exception (Modified or Skipped).
    /// </summary>
    public ExceptionType ExceptionType { get; private set; }

    /// <summary>
    /// Gets the modified amount (null = use series amount).
    /// </summary>
    public MoneyValue? ModifiedAmount { get; private set; }

    /// <summary>
    /// Gets the modified description (null = use series description).
    /// </summary>
    public string? ModifiedDescription { get; private set; }

    /// <summary>
    /// Gets the modified date (null = use original date; allows rescheduling).
    /// </summary>
    public DateOnly? ModifiedDate { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp when the exception was created.
    /// </summary>
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp when the exception was last updated.
    /// </summary>
    public DateTime UpdatedAtUtc { get; private set; }

    /// <summary>
    /// Creates a modified exception for a recurring transaction instance.
    /// </summary>
    /// <param name="recurringTransactionId">The recurring transaction identifier.</param>
    /// <param name="originalDate">The original scheduled date.</param>
    /// <param name="modifiedAmount">Optional modified amount.</param>
    /// <param name="modifiedDescription">Optional modified description.</param>
    /// <param name="modifiedDate">Optional modified date.</param>
    /// <returns>A new <see cref="RecurringTransactionException"/> instance.</returns>
    /// <exception cref="DomainException">Thrown when validation fails.</exception>
    public static RecurringTransactionException CreateModified(
        Guid recurringTransactionId,
        DateOnly originalDate,
        MoneyValue? modifiedAmount,
        string? modifiedDescription,
        DateOnly? modifiedDate)
    {
        if (recurringTransactionId == Guid.Empty)
        {
            throw new DomainException("Recurring transaction ID is required.");
        }

        var trimmedDescription = modifiedDescription?.Trim();
        var hasDescription = !string.IsNullOrEmpty(trimmedDescription);

        if (modifiedAmount is null && !hasDescription && modifiedDate is null)
        {
            throw new DomainException("At least one modification is required (amount, description, or date).");
        }

        var now = DateTime.UtcNow;
        return new RecurringTransactionException
        {
            Id = Guid.NewGuid(),
            RecurringTransactionId = recurringTransactionId,
            OriginalDate = originalDate,
            ExceptionType = ExceptionType.Modified,
            ModifiedAmount = modifiedAmount,
            ModifiedDescription = hasDescription ? trimmedDescription : null,
            ModifiedDate = modifiedDate,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };
    }

    /// <summary>
    /// Creates a skipped exception for a recurring transaction instance.
    /// </summary>
    /// <param name="recurringTransactionId">The recurring transaction identifier.</param>
    /// <param name="originalDate">The original scheduled date to skip.</param>
    /// <returns>A new <see cref="RecurringTransactionException"/> instance.</returns>
    /// <exception cref="DomainException">Thrown when validation fails.</exception>
    public static RecurringTransactionException CreateSkipped(
        Guid recurringTransactionId,
        DateOnly originalDate)
    {
        if (recurringTransactionId == Guid.Empty)
        {
            throw new DomainException("Recurring transaction ID is required.");
        }

        var now = DateTime.UtcNow;
        return new RecurringTransactionException
        {
            Id = Guid.NewGuid(),
            RecurringTransactionId = recurringTransactionId,
            OriginalDate = originalDate,
            ExceptionType = ExceptionType.Skipped,
            ModifiedAmount = null,
            ModifiedDescription = null,
            ModifiedDate = null,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };
    }

    /// <summary>
    /// Updates the exception with new modified values.
    /// </summary>
    /// <param name="modifiedAmount">Optional modified amount.</param>
    /// <param name="modifiedDescription">Optional modified description.</param>
    /// <param name="modifiedDate">Optional modified date.</param>
    /// <exception cref="DomainException">Thrown when validation fails.</exception>
    public void Update(
        MoneyValue? modifiedAmount,
        string? modifiedDescription,
        DateOnly? modifiedDate)
    {
        var trimmedDescription = modifiedDescription?.Trim();
        var hasDescription = !string.IsNullOrEmpty(trimmedDescription);

        if (modifiedAmount is null && !hasDescription && modifiedDate is null)
        {
            throw new DomainException("At least one modification is required (amount, description, or date).");
        }

        this.ModifiedAmount = modifiedAmount;
        this.ModifiedDescription = hasDescription ? trimmedDescription : null;
        this.ModifiedDate = modifiedDate;
        this.UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the effective date for this exception (modified date if set, otherwise original date).
    /// </summary>
    /// <returns>The effective date.</returns>
    public DateOnly GetEffectiveDate()
    {
        return this.ModifiedDate ?? this.OriginalDate;
    }
}
