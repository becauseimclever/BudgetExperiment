// <copyright file="ClearedBalanceDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// DTO representing the computed cleared balance for an account up to an optional date.
/// </summary>
public sealed class ClearedBalanceDto
{
    /// <summary>Gets or sets the account identifier.</summary>
    public Guid AccountId
    {
        get; set;
    }

    /// <summary>Gets or sets the sum of cleared transaction amounts.</summary>
    public decimal ClearedBalance
    {
        get; set;
    }

    /// <summary>Gets or sets the account's initial balance.</summary>
    public decimal InitialBalance
    {
        get; set;
    }

    /// <summary>Gets or sets the optional upper bound date used to compute the balance.</summary>
    public DateOnly? UpToDate
    {
        get; set;
    }

    /// <summary>Gets or sets the currency code.</summary>
    public string Currency { get; set; } = string.Empty;
}
