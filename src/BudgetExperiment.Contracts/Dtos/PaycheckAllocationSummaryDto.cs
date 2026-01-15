// <copyright file="PaycheckAllocationSummaryDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// DTO for the complete paycheck allocation summary.
/// </summary>
public sealed class PaycheckAllocationSummaryDto
{
    /// <summary>Gets or sets the individual allocations.</summary>
    public List<PaycheckAllocationDto> Allocations { get; set; } = new();

    /// <summary>Gets or sets the total per paycheck.</summary>
    public MoneyDto TotalPerPaycheck { get; set; } = new();

    /// <summary>Gets or sets the optional paycheck amount.</summary>
    public MoneyDto? PaycheckAmount { get; set; }

    /// <summary>Gets or sets the remaining per paycheck after allocations.</summary>
    public MoneyDto RemainingPerPaycheck { get; set; } = new();

    /// <summary>Gets or sets the shortfall amount if allocations exceed paycheck.</summary>
    public MoneyDto Shortfall { get; set; } = new();

    /// <summary>Gets or sets the total annual bills.</summary>
    public MoneyDto TotalAnnualBills { get; set; } = new();

    /// <summary>Gets or sets the optional total annual income.</summary>
    public MoneyDto? TotalAnnualIncome { get; set; }

    /// <summary>Gets or sets the warnings.</summary>
    public List<PaycheckAllocationWarningDto> Warnings { get; set; } = new();

    /// <summary>Gets or sets a value indicating whether there are any warnings.</summary>
    public bool HasWarnings { get; set; }

    /// <summary>Gets or sets a value indicating whether annual bills exceed income.</summary>
    public bool CannotReconcile { get; set; }

    /// <summary>Gets or sets the paycheck frequency.</summary>
    public string PaycheckFrequency { get; set; } = string.Empty;
}
