// <copyright file="TransactionsController.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Application.Categorization;
using BudgetExperiment.Contracts.Dtos;


using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetExperiment.Api.Controllers;

/// <summary>
/// REST API controller for transaction operations.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public sealed class TransactionsController : ControllerBase
{
    private readonly TransactionService _service;
    private readonly IUncategorizedTransactionService _uncategorizedService;

    /// <summary>
    /// Initializes a new instance of the <see cref="TransactionsController"/> class.
    /// </summary>
    /// <param name="service">The transaction service.</param>
    /// <param name="uncategorizedService">The uncategorized transaction service.</param>
    public TransactionsController(TransactionService service, IUncategorizedTransactionService uncategorizedService)
    {
        this._service = service;
        this._uncategorizedService = uncategorizedService;
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
    public async Task<IActionResult> UpdateAsync(Guid id, [FromBody] TransactionUpdateDto dto, CancellationToken cancellationToken)
    {
        var transaction = await this._service.UpdateAsync(id, dto, cancellationToken);
        if (transaction is null)
        {
            return this.NotFound();
        }

        return this.Ok(transaction);
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

        var result = await this._uncategorizedService.GetPagedAsync(filter, cancellationToken);

        // Add pagination header
        this.Response.Headers["X-Pagination-TotalCount"] = result.TotalCount.ToString();

        return this.Ok(result);
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

        var result = await this._uncategorizedService.BulkCategorizeAsync(request, cancellationToken);
        return this.Ok(result);
    }
}

