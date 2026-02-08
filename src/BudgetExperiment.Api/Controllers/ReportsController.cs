// <copyright file="ReportsController.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetExperiment.Api.Controllers;

/// <summary>
/// REST API controller for financial reports.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public sealed class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;
    private readonly IBudgetProgressService _budgetProgressService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReportsController"/> class.
    /// </summary>
    /// <param name="reportService">The report service.</param>
    /// <param name="budgetProgressService">The budget progress service.</param>
    public ReportsController(IReportService reportService, IBudgetProgressService budgetProgressService)
    {
        this._reportService = reportService;
        this._budgetProgressService = budgetProgressService;
    }

    /// <summary>
    /// Gets the monthly category spending report.
    /// </summary>
    /// <param name="year">The year.</param>
    /// <param name="month">The month (1-12).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The monthly category report.</returns>
    [HttpGet("categories/monthly")]
    [ProducesResponseType<MonthlyCategoryReportDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetMonthlyCategoryReportAsync(
        [FromQuery] int year,
        [FromQuery] int month,
        CancellationToken cancellationToken)
    {
        if (month < 1 || month > 12)
        {
            return this.BadRequest("Month must be between 1 and 12.");
        }

        if (year < 2000 || year > 2100)
        {
            return this.BadRequest("Year must be between 2000 and 2100.");
        }

        var report = await this._reportService.GetMonthlyCategoryReportAsync(year, month, cancellationToken);
        return this.Ok(report);
    }

    /// <summary>
    /// Gets the category spending report for an arbitrary date range.
    /// </summary>
    /// <param name="startDate">The start date (inclusive).</param>
    /// <param name="endDate">The end date (inclusive).</param>
    /// <param name="accountId">Optional account filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The date range category report.</returns>
    [HttpGet("categories/range")]
    [ProducesResponseType<DateRangeCategoryReportDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetCategoryReportByRangeAsync(
        [FromQuery] DateOnly startDate,
        [FromQuery] DateOnly endDate,
        [FromQuery] Guid? accountId,
        CancellationToken cancellationToken)
    {
        if (endDate < startDate)
        {
            return this.BadRequest("End date must be on or after start date.");
        }

        if (endDate.DayNumber - startDate.DayNumber > 366)
        {
            return this.BadRequest("Date range cannot exceed one year.");
        }

        var report = await this._reportService.GetCategoryReportByRangeAsync(startDate, endDate, accountId, cancellationToken);
        return this.Ok(report);
    }

    /// <summary>
    /// Gets spending trends over multiple months.
    /// </summary>
    /// <param name="months">Number of months to include (default 6, max 24).</param>
    /// <param name="endYear">Optional end year (defaults to current).</param>
    /// <param name="endMonth">Optional end month (defaults to current).</param>
    /// <param name="categoryId">Optional category filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The spending trends report.</returns>
    [HttpGet("trends")]
    [ProducesResponseType<SpendingTrendsReportDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetSpendingTrendsAsync(
        [FromQuery] int months = 6,
        [FromQuery] int? endYear = null,
        [FromQuery] int? endMonth = null,
        [FromQuery] Guid? categoryId = null,
        CancellationToken cancellationToken = default)
    {
        if (months < 1 || months > 24)
        {
            return this.BadRequest("Months must be between 1 and 24.");
        }

        if (endMonth.HasValue && (endMonth.Value < 1 || endMonth.Value > 12))
        {
            return this.BadRequest("End month must be between 1 and 12.");
        }

        if (endYear.HasValue && (endYear.Value < 2000 || endYear.Value > 2100))
        {
            return this.BadRequest("End year must be between 2000 and 2100.");
        }

        var report = await this._reportService.GetSpendingTrendsAsync(months, endYear, endMonth, categoryId, cancellationToken);
        return this.Ok(report);
    }

    /// <summary>
    /// Gets a spending summary for a single day.
    /// </summary>
    /// <param name="date">The date (yyyy-MM-dd).</param>
    /// <param name="accountId">Optional account filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The day summary.</returns>
    [HttpGet("day-summary/{date}")]
    [ProducesResponseType<DaySummaryDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDaySummaryAsync(
        DateOnly date,
        [FromQuery] Guid? accountId,
        CancellationToken cancellationToken)
    {
        var summary = await this._reportService.GetDaySummaryAsync(date, accountId, cancellationToken);
        return this.Ok(summary);
    }

    /// <summary>
    /// Gets the budget vs. actual comparison for a specific month.
    /// Convenience endpoint delegating to the existing budget progress service.
    /// </summary>
    /// <param name="year">The year.</param>
    /// <param name="month">The month (1-12).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The budget summary with per-category progress.</returns>
    [HttpGet("budget-comparison")]
    [ProducesResponseType<BudgetSummaryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetBudgetComparisonAsync(
        [FromQuery] int year,
        [FromQuery] int month,
        CancellationToken cancellationToken)
    {
        if (month < 1 || month > 12)
        {
            return this.BadRequest("Month must be between 1 and 12.");
        }

        if (year < 2000 || year > 2100)
        {
            return this.BadRequest("Year must be between 2000 and 2100.");
        }

        var summary = await this._budgetProgressService.GetMonthlySummaryAsync(year, month, cancellationToken);
        return this.Ok(summary);
    }
}
