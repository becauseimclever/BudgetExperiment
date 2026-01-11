// <copyright file="TransactionListItem.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Client.Models;

/// <summary>
/// Unified model for displaying both actual transactions and recurring transaction instances
/// in a combined transaction list view.
/// </summary>
public sealed class TransactionListItem
{
    /// <summary>Gets or sets the unique identifier (transaction ID or recurring transaction ID).</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the transaction date.</summary>
    public DateOnly Date { get; set; }

    /// <summary>Gets or sets the description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional category.</summary>
    public string? Category { get; set; }

    /// <summary>Gets or sets the amount.</summary>
    public MoneyModel Amount { get; set; } = new();

    /// <summary>Gets or sets whether this is a recurring transaction instance (not yet realized).</summary>
    public bool IsRecurring { get; set; }

    /// <summary>Gets or sets whether this recurring instance has been modified from the series default.</summary>
    public bool IsModified { get; set; }

    /// <summary>Gets or sets the creation timestamp for actual transactions.</summary>
    public DateTime? CreatedAt { get; set; }

    /// <summary>Gets or sets whether this transaction is part of a transfer.</summary>
    public bool IsTransfer { get; set; }

    /// <summary>Gets or sets the transfer identifier (null if not a transfer).</summary>
    public Guid? TransferId { get; set; }

    /// <summary>Gets or sets the transfer direction (null if not a transfer).</summary>
    public string? TransferDirection { get; set; }

    /// <summary>
    /// Creates a TransactionListItem from an actual transaction.
    /// </summary>
    /// <param name="transaction">The transaction model.</param>
    /// <returns>A new TransactionListItem.</returns>
    public static TransactionListItem FromTransaction(TransactionModel transaction)
    {
        return new TransactionListItem
        {
            Id = transaction.Id,
            Date = transaction.Date,
            Description = transaction.Description,
            Category = transaction.Category,
            Amount = transaction.Amount,
            IsRecurring = false,
            IsModified = false,
            CreatedAt = transaction.CreatedAt,
            IsTransfer = transaction.IsTransfer,
            TransferId = transaction.TransferId,
            TransferDirection = transaction.TransferDirection,
        };
    }

    /// <summary>
    /// Creates a TransactionListItem from a recurring transaction instance.
    /// </summary>
    /// <param name="instance">The recurring instance model.</param>
    /// <returns>A new TransactionListItem.</returns>
    public static TransactionListItem FromRecurringInstance(RecurringInstanceModel instance)
    {
        return new TransactionListItem
        {
            Id = instance.RecurringTransactionId,
            Date = instance.ScheduledDate,
            Description = instance.Description,
            Category = instance.Category,
            Amount = instance.Amount,
            IsRecurring = true,
            IsModified = instance.IsModified,
            CreatedAt = null,
        };
    }
}
