// <copyright file="TransactionDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// DTO for returning transaction details.
/// </summary>
public sealed class TransactionDto
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the account identifier.</summary>
    public Guid AccountId { get; set; }

    /// <summary>Gets or sets the transaction amount.</summary>
    public MoneyDto Amount { get; set; } = new();

    /// <summary>Gets or sets the transaction date.</summary>
    public DateOnly Date { get; set; }

    /// <summary>Gets or sets the description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional category identifier.</summary>
    public Guid? CategoryId { get; set; }

    /// <summary>Gets or sets the creation timestamp (UTC).</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Gets or sets the last update timestamp (UTC).</summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>Gets or sets whether this transaction is part of a transfer.</summary>
    public bool IsTransfer { get; set; }

    /// <summary>Gets or sets the transfer identifier (null if not a transfer).</summary>
    public Guid? TransferId { get; set; }

    /// <summary>Gets or sets the transfer direction (null if not a transfer).</summary>
    public string? TransferDirection { get; set; }

    /// <summary>Gets or sets the recurring transaction identifier (null if not from recurring).</summary>
    public Guid? RecurringTransactionId { get; set; }

    /// <summary>Gets or sets the scheduled instance date from the recurring transaction (null if not from recurring).</summary>
    public DateOnly? RecurringInstanceDate { get; set; }

    /// <summary>Gets or sets the recurring transfer identifier (null if not from recurring transfer).</summary>
    public Guid? RecurringTransferId { get; set; }

    /// <summary>Gets or sets the scheduled instance date from the recurring transfer (null if not from recurring transfer).</summary>
    public DateOnly? RecurringTransferInstanceDate { get; set; }
}

/// <summary>
/// DTO for creating a new transaction.
/// </summary>
public sealed class TransactionCreateDto
{
    /// <summary>Gets or sets the account identifier.</summary>
    public Guid AccountId { get; set; }

    /// <summary>Gets or sets the transaction amount.</summary>
    public MoneyDto Amount { get; set; } = new();

    /// <summary>Gets or sets the transaction date.</summary>
    public DateOnly Date { get; set; }

    /// <summary>Gets or sets the description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional category identifier.</summary>
    public Guid? CategoryId { get; set; }
}
