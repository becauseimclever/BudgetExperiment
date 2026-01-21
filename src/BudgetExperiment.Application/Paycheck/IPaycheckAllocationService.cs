// <copyright file="IPaycheckAllocationService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.Paycheck;

/// <summary>
/// Service for calculating paycheck allocations for recurring bills.
/// </summary>
public interface IPaycheckAllocationService
{
    /// <summary>
    /// Gets the paycheck allocation summary.
    /// </summary>
    /// <param name="paycheckFrequency">The paycheck frequency.</param>
    /// <param name="paycheckAmount">Optional paycheck amount for income calculations.</param>
    /// <param name="accountId">Optional account filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The allocation summary DTO.</returns>
    Task<PaycheckAllocationSummaryDto> GetAllocationSummaryAsync(
        RecurrenceFrequency paycheckFrequency,
        decimal? paycheckAmount = null,
        Guid? accountId = null,
        CancellationToken cancellationToken = default);
}
