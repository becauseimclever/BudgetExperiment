// <copyright file="TransactionListItem.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;

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
    public MoneyDto Amount { get; set; } = new();

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

    /// <summary>Gets or sets whether this is a recurring transfer instance.</summary>
    public bool IsRecurringTransfer { get; set; }

    /// <summary>Gets or sets the recurring transfer ID (if from recurring transfer).</summary>
    public Guid? RecurringTransferId { get; set; }

    /// <summary>
    /// Creates a TransactionListItem from an actual transaction.
    /// </summary>
    /// <param name="transaction">The transaction DTO.</param>
    /// <returns>A new TransactionListItem.</returns>
    public static TransactionListItem FromTransaction(TransactionDto transaction)
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
    /// <param name="instance">The recurring instance DTO.</param>
    /// <returns>A new TransactionListItem.</returns>
    public static TransactionListItem FromRecurringInstance(RecurringInstanceDto instance)
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

    /// <summary>
    /// Creates a TransactionListItem from a recurring transfer instance.
    /// </summary>
    /// <param name="instance">The recurring transfer instance DTO.</param>
    /// <param name="isSource">True if viewing from source account perspective (outgoing), false for destination (incoming).</param>
    /// <returns>A new TransactionListItem.</returns>
    public static TransactionListItem FromRecurringTransferInstance(RecurringTransferInstanceDto instance, bool isSource)
    {
        var amount = isSource
            ? new MoneyDto { Currency = instance.Amount.Currency, Amount = -instance.Amount.Amount }
            : instance.Amount;
        var description = isSource
            ? $"Transfer to {instance.DestinationAccountName}: {instance.Description}"
            : $"Transfer from {instance.SourceAccountName}: {instance.Description}";

        return new TransactionListItem
        {
            Id = instance.RecurringTransferId,
            Date = instance.EffectiveDate,
            Description = description,
            Category = null,
            Amount = amount,
            IsRecurring = true,
            IsRecurringTransfer = true,
            IsModified = instance.IsModified,
            CreatedAt = null,
            IsTransfer = true,
            RecurringTransferId = instance.RecurringTransferId,
            TransferDirection = isSource ? "Source" : "Destination",
        };
    }
}
