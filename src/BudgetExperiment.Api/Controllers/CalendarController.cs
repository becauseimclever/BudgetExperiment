// <copyright file="CalendarController.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetExperiment.Api.Controllers;

/// <summary>
/// REST API controller for calendar-related operations.
/// </summary>
[ApiVersion("1.0")]
[ApiController]
[Authorize]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
public sealed class CalendarController : ControllerBase
{
    private readonly ICalendarService _calendarService;
    private readonly ICalendarGridService _gridService;
    private readonly IDayDetailService _dayDetailService;
    private readonly ITransactionListService _transactionListService;
    private readonly IKakeiboCalendarService _kakeiboCalendarService;
    private readonly IReflectionService _reflectionService;
    private readonly IFeatureFlagService _featureFlagService;
    private readonly IUserContext _userContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="CalendarController"/> class.
    /// </summary>
    /// <param name="calendarService">The calendar service.</param>
    /// <param name="gridService">The calendar grid service.</param>
    /// <param name="dayDetailService">The day detail service.</param>
    /// <param name="transactionListService">The transaction list service.</param>
    /// <param name="kakeiboCalendarService">The Kakeibo calendar analytics service.</param>
    /// <param name="reflectionService">The monthly reflection service.</param>
    /// <param name="featureFlagService">The feature flag service.</param>
    /// <param name="userContext">The current user context.</param>
    public CalendarController(
        ICalendarService calendarService,
        ICalendarGridService gridService,
        IDayDetailService dayDetailService,
        ITransactionListService transactionListService,
        IKakeiboCalendarService kakeiboCalendarService,
        IReflectionService reflectionService,
        IFeatureFlagService featureFlagService,
        IUserContext userContext)
    {
        _calendarService = calendarService;
        _gridService = gridService;
        _dayDetailService = dayDetailService;
        _transactionListService = transactionListService;
        _kakeiboCalendarService = kakeiboCalendarService;
        _reflectionService = reflectionService;
        _featureFlagService = featureFlagService;
        _userContext = userContext;
    }

