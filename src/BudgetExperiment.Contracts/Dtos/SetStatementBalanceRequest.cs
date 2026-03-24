// <copyright file="SetStatementBalanceRequest.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>Request to set the statement balance for an account.</summary>
public sealed class SetStatementBalanceRequest
{
    /// <summary>Gets or sets the account identifier.</summary>
    public Guid AccountId { get; set; }

    /// <summary>Gets or sets the statement closing date.</summary>
    public DateOnly StatementDate { get; set; }

    /// <summary>Gets or sets the balance from the bank statement.</summary>
    public decimal Balance { get; set; }
}
