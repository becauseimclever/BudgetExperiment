// <copyright file="ExportController.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Globalization;

using BudgetExperiment.Application.Export;
using BudgetExperiment.Contracts.Dtos;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetExperiment.Api.Controllers;

/// <summary>
/// REST API controller for report exports.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/exports")]
public sealed class ExportController : ControllerBase
{
    private readonly IReportService _reportService;
    private readonly IBudgetProgressService _budgetProgressService;
    private readonly IExportService _exportService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExportController"/> class.
    /// </summary>
    /// <param name="reportService">Report service.</param>
    /// <param name="budgetProgressService">Budget progress service.</param>
    /// <param name="exportService">Export service.</param>
    public ExportController(
        IReportService reportService,
        IBudgetProgressService budgetProgressService,
        IExportService exportService)
    {
        this._reportService = reportService;
        this._budgetProgressService = budgetProgressService;
        this._exportService = exportService;
    }

    /// <summary>
    /// Exports the monthly category report as CSV.
    /// </summary>
    /// <param name="year">Year (e.g., 2026).</param>
    /// <param name="month">Month (1-12).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>CSV file content.</returns>
    [HttpGet("categories/monthly")]
    [Produces("text/csv")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExportMonthlyCategoryReportAsync(
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
        var table = BuildCategoryTable(
            $"Monthly Categories {report.Year}-{report.Month:D2}",
            report.Categories);
        var fileName = $"monthly-categories-{year}-{month:D2}";
        var export = await this._exportService.ExportTableAsync(table, ExportFormat.Csv, fileName, cancellationToken);
        return this.File(export.Content, export.ContentType, export.FileName);
    }

    /// <summary>
    /// Exports the category report for a date range as CSV.
    /// </summary>
    /// <param name="startDate">Start date (yyyy-MM-dd).</param>
    /// <param name="endDate">End date (yyyy-MM-dd).</param>
    /// <param name="accountId">Optional account filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>CSV file content.</returns>
    [HttpGet("categories/range")]
    [Produces("text/csv")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExportCategoryReportByRangeAsync(
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
        var title = $"Category Range {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}";
        var table = BuildCategoryTable(title, report.Categories);
        var fileName = $"category-range-{startDate:yyyy-MM-dd}-to-{endDate:yyyy-MM-dd}";
        var export = await this._exportService.ExportTableAsync(table, ExportFormat.Csv, fileName, cancellationToken);
        return this.File(export.Content, export.ContentType, export.FileName);
    }

    /// <summary>
    /// Exports spending trends as CSV.
    /// </summary>
    /// <param name="months">Number of months to include.</param>
    /// <param name="endYear">Optional end year.</param>
    /// <param name="endMonth">Optional end month.</param>
    /// <param name="categoryId">Optional category filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>CSV file content.</returns>
    [HttpGet("trends")]
    [Produces("text/csv")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExportSpendingTrendsAsync(
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
        var table = BuildTrendsTable(report);
        var fileName = $"spending-trends-{months}m";
        var export = await this._exportService.ExportTableAsync(table, ExportFormat.Csv, fileName, cancellationToken);
        return this.File(export.Content, export.ContentType, export.FileName);
    }

    /// <summary>
    /// Exports the budget comparison report as CSV.
    /// </summary>
    /// <param name="year">Year.</param>
    /// <param name="month">Month.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>CSV file content.</returns>
    [HttpGet("budget-comparison")]
    [Produces("text/csv")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExportBudgetComparisonAsync(
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
        var table = BuildBudgetComparisonTable(summary);
        var fileName = $"budget-comparison-{year}-{month:D2}";
        var export = await this._exportService.ExportTableAsync(table, ExportFormat.Csv, fileName, cancellationToken);
        return this.File(export.Content, export.ContentType, export.FileName);
    }

    private static ExportTable BuildCategoryTable(string title, IReadOnlyList<CategorySpendingDto> categories)
    {
        var columns = new List<string> { "Category", "Amount", "Currency", "Percentage", "Transactions" };
        var rows = categories
            .Select(category => new List<string>
            {
                category.CategoryName,
                category.Amount.Amount.ToString("0.00", CultureInfo.InvariantCulture),
                category.Amount.Currency,
                category.Percentage.ToString("0.0", CultureInfo.InvariantCulture),
                category.TransactionCount.ToString(CultureInfo.InvariantCulture),
            })
            .Cast<IReadOnlyList<string>>()
            .ToList();

        return new ExportTable(title, columns, rows);
    }

    private static ExportTable BuildTrendsTable(SpendingTrendsReportDto report)
    {
        var columns = new List<string> { "Month", "Income", "Spending", "Net", "Transactions" };
        var rows = report.MonthlyData
            .OrderBy(point => point.Year)
            .ThenBy(point => point.Month)
            .Select(point => new List<string>
            {
                new DateOnly(point.Year, point.Month, 1).ToString("yyyy-MM", CultureInfo.InvariantCulture),
                point.TotalIncome.Amount.ToString("0.00", CultureInfo.InvariantCulture),
                point.TotalSpending.Amount.ToString("0.00", CultureInfo.InvariantCulture),
                point.NetAmount.Amount.ToString("0.00", CultureInfo.InvariantCulture),
                point.TransactionCount.ToString(CultureInfo.InvariantCulture),
            })
            .Cast<IReadOnlyList<string>>()
            .ToList();

        return new ExportTable("Spending Trends", columns, rows);
    }

    private static ExportTable BuildBudgetComparisonTable(BudgetSummaryDto summary)
    {
        var columns = new List<string>
        {
            "Category",
            "Budgeted",
            "Spent",
            "Remaining",
            "PercentUsed",
            "Status",
            "Transactions",
        };

        var rows = summary.CategoryProgress
            .Select(category => new List<string>
            {
                category.CategoryName,
                category.TargetAmount.Amount.ToString("0.00", CultureInfo.InvariantCulture),
                category.SpentAmount.Amount.ToString("0.00", CultureInfo.InvariantCulture),
                category.RemainingAmount.Amount.ToString("0.00", CultureInfo.InvariantCulture),
                category.PercentUsed.ToString("0.0", CultureInfo.InvariantCulture),
                category.Status,
                category.TransactionCount.ToString(CultureInfo.InvariantCulture),
            })
            .Cast<IReadOnlyList<string>>()
            .ToList();

        return new ExportTable("Budget Comparison", columns, rows);
    }
}
