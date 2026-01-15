// <copyright file="AllocationsController.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Application.Services;
using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain;

using Microsoft.AspNetCore.Mvc;

namespace BudgetExperiment.Api.Controllers;

/// <summary>
/// REST API controller for paycheck allocation operations.
/// </summary>
[ApiController]
[Route("api/v1/allocations")]
[Produces("application/json")]
public sealed class AllocationsController : ControllerBase
{
    private readonly IPaycheckAllocationService _service;

    /// <summary>
    /// Initializes a new instance of the <see cref="AllocationsController"/> class.
    /// </summary>
    /// <param name="service">The paycheck allocation service.</param>
    public AllocationsController(IPaycheckAllocationService service)
    {
        this._service = service;
    }

    /// <summary>
    /// Gets paycheck allocation summary for recurring bills.
    /// </summary>
    /// <param name="frequency">The paycheck frequency (Weekly, BiWeekly, Monthly).</param>
    /// <param name="amount">Optional paycheck amount for income calculations.</param>
    /// <param name="accountId">Optional account filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The paycheck allocation summary.</returns>
    /// <response code="200">Returns the allocation summary.</response>
    /// <response code="400">If frequency is missing or invalid, or amount is invalid.</response>
    [HttpGet("paycheck")]
    [ProducesResponseType<PaycheckAllocationSummaryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetPaycheckAllocationAsync(
        [FromQuery] string? frequency,
        [FromQuery] decimal? amount = null,
        [FromQuery] Guid? accountId = null,
        CancellationToken cancellationToken = default)
    {
        // Validate frequency is provided
        if (string.IsNullOrWhiteSpace(frequency))
        {
            return this.BadRequest(new ProblemDetails
            {
                Title = "Frequency is required",
                Detail = "The 'frequency' query parameter is required. Valid values: Weekly, BiWeekly, Monthly.",
                Status = StatusCodes.Status400BadRequest,
            });
        }

        // Parse and validate frequency
        if (!Enum.TryParse<RecurrenceFrequency>(frequency, ignoreCase: true, out var paycheckFrequency))
        {
            return this.BadRequest(new ProblemDetails
            {
                Title = "Invalid frequency",
                Detail = $"'{frequency}' is not a valid paycheck frequency. Valid values: Weekly, BiWeekly, Monthly.",
                Status = StatusCodes.Status400BadRequest,
            });
        }

        // Validate paycheck frequency is sensible for paychecks (not Daily, Quarterly, Yearly)
        if (paycheckFrequency == RecurrenceFrequency.Daily ||
            paycheckFrequency == RecurrenceFrequency.Quarterly ||
            paycheckFrequency == RecurrenceFrequency.Yearly)
        {
            return this.BadRequest(new ProblemDetails
            {
                Title = "Invalid frequency",
                Detail = $"'{frequency}' is not a valid paycheck frequency. Valid values: Weekly, BiWeekly, Monthly.",
                Status = StatusCodes.Status400BadRequest,
            });
        }

        // Validate amount if provided
        if (amount.HasValue && amount.Value <= 0)
        {
            return this.BadRequest(new ProblemDetails
            {
                Title = "Invalid amount",
                Detail = "Paycheck amount must be greater than zero.",
                Status = StatusCodes.Status400BadRequest,
            });
        }

        var summary = await this._service.GetAllocationSummaryAsync(
            paycheckFrequency,
            amount,
            accountId,
            cancellationToken);

        return this.Ok(summary);
    }
}
