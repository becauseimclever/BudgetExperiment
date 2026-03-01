// <copyright file="AccountUpdateDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

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
