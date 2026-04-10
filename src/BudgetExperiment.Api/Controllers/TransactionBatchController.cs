// <copyright file="TransactionBatchController.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Application.Transactions;
using BudgetExperiment.Contracts.Dtos;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetExperiment.Api.Controllers;

/// <summary>
/// REST API controller for transaction mutation and batch operations.
/// </summary>
[ApiVersion("1.0")]
[ApiController]
[Authorize]
[Route("api/v{version:apiVersion}/transactions")]
[Produces("application/json")]
public sealed class TransactionBatchController : ControllerBase
{
    private readonly ITransactionService _service;
    private readonly IUncategorizedTransactionService _uncategorizedService;
    private readonly ICategorizationEngine _categorizationEngine;

    /// <summary>
    /// Initializes a new instance of the <see cref="TransactionBatchController"/> class.
    /// </summary>
    /// <param name="service">The transaction service.</param>
    /// <param name="uncategorizedService">The uncategorized transaction service.</param>
    /// <param name="categorizationEngine">The categorization engine for rule-based suggestions.</param>
    public TransactionBatchController(
        ITransactionService service,
        IUncategorizedTransactionService uncategorizedService,
        ICategorizationEngine categorizationEngine)
    {
        _service = service;
        _uncategorizedService = uncategorizedService;
        _categorizationEngine = categorizationEngine;
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
        var transaction = await _service.CreateAsync(dto, cancellationToken);
        return this.CreatedAtAction("GetById", "TransactionQuery", new { id = transaction.Id }, transaction);
    }

    /// <summary>
    /// Updates an existing transaction.
    /// </summary>
    /// <param name="id">The transaction identifier.</param>
    /// <param name="dto">The transaction update data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated transaction.</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType<TransactionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateAsync(Guid id, [FromBody] TransactionUpdateDto dto, CancellationToken cancellationToken)
    {
        string? expectedVersion = null;
        if (this.Request.Headers.TryGetValue("If-Match", out var ifMatch))
        {
            expectedVersion = ifMatch.ToString().Trim('"');
        }

        var transaction = await _service.UpdateAsync(id, dto, expectedVersion, cancellationToken);
        if (transaction is null)
        {
            return this.NotFound();
        }

        if (transaction.Version is not null)
        {
            this.Response.Headers.ETag = $"\"{transaction.Version}\"";
        }

        return this.Ok(transaction);
    }

    /// <summary>
    /// Deletes a transaction by ID.
    /// </summary>
    /// <param name="id">The transaction identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>204 No Content on success; 404 if not found.</returns>
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

    /// <summary>
    /// Returns rule-based category suggestions for a batch of transactions.
    /// Only returns suggestions for uncategorized transactions that have a matching rule.
    /// </summary>
    /// <param name="request">The request containing transaction IDs.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A response mapping transaction IDs to suggested categories.</returns>
    [HttpPost("suggest-categories")]
    [ProducesResponseType<BatchSuggestCategoriesResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SuggestCategoriesAsync(
        [FromBody] BatchSuggestCategoriesRequest request,
        CancellationToken cancellationToken)
    {
        if (request.TransactionIds.Count == 0)
        {
            return this.BadRequest("At least one transaction ID is required.");
        }

        if (request.TransactionIds.Count > 100)
        {
            return this.BadRequest("Maximum 100 transactions per request.");
        }

        var suggestions = await _categorizationEngine.GetBatchSuggestionsAsync(
            request.TransactionIds,
            cancellationToken);

        return this.Ok(new BatchSuggestCategoriesResponse { Suggestions = suggestions });
    }

    /// <summary>
    /// Bulk categorizes multiple transactions with the specified category.
    /// </summary>
    /// <param name="request">The bulk categorize request containing transaction IDs and category ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A response indicating success/failure counts and any errors.</returns>
    [HttpPost("bulk-categorize")]
    [ProducesResponseType<BulkCategorizeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BulkCategorizeAsync(
        [FromBody] BulkCategorizeRequest request,
        CancellationToken cancellationToken)
    {
        if (request.CategoryId == Guid.Empty)
        {
            return this.BadRequest("CategoryId is required.");
        }

        if (request.TransactionIds.Count == 0)
        {
            return this.BadRequest("At least one transaction ID is required.");
        }

        var result = await _uncategorizedService.BulkCategorizeAsync(request, cancellationToken);
        return this.Ok(result);
    }

    /// <summary>
    /// Updates the location on a transaction.
    /// </summary>
    /// <param name="id">The transaction identifier.</param>
    /// <param name="dto">The location data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated transaction.</returns>
    [HttpPatch("{id:guid}/location")]
    [ProducesResponseType<TransactionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateLocationAsync(
        Guid id,
        [FromBody] TransactionLocationUpdateDto dto,
        CancellationToken cancellationToken)
    {
        string? expectedVersion = null;
        if (this.Request.Headers.TryGetValue("If-Match", out var ifMatch))
        {
            expectedVersion = ifMatch.ToString().Trim('"');
        }

        var transaction = await _service.UpdateLocationAsync(id, dto, expectedVersion, cancellationToken);
        if (transaction is null)
        {
            return this.NotFound();
        }

        if (transaction.Version is not null)
        {
            this.Response.Headers.ETag = $"\"{transaction.Version}\"";
        }

        return this.Ok(transaction);
    }

    /// <summary>
    /// Updates the category on a transaction (quick category assignment).
    /// </summary>
    /// <param name="id">The transaction identifier.</param>
    /// <param name="dto">The category update data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated transaction.</returns>
    [HttpPatch("{id:guid}/category")]
    [ProducesResponseType<TransactionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateCategoryAsync(
        Guid id,
        [FromBody] TransactionCategoryUpdateDto dto,
        CancellationToken cancellationToken)
    {
        string? expectedVersion = null;
        if (this.Request.Headers.TryGetValue("If-Match", out var ifMatch))
        {
            expectedVersion = ifMatch.ToString().Trim('"');
        }

        var transaction = await _service.UpdateCategoryAsync(id, dto, expectedVersion, cancellationToken);
        if (transaction is null)
        {
            return this.NotFound();
        }

        if (transaction.Version is not null)
        {
            this.Response.Headers.ETag = $"\"{transaction.Version}\"";
        }

        return this.Ok(transaction);
    }

    /// <summary>
    /// Clears the location from a transaction.
    /// </summary>
    /// <param name="id">The transaction identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>204 No Content on success; 404 if not found.</returns>
    [HttpDelete("{id:guid}/location")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteLocationAsync(Guid id, CancellationToken cancellationToken)
    {
        var cleared = await _service.ClearLocationAsync(id, cancellationToken);
        if (!cleared)
        {
            return this.NotFound();
        }

        return this.NoContent();
    }
}
