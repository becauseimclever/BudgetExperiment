// <copyright file="DayDetailItemDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// DTO for a single item (transaction or recurring instance) in the day detail view.
/// </summary>
public sealed class DayDetailItemDto
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the item type ("transaction" or "recurring").</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Gets or sets the description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the amount.</summary>
    public MoneyDto Amount { get; set; } = new();

    /// <summary>Gets or sets the category.</summary>
    public string? Category { get; set; }

    /// <summary>Gets or sets the account name.</summary>
    public string AccountName { get; set; } = string.Empty;

    /// <summary>Gets or sets the account ID.</summary>
    public Guid AccountId { get; set; }

    /// <summary>Gets or sets the creation timestamp (for actual transactions).</summary>
    public DateTime? CreatedAt { get; set; }

    /// <summary>Gets or sets a value indicating whether this recurring instance has been modified.</summary>
    public bool IsModified { get; set; }

    /// <summary>Gets or sets a value indicating whether this recurring instance has been skipped.</summary>
    public bool IsSkipped { get; set; }

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
}
