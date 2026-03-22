// <copyright file="UnifiedTransactionItemDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// DTO for a single item in the unified transaction list.
/// </summary>
public sealed class UnifiedTransactionItemDto
{
    /// <summary>Gets or sets the transaction identifier.</summary>
    public Guid Id
    {
        get; set;
    }

    /// <summary>Gets or sets the transaction date.</summary>
    public DateOnly Date
    {
        get; set;
    }

    /// <summary>Gets or sets the description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the monetary amount.</summary>
    public MoneyDto Amount { get; set; } = new();

    /// <summary>Gets or sets the account identifier.</summary>
    public Guid AccountId
    {
        get; set;
    }

    /// <summary>Gets or sets the account name.</summary>
    public string AccountName { get; set; } = string.Empty;

    /// <summary>Gets or sets the category identifier (null if uncategorized).</summary>
    public Guid? CategoryId
    {
        get; set;
    }

    /// <summary>Gets or sets the category name (null if uncategorized).</summary>
    public string? CategoryName
    {
        get; set;
    }

    /// <summary>Gets or sets a value indicating whether this transaction is from a recurring definition.</summary>
    public bool IsRecurring
    {
        get; set;
    }

    /// <summary>Gets or sets a value indicating whether this transaction is part of a transfer.</summary>
    public bool IsTransfer
    {
        get; set;
    }

    /// <summary>Gets or sets the concurrency version token for optimistic concurrency on inline edits.</summary>
    public string? Version
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the running balance after this transaction (only populated when filtered to a single account).
    /// </summary>
    public MoneyDto? RunningBalance
    {
        get; set;
    }
}
