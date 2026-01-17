// <copyright file="RecurringTransfersController.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Application.Services;
using BudgetExperiment.Contracts.Dtos;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetExperiment.Api.Controllers;

/// <summary>
/// REST API controller for recurring transfer operations.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/recurring-transfers")]
[Produces("application/json")]
public sealed class RecurringTransfersController : ControllerBase
{
    private readonly RecurringTransferService _service;
    private readonly IRecurringTransferInstanceService _instanceService;
    private readonly IRecurringTransferRealizationService _realizationService;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecurringTransfersController"/> class.
    /// </summary>
    /// <param name="service">The recurring transfer service.</param>
    /// <param name="instanceService">The recurring transfer instance service.</param>
    /// <param name="realizationService">The recurring transfer realization service.</param>
    public RecurringTransfersController(
        RecurringTransferService service,
        IRecurringTransferInstanceService instanceService,
        IRecurringTransferRealizationService realizationService)
    {
        this._service = service;
        this._instanceService = instanceService;
        this._realizationService = realizationService;
    }

    /// <summary>
    /// Gets all recurring transfers.
    /// </summary>
    /// <param name="sourceAccountId">Optional filter by source account.</param>
    /// <param name="destinationAccountId">Optional filter by destination account.</param>
    /// <param name="accountId">Optional filter by either source or destination account.</param>
    /// <param name="isActive">Optional filter by active status.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of recurring transfers.</returns>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<RecurringTransferDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllAsync(
        [FromQuery] Guid? sourceAccountId,
        [FromQuery] Guid? destinationAccountId,
        [FromQuery] Guid? accountId,
        [FromQuery] bool? isActive,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<RecurringTransferDto> transfers;

        if (accountId.HasValue)
        {
            transfers = await this._service.GetByAccountIdAsync(accountId.Value, cancellationToken);
        }
        else if (sourceAccountId.HasValue || destinationAccountId.HasValue)
        {
            // For specific source/destination filters, get all and filter
            transfers = await this._service.GetAllAsync(cancellationToken);

            if (sourceAccountId.HasValue)
            {
                transfers = transfers.Where(t => t.SourceAccountId == sourceAccountId.Value).ToList();
            }

            if (destinationAccountId.HasValue)
            {
                transfers = transfers.Where(t => t.DestinationAccountId == destinationAccountId.Value).ToList();
            }
        }
        else if (isActive == true)
        {
            transfers = await this._service.GetActiveAsync(cancellationToken);
        }
        else
        {
            transfers = await this._service.GetAllAsync(cancellationToken);
        }

        // Apply active filter if combined with other filters
        if (isActive.HasValue && !accountId.HasValue && (sourceAccountId.HasValue || destinationAccountId.HasValue))
        {
            transfers = transfers.Where(t => t.IsActive == isActive.Value).ToList();
        }

