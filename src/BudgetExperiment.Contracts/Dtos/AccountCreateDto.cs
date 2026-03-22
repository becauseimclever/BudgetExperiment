// <copyright file="AccountCreateDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// DTO for creating a new account.
/// </summary>
public sealed class AccountCreateDto
{
    /// <summary>Gets or sets the account name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the account type as string (Checking, Savings, CreditCard, Cash, Other).</summary>
    public string Type { get; set; } = "Checking";

    /// <summary>Gets or sets the initial balance amount (defaults to 0).</summary>
    public decimal InitialBalance
    {
        get; set;
    }

    /// <summary>Gets or sets the initial balance currency (defaults to USD).</summary>
    public string InitialBalanceCurrency { get; set; } = "USD";

    /// <summary>Gets or sets the date as of which the initial balance is recorded (defaults to today).</summary>
    public DateOnly? InitialBalanceDate
    {
        get; set;
    }

    /// <summary>Gets or sets the budget scope ("Shared" or "Personal"). Defaults to "Shared".</summary>
    public string Scope { get; set; } = "Shared";
}
