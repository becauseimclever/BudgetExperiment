// <copyright file="IReportService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Application.Reports;

/// <summary>
/// Service interface for generating financial reports.
/// </summary>
public interface IReportService
{
    /// <summary>
    /// Gets the monthly category spending report for a specific month.
    /// </summary>
    /// <param name="year">The year.</param>
    /// <param name="month">The month (1-12).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The monthly category report DTO.</returns>
    Task<MonthlyCategoryReportDto> GetMonthlyCategoryReportAsync(int year, int month, CancellationToken cancellationToken = default);
}
