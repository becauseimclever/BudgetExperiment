// <copyright file="ReconciliationRecordDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// DTO representing a completed reconciliation record.
/// </summary>
public sealed class ReconciliationRecordDto
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
    public decimal StatementBalance
    {
        get; set;
    }

    /// <summary>Gets or sets the currency code for the statement balance.</summary>
    public string StatementBalanceCurrency { get; set; } = string.Empty;

    /// <summary>Gets or sets the sum of cleared transaction amounts at reconciliation time.</summary>
    public decimal ClearedBalance
    {
        get; set;
    }

    /// <summary>Gets or sets the number of transactions locked to this reconciliation.</summary>
    public int TransactionCount
    {
        get; set;
    }

    /// <summary>Gets or sets the UTC timestamp when the reconciliation was completed.</summary>
    public DateTime CompletedAtUtc
    {
        get; set;
    }
}
