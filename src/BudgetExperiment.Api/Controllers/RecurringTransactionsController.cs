// <copyright file="RecurringTransactionsController.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetExperiment.Api.Controllers;

/// <summary>
/// REST API controller for recurring transaction operations.
/// </summary>
[ApiVersion("1.0")]
[ApiController]
[Authorize]
[Route("api/v{version:apiVersion}/recurring-transactions")]
[Produces("application/json")]
public sealed class RecurringTransactionsController : ControllerBase
{
    private readonly IRecurringTransactionService _service;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecurringTransactionsController"/> class.
    /// </summary>
    /// <param name="service">The recurring transaction service.</param>
    public RecurringTransactionsController(IRecurringTransactionService service)
    {
        _service = service;
    }

    /// <summary>
    /// Gets all recurring transactions.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of recurring transactions.</returns>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<RecurringTransactionDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllAsync(CancellationToken cancellationToken)
    {
        var recurring = await _service.GetAllAsync(cancellationToken);
        return this.Ok(recurring);
    }

    /// <summary>
    /// Gets a specific recurring transaction by ID.
    /// </summary>
    /// <param name="id">The recurring transaction identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The recurring transaction if found.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<RecurringTransactionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var recurring = await _service.GetByIdAsync(id, cancellationToken);
        if (recurring is null)
        {
            return this.NotFound();
        }

        if (recurring.Version is not null)
        {
            this.Response.Headers.ETag = $"\"{recurring.Version}\"";
        }

        return this.Ok(recurring);
    }

    /// <summary>
    /// Creates a new recurring transaction.
    /// </summary>
    /// <param name="dto">The recurring transaction creation data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created recurring transaction.</returns>
    [HttpPost]
    [ProducesResponseType<RecurringTransactionDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateAsync([FromBody] RecurringTransactionCreateDto dto, CancellationToken cancellationToken)
    {
        var recurring = await _service.CreateAsync(dto, cancellationToken);
        return this.CreatedAtAction("GetById", new { id = recurring.Id }, recurring);
    }

    /// <summary>
    /// Updates a recurring transaction.
    /// </summary>
    /// <param name="id">The recurring transaction identifier.</param>
    /// <param name="dto">The update data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated recurring transaction.</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType<RecurringTransactionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateAsync(Guid id, [FromBody] RecurringTransactionUpdateDto dto, CancellationToken cancellationToken)
    {
        string? expectedVersion = null;
        if (this.Request.Headers.TryGetValue("If-Match", out var ifMatch))
        {
            expectedVersion = ifMatch.ToString().Trim('"');
        }

        var recurring = await _service.UpdateAsync(id, dto, expectedVersion, cancellationToken);
        if (recurring is null)
        {
            return this.NotFound();
        }

        if (recurring.Version is not null)
        {
            this.Response.Headers.ETag = $"\"{recurring.Version}\"";
        }

        return this.Ok(recurring);
    }

    /// <summary>
    /// Deletes a recurring transaction.
    /// </summary>
    /// <param name="id">The recurring transaction identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content if successful.</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _service.DeleteAsync(id, cancellationToken);
        if (!deleted)
        {
            return this.NotFound();
        }

        return this.NoContent();
    }
}
