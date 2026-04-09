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
[ApiVersion("1.0")]
[ApiController]
[Authorize]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
public sealed class ReportsController : ControllerBase
{
    private const string KakeiboDateRangeFeatureFlag = "Kakeibo:DateRangeReports";

    private readonly IReportService _reportService;
    private readonly IBudgetProgressService _budgetProgressService;
    private readonly IKakeiboReportService _kakeiboReportService;
    private readonly IFeatureFlagService _featureFlagService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReportsController"/> class.
    /// </summary>
    /// <param name="reportService">The report service.</param>
    /// <param name="budgetProgressService">The budget progress service.</param>
    /// <param name="kakeiboReportService">The Kakeibo date-range report service.</param>
    /// <param name="featureFlagService">The feature flag service.</param>
    public ReportsController(
        IReportService reportService,
        IBudgetProgressService budgetProgressService,
        IKakeiboReportService kakeiboReportService,
        IFeatureFlagService featureFlagService)
    {
        _reportService = reportService;
        _budgetProgressService = budgetProgressService;
        _kakeiboReportService = kakeiboReportService;
        _featureFlagService = featureFlagService;
    }

    /// <summary>
    /// Gets the monthly category spending report.
    /// </summary>
    /// <param name="year">The year (e.g., 2026).</param>
    /// <param name="month">The month (1-12).</param>
    /// <param name="groupByKakeibo">Whether to include Kakeibo grouped summary.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The monthly category report with per-category spending totals and percentages.</returns>
    /// <remarks>
    /// Example: <c>GET /api/v1/reports/categories/monthly?year=2026&amp;month=2</c>
    /// Returns overall income/spending totals and a breakdown by category.
    /// </remarks>
    [HttpGet("categories/monthly")]
    [ProducesResponseType<MonthlyCategoryReportDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetMonthlyCategoryReportAsync(
        [FromQuery] int year,
        [FromQuery] int month,
        [FromQuery] bool groupByKakeibo = false,
        CancellationToken cancellationToken = default)
    {
        if (month < 1 || month > 12)
        {
            return this.BadRequest("Month must be between 1 and 12.");
        }

        if (year < 2000 || year > 2100)
        {
            return this.BadRequest("Year must be between 2000 and 2100.");
        }

        var report = await _reportService.GetMonthlyCategoryReportAsync(year, month, groupByKakeibo, cancellationToken);
        return this.Ok(report);
    }

    /// <summary>
    /// Gets the category spending report for an arbitrary date range.
    /// </summary>
    /// <param name="startDate">The start date, inclusive (format: yyyy-MM-dd, e.g., 2026-01-15).</param>
    /// <param name="endDate">The end date, inclusive (format: yyyy-MM-dd, e.g., 2026-02-15).</param>
    /// <param name="accountId">Optional account filter.</param>
    /// <param name="groupByKakeibo">Whether to include Kakeibo grouped summary.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The date range category report with per-category spending totals and percentages.</returns>
    /// <remarks>
    /// Example: <c>GET /api/v1/reports/categories/range?startDate=2026-01-15&amp;endDate=2026-02-15</c>
    /// Supports paycheck-to-paycheck analysis or custom period analysis.
    /// Maximum range: 366 days.
    /// </remarks>
    [HttpGet("categories/range")]
    [ProducesResponseType<DateRangeCategoryReportDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetCategoryReportByRangeAsync(
        [FromQuery] DateOnly startDate,
        [FromQuery] DateOnly endDate,
        [FromQuery] Guid? accountId,
        [FromQuery] bool groupByKakeibo = false,
        CancellationToken cancellationToken = default)
    {
        if (endDate < startDate)
        {
            return this.BadRequest("End date must be on or after start date.");
        }

        if (endDate.DayNumber - startDate.DayNumber > 366)
        {
            return this.BadRequest("Date range cannot exceed one year.");
        }

        var report = await _reportService.GetCategoryReportByRangeAsync(startDate, endDate, accountId, groupByKakeibo, cancellationToken);
        return this.Ok(report);
    }

    /// <summary>
    /// Gets spending trends over multiple months.
    /// </summary>
    /// <param name="months">Number of months to include (default 6, max 24).</param>
    /// <param name="endYear">Optional end year (defaults to current year).</param>
    /// <param name="endMonth">Optional end month (defaults to current month).</param>
    /// <param name="categoryId">Optional category filter to view a single category over time.</param>
    /// <param name="groupByKakeibo">Whether to include Kakeibo grouped summary.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Monthly totals with income, spending, net, and trend direction.</returns>
    /// <remarks>
    /// Example: <c>GET /api/v1/reports/trends?months=12</c>
    /// Example with category filter: <c>GET /api/v1/reports/trends?months=6&amp;categoryId=abc123</c>
    /// Includes average monthly spending/income and overall trend direction (increasing/stable/decreasing).
    /// </remarks>
    [HttpGet("trends")]
    [ProducesResponseType<SpendingTrendsReportDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetSpendingTrendsAsync(
        [FromQuery] int months = 6,
        [FromQuery] int? endYear = null,
        [FromQuery] int? endMonth = null,
        [FromQuery] Guid? categoryId = null,
        [FromQuery] bool groupByKakeibo = false,
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

        var report = await _reportService.GetSpendingTrendsAsync(months, endYear, endMonth, categoryId, groupByKakeibo, cancellationToken);
        return this.Ok(report);
    }

    /// <summary>
    /// Gets a spending summary for a single day.
    /// </summary>
    /// <param name="date">The date (format: yyyy-MM-dd, e.g., 2026-02-05).</param>
    /// <param name="accountId">Optional account filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Income, spending, net totals and top 3 spending categories for the day.</returns>
    /// <remarks>
    /// Example: <c>GET /api/v1/reports/day-summary/2026-02-05</c>
    /// Returns zero totals for days with no transactions.
    /// </remarks>
    [HttpGet("day-summary/{date}")]
    [ProducesResponseType<DaySummaryDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDaySummaryAsync(
        DateOnly date,
        [FromQuery] Guid? accountId,
        CancellationToken cancellationToken)
    {
        var summary = await _reportService.GetDaySummaryAsync(date, accountId, cancellationToken);
        return this.Ok(summary);
    }

    /// <summary>
    /// Gets the budget vs. actual comparison for a specific month.
    /// Convenience endpoint delegating to the existing budget progress service.
    /// </summary>
    /// <param name="year">The year (e.g., 2026).</param>
    /// <param name="month">The month (1-12).</param>
    /// <param name="groupByKakeibo">Whether to include Kakeibo grouped summary.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The budget summary with per-category target vs. spent vs. remaining and status.</returns>
    /// <remarks>
    /// Example: <c>GET /api/v1/reports/budget-comparison?year=2026&amp;month=2</c>
    /// Returns per-category budget progress (OnTrack/Warning/OverBudget/NoBudgetSet).
    /// Same data shape as the budget goals summary endpoint.
    /// </remarks>
    [HttpGet("budget-comparison")]
    [ProducesResponseType<BudgetSummaryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetBudgetComparisonAsync(
        [FromQuery] int year,
        [FromQuery] int month,
        [FromQuery] bool groupByKakeibo = false,
        CancellationToken cancellationToken = default)
    {
        if (month < 1 || month > 12)
        {
            return this.BadRequest("Month must be between 1 and 12.");
        }

        if (year < 2000 || year > 2100)
        {
            return this.BadRequest("Year must be between 2000 and 2100.");
        }

        var summary = await _budgetProgressService.GetMonthlySummaryAsync(year, month, groupByKakeibo, cancellationToken);
        return this.Ok(summary);
    }

    /// <summary>
    /// Gets spending aggregated by geographic location for a date range.
    /// </summary>
    /// <param name="startDate">The start date, inclusive (format: yyyy-MM-dd).</param>
    /// <param name="endDate">The end date, inclusive (format: yyyy-MM-dd).</param>
    /// <param name="accountId">Optional account filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The location spending report with per-region aggregation.</returns>
    /// <remarks>
    /// Example: <c>GET /api/v1/reports/spending-by-location?startDate=2026-01-01&amp;endDate=2026-01-31</c>
    /// Maximum range: 366 days.
    /// </remarks>
    [HttpGet("spending-by-location")]
    [ProducesResponseType<LocationSpendingReportDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetSpendingByLocationAsync(
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

        var report = await _reportService.GetSpendingByLocationAsync(startDate, endDate, accountId, cancellationToken);
        return this.Ok(report);
    }

    /// <summary>
    /// Gets Kakeibo bucket totals (Essentials, Wants, Culture, Unexpected) for a date range.
    /// Returns daily, weekly (ISO Monday–Sunday), and overall totals for all four buckets.
    /// Income and Transfer transactions are excluded. Zero-spend buckets are always included.
    /// Feature-flagged: returns 404 when <c>Kakeibo:DateRangeReports</c> is disabled.
    /// </summary>
    /// <param name="from">Start date (inclusive), ISO 8601 format (e.g., 2026-03-01).</param>
    /// <param name="to">End date (inclusive), ISO 8601 format (e.g., 2026-03-31).</param>
    /// <param name="accountId">Optional account filter; omit for all accounts.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A <see cref="KakeiboSummary"/> with daily, weekly, and total bucket aggregations,
    /// or 400 if <paramref name="from"/> is after <paramref name="to"/>,
    /// or 404 if the feature flag is disabled.
    /// </returns>
    /// <remarks>
    /// Example: <c>GET /api/v1/reports/kakeibo?from=2026-03-01&amp;to=2026-03-31</c>.
    /// </remarks>
    [HttpGet("kakeibo")]
    [ProducesResponseType<KakeiboSummary>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetKakeiboReportAsync(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        [FromQuery] Guid? accountId,
        CancellationToken cancellationToken)
    {
        if (!await _featureFlagService.IsEnabledAsync(KakeiboDateRangeFeatureFlag, cancellationToken))
        {
            return this.NotFound();
        }

        if (from > to)
        {
            return this.BadRequest("'from' must be on or before 'to'.");
        }

        var summary = await _kakeiboReportService.GetKakeiboSummaryAsync(from, to, accountId, cancellationToken);
        return this.Ok(summary);
    }
}
