// <copyright file="ReconciliationRecord.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Reconciliation;

/// <summary>
/// Represents a completed statement reconciliation record for an account.
/// Immutable after creation — the single source of truth that a reconciliation occurred.
/// </summary>
public sealed class ReconciliationRecord
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReconciliationRecord"/> class.
    /// </summary>
    /// <remarks>Private constructor for EF Core and factory method.</remarks>
    private ReconciliationRecord()
    {
    }

    /// <summary>Gets the unique identifier.</summary>
    public Guid Id { get; private set; }

    /// <summary>Gets the identifier of the account that was reconciled.</summary>
    public Guid AccountId { get; private set; }

    /// <summary>Gets the statement closing date.</summary>
    public DateOnly StatementDate { get; private set; }

    /// <summary>Gets the balance reported on the bank statement.</summary>
    public MoneyValue StatementBalance { get; private set; } = null!;

    /// <summary>Gets the sum of all cleared transactions at the time of reconciliation.</summary>
    public MoneyValue ClearedBalance { get; private set; } = null!;

    /// <summary>Gets the number of transactions locked to this reconciliation.</summary>
    public int TransactionCount { get; private set; }

    /// <summary>Gets the UTC timestamp when the reconciliation was completed.</summary>
    public DateTime CompletedAtUtc { get; private set; }

    /// <summary>Gets the identifier of the user who completed the reconciliation.</summary>
    public Guid CompletedByUserId { get; private set; }

    /// <summary>Gets the budget scope of the reconciliation.</summary>
    public BudgetScope Scope { get; private set; }

    /// <summary>Gets the owner user ID (null for shared scope).</summary>
    public Guid? OwnerUserId { get; private set; }

    /// <summary>
    /// Creates a new <see cref="ReconciliationRecord"/> after validating that balances match.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="statementDate">The statement closing date.</param>
    /// <param name="statementBalance">The balance from the bank statement.</param>
    /// <param name="clearedBalance">The computed cleared balance.</param>
    /// <param name="transactionCount">Number of transactions being locked.</param>
    /// <param name="completedByUserId">Identifier of the user completing the reconciliation.</param>
    /// <param name="scope">The budget scope.</param>
    /// <param name="ownerUserId">The owner user ID (null for shared scope).</param>
    /// <returns>A new <see cref="ReconciliationRecord"/> instance.</returns>
    /// <exception cref="DomainException">Thrown when statement balance does not match cleared balance.</exception>
    public static ReconciliationRecord Create(
        Guid accountId,
        DateOnly statementDate,
        MoneyValue statementBalance,
        MoneyValue clearedBalance,
        int transactionCount,
        Guid completedByUserId,
        BudgetScope scope,
        Guid? ownerUserId)
    {
        if (statementBalance != clearedBalance)
        {
            throw new DomainException(
                "Cannot complete reconciliation: statement balance does not match cleared balance.",
                DomainExceptionType.Validation);
        }

        return new ReconciliationRecord
        {
            Id = Guid.CreateVersion7(),
            AccountId = accountId,
            StatementDate = statementDate,
            StatementBalance = statementBalance,
            ClearedBalance = clearedBalance,
            TransactionCount = transactionCount,
            CompletedAtUtc = DateTime.UtcNow,
            CompletedByUserId = completedByUserId,
            Scope = scope,
            OwnerUserId = ownerUserId,
        };
    }
}
