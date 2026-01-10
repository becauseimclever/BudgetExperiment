// <copyright file="TransactionsController.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Application.Dtos;
using BudgetExperiment.Application.Services;

using Microsoft.AspNetCore.Mvc;

namespace BudgetExperiment.Api.Controllers;

/// <summary>
/// REST API controller for transaction operations.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public sealed class TransactionsController : ControllerBase
{
    private readonly TransactionService _service;

    /// <summary>
    /// Initializes a new instance of the <see cref="TransactionsController"/> class.
    /// </summary>
    /// <param name="service">The transaction service.</param>
    public TransactionsController(TransactionService service)
    {
        this._service = service;
    }

    /// <summary>
    /// Queries transactions by date range with optional account filter.
    /// </summary>
    /// <param name="startDate">The start date (inclusive).</param>
    /// <param name="endDate">The end date (inclusive).</param>
    /// <param name="accountId">Optional account filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of transactions.</returns>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<TransactionDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetByDateRangeAsync(
        [FromQuery] DateOnly startDate,
        [FromQuery] DateOnly endDate,
        [FromQuery] Guid? accountId,
        CancellationToken cancellationToken)
    {
        if (startDate > endDate)
        {
            return this.BadRequest("startDate must be less than or equal to endDate.");
        }

        var transactions = await this._service.GetByDateRangeAsync(startDate, endDate, accountId, cancellationToken);
        return this.Ok(transactions);
    }

    /// <summary>
    /// Gets a specific transaction by ID.
    /// </summary>
    /// <param name="id">The transaction identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The transaction if found.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<TransactionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var transaction = await this._service.GetByIdAsync(id, cancellationToken);
        if (transaction is null)
        {
            return this.NotFound();
        }

        return this.Ok(transaction);
    }

    /// <summary>
    /// Creates a new transaction.
    /// </summary>
    /// <param name="dto">The transaction creation data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created transaction.</returns>
    [HttpPost]
    [ProducesResponseType<TransactionDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateAsync([FromBody] TransactionCreateDto dto, CancellationToken cancellationToken)
    {
        var transaction = await this._service.CreateAsync(dto, cancellationToken);
        return this.CreatedAtAction(nameof(this.GetByIdAsync), new { id = transaction.Id }, transaction);
    }
}
