// <copyright file="AccountModel.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;

namespace BudgetExperiment.Client.Models;

/// <summary>
/// Client-side model for account data.
/// </summary>
public sealed class AccountModel
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the account name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the account type.</summary>
    public AccountType Type { get; set; }

    /// <summary>Gets or sets the creation timestamp (UTC).</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Gets or sets the last update timestamp (UTC).</summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>Gets or sets the transactions for this account.</summary>
    public List<TransactionModel> Transactions { get; set; } = new();
}

/// <summary>
/// Client-side model for creating a new account.
/// </summary>
public sealed class AccountCreateModel
{
    /// <summary>Gets or sets the account name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the account type.</summary>
    public AccountType Type { get; set; }
}
