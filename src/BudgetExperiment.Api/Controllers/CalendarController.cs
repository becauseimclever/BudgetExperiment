// <copyright file="CalendarController.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Application.Dtos;
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
    private readonly CalendarService _service;

    /// <summary>
    /// Initializes a new instance of the <see cref="CalendarController"/> class.
    /// </summary>
    /// <param name="service">The calendar service.</param>
    public CalendarController(CalendarService service)
    {
        this._service = service;
    }

    /// <summary>
    /// Gets daily transaction totals for a month (for calendar view).
    /// </summary>
    /// <param name="year">The year.</param>
    /// <param name="month">The month (1-12).</param>
    /// <param name="accountId">Optional account filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of daily totals for days with transactions.</returns>
    [HttpGet("summary")]
    [ProducesResponseType<IReadOnlyList<DailyTotalDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
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

        var summary = await this._service.GetMonthlySummaryAsync(year, month, accountId, cancellationToken);
        return this.Ok(summary);
    }
}
