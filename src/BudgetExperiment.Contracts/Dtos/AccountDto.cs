// <copyright file="AccountDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// DTO for returning account details.
/// </summary>
public sealed class AccountDto
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the account name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the account type as string.</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Gets or sets the creation timestamp (UTC).</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Gets or sets the last update timestamp (UTC).</summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>Gets or sets the transactions for this account.</summary>
    public IReadOnlyList<TransactionDto> Transactions { get; set; } = new List<TransactionDto>();
}

/// <summary>
/// DTO for creating a new account.
/// </summary>
public sealed class AccountCreateDto
{
    /// <summary>Gets or sets the account name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the account type as string (Checking, Savings, CreditCard, Cash, Other).</summary>
    public string Type { get; set; } = "Checking";
}
