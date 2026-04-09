// <copyright file="TransfersController.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetExperiment.Api.Controllers;

/// <summary>
/// REST API controller for account transfer operations.
/// </summary>
[ApiVersion("1.0")]
[ApiController]
[Authorize]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
public sealed class TransfersController : ControllerBase
{
    private const string AtomicDeletionFlag = "feature-transfer-atomic-deletion";

    private readonly ITransferService _service;
    private readonly IFeatureFlagService _featureFlagService;

    /// <summary>
    /// Initializes a new instance of the <see cref="TransfersController"/> class.
    /// </summary>
    /// <param name="service">The transfer service.</param>
    /// <param name="featureFlagService">The feature flag service.</param>
    public TransfersController(ITransferService service, IFeatureFlagService featureFlagService)
    {
        _service = service;
        _featureFlagService = featureFlagService;
    }

    /// <summary>
    /// Creates a new transfer between accounts.
    /// </summary>
    /// <param name="request">The transfer creation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created transfer details.</returns>
    /// <response code="201">Transfer created successfully.</response>
    /// <response code="400">Invalid request data.</response>
    [HttpPost]
    [ProducesResponseType<TransferResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateAsync(
        [FromBody] CreateTransferRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _service.CreateAsync(request, cancellationToken);
        var locationUri = $"/api/v1/transfers/{result.TransferId}";
        return this.Created(locationUri, result);
    }

    /// <summary>
    /// Gets a specific transfer by its identifier.
    /// </summary>
    /// <param name="transferId">The transfer identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The transfer details if found.</returns>
    /// <response code="200">Transfer found.</response>
    /// <response code="404">Transfer not found.</response>
    [HttpGet("{transferId:guid}")]
    [ProducesResponseType<TransferResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByIdAsync(Guid transferId, CancellationToken cancellationToken)
    {
        var result = await _service.GetByIdAsync(transferId, cancellationToken);
        if (result is null)
        {
            return this.NotFound();
        }

        return this.Ok(result);
    }

    /// <summary>
    /// Lists transfers with optional filtering and pagination.
    /// </summary>
    /// <param name="accountId">Optional filter by account (transfers involving this account).</param>
    /// <param name="from">Optional start date filter (inclusive).</param>
    /// <param name="to">Optional end date filter (inclusive).</param>
    /// <param name="page">Page number (1-based, default 1).</param>
    /// <param name="pageSize">Page size (default 20, max 100).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paged list of transfer summaries.</returns>
    /// <response code="200">Returns the paged list of transfers.</response>
    [HttpGet]
    [ProducesResponseType<TransferListPageResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListAsync(
        [FromQuery] Guid? accountId,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (page < 1)
        {
            page = 1;
        }

        if (pageSize < 1)
        {
            pageSize = 20;
        }
        else if (pageSize > 100)
        {
            pageSize = 100;
        }

        var result = await _service.ListAsync(accountId, from, to, page, pageSize, cancellationToken);
        this.Response.Headers["X-Pagination-TotalCount"] = result.TotalCount.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return this.Ok(result);
    }

    /// <summary>
    /// Updates an existing transfer.
    /// </summary>
    /// <param name="transferId">The transfer identifier.</param>
    /// <param name="request">The update request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated transfer details.</returns>
    /// <response code="200">Transfer updated successfully.</response>
    /// <response code="400">Invalid request data.</response>
    /// <response code="404">Transfer not found.</response>
    [HttpPut("{transferId:guid}")]
    [ProducesResponseType<TransferResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateAsync(
        Guid transferId,
        [FromBody] UpdateTransferRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _service.UpdateAsync(transferId, request, cancellationToken);
        if (result is null)
        {
            return this.NotFound();
        }

        return this.Ok(result);
    }

    /// <summary>
    /// Deletes a transfer and both associated transactions atomically.
    /// Requires feature flag <c>feature-transfer-atomic-deletion</c> to be enabled.
    /// </summary>
    /// <param name="transferId">The transfer identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content if successful.</returns>
    /// <response code="204">Transfer deleted successfully.</response>
    /// <response code="403">Feature is not enabled.</response>
    /// <response code="404">Transfer not found.</response>
    [HttpDelete("{transferId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(Guid transferId, CancellationToken cancellationToken)
    {
        if (!await _featureFlagService.IsEnabledAsync(AtomicDeletionFlag, cancellationToken))
        {
            return this.Forbid();
        }

        var deleted = await _service.DeleteTransferAsync(transferId, cancellationToken);
        if (!deleted)
        {
            return this.NotFound();
        }

        return this.NoContent();
    }
}
