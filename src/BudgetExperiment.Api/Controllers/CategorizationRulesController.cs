// <copyright file="CategorizationRulesController.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Application.Services;
using BudgetExperiment.Contracts.Dtos;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetExperiment.Api.Controllers;

/// <summary>
/// REST API controller for categorization rule operations.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/[controller]")]
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
    /// Gets all categorization rules.
    /// </summary>
    /// <param name="activeOnly">If true, returns only active rules.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of categorization rules.</returns>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<CategorizationRuleDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllAsync([FromQuery] bool activeOnly = false, CancellationToken cancellationToken = default)
    {
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
    public async Task<IActionResult> UpdateAsync(Guid id, [FromBody] CategorizationRuleUpdateDto dto, CancellationToken cancellationToken)
    {
        var rule = await this._service.UpdateAsync(id, dto, cancellationToken);
        if (rule is null)
        {
            return this.NotFound();
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
}
