// <copyright file="RecurringTransferException.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Recurring;

/// <summary>
/// Represents an exception (modification or skip) for a specific instance of a recurring transfer.
/// </summary>
public sealed class RecurringTransferException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RecurringTransferException"/> class.
    /// </summary>
    /// <remarks>
    /// Private constructor for EF Core and factory method.
    /// </remarks>
    private RecurringTransferException()
    {
    }

    /// <summary>
    /// Gets the unique identifier.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the identifier of the recurring transfer this exception belongs to.
    /// </summary>
    public Guid RecurringTransferId { get; private set; }

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
    /// Creates a modified exception for a recurring transfer instance.
    /// </summary>
    /// <param name="recurringTransferId">The recurring transfer identifier.</param>
    /// <param name="originalDate">The original scheduled date.</param>
    /// <param name="modifiedAmount">Optional modified amount (must be positive if provided).</param>
    /// <param name="modifiedDescription">Optional modified description.</param>
    /// <param name="modifiedDate">Optional modified date.</param>
    /// <returns>A new <see cref="RecurringTransferException"/> instance.</returns>
    /// <exception cref="DomainException">Thrown when validation fails.</exception>
    public static RecurringTransferException CreateModified(
        Guid recurringTransferId,
        DateOnly originalDate,
        MoneyValue? modifiedAmount,
        string? modifiedDescription,
        DateOnly? modifiedDate)
    {
        if (recurringTransferId == Guid.Empty)
        {
            throw new DomainException("Recurring transfer ID is required.");
        }

        if (modifiedAmount is not null && modifiedAmount.Amount <= 0)
        {
            throw new DomainException("Modified amount must be positive.");
        }

        var trimmedDescription = modifiedDescription?.Trim();
        var hasDescription = !string.IsNullOrEmpty(trimmedDescription);

        if (modifiedAmount is null && !hasDescription && modifiedDate is null)
        {
            throw new DomainException("At least one modification is required (amount, description, or date).");
        }

        var now = DateTime.UtcNow;
        return new RecurringTransferException
        {
            Id = Guid.NewGuid(),
            RecurringTransferId = recurringTransferId,
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
    /// Creates a skipped exception for a recurring transfer instance.
    /// </summary>
    /// <param name="recurringTransferId">The recurring transfer identifier.</param>
    /// <param name="originalDate">The original scheduled date to skip.</param>
    /// <returns>A new <see cref="RecurringTransferException"/> instance.</returns>
    /// <exception cref="DomainException">Thrown when validation fails.</exception>
    public static RecurringTransferException CreateSkipped(
        Guid recurringTransferId,
        DateOnly originalDate)
    {
        if (recurringTransferId == Guid.Empty)
        {
            throw new DomainException("Recurring transfer ID is required.");
        }

        var now = DateTime.UtcNow;
        return new RecurringTransferException
        {
            Id = Guid.NewGuid(),
            RecurringTransferId = recurringTransferId,
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
    /// <param name="modifiedAmount">Optional modified amount (must be positive if provided).</param>
    /// <param name="modifiedDescription">Optional modified description.</param>
    /// <param name="modifiedDate">Optional modified date.</param>
    /// <exception cref="DomainException">Thrown when validation fails.</exception>
    public void Update(
        MoneyValue? modifiedAmount,
        string? modifiedDescription,
        DateOnly? modifiedDate)
    {
        if (this.ExceptionType == ExceptionType.Skipped)
        {
            throw new DomainException("Cannot update a skipped exception.");
        }

        if (modifiedAmount is not null && modifiedAmount.Amount <= 0)
        {
            throw new DomainException("Modified amount must be positive.");
        }

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
