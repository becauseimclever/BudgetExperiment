// <copyright file="CalendarController.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Application.Services;

using Microsoft.AspNetCore.Mvc;

namespace BudgetExperiment.Api.Controllers;

/// <summary>
/// REST API controller for calendar-related operations.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public sealed class CalendarController : ControllerBase
{
    private readonly CalendarService _calendarService;
    private readonly ICalendarGridService _gridService;

    /// <summary>
    /// Initializes a new instance of the <see cref="CalendarController"/> class.
    /// </summary>
    /// <param name="calendarService">The calendar service.</param>
    /// <param name="gridService">The calendar grid service.</param>
    public CalendarController(CalendarService calendarService, ICalendarGridService gridService)
    {
        this._calendarService = calendarService;
        this._gridService = gridService;
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

        var grid = await this._gridService.GetCalendarGridAsync(year, month, accountId, cancellationToken);
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
        var detail = await this._gridService.GetDayDetailAsync(date, accountId, cancellationToken);
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
            var result = await this._gridService.GetAccountTransactionListAsync(
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

        var summary = await this._calendarService.GetMonthlySummaryAsync(year, month, accountId, cancellationToken);
        return this.Ok(summary);
    }
}
