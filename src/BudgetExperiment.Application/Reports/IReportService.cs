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

    /// <summary>
    /// Gets the category spending report for an arbitrary date range.
    /// </summary>
    /// <param name="startDate">The start date (inclusive).</param>
    /// <param name="endDate">The end date (inclusive).</param>
    /// <param name="accountId">Optional account filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The date range category report DTO.</returns>
    Task<DateRangeCategoryReportDto> GetCategoryReportByRangeAsync(DateOnly startDate, DateOnly endDate, Guid? accountId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets spending trends over multiple months.
    /// </summary>
    /// <param name="months">Number of months to include (default 6, max 24).</param>
    /// <param name="endYear">Optional end year (defaults to current).</param>
    /// <param name="endMonth">Optional end month (defaults to current).</param>
    /// <param name="categoryId">Optional category filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The spending trends report DTO.</returns>
    Task<SpendingTrendsReportDto> GetSpendingTrendsAsync(int months = 6, int? endYear = null, int? endMonth = null, Guid? categoryId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a spending summary for a single day.
    /// </summary>
    /// <param name="date">The date to summarize.</param>
    /// <param name="accountId">Optional account filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The day summary DTO.</returns>
    Task<DaySummaryDto> GetDaySummaryAsync(DateOnly date, Guid? accountId = null, CancellationToken cancellationToken = default);
}
