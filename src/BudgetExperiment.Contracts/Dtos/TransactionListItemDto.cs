// <copyright file="TransactionListItemDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// DTO for a single item (transaction or recurring instance) in the transaction list view.
/// Similar to <see cref="DayDetailItemDto"/> but includes date field for multi-day lists.
/// </summary>
public sealed class TransactionListItemDto
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the item type ("transaction", "recurring", or "recurring-transfer").</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Gets or sets the date of this item.</summary>
    public DateOnly Date { get; set; }

    /// <summary>Gets or sets the description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the amount.</summary>
    public MoneyDto Amount { get; set; } = new();

    /// <summary>Gets or sets the category.</summary>
    public string? Category { get; set; }

    /// <summary>Gets or sets the creation timestamp (for actual transactions).</summary>
    public DateTime? CreatedAt { get; set; }

    /// <summary>Gets or sets a value indicating whether this recurring instance has been modified.</summary>
    public bool IsModified { get; set; }

    /// <summary>Gets or sets the recurring transaction ID (for recurring items).</summary>
    public Guid? RecurringTransactionId { get; set; }

    /// <summary>Gets or sets the recurring transfer ID (for recurring transfer items).</summary>
    public Guid? RecurringTransferId { get; set; }

    /// <summary>Gets or sets whether this transaction is part of a transfer.</summary>
    public bool IsTransfer { get; set; }

    /// <summary>Gets or sets the transfer identifier (null if not a transfer).</summary>
    public Guid? TransferId { get; set; }

    /// <summary>Gets or sets the transfer direction (null if not a transfer).</summary>
    public string? TransferDirection { get; set; }

    /// <summary>Gets or sets the running balance after this transaction.</summary>
    public MoneyDto RunningBalance { get; set; } = new();
}