    /// <summary>
    /// Gets a complete calendar grid with all data pre-computed.
    /// </summary>
    /// <param name="year">The year.</param>
    /// <param name="month">The month (1-12).</param>
    /// <param name="accountId">Optional account filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A complete calendar grid DTO.</returns>
    [HttpGet("grid")]
    [ProducesResponseType<CalendarGridDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetCalendarGridAsync(
        [FromQuery] int year,
        [FromQuery] int month,
        [FromQuery] Guid? accountId,
        CancellationToken cancellationToken)
    {
        if (month < 1 || month > 12)
        {
            return this.BadRequest("Month must be between 1 and 12.");
        }

        if (year < 1 || year > 9999)
        {
            return this.BadRequest("Year must be between 1 and 9999.");
        }

        var grid = await _gridService.GetCalendarGridAsync(year, month, accountId, cancellationToken);

        var userId = _userContext.UserIdAsGuid ?? Guid.Empty;

        var kakeiboEnabled = await _featureFlagService.IsEnabledAsync("Kakeibo:CalendarOverlay", cancellationToken);
        if (kakeiboEnabled && userId != Guid.Empty && grid.Days.Count > 0)
        {
            var gridStart = grid.Days[0].Date;
            var weekCount = grid.Days.Count / 7;

            var monthBreakdownTask = _kakeiboCalendarService.GetMonthBreakdownAsync(year, month, userId, cancellationToken);
            var weekBreakdownsTask = _kakeiboCalendarService.GetGridWeekBreakdownsAsync(gridStart, weekCount, userId, cancellationToken);

            var monthBreakdown = await monthBreakdownTask;
            var weekBreakdowns = await weekBreakdownsTask;

            grid.KakeiboBreakdown = MapBreakdown(monthBreakdown);
            grid.WeekKakeiboBreakdowns = weekBreakdowns.ToDictionary(
                kvp => kvp.Key,
                kvp => MapBreakdown(kvp.Value));
        }

        var reflectionEnabled = await _featureFlagService.IsEnabledAsync("Kakeibo:MonthlyReflectionPrompts", cancellationToken);
        if (reflectionEnabled && userId != Guid.Empty)
        {
            var reflection = await _reflectionService.GetByMonthAsync(year, month, userId, cancellationToken);
            if (reflection is { SavingsGoal: > 0 })
            {
                var actualSavings = reflection.ActualSavings ?? 0m;
                var remaining = reflection.SavingsGoal - actualSavings;
                var progressPct = (int)Math.Clamp(
                    Math.Round(actualSavings / reflection.SavingsGoal * 100m),
                    0,
                    100);

                grid.SavingsProgress = new SavingsProgressResponse
                {
                    SavingsGoal = reflection.SavingsGoal,
                    ActualSavings = actualSavings,
                    Remaining = remaining,
                    ProgressPercentage = progressPct,
                };
            }
        }

        return this.Ok(grid);
    }

    /// <summary>
    /// Gets detailed information for a specific day.
    /// </summary>
    /// <param name="date">The date (YYYY-MM-DD format).</param>
    /// <param name="accountId">Optional account filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Day detail DTO with all transactions and recurring instances.</returns>
    [HttpGet("day/{date}")]
    [ProducesResponseType<DayDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetDayDetailAsync(
        [FromRoute] DateOnly date,
        [FromQuery] Guid? accountId,
        CancellationToken cancellationToken)
    {
        var detail = await _dayDetailService.GetDayDetailAsync(date, accountId, cancellationToken);

        var kakeiboEnabled = await _featureFlagService.IsEnabledAsync("Kakeibo:CalendarOverlay", cancellationToken);
        var userId = _userContext.UserIdAsGuid ?? Guid.Empty;
        if (kakeiboEnabled && userId != Guid.Empty)
        {
            var dominantCategory = await _kakeiboCalendarService.GetDominantCategoryAsync(date, userId, cancellationToken);
            detail.DominantKakeiboCategory = dominantCategory?.ToString();
        }

        return this.Ok(detail);
    }

    /// <summary>
    /// Gets a pre-merged transaction list for an account over a date range.
    /// </summary>
    /// <param name="accountId">The account ID.</param>
    /// <param name="startDate">The start date of the range (YYYY-MM-DD format).</param>
    /// <param name="endDate">The end date of the range (YYYY-MM-DD format).</param>
    /// <param name="includeRecurring">Whether to include recurring transaction instances.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Transaction list DTO with all items pre-merged and summaries pre-computed.</returns>
    [HttpGet("accounts/{accountId}/transactions")]
    [ProducesResponseType<TransactionListDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAccountTransactionListAsync(
        [FromRoute] Guid accountId,
        [FromQuery] DateOnly startDate,
        [FromQuery] DateOnly endDate,
        [FromQuery] bool includeRecurring = true,
        CancellationToken cancellationToken = default)
    {
        if (startDate > endDate)
        {
            return this.BadRequest("Start date must be before or equal to end date.");
        }

        try
        {
            var result = await _transactionListService.GetAccountTransactionListAsync(
                accountId,
                startDate,
                endDate,
                includeRecurring,
                cancellationToken);
            return this.Ok(result);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return this.NotFound(ex.Message);
        }
    }

    /// <summary>
    /// Gets daily transaction totals for a month (for calendar view).
    /// </summary>
    /// <param name="year">The year.</param>
    /// <param name="month">The month (1-12).</param>
    /// <param name="accountId">Optional account filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of daily totals for days with transactions.</returns>
    /// <remarks>
    /// This endpoint is deprecated. Use GET /api/v1/calendar/grid instead for a complete calendar view.
    /// </remarks>
    [HttpGet("summary")]
    [ProducesResponseType<IReadOnlyList<DailyTotalDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Obsolete("Use GetCalendarGridAsync instead.")]
    public async Task<IActionResult> GetMonthlySummaryAsync(
        [FromQuery] int year,
        [FromQuery] int month,
        [FromQuery] Guid? accountId,
        CancellationToken cancellationToken)
    {
        if (month < 1 || month > 12)
        {
            return this.BadRequest("Month must be between 1 and 12.");
        }

        if (year < 1 || year > 9999)
        {
            return this.BadRequest("Year must be between 1 and 9999.");
        }

        var summary = await _calendarService.GetMonthlySummaryAsync(year, month, accountId, cancellationToken);
        return this.Ok(summary);
    }

    /// <summary>
    /// Gets the monthly spending heatmap data for the calendar overlay.
    /// </summary>
    /// <param name="year">The year.</param>
    /// <param name="month">The month (1-12).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Heatmap data with per-day spend and intensity.</returns>
    [HttpGet("heatmap/{year:int}/{month:int}")]
    [ProducesResponseType<HeatmapDataResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCalendarHeatmapAsync(
        [FromRoute] int year,
        [FromRoute] int month,
        CancellationToken cancellationToken)
    {
        if (!await _featureFlagService.IsEnabledAsync("Calendar:SpendingHeatmap", cancellationToken))
        {
            return this.NotFound();
        }

        if (month < 1 || month > 12)
        {
            return this.BadRequest("Month must be between 1 and 12.");
        }

        if (year < 1 || year > 9999)
        {
            return this.BadRequest("Year must be between 1 and 9999.");
        }

        var userId = _userContext.UserIdAsGuid ?? Guid.Empty;
        var heatmap = await _kakeiboCalendarService.GetMonthHeatmapAsync(year, month, userId, cancellationToken);

        var days = heatmap.ToDictionary(
            kvp => kvp.Key,
            kvp => new HeatmapDayResponse
            {
                Spend = kvp.Value.Spend,
                Intensity = kvp.Value.Intensity.ToString(),
            });

        var totalSpend = heatmap.Values.Sum(d => d.Spend);
        var daysWithSpend = heatmap.Values.Count(d => d.Spend > 0);
        var dailyAverage = daysWithSpend > 0 ? totalSpend / daysWithSpend : 0m;

        return this.Ok(new HeatmapDataResponse
        {
            DailyAverageSpend = dailyAverage,
            Days = days,
        });
    }

    private static KakeiboBreakdownDto MapBreakdown(KakeiboBreakdown breakdown) =>
        new()
        {
            Essentials = breakdown.EssentialsAmount,
            Wants = breakdown.WantsAmount,
            Culture = breakdown.CultureAmount,
            Unexpected = breakdown.UnexpectedAmount,
        };
}
