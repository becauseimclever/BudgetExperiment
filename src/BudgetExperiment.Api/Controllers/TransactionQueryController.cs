// <copyright file="TransactionQueryController.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Application.Transactions;
using BudgetExperiment.Contracts.Dtos;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetExperiment.Api.Controllers;

/// <summary>
/// REST API controller for transaction query operations (GET endpoints).
/// </summary>
[ApiVersion("1.0")]
[ApiController]
[Authorize]
[Route("api/v{version:apiVersion}/transactions")]
[Produces("application/json")]
public sealed class TransactionQueryController : ControllerBase
{
    private readonly ITransactionService _service;
    private readonly IUncategorizedTransactionService _uncategorizedService;
    private readonly IUnifiedTransactionService _unifiedService;

    /// <summary>
    /// Initializes a new instance of the <see cref="TransactionQueryController"/> class.
    /// </summary>
    /// <param name="service">The transaction service.</param>
    /// <param name="uncategorizedService">The uncategorized transaction service.</param>
    /// <param name="unifiedService">The unified transaction service.</param>
    public TransactionQueryController(
        ITransactionService service,
        IUncategorizedTransactionService uncategorizedService,
        IUnifiedTransactionService unifiedService)
    {
        _service = service;
        _uncategorizedService = uncategorizedService;
        _unifiedService = unifiedService;
    }

    /// <summary>
    /// Queries transactions by date range with optional account filter.
    /// </summary>
    /// <param name="startDate">The start date (inclusive).</param>
    /// <param name="endDate">The end date (inclusive).</param>
    /// <param name="accountId">Optional account filter.</param>
    /// <param name="kakeiboCategory">Optional Kakeibo category filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of transactions.</returns>
    /// <remarks>
    /// Deprecated. Use GET /api/v2/transactions/by-date-range with page/pageSize parameters instead.
    /// </remarks>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<TransactionDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Obsolete("Use GET /api/v2/transactions/by-date-range with pagination.")]
    public async Task<IActionResult> GetByDateRangeAsync(
        [FromQuery] DateOnly startDate,
        [FromQuery] DateOnly endDate,
        [FromQuery] Guid? accountId,
        [FromQuery] string? kakeiboCategory,
        CancellationToken cancellationToken)
    {
        this.Response.Headers.Append("Deprecation", "true");
        this.Response.Headers.Append("Sunset", "2026-07-09");
        this.Response.Headers.Append("Link", "</api/v2/transactions/by-date-range>; rel=\"successor-version\"");

        if (startDate > endDate)
        {
            return this.BadRequest("startDate must be less than or equal to endDate.");
        }

        KakeiboCategory? filter = null;
        if (!string.IsNullOrWhiteSpace(kakeiboCategory))
        {
            if (!Enum.TryParse<KakeiboCategory>(kakeiboCategory, ignoreCase: true, out var parsed))
            {
                return this.BadRequest("kakeiboCategory must be a valid Kakeibo category.");
            }

            filter = parsed;
        }

        var transactions = await _service.GetByDateRangeAsync(startDate, endDate, accountId, filter, cancellationToken);
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
    /// <param name="kakeiboCategory">Optional Kakeibo category filter.</param>
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
        [FromQuery] string? kakeiboCategory,
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
            KakeiboCategory = kakeiboCategory,
            SortBy = sortBy,
            SortDescending = sortDescending,
            Page = page,
            PageSize = pageSize,
        };

        var result = await _unifiedService.GetPagedAsync(filter, cancellationToken);

        this.Response.Headers["X-Pagination-TotalCount"] = result.TotalCount.ToString();

        return this.Ok(result);
    }
}
