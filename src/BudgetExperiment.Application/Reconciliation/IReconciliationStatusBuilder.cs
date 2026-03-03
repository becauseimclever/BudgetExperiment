// <copyright file="IReconciliationStatusBuilder.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Application.Reconciliation;

/// <summary>
/// Builds reconciliation status reports for a given period.
/// </summary>
public interface IReconciliationStatusBuilder
{
    /// <summary>
    /// Gets the reconciliation status for a specific year and month,
    /// showing which recurring instances are matched, pending, or missing.
    /// </summary>
    /// <param name="year">The year to report on.</param>
    /// <param name="month">The month to report on.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The reconciliation status for the period.</returns>
    Task<ReconciliationStatusDto> GetReconciliationStatusAsync(
        int year,
        int month,
        CancellationToken cancellationToken = default);
}
