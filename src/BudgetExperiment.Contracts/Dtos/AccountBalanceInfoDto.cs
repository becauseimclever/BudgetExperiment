// <copyright file="AccountBalanceInfoDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// Balance information for a single account, included in unified transaction list
/// when filtered to a single account.
/// </summary>
public sealed class AccountBalanceInfoDto
{
    /// <summary>Gets or sets the account's initial balance.</summary>
    public MoneyDto InitialBalance { get; set; } = new();

    /// <summary>Gets or sets the date of the initial balance.</summary>
    public DateOnly InitialBalanceDate
    {
        get; set;
    }

    /// <summary>Gets or sets the current balance (initial balance + sum of all transactions).</summary>
    public MoneyDto CurrentBalance { get; set; } = new();
}
