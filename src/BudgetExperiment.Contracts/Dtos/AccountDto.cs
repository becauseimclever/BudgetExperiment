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

    /// <summary>Gets or sets the initial balance amount.</summary>
    public decimal InitialBalance { get; set; }

    /// <summary>Gets or sets the initial balance currency.</summary>
    public string InitialBalanceCurrency { get; set; } = "USD";

    /// <summary>Gets or sets the date as of which the initial balance was recorded.</summary>
    public DateOnly InitialBalanceDate { get; set; }

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

    /// <summary>Gets or sets the initial balance amount (defaults to 0).</summary>
    public decimal InitialBalance { get; set; }

    /// <summary>Gets or sets the initial balance currency (defaults to USD).</summary>
    public string InitialBalanceCurrency { get; set; } = "USD";

    /// <summary>Gets or sets the date as of which the initial balance is recorded (defaults to today).</summary>
    public DateOnly? InitialBalanceDate { get; set; }

    /// <summary>Gets or sets the budget scope ("Shared" or "Personal"). Defaults to "Shared".</summary>
    public string Scope { get; set; } = "Shared";
}

/// <summary>
/// DTO for updating an existing account.
/// </summary>
public sealed class AccountUpdateDto
{
    /// <summary>Gets or sets the account name (optional - only updates if provided).</summary>
    public string? Name { get; set; }

    /// <summary>Gets or sets the account type as string (optional - only updates if provided).</summary>
    public string? Type { get; set; }

    /// <summary>Gets or sets the initial balance amount (optional - only updates if provided).</summary>
    public decimal? InitialBalance { get; set; }

    /// <summary>Gets or sets the initial balance currency (optional - only updates if provided).</summary>
    public string? InitialBalanceCurrency { get; set; }

    /// <summary>Gets or sets the date as of which the initial balance is recorded (optional).</summary>
    public DateOnly? InitialBalanceDate { get; set; }
}
