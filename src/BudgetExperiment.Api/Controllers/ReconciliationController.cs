// <copyright file="ReconciliationController.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetExperiment.Api.Controllers;

/// <summary>
/// REST API controller for recurring transaction reconciliation operations.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/reconciliation")]
[Produces("application/json")]
public sealed class ReconciliationController : ControllerBase
{
    private readonly IReconciliationService _reconciliationService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReconciliationController"/> class.
    /// </summary>
    /// <param name="reconciliationService">The reconciliation service.</param>
    public ReconciliationController(IReconciliationService reconciliationService)
    {
        this._reconciliationService = reconciliationService;
    }

    /// <summary>
    /// Gets the reconciliation status for a specific month.
    /// </summary>
    /// <param name="year">The year.</param>
    /// <param name="month">The month (1-12).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The reconciliation status for the period.</returns>
    [HttpGet("status")]
    [ProducesResponseType<ReconciliationStatusDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetStatusAsync(
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

        var status = await this._reconciliationService.GetReconciliationStatusAsync(year, month, cancellationToken);
        return this.Ok(status);
    }

    /// <summary>
    /// Gets pending reconciliation matches awaiting user review.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of pending matches.</returns>
    [HttpGet("pending")]
    [ProducesResponseType<IReadOnlyList<ReconciliationMatchDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPendingMatchesAsync(CancellationToken cancellationToken)
    {
        var matches = await this._reconciliationService.GetPendingMatchesAsync(cancellationToken);
        return this.Ok(matches);
    }

    /// <summary>
    /// Finds potential matches for specified transactions.
    /// </summary>
    /// <param name="request">The find matches request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The matches found grouped by transaction.</returns>
    [HttpPost("find-matches")]
    [ProducesResponseType<FindMatchesResult>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> FindMatchesAsync(
        [FromBody] FindMatchesRequest request,
        CancellationToken cancellationToken)
    {
        if (request.TransactionIds.Count == 0)
        {
            return this.BadRequest("At least one transaction ID is required.");
        }

        if (request.StartDate > request.EndDate)
        {
            return this.BadRequest("Start date must be before or equal to end date.");
        }

        var result = await this._reconciliationService.FindMatchesAsync(request, cancellationToken);
        return this.Ok(result);
    }

    /// <summary>
    /// Manually matches a transaction to a recurring transaction instance.
    /// </summary>
    /// <param name="request">The manual match request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created match.</returns>
    [HttpPost("match")]
    [ProducesResponseType<ReconciliationMatchDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ManualMatchAsync(
        [FromBody] ManualMatchRequest request,
        CancellationToken cancellationToken)
    {
        var match = await this._reconciliationService.CreateManualMatchAsync(request, cancellationToken);
        if (match is null)
        {
            return this.NotFound("Transaction or recurring transaction not found.");
        }

        return this.CreatedAtAction(nameof(GetPendingMatchesAsync), match);
    }

    /// <summary>
    /// Accepts a suggested match, linking the transaction to the recurring instance.
    /// </summary>
    /// <param name="matchId">The match identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The accepted match.</returns>
    [HttpPost("accept/{matchId:guid}")]
    [ProducesResponseType<ReconciliationMatchDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AcceptMatchAsync(Guid matchId, CancellationToken cancellationToken)
    {
        var match = await this._reconciliationService.AcceptMatchAsync(matchId, cancellationToken);
        if (match is null)
        {
            return this.NotFound();
        }

        return this.Ok(match);
    }

    /// <summary>
    /// Rejects a suggested match.
    /// </summary>
    /// <param name="matchId">The match identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The rejected match.</returns>
    [HttpPost("reject/{matchId:guid}")]
    [ProducesResponseType<ReconciliationMatchDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RejectMatchAsync(Guid matchId, CancellationToken cancellationToken)
    {
        var match = await this._reconciliationService.RejectMatchAsync(matchId, cancellationToken);
        if (match is null)
        {
            return this.NotFound();
        }

        return this.Ok(match);
    }

    /// <summary>
    /// Unlinks a matched transaction, returning it and the recurring instance to unmatched state.
    /// </summary>
    /// <param name="matchId">The match identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The unlinked match.</returns>
    [HttpDelete("matches/{matchId:guid}")]
    [ProducesResponseType<ReconciliationMatchDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UnlinkMatchAsync(Guid matchId, CancellationToken cancellationToken)
    {
        try
        {
            var match = await this._reconciliationService.UnlinkMatchAsync(matchId, cancellationToken);
            if (match is null)
            {
                return this.NotFound();
            }

            return this.Ok(match);
        }
        catch (DomainException ex)
        {
            return this.BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Bulk accepts multiple matches.
    /// </summary>
    /// <param name="request">The bulk accept request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of accepted matches.</returns>
    [HttpPost("bulk-accept")]
    [ProducesResponseType<IReadOnlyList<ReconciliationMatchDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BulkAcceptMatchesAsync(
        [FromBody] BulkMatchActionRequest request,
        CancellationToken cancellationToken)
    {
        if (request.MatchIds.Count == 0)
        {
            return this.BadRequest("At least one match ID is required.");
        }

        var accepted = await this._reconciliationService.BulkAcceptMatchesAsync(request, cancellationToken);
        return this.Ok(accepted);
    }

    /// <summary>
    /// Gets matches for a specific recurring transaction.
    /// </summary>
    /// <param name="recurringTransactionId">The recurring transaction identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of matches for the recurring transaction.</returns>
    [HttpGet("recurring/{recurringTransactionId:guid}")]
    [ProducesResponseType<IReadOnlyList<ReconciliationMatchDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMatchesForRecurringTransactionAsync(
        Guid recurringTransactionId,
        CancellationToken cancellationToken)
    {
        var matches = await this._reconciliationService.GetMatchesForRecurringTransactionAsync(
            recurringTransactionId,
            cancellationToken);
        return this.Ok(matches);
    }

    /// <summary>
    /// Gets recurring instances that can be linked to a specific transaction.
    /// Returns instances within ±30 days of the transaction date.
    /// </summary>
    /// <param name="transactionId">The transaction identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of linkable recurring instances.</returns>
    [HttpGet("linkable-instances")]
    [ProducesResponseType<IReadOnlyList<LinkableInstanceDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLinkableInstancesAsync(
        [FromQuery] Guid transactionId,
        CancellationToken cancellationToken)
    {
        var instances = await this._reconciliationService.GetLinkableInstancesAsync(transactionId, cancellationToken);
        return this.Ok(instances);
    }
}
