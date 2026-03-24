// <copyright file="StatementReconciliationController.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Application.Reconciliation;
using BudgetExperiment.Contracts.Dtos;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetExperiment.Api.Controllers;

/// <summary>REST API for statement-based reconciliation (Feature 125b).</summary>
[ApiVersion("1.0")]
[ApiController]
[Authorize]
[Route("api/v{version:apiVersion}/statement-reconciliation")]
[Produces("application/json")]
public sealed class StatementReconciliationController : ControllerBase
{
    private readonly IStatementReconciliationService _service;

    /// <summary>
    /// Initializes a new instance of the <see cref="StatementReconciliationController"/> class.
    /// </summary>
    /// <param name="service">The statement reconciliation service.</param>
    public StatementReconciliationController(IStatementReconciliationService service)
    {
        _service = service;
    }

    /// <summary>Marks a single transaction as cleared.</summary>
    /// <param name="request">The mark cleared request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated transaction DTO.</returns>
    [HttpPost("clear")]
    [ProducesResponseType<TransactionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkClearedAsync(
        [FromBody] MarkClearedRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _service.MarkClearedAsync(request.TransactionId, request.ClearedDate, cancellationToken);
        return this.Ok(result);
    }

    /// <summary>Unclears a single transaction (only allowed when not reconciled).</summary>
    /// <param name="request">The mark uncleared request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated transaction DTO.</returns>
    [HttpPost("unclear")]
    [ProducesResponseType<TransactionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkUnclearedAsync(
        [FromBody] MarkUnclearedRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _service.MarkUnclearedAsync(request.TransactionId, cancellationToken);
        return this.Ok(result);
    }

    /// <summary>Bulk-marks multiple transactions as cleared.</summary>
    /// <param name="request">The bulk mark cleared request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated transaction DTOs for all affected transactions.</returns>
    [HttpPost("bulk-clear")]
    [ProducesResponseType<IReadOnlyList<TransactionDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BulkMarkClearedAsync(
        [FromBody] BulkMarkClearedRequest request,
        CancellationToken cancellationToken)
    {
        if (request.TransactionIds.Count == 0)
        {
            return this.BadRequest("At least one transaction ID is required.");
        }

        var results = await _service.BulkMarkClearedAsync(request.TransactionIds, request.ClearedDate, cancellationToken);
        return this.Ok(results);
    }

    /// <summary>Bulk-unclears multiple transactions, skipping any locked to a reconciliation.</summary>
    /// <param name="request">The bulk mark uncleared request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated transaction DTOs for successfully uncleared transactions.</returns>
    [HttpPost("bulk-unclear")]
    [ProducesResponseType<IReadOnlyList<TransactionDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BulkMarkUnclearedAsync(
        [FromBody] BulkMarkUnclearedRequest request,
        CancellationToken cancellationToken)
    {
        if (request.TransactionIds.Count == 0)
        {
            return this.BadRequest("At least one transaction ID is required.");
        }

        var results = await _service.BulkMarkUnclearedAsync(request.TransactionIds, cancellationToken);
        return this.Ok(results);
    }

    /// <summary>Gets the active (non-completed) statement balance for an account.</summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The active statement balance DTO, or 404 if none exists.</returns>
    [HttpGet("statement-balance")]
    [ProducesResponseType<StatementBalanceDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetActiveStatementBalanceAsync(
        [FromQuery] Guid accountId,
        CancellationToken cancellationToken)
    {
        var result = await _service.GetActiveStatementBalanceAsync(accountId, cancellationToken);
        if (result is null)
        {
            return this.NotFound();
        }

        return this.Ok(result);
    }

    /// <summary>Gets the computed cleared balance for an account up to an optional date.</summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="upToDate">Optional upper bound date (inclusive).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Cleared balance DTO.</returns>
    [HttpGet("cleared-balance")]
    [ProducesResponseType<ClearedBalanceDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetClearedBalanceAsync(
        [FromQuery] Guid accountId,
        [FromQuery] DateOnly? upToDate,
        CancellationToken cancellationToken)
    {
        var result = await _service.GetClearedBalanceAsync(accountId, upToDate, cancellationToken);
        return this.Ok(result);
    }

    /// <summary>Creates or updates the active statement balance for an account.</summary>
    /// <param name="request">The set statement balance request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created or updated statement balance DTO.</returns>
    [HttpPost("statement-balance")]
    [ProducesResponseType<StatementBalanceDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetStatementBalanceAsync(
        [FromBody] SetStatementBalanceRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _service.SetStatementBalanceAsync(
            request.AccountId,
            request.StatementDate,
            request.Balance,
            cancellationToken);
        return this.Ok(result);
    }

    /// <summary>Completes reconciliation for an account.</summary>
    /// <remarks>
    /// Validates that cleared balance equals statement balance before completing.
    /// Returns 422 if there is a non-zero difference.
    /// </remarks>
    /// <param name="request">The complete reconciliation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created reconciliation record DTO with 201 status, or 422 if not balanced.</returns>
    [HttpPost("complete")]
    [ProducesResponseType<ReconciliationRecordDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CompleteReconciliationAsync(
        [FromBody] CompleteReconciliationRequest request,
        CancellationToken cancellationToken)
    {
        var statementBalance = await _service.GetActiveStatementBalanceAsync(request.AccountId, cancellationToken);
        if (statementBalance is null)
        {
            return this.UnprocessableEntity("No active statement balance found for account.");
        }

        var clearedBalance = await _service.GetClearedBalanceAsync(request.AccountId, statementBalance.StatementDate, cancellationToken);
        var difference = statementBalance.Balance - clearedBalance.ClearedBalance;

        if (difference != 0m)
        {
            return this.UnprocessableEntity($"Cannot complete reconciliation: difference is {difference:F2}. Balance must be zero.");
        }

        var record = await _service.CompleteReconciliationAsync(request.AccountId, cancellationToken);
        return this.StatusCode(StatusCodes.Status201Created, record);
    }

    /// <summary>Gets the reconciliation history for an account, paged.</summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="page">1-based page number.</param>
    /// <param name="pageSize">Number of items per page.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paged list of reconciliation record DTOs sorted by date descending.</returns>
    [HttpGet("history")]
    [ProducesResponseType<IReadOnlyList<ReconciliationRecordDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetReconciliationHistoryAsync(
        [FromQuery] Guid accountId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var history = await _service.GetReconciliationHistoryAsync(accountId, page, pageSize, cancellationToken);
        return this.Ok(history);
    }

    /// <summary>Gets all transactions locked to a specific reconciliation record.</summary>
    /// <param name="reconciliationRecordId">The reconciliation record identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Transaction DTOs locked to the reconciliation record.</returns>
    [HttpGet("records/{reconciliationRecordId:guid}/transactions")]
    [ProducesResponseType<IReadOnlyList<TransactionDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetReconciliationTransactionsAsync(
        Guid reconciliationRecordId,
        CancellationToken cancellationToken)
    {
        var transactions = await _service.GetReconciliationTransactionsAsync(reconciliationRecordId, cancellationToken);
        return this.Ok(transactions);
    }

    /// <summary>Unlocks a reconciled transaction from its reconciliation record.</summary>
    /// <param name="transactionId">The transaction identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>204 No Content.</returns>
    [HttpPost("unlock/{transactionId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnlockTransactionAsync(
        Guid transactionId,
        CancellationToken cancellationToken)
    {
        await _service.UnlockTransactionAsync(transactionId, cancellationToken);
        return this.NoContent();
    }
}
