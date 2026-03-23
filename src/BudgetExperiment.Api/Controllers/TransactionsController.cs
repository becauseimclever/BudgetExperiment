// <copyright file="TransactionsController.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Application.Categorization;
using BudgetExperiment.Application.Transactions;
using BudgetExperiment.Contracts.Dtos;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetExperiment.Api.Controllers;

/// <summary>
/// REST API controller for transaction operations.
/// </summary>
[ApiVersion("1.0")]
[ApiController]
[Authorize]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
public sealed class TransactionsController : ControllerBase
{
    private readonly ITransactionService _service;
    private readonly IUncategorizedTransactionService _uncategorizedService;
    private readonly IUnifiedTransactionService _unifiedService;
    private readonly ICategorizationEngine _categorizationEngine;

    /// <summary>
    /// Initializes a new instance of the <see cref="TransactionsController"/> class.
    /// </summary>
    /// <param name="service">The transaction service.</param>
    /// <param name="uncategorizedService">The uncategorized transaction service.</param>
    /// <param name="unifiedService">The unified transaction service.</param>
    /// <param name="categorizationEngine">The categorization engine for rule-based suggestions.</param>
    public TransactionsController(
        ITransactionService service,
        IUncategorizedTransactionService uncategorizedService,
        IUnifiedTransactionService unifiedService,
        ICategorizationEngine categorizationEngine)
    {
        _service = service;
        _uncategorizedService = uncategorizedService;
        _unifiedService = unifiedService;
        _categorizationEngine = categorizationEngine;
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

        var transactions = await _service.GetByDateRangeAsync(startDate, endDate, accountId, cancellationToken);
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
        var transaction = await _service.GetByIdAsync(id, cancellationToken);
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
        return this.CreatedAtAction("GetById", new { id = transaction.Id }, transaction);
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
    /// Gets uncategorized transactions with optional filtering, sorting, and paging.
    /// </summary>
    /// <param name="startDate">Optional start date filter (inclusive).</param>
    /// <param name="endDate">Optional end date filter (inclusive).</param>
    /// <param name="minAmount">Optional minimum amount filter (absolute value).</param>
    /// <param name="maxAmount">Optional maximum amount filter (absolute value).</param>
    /// <param name="descriptionContains">Optional description contains filter (case-insensitive).</param>
    /// <param name="accountId">Optional account filter.</param>
    /// <param name="sortBy">Sort field: Date (default), Amount, or Description.</param>
    /// <param name="sortDescending">Sort direction (default: true for descending).</param>
    /// <param name="page">Page number (1-based, default: 1).</param>
    /// <param name="pageSize">Page size (default: 50, max: 100).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paged list of uncategorized transactions.</returns>
    [HttpGet("uncategorized")]
    [ProducesResponseType<UncategorizedTransactionPageDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUncategorizedAsync(
        [FromQuery] DateOnly? startDate,
        [FromQuery] DateOnly? endDate,
        [FromQuery] decimal? minAmount,
        [FromQuery] decimal? maxAmount,
        [FromQuery] string? descriptionContains,
        [FromQuery] Guid? accountId,
        [FromQuery] string sortBy = "Date",
        [FromQuery] bool sortDescending = true,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var filter = new UncategorizedTransactionFilterDto
        {
            StartDate = startDate,
            EndDate = endDate,
            MinAmount = minAmount,
            MaxAmount = maxAmount,
            DescriptionContains = descriptionContains,
            AccountId = accountId,
            SortBy = sortBy,
            SortDescending = sortDescending,
            Page = page,
            PageSize = pageSize,
        };

        var result = await _uncategorizedService.GetPagedAsync(filter, cancellationToken);

        // Add pagination header
        this.Response.Headers["X-Pagination-TotalCount"] = result.TotalCount.ToString();

        return this.Ok(result);
    }

    /// <summary>
    /// Gets a unified, paginated, filtered, and sorted list of all transactions across all accounts.
    /// </summary>
    /// <param name="accountId">Optional account filter.</param>
    /// <param name="categoryId">Optional category filter.</param>
    /// <param name="uncategorized">If true, show only uncategorized transactions.</param>
    /// <param name="startDate">Optional start date filter (inclusive).</param>
    /// <param name="endDate">Optional end date filter (inclusive).</param>
    /// <param name="description">Optional description search (contains, case-insensitive).</param>
    /// <param name="minAmount">Optional minimum amount filter (absolute value).</param>
    /// <param name="maxAmount">Optional maximum amount filter (absolute value).</param>
    /// <param name="sortBy">Sort field: date (default), description, amount, category, account.</param>
    /// <param name="sortDescending">Sort direction (default: true for descending).</param>
    /// <param name="page">Page number (1-based, default: 1).</param>
    /// <param name="pageSize">Page size (default: 50, max: 100).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paged list of unified transactions with summary and optional balance info.</returns>
    [HttpGet("paged")]
    [ProducesResponseType<UnifiedTransactionPageDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPagedAsync(
        [FromQuery] Guid? accountId,
        [FromQuery] Guid? categoryId,
        [FromQuery] bool? uncategorized,
        [FromQuery] DateOnly? startDate,
        [FromQuery] DateOnly? endDate,
        [FromQuery] string? description,
        [FromQuery] decimal? minAmount,
        [FromQuery] decimal? maxAmount,
        [FromQuery] string sortBy = "date",
        [FromQuery] bool sortDescending = true,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var filter = new UnifiedTransactionFilterDto
        {
            AccountId = accountId,
            CategoryId = categoryId,
            Uncategorized = uncategorized,
            StartDate = startDate,
            EndDate = endDate,
            Description = description,
            MinAmount = minAmount,
            MaxAmount = maxAmount,
            SortBy = sortBy,
            SortDescending = sortDescending,
            Page = page,
            PageSize = pageSize,
        };

        var result = await _unifiedService.GetPagedAsync(filter, cancellationToken);

        this.Response.Headers["X-Pagination-TotalCount"] = result.TotalCount.ToString();

        return this.Ok(result);
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
