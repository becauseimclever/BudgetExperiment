// <copyright file="RecurringTransferInstanceController.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetExperiment.Api.Controllers;

/// <summary>
/// REST API controller for recurring transfer instance management operations.
/// </summary>
[ApiVersion("1.0")]
[ApiController]
[Authorize]
[Route("api/v{version:apiVersion}/recurring-transfers")]
[Produces("application/json")]
public sealed class RecurringTransferInstanceController : ControllerBase
{
    private readonly IRecurringTransferService _service;
    private readonly IRecurringTransferInstanceService _instanceService;
    private readonly IRecurringTransferRealizationService _realizationService;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecurringTransferInstanceController"/> class.
    /// </summary>
    /// <param name="service">The recurring transfer service.</param>
    /// <param name="instanceService">The recurring transfer instance service.</param>
    /// <param name="realizationService">The recurring transfer realization service.</param>
    public RecurringTransferInstanceController(
        IRecurringTransferService service,
        IRecurringTransferInstanceService instanceService,
        IRecurringTransferRealizationService realizationService)
    {
        _service = service;
        _instanceService = instanceService;
        _realizationService = realizationService;
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

        var instances = await _instanceService.GetProjectedInstancesAsync(from, to, accountId, cancellationToken);
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

        var instances = await _instanceService.GetInstancesAsync(id, from, to, cancellationToken);
        if (instances is null)
        {
            return this.NotFound();
        }

        return this.Ok(instances);
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
        var transfer = await _service.SkipNextAsync(id, cancellationToken);
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
        var transfer = await _service.PauseAsync(id, cancellationToken);
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
        var transfer = await _service.ResumeAsync(id, cancellationToken);
        if (transfer is null)
        {
            return this.NotFound();
        }

        return this.Ok(transfer);
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
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ModifyInstanceAsync(
        Guid id,
        DateOnly date,
        [FromBody] RecurringTransferInstanceModifyDto dto,
        CancellationToken cancellationToken)
    {
        string? expectedVersion = null;
        if (this.Request.Headers.TryGetValue("If-Match", out var ifMatch))
        {
            expectedVersion = ifMatch.ToString().Trim('"');
        }

        var instance = await _instanceService.ModifyInstanceAsync(id, date, dto, expectedVersion, cancellationToken);
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
        var skipped = await _instanceService.SkipInstanceAsync(id, date, cancellationToken);
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
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateFutureAsync(
        Guid id,
        DateOnly date,
        [FromBody] RecurringTransferUpdateDto dto,
        CancellationToken cancellationToken)
    {
        string? expectedVersion = null;
        if (this.Request.Headers.TryGetValue("If-Match", out var ifMatch))
        {
            expectedVersion = ifMatch.ToString().Trim('"');
        }

        var transfer = await _service.UpdateFromDateAsync(id, date, dto, expectedVersion, cancellationToken);
        if (transfer is null)
        {
            return this.NotFound();
        }

        if (transfer.Version is not null)
        {
            this.Response.Headers.ETag = $"\"{transfer.Version}\"";
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
        var transfer = await _realizationService.RealizeInstanceAsync(id, request, cancellationToken);
        return this.CreatedAtAction(
            "GetById",
            "Transfers",
            new { id = transfer.TransferId },
            transfer);
    }
}
