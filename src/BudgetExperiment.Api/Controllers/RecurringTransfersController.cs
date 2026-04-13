// <copyright file="RecurringTransfersController.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetExperiment.Api.Controllers;

/// <summary>
/// REST API controller for recurring transfer operations.
/// </summary>
[ApiVersion("1.0")]
[ApiController]
[Authorize]
[Route("api/v{version:apiVersion}/recurring-transfers")]
[Produces("application/json")]
public sealed class RecurringTransfersController : ControllerBase
{
    private readonly IRecurringTransferService _service;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecurringTransfersController"/> class.
    /// </summary>
    /// <param name="service">The recurring transfer service.</param>
    public RecurringTransfersController(IRecurringTransferService service)
    {
        _service = service;
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
            transfers = await _service.GetByAccountIdAsync(accountId.Value, cancellationToken);
        }
        else if (sourceAccountId.HasValue || destinationAccountId.HasValue)
        {
            // For specific source/destination filters, get all and filter
            transfers = await _service.GetAllAsync(cancellationToken);

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
            transfers = await _service.GetActiveAsync(cancellationToken);
        }
        else
        {
            transfers = await _service.GetAllAsync(cancellationToken);
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
        var transfer = await _service.GetByIdAsync(id, cancellationToken);
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
        var transfer = await _service.CreateAsync(dto, cancellationToken);
        return this.CreatedAtAction(
            "GetById",
            new
            {
                id = transfer.Id,
            },
            transfer);
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
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateAsync(Guid id, [FromBody] RecurringTransferUpdateDto dto, CancellationToken cancellationToken)
    {
        string? expectedVersion = null;
        if (this.Request.Headers.TryGetValue("If-Match", out var ifMatch))
        {
            expectedVersion = ifMatch.ToString().Trim('"');
        }

        var transfer = await _service.UpdateAsync(id, dto, expectedVersion, cancellationToken);
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
        var deleted = await _service.DeleteAsync(id, cancellationToken);
        if (!deleted)
        {
            return this.NotFound();
        }

        return this.NoContent();
    }
}
