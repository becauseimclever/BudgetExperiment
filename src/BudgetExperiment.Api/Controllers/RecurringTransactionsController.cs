// <copyright file="RecurringTransactionsController.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Application.Services;

using Microsoft.AspNetCore.Mvc;

namespace BudgetExperiment.Api.Controllers;

/// <summary>
/// REST API controller for recurring transaction operations.
/// </summary>
[ApiController]
[Route("api/v1/recurring-transactions")]
[Produces("application/json")]
public sealed class RecurringTransactionsController : ControllerBase
{
    private readonly RecurringTransactionService _service;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecurringTransactionsController"/> class.
    /// </summary>
    /// <param name="service">The recurring transaction service.</param>
    public RecurringTransactionsController(RecurringTransactionService service)
    {
        this._service = service;
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
        var recurring = await this._service.GetAllAsync(cancellationToken);
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
        var recurring = await this._service.GetByIdAsync(id, cancellationToken);
        if (recurring is null)
        {
            return this.NotFound();
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
        var recurring = await this._service.CreateAsync(dto, cancellationToken);
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
    public async Task<IActionResult> UpdateAsync(Guid id, [FromBody] RecurringTransactionUpdateDto dto, CancellationToken cancellationToken)
    {
        var recurring = await this._service.UpdateAsync(id, dto, cancellationToken);
        if (recurring is null)
        {
            return this.NotFound();
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
        var deleted = await this._service.DeleteAsync(id, cancellationToken);
        if (!deleted)
        {
            return this.NotFound();
        }

        return this.NoContent();
    }

    /// <summary>
    /// Skips the next occurrence of a recurring transaction.
    /// </summary>
    /// <param name="id">The recurring transaction identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated recurring transaction.</returns>
    [HttpPost("{id:guid}/skip")]
    [ProducesResponseType<RecurringTransactionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SkipNextAsync(Guid id, CancellationToken cancellationToken)
    {
        var recurring = await this._service.SkipNextAsync(id, cancellationToken);
        if (recurring is null)
        {
            return this.NotFound();
        }

        return this.Ok(recurring);
    }

    /// <summary>
    /// Pauses a recurring transaction.
    /// </summary>
    /// <param name="id">The recurring transaction identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated recurring transaction.</returns>
    [HttpPost("{id:guid}/pause")]
    [ProducesResponseType<RecurringTransactionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PauseAsync(Guid id, CancellationToken cancellationToken)
    {
        var recurring = await this._service.PauseAsync(id, cancellationToken);
        if (recurring is null)
        {
            return this.NotFound();
        }

        return this.Ok(recurring);
    }

    /// <summary>
    /// Resumes a paused recurring transaction.
    /// </summary>
    /// <param name="id">The recurring transaction identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated recurring transaction.</returns>
    [HttpPost("{id:guid}/resume")]
    [ProducesResponseType<RecurringTransactionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResumeAsync(Guid id, CancellationToken cancellationToken)
    {
        var recurring = await this._service.ResumeAsync(id, cancellationToken);
        if (recurring is null)
        {
            return this.NotFound();
        }

        return this.Ok(recurring);
    }

    /// <summary>
    /// Gets projected recurring transactions for a date range.
    /// </summary>
    /// <param name="from">Start date (inclusive).</param>
    /// <param name="to">End date (inclusive).</param>
    /// <param name="accountId">Optional account filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of projected instances.</returns>
    [HttpGet("projected")]
    [ProducesResponseType<IReadOnlyList<RecurringInstanceDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetProjectedAsync(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        [FromQuery] Guid? accountId,
        CancellationToken cancellationToken)
    {
        if (from > to)
        {
            return this.BadRequest("from must be less than or equal to to.");
        }

        var instances = await this._service.GetProjectedInstancesAsync(from, to, accountId, cancellationToken);
        return this.Ok(instances);
    }

    /// <summary>
    /// Gets instances of a recurring transaction within a date range.
    /// </summary>
    /// <param name="id">The recurring transaction identifier.</param>
    /// <param name="from">Start date (inclusive).</param>
    /// <param name="to">End date (inclusive).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of instances.</returns>
    [HttpGet("{id:guid}/instances")]
    [ProducesResponseType<IReadOnlyList<RecurringInstanceDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetInstancesAsync(
        Guid id,
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        CancellationToken cancellationToken)
    {
        if (from > to)
        {
            return this.BadRequest("from must be less than or equal to to.");
        }

        var instances = await this._service.GetInstancesAsync(id, from, to, cancellationToken);
        if (instances is null)
        {
            return this.NotFound();
        }

        return this.Ok(instances);
    }

    /// <summary>
    /// Modifies a single instance of a recurring transaction.
    /// </summary>
    /// <param name="id">The recurring transaction identifier.</param>
    /// <param name="date">The scheduled date of the instance.</param>
    /// <param name="dto">The modification data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The modified instance.</returns>
    [HttpPut("{id:guid}/instances/{date}")]
    [ProducesResponseType<RecurringInstanceDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ModifyInstanceAsync(
        Guid id,
        DateOnly date,
        [FromBody] RecurringInstanceModifyDto dto,
        CancellationToken cancellationToken)
    {
        var instance = await this._service.ModifyInstanceAsync(id, date, dto, cancellationToken);
        if (instance is null)
        {
            return this.NotFound();
        }

        return this.Ok(instance);
    }

    /// <summary>
    /// Skips/deletes a single instance of a recurring transaction.
    /// </summary>
    /// <param name="id">The recurring transaction identifier.</param>
    /// <param name="date">The scheduled date of the instance.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content if successful.</returns>
    [HttpDelete("{id:guid}/instances/{date}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SkipInstanceAsync(Guid id, DateOnly date, CancellationToken cancellationToken)
    {
        var skipped = await this._service.SkipInstanceAsync(id, date, cancellationToken);
        if (!skipped)
        {
            return this.NotFound();
        }

        return this.NoContent();
    }

    /// <summary>
    /// Updates this instance and all future instances (modifies the series).
    /// </summary>
    /// <param name="id">The recurring transaction identifier.</param>
    /// <param name="date">The date from which to apply changes.</param>
    /// <param name="dto">The update data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated recurring transaction.</returns>
    [HttpPut("{id:guid}/instances/{date}/future")]
    [ProducesResponseType<RecurringTransactionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateFutureAsync(
        Guid id,
        DateOnly date,
        [FromBody] RecurringTransactionUpdateDto dto,
        CancellationToken cancellationToken)
    {
        var recurring = await this._service.UpdateFromDateAsync(id, date, dto, cancellationToken);
        if (recurring is null)
        {
            return this.NotFound();
        }

        return this.Ok(recurring);
    }
}
