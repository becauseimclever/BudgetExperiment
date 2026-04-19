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
    public Guid Id
    {
        get; set;
    }

    /// <summary>Gets or sets the account name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the account type as string.</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Gets or sets the initial balance amount.</summary>
    public decimal InitialBalance
    {
        get; set;
    }

    /// <summary>Gets or sets the initial balance currency.</summary>
    public string InitialBalanceCurrency { get; set; } = "USD";

    /// <summary>Gets or sets the date as of which the initial balance was recorded.</summary>
    public DateOnly InitialBalanceDate
    {
        get; set;
    }

    /// <summary>Gets or sets the creation timestamp (UTC).</summary>
    public DateTime CreatedAtUtc
    {
        get; set;
    }

    /// <summary>Gets or sets the last update timestamp (UTC).</summary>
    public DateTime UpdatedAtUtc
    {
        get; set;
    }

    /// <summary>Gets or sets the concurrency version token for optimistic concurrency.</summary>
    public string? Version
    {
        get; set;
    }

    /// <summary>Gets or sets the transactions for this account.</summary>
    public IReadOnlyList<TransactionDto> Transactions { get; set; } = new List<TransactionDto>();
}
