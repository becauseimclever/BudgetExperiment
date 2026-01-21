// <copyright file="BillInfo.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Recurring;

/// <summary>
/// Lightweight representation of a bill for allocation calculation.
/// </summary>
public sealed record BillInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BillInfo"/> class.
    /// </summary>
    private BillInfo()
    {
    }

    /// <summary>
    /// Gets the bill description.
    /// </summary>
    public string Description { get; private init; } = string.Empty;

    /// <summary>
    /// Gets the bill amount (always positive).
    /// </summary>
    public MoneyValue Amount { get; private init; } = null!;

    /// <summary>
    /// Gets the recurrence frequency of the bill.
    /// </summary>
    public RecurrenceFrequency Frequency { get; private init; }

    /// <summary>
    /// Gets the optional source recurring transaction identifier.
    /// </summary>
    public Guid? SourceRecurringTransactionId { get; private init; }

    /// <summary>
    /// Creates a new <see cref="BillInfo"/> instance.
    /// </summary>
    /// <param name="description">The bill description.</param>
    /// <param name="amount">The bill amount.</param>
    /// <param name="frequency">The recurrence frequency.</param>
    /// <param name="sourceRecurringTransactionId">Optional source recurring transaction ID.</param>
    /// <returns>A new <see cref="BillInfo"/> instance.</returns>
    /// <exception cref="DomainException">Thrown when description is empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown when amount is null.</exception>
    public static BillInfo Create(
        string description,
        MoneyValue amount,
        RecurrenceFrequency frequency,
        Guid? sourceRecurringTransactionId = null)
    {
        ArgumentNullException.ThrowIfNull(amount);

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new DomainException("Bill description is required.");
        }

        return new BillInfo
        {
            Description = description.Trim(),
            Amount = amount.Abs(),
            Frequency = frequency,
            SourceRecurringTransactionId = sourceRecurringTransactionId,
        };
    }

    /// <summary>
    /// Creates a <see cref="BillInfo"/> from a <see cref="RecurringTransaction"/>.
    /// </summary>
    /// <param name="recurring">The recurring transaction.</param>
    /// <returns>A new <see cref="BillInfo"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when recurring is null.</exception>
    public static BillInfo FromRecurringTransaction(RecurringTransaction recurring)
    {
        ArgumentNullException.ThrowIfNull(recurring);

        return new BillInfo
        {
            Description = recurring.Description,
            Amount = recurring.Amount.Abs(),
            Frequency = recurring.RecurrencePattern.Frequency,
            SourceRecurringTransactionId = recurring.Id,
        };
    }
}
