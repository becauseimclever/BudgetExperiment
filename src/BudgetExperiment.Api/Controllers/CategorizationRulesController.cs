// <copyright file="CategorizationRulesController.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetExperiment.Api.Controllers;

/// <summary>
/// REST API controller for categorization rule operations.
/// </summary>
[ApiVersion("1.0")]
[ApiController]
[Authorize]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
public sealed class CategorizationRulesController : ControllerBase
{
    private readonly ICategorizationRuleService _service;

    /// <summary>
    /// Initializes a new instance of the <see cref="CategorizationRulesController"/> class.
    /// </summary>
    /// <param name="service">The categorization rule service.</param>
    public CategorizationRulesController(ICategorizationRuleService service)
    {
        this._service = service;
    }

    /// <summary>
    /// Gets categorization rules. When page/pageSize are provided, returns a paginated response.
    /// Otherwise returns all rules for backward compatibility.
    /// </summary>
    /// <param name="activeOnly">If true, returns only active rules (non-paginated mode only).</param>
    /// <param name="page">Page number (1-based) for paginated results.</param>
    /// <param name="pageSize">Number of items per page.</param>
    /// <param name="search">Search text to filter by rule name or pattern.</param>
    /// <param name="categoryId">Category ID to filter by.</param>
    /// <param name="status">Status filter: "active", "inactive", or null for all.</param>
    /// <param name="sortBy">Sort field: "priority", "name", "category", "createdAt".</param>
    /// <param name="sortDirection">Sort direction: "asc" or "desc".</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of categorization rules or a paged response.</returns>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<CategorizationRuleDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<CategorizationRulePageResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllAsync(
        [FromQuery] bool activeOnly = false,
        [FromQuery] int? page = null,
        [FromQuery] int? pageSize = null,
        [FromQuery] string? search = null,
        [FromQuery] Guid? categoryId = null,
        [FromQuery] string? status = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDirection = null,
        CancellationToken cancellationToken = default)
    {
        if (page.HasValue || pageSize.HasValue)
        {
            var request = new CategorizationRuleListRequest
            {
                Page = page ?? 1,
                PageSize = pageSize ?? 25,
                Search = search,
                CategoryId = categoryId,
                Status = status,
                SortBy = sortBy,
                SortDirection = sortDirection,
            };

            var pagedResult = await this._service.ListPagedAsync(request, cancellationToken);
            this.Response.Headers["X-Pagination-TotalCount"] = pagedResult.TotalCount.ToString(System.Globalization.CultureInfo.InvariantCulture);
            return this.Ok(pagedResult);
        }

        var rules = await this._service.GetAllAsync(activeOnly, cancellationToken);
        return this.Ok(rules);
    }

    /// <summary>
    /// Gets a specific categorization rule by ID.
    /// </summary>
    /// <param name="id">The rule identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The categorization rule if found.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<CategorizationRuleDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var rule = await this._service.GetByIdAsync(id, cancellationToken);
        if (rule is null)
        {
            return this.NotFound();
        }

        if (rule.Version is not null)
        {
            this.Response.Headers.ETag = $"\"{rule.Version}\"";
        }

        return this.Ok(rule);
    }

    /// <summary>
    /// Creates a new categorization rule.
    /// </summary>
    /// <param name="dto">The rule creation data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created rule.</returns>
    [HttpPost]
    [ProducesResponseType<CategorizationRuleDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateAsync([FromBody] CategorizationRuleCreateDto dto, CancellationToken cancellationToken)
    {
        var rule = await this._service.CreateAsync(dto, cancellationToken);
        return this.CreatedAtAction("GetById", new { id = rule.Id }, rule);
    }

    /// <summary>
    /// Updates an existing categorization rule.
    /// </summary>
    /// <param name="id">The rule identifier.</param>
    /// <param name="dto">The rule update data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated rule.</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType<CategorizationRuleDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateAsync(Guid id, [FromBody] CategorizationRuleUpdateDto dto, CancellationToken cancellationToken)
    {
        string? expectedVersion = null;
        if (this.Request.Headers.TryGetValue("If-Match", out var ifMatch))
        {
            expectedVersion = ifMatch.ToString().Trim('"');
        }

        var rule = await this._service.UpdateAsync(id, dto, expectedVersion, cancellationToken);
        if (rule is null)
        {
            return this.NotFound();
        }

        if (rule.Version is not null)
        {
            this.Response.Headers.ETag = $"\"{rule.Version}\"";
        }

        return this.Ok(rule);
    }

    /// <summary>
    /// Deletes a categorization rule.
    /// </summary>
    /// <param name="id">The rule identifier.</param>
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
    /// Activates a deactivated categorization rule.
    /// </summary>
    /// <param name="id">The rule identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content if successful.</returns>
    [HttpPost("{id:guid}/activate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ActivateAsync(Guid id, CancellationToken cancellationToken)
    {
        var activated = await this._service.ActivateAsync(id, cancellationToken);
        if (!activated)
        {
            return this.NotFound();
        }

        return this.NoContent();
    }

    /// <summary>
    /// Deactivates an active categorization rule.
    /// </summary>
    /// <param name="id">The rule identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content if successful.</returns>
    [HttpPost("{id:guid}/deactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateAsync(Guid id, CancellationToken cancellationToken)
    {
        var deactivated = await this._service.DeactivateAsync(id, cancellationToken);
        if (!deactivated)
        {
            return this.NotFound();
        }

        return this.NoContent();
    }

    /// <summary>
    /// Reorders categorization rules by setting new priorities.
    /// </summary>
    /// <param name="request">The reorder request containing ordered rule IDs.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content if successful.</returns>
    [HttpPut("reorder")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ReorderAsync([FromBody] ReorderRulesRequest request, CancellationToken cancellationToken)
    {
        await this._service.ReorderAsync(request.RuleIds, cancellationToken);
        return this.NoContent();
    }

    /// <summary>
    /// Tests a pattern against existing transaction descriptions.
    /// </summary>
    /// <param name="request">The test pattern request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The matching transaction descriptions.</returns>
    [HttpPost("test")]
    [ProducesResponseType<TestPatternResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> TestPatternAsync([FromBody] TestPatternRequest request, CancellationToken cancellationToken)
    {
        var result = await this._service.TestPatternAsync(request, cancellationToken);
        return this.Ok(result);
    }

    /// <summary>
    /// Applies categorization rules to transactions.
    /// </summary>
    /// <param name="request">The apply rules request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the bulk categorization operation.</returns>
    [HttpPost("apply")]
    [ProducesResponseType<ApplyRulesResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ApplyRulesAsync([FromBody] ApplyRulesRequest request, CancellationToken cancellationToken)
    {
        var result = await this._service.ApplyRulesAsync(request, cancellationToken);
        return this.Ok(result);
    }

    /// <summary>
    /// Bulk deletes categorization rules.
    /// </summary>
    /// <param name="request">The bulk action request containing rule IDs.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of deleted rules.</returns>
    [HttpDelete("bulk")]
    [ProducesResponseType<BulkRuleActionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BulkDeleteAsync([FromBody] BulkRuleActionRequest request, CancellationToken cancellationToken)
    {
        if (request.Ids.Count == 0)
        {
            return this.BadRequest("No rule IDs provided.");
        }

        var count = await this._service.BulkDeleteAsync(request.Ids, cancellationToken);
        return this.Ok(new BulkRuleActionResponse { AffectedCount = count });
    }

    /// <summary>
    /// Bulk activates categorization rules.
    /// </summary>
    /// <param name="request">The bulk action request containing rule IDs.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of activated rules.</returns>
    [HttpPost("bulk/activate")]
    [ProducesResponseType<BulkRuleActionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BulkActivateAsync([FromBody] BulkRuleActionRequest request, CancellationToken cancellationToken)
    {
        if (request.Ids.Count == 0)
        {
            return this.BadRequest("No rule IDs provided.");
        }

        var count = await this._service.BulkActivateAsync(request.Ids, cancellationToken);
        return this.Ok(new BulkRuleActionResponse { AffectedCount = count });
    }

    /// <summary>
    /// Bulk deactivates categorization rules.
    /// </summary>
    /// <param name="request">The bulk action request containing rule IDs.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of deactivated rules.</returns>
    [HttpPost("bulk/deactivate")]
    [ProducesResponseType<BulkRuleActionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BulkDeactivateAsync([FromBody] BulkRuleActionRequest request, CancellationToken cancellationToken)
    {
        if (request.Ids.Count == 0)
        {
            return this.BadRequest("No rule IDs provided.");
        }

        var count = await this._service.BulkDeactivateAsync(request.Ids, cancellationToken);
        return this.Ok(new BulkRuleActionResponse { AffectedCount = count });
    }
}
