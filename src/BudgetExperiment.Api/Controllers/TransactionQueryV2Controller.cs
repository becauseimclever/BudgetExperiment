// <copyright file="TransactionQueryV2Controller.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Application.Transactions;
using BudgetExperiment.Contracts.Dtos;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetExperiment.Api.Controllers;

/// <summary>
/// REST API controller for v2 transaction query operations (GET endpoints).
/// </summary>
[ApiVersion("2.0")]
[ApiController]
[Authorize]
[Route("api/v{version:apiVersion}/transactions")]
[Produces("application/json")]
public sealed class TransactionQueryV2Controller : ControllerBase
{
    private const int MaxPageSize = 100;
    private readonly IUnifiedTransactionService _unifiedService;

    /// <summary>
    /// Initializes a new instance of the <see cref="TransactionQueryV2Controller"/> class.
    /// </summary>
    /// <param name="unifiedService">The unified transaction service.</param>
    public TransactionQueryV2Controller(IUnifiedTransactionService unifiedService)
    {
        _unifiedService = unifiedService;
    }

    /// <summary>
    /// Queries transactions by date range with pagination.
    /// </summary>
    /// <param name="startDate">The start date (inclusive).</param>
    /// <param name="endDate">The end date (inclusive).</param>
    /// <param name="page">Page number (1-based, default: 1).</param>
    /// <param name="pageSize">Page size (default: 50, max: 100).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paged list of transactions.</returns>
    [HttpGet("by-date-range")]
    [ProducesResponseType<UnifiedTransactionPageDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetByDateRangePagedAsync(
        [FromQuery] DateOnly startDate,
        [FromQuery] DateOnly endDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        if (startDate > endDate)
        {
            return this.BadRequest("startDate must be less than or equal to endDate.");
        }

        if (page < 1)
        {
            return this.BadRequest("page must be at least 1.");
        }

        if (pageSize > MaxPageSize)
        {
            return this.BadRequest($"pageSize cannot exceed {MaxPageSize}.");
        }

        var filter = new UnifiedTransactionFilterDto
        {
            StartDate = startDate,
            EndDate = endDate,
            Page = page,
            PageSize = pageSize,
        };

        var result = await _unifiedService.GetPagedAsync(filter, cancellationToken);

        this.Response.Headers["X-Pagination-TotalCount"] = result.TotalCount.ToString();
        return this.Ok(result);
    }
}
