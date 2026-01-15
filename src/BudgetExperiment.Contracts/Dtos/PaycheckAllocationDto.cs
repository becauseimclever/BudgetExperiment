// <copyright file="PaycheckAllocationDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// DTO for a single bill allocation per paycheck.
/// </summary>
public sealed class PaycheckAllocationDto
{
    /// <summary>Gets or sets the bill description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the bill amount.</summary>
    public MoneyDto BillAmount { get; set; } = new();

    /// <summary>Gets or sets the bill frequency.</summary>
    public string BillFrequency { get; set; } = string.Empty;

    /// <summary>Gets or sets the amount to allocate per paycheck.</summary>
    public MoneyDto AmountPerPaycheck { get; set; } = new();

    /// <summary>Gets or sets the annual amount for this bill.</summary>
    public MoneyDto AnnualAmount { get; set; } = new();

    /// <summary>Gets or sets the optional recurring transaction identifier.</summary>
    public Guid? RecurringTransactionId { get; set; }
}
