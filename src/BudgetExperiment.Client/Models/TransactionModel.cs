// <copyright file="TransactionModel.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Client.Models;

/// <summary>
/// Client-side model for transaction data.
/// </summary>
public sealed class TransactionModel
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the account identifier.</summary>
    public Guid AccountId { get; set; }

    /// <summary>Gets or sets the transaction amount.</summary>
    public MoneyModel Amount { get; set; } = new();

    /// <summary>Gets or sets the transaction date.</summary>
    public DateOnly Date { get; set; }

    /// <summary>Gets or sets the description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional category.</summary>
    public string? Category { get; set; }

    /// <summary>Gets or sets the creation timestamp (UTC).</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Gets or sets the last update timestamp (UTC).</summary>
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Client-side model for creating a new transaction.
/// </summary>
public sealed class TransactionCreateModel
{
    /// <summary>Gets or sets the account identifier.</summary>
    public Guid AccountId { get; set; }

    /// <summary>Gets or sets the transaction amount.</summary>
    public decimal Amount { get; set; }

    /// <summary>Gets or sets the currency code.</summary>
    public string Currency { get; set; } = "USD";

    /// <summary>Gets or sets the transaction date.</summary>
    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    /// <summary>Gets or sets the description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional category.</summary>
    public string? Category { get; set; }
}
