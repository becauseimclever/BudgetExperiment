// <copyright file="StatementBalance.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Reconciliation;

/// <summary>
/// Represents the active statement balance entry for a pending reconciliation.
/// Only one active (non-completed) statement balance may exist per account at a time.
/// </summary>
public sealed class StatementBalance
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StatementBalance"/> class.
    /// </summary>
    /// <remarks>Private constructor for EF Core and factory method.</remarks>
    private StatementBalance()
    {
    }

    /// <summary>Gets the unique identifier.</summary>
    public Guid Id
    {
        get; private set;
    }

    /// <summary>Gets the identifier of the account this statement balance belongs to.</summary>
    public Guid AccountId
    {
        get; private set;
    }

    /// <summary>Gets the statement closing date.</summary>
    public DateOnly StatementDate
    {
        get; private set;
    }

    /// <summary>Gets the balance as reported on the bank statement.</summary>
    public MoneyValue Balance { get; private set; } = null!;

    /// <summary>Gets a value indicating whether the reconciliation using this balance has been completed.</summary>
    public bool IsCompleted
    {
        get; private set;
    }

    /// <summary>Gets the UTC timestamp when this record was created.</summary>
    public DateTime CreatedAtUtc
    {
        get; private set;
    }

    /// <summary>Gets the UTC timestamp when this record was last updated.</summary>
    public DateTime UpdatedAtUtc
    {
        get; private set;
    }

    /// <summary>
    /// Creates a new <see cref="StatementBalance"/> for the given account and statement date.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="statementDate">The statement closing date.</param>
    /// <param name="balance">The balance reported on the statement.</param>
    /// <returns>A new <see cref="StatementBalance"/> instance.</returns>
    public static StatementBalance Create(Guid accountId, DateOnly statementDate, MoneyValue balance)
    {
        var now = DateTime.UtcNow;
        return new StatementBalance
        {
            Id = Guid.CreateVersion7(),
            AccountId = accountId,
            StatementDate = statementDate,
            Balance = balance,
            IsCompleted = false,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };
    }

    /// <summary>
    /// Updates the statement balance with a new value.
    /// </summary>
    /// <param name="balance">The new balance.</param>
    /// <exception cref="DomainException">Thrown when the statement balance has already been completed.</exception>
    public void UpdateBalance(MoneyValue balance)
    {
        if (IsCompleted)
        {
            throw new DomainException(
                "Cannot modify a completed statement balance.",
                DomainExceptionType.InvalidOperation);
        }

        Balance = balance;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks this statement balance as completed after a successful reconciliation.
    /// </summary>
    public void MarkCompleted()
    {
        IsCompleted = true;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
