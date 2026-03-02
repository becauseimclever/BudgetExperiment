// <copyright file="RecurringController.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetExperiment.Api.Controllers;

/// <summary>
/// REST API controller for cross-cutting recurring operations (past-due, batch operations).
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/recurring")]
[Produces("application/json")]
public sealed class RecurringController : ControllerBase
{
    private readonly IPastDueService _pastDueService;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecurringController"/> class.
    /// </summary>
    /// <param name="pastDueService">The past-due service.</param>
    public RecurringController(IPastDueService pastDueService)
    {
        this._pastDueService = pastDueService;
    }

    /// <summary>
    /// Gets a summary of all past-due recurring items.
    /// </summary>
    /// <param name="accountId">Optional account ID to filter by.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Summary of past-due recurring items.</returns>
    [HttpGet("past-due")]
    [ProducesResponseType<PastDueSummaryDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPastDueAsync(
        [FromQuery] Guid? accountId,
        CancellationToken cancellationToken)
    {
        var summary = await this._pastDueService.GetPastDueItemsAsync(accountId, cancellationToken);
        return this.Ok(summary);
    }

    /// <summary>
    /// Realizes multiple past-due items in batch.
    /// </summary>
    /// <param name="request">The batch realize request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Results of the batch operation.</returns>
    [HttpPost("realize-batch")]
    [ProducesResponseType<BatchRealizeResultDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RealizeBatchAsync(
        [FromBody] BatchRealizeRequest request,
        CancellationToken cancellationToken)
    {
        if (request?.Items == null || request.Items.Count == 0)
        {
            return this.BadRequest("At least one item is required.");
        }

        var result = await this._pastDueService.RealizeBatchAsync(request, cancellationToken);
        return this.Ok(result);
    }
}