        return this.Ok(transfers);
    }

    /// <summary>
    /// Gets a specific recurring transfer by ID.
    /// </summary>
    /// <param name="id">The recurring transfer identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The recurring transfer if found.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<RecurringTransferDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var transfer = await this._service.GetByIdAsync(id, cancellationToken);
        if (transfer is null)
        {
            return this.NotFound();
        }

        return this.Ok(transfer);
    }

    /// <summary>
    /// Creates a new recurring transfer.
    /// </summary>
    /// <param name="dto">The recurring transfer creation data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created recurring transfer.</returns>
    [HttpPost]
    [ProducesResponseType<RecurringTransferDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateAsync([FromBody] RecurringTransferCreateDto dto, CancellationToken cancellationToken)
    {
        var transfer = await this._service.CreateAsync(dto, cancellationToken);
        return this.CreatedAtAction("GetById", new { id = transfer.Id }, transfer);
    }

    /// <summary>
    /// Updates a recurring transfer.
    /// </summary>
    /// <param name="id">The recurring transfer identifier.</param>
    /// <param name="dto">The update data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated recurring transfer.</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType<RecurringTransferDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateAsync(Guid id, [FromBody] RecurringTransferUpdateDto dto, CancellationToken cancellationToken)
    {
        var transfer = await this._service.UpdateAsync(id, dto, cancellationToken);
        if (transfer is null)
        {
            return this.NotFound();
        }

        return this.Ok(transfer);
    }

    /// <summary>
    /// Deletes a recurring transfer.
    /// </summary>
    /// <param name="id">The recurring transfer identifier.</param>
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
    /// Skips the next occurrence of a recurring transfer.
    /// </summary>
    /// <param name="id">The recurring transfer identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated recurring transfer.</returns>
    [HttpPost("{id:guid}/skip")]
    [ProducesResponseType<RecurringTransferDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SkipNextAsync(Guid id, CancellationToken cancellationToken)
    {
        var transfer = await this._service.SkipNextAsync(id, cancellationToken);
        if (transfer is null)
        {
            return this.NotFound();
        }

        return this.Ok(transfer);
    }

    /// <summary>
    /// Pauses a recurring transfer.
    /// </summary>
    /// <param name="id">The recurring transfer identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated recurring transfer.</returns>
    [HttpPost("{id:guid}/pause")]
    [ProducesResponseType<RecurringTransferDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PauseAsync(Guid id, CancellationToken cancellationToken)
    {
        var transfer = await this._service.PauseAsync(id, cancellationToken);
        if (transfer is null)
        {
            return this.NotFound();
        }

        return this.Ok(transfer);
    }

    /// <summary>
    /// Resumes a paused recurring transfer.
    /// </summary>
    /// <param name="id">The recurring transfer identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated recurring transfer.</returns>
    [HttpPost("{id:guid}/resume")]
    [ProducesResponseType<RecurringTransferDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResumeAsync(Guid id, CancellationToken cancellationToken)
    {
        var transfer = await this._service.ResumeAsync(id, cancellationToken);
        if (transfer is null)
        {
            return this.NotFound();
        }

        return this.Ok(transfer);
    }

    /// <summary>
    /// Gets projected recurring transfers for a date range.
    /// </summary>
    /// <param name="from">Start date (inclusive).</param>
    /// <param name="to">End date (inclusive).</param>
    /// <param name="accountId">Optional account filter (matches source or destination).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of projected instances.</returns>
    [HttpGet("projected")]
    [ProducesResponseType<IReadOnlyList<RecurringTransferInstanceDto>>(StatusCodes.Status200OK)]
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

        var instances = await this._instanceService.GetProjectedInstancesAsync(from, to, accountId, cancellationToken);
        return this.Ok(instances);
    }

    /// <summary>
    /// Gets instances of a recurring transfer within a date range.
    /// </summary>
    /// <param name="id">The recurring transfer identifier.</param>
    /// <param name="from">Start date (inclusive).</param>
    /// <param name="to">End date (inclusive).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of instances.</returns>
    [HttpGet("{id:guid}/instances")]
    [ProducesResponseType<IReadOnlyList<RecurringTransferInstanceDto>>(StatusCodes.Status200OK)]
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

        var instances = await this._instanceService.GetInstancesAsync(id, from, to, cancellationToken);
        if (instances is null)
        {
            return this.NotFound();
        }

        return this.Ok(instances);
    }

    /// <summary>
    /// Modifies a single instance of a recurring transfer.
    /// </summary>
    /// <param name="id">The recurring transfer identifier.</param>
    /// <param name="date">The scheduled date of the instance.</param>
    /// <param name="dto">The modification data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The modified instance.</returns>
    [HttpPut("{id:guid}/instances/{date}")]
    [ProducesResponseType<RecurringTransferInstanceDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ModifyInstanceAsync(
        Guid id,
        DateOnly date,
        [FromBody] RecurringTransferInstanceModifyDto dto,
        CancellationToken cancellationToken)
    {
        var instance = await this._instanceService.ModifyInstanceAsync(id, date, dto, cancellationToken);
        if (instance is null)
        {
            return this.NotFound();
        }

        return this.Ok(instance);
    }

    /// <summary>
    /// Skips/deletes a single instance of a recurring transfer.
    /// </summary>
    /// <param name="id">The recurring transfer identifier.</param>
    /// <param name="date">The scheduled date of the instance.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content if successful.</returns>
    [HttpDelete("{id:guid}/instances/{date}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SkipInstanceAsync(Guid id, DateOnly date, CancellationToken cancellationToken)
    {
        var skipped = await this._instanceService.SkipInstanceAsync(id, date, cancellationToken);
        if (!skipped)
        {
            return this.NotFound();
        }

        return this.NoContent();
    }

    /// <summary>
    /// Updates this instance and all future instances (modifies the series).
    /// </summary>
    /// <param name="id">The recurring transfer identifier.</param>
    /// <param name="date">The date from which to apply changes.</param>
    /// <param name="dto">The update data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated recurring transfer.</returns>
    [HttpPut("{id:guid}/instances/{date}/future")]
    [ProducesResponseType<RecurringTransferDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateFutureAsync(
        Guid id,
        DateOnly date,
        [FromBody] RecurringTransferUpdateDto dto,
        CancellationToken cancellationToken)
    {
        var transfer = await this._service.UpdateFromDateAsync(id, date, dto, cancellationToken);
        if (transfer is null)
        {
            return this.NotFound();
        }

        return this.Ok(transfer);
    }

    /// <summary>
    /// Realizes a recurring transfer instance, creating actual transfer transactions.
    /// </summary>
    /// <param name="id">The recurring transfer identifier.</param>
    /// <param name="request">The realization request with the instance date and optional overrides.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created transfer.</returns>
    [HttpPost("{id:guid}/realize")]
    [ProducesResponseType<TransferResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RealizeAsync(
        Guid id,
        [FromBody] RealizeRecurringTransferRequest request,
        CancellationToken cancellationToken)
    {
        var transfer = await this._realizationService.RealizeInstanceAsync(id, request, cancellationToken);
        return this.CreatedAtAction(
            "GetById",
            "Transfers",
            new { id = transfer.TransferId },
            transfer);
    }
}
