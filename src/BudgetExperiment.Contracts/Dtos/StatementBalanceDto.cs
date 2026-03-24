// <copyright file="StatementBalanceDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// DTO representing an active statement balance entry for reconciliation.
/// </summary>
public sealed class StatementBalanceDto
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id
    {
        get; set;
    }

    /// <summary>Gets or sets the account identifier.</summary>
    public Guid AccountId
    {
        get; set;
    }

    /// <summary>Gets or sets the statement closing date.</summary>
    public DateOnly StatementDate
    {
        get; set;
    }

    /// <summary>Gets or sets the balance reported on the bank statement.</summary>
    public decimal Balance
    {
        get; set;
    }

    /// <summary>Gets or sets the currency code.</summary>
    public string Currency { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether this statement balance has been completed.</summary>
    public bool IsCompleted
    {
        get; set;
    }
}
