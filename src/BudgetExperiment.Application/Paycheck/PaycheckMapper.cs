// <copyright file="PaycheckMapper.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Application.Paycheck;

/// <summary>
/// Mappers for paycheck allocation entities to DTOs.
/// </summary>
public static class PaycheckMapper
{
    /// <summary>
    /// Maps a <see cref="PaycheckAllocation"/> to a <see cref="PaycheckAllocationDto"/>.
    /// </summary>
    /// <param name="allocation">The allocation.</param>
    /// <returns>The mapped DTO.</returns>
    public static PaycheckAllocationDto ToDto(PaycheckAllocation allocation)
    {
        return new PaycheckAllocationDto
        {
            Description = allocation.Bill.Description,
            BillAmount = CommonMapper.ToDto(allocation.Bill.Amount),
            BillFrequency = allocation.Bill.Frequency.ToString(),
            AmountPerPaycheck = CommonMapper.ToDto(allocation.AmountPerPaycheck),
            AnnualAmount = CommonMapper.ToDto(allocation.AnnualAmount),
            RecurringTransactionId = allocation.Bill.SourceRecurringTransactionId,
        };
    }

    /// <summary>
    /// Maps a <see cref="PaycheckAllocationWarning"/> to a <see cref="PaycheckAllocationWarningDto"/>.
    /// </summary>
    /// <param name="warning">The warning.</param>
    /// <returns>The mapped DTO.</returns>
    public static PaycheckAllocationWarningDto ToDto(PaycheckAllocationWarning warning)
    {
        return new PaycheckAllocationWarningDto
        {
            Type = warning.Type.ToString(),
            Message = warning.Message,
            Amount = warning.Amount is not null ? CommonMapper.ToDto(warning.Amount) : null,
        };
    }

    /// <summary>
    /// Maps a <see cref="PaycheckAllocationSummary"/> to a <see cref="PaycheckAllocationSummaryDto"/>.
    /// </summary>
    /// <param name="summary">The summary.</param>
    /// <returns>The mapped DTO.</returns>
    public static PaycheckAllocationSummaryDto ToDto(PaycheckAllocationSummary summary)
    {
        return new PaycheckAllocationSummaryDto
        {
            Allocations = summary.Allocations.Select(ToDto).ToList(),
            TotalPerPaycheck = CommonMapper.ToDto(summary.TotalPerPaycheck),
            PaycheckAmount = summary.PaycheckAmount is not null ? CommonMapper.ToDto(summary.PaycheckAmount) : null,
            RemainingPerPaycheck = CommonMapper.ToDto(summary.RemainingPerPaycheck),
            Shortfall = CommonMapper.ToDto(summary.Shortfall),
            TotalAnnualBills = CommonMapper.ToDto(summary.TotalAnnualBills),
            TotalAnnualIncome = summary.TotalAnnualIncome is not null ? CommonMapper.ToDto(summary.TotalAnnualIncome) : null,
            Warnings = summary.Warnings.Select(ToDto).ToList(),
            HasWarnings = summary.HasWarnings,
            CannotReconcile = summary.CannotReconcile,
            PaycheckFrequency = summary.PaycheckFrequency.ToString(),
        };
    }
}
