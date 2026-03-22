// <copyright file="CategoriesController.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetExperiment.Api.Controllers;

/// <summary>
/// REST API controller for budget category operations.
/// </summary>
[ApiVersion("1.0")]
[ApiController]
[Authorize]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
public sealed class CategoriesController : ControllerBase
{
    private readonly IBudgetCategoryService _service;

    /// <summary>
    /// Initializes a new instance of the <see cref="CategoriesController"/> class.
    /// </summary>
    /// <param name="service">The budget category service.</param>
    public CategoriesController(IBudgetCategoryService service)
    {
        _service = service;
    }

    /// <summary>
    /// Gets all budget categories.
    /// </summary>
    /// <param name="activeOnly">If true, returns only active categories.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of budget categories.</returns>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<BudgetCategoryDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllAsync([FromQuery] bool activeOnly = false, CancellationToken cancellationToken = default)
    {
        var categories = activeOnly
            ? await _service.GetActiveAsync(cancellationToken)
            : await _service.GetAllAsync(cancellationToken);
        return this.Ok(categories);
    }

    /// <summary>
    /// Gets a specific budget category by ID.
    /// </summary>
    /// <param name="id">The category identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The budget category if found.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<BudgetCategoryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var category = await _service.GetByIdAsync(id, cancellationToken);
        if (category is null)
        {
            return this.NotFound();
        }

        if (category.Version is not null)
        {
            this.Response.Headers.ETag = $"\"{category.Version}\"";
        }

        return this.Ok(category);
    }

    /// <summary>
    /// Creates a new budget category.
    /// </summary>
    /// <param name="dto">The category creation data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created category.</returns>
    [HttpPost]
    [ProducesResponseType<BudgetCategoryDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateAsync([FromBody] BudgetCategoryCreateDto dto, CancellationToken cancellationToken)
    {
        var category = await _service.CreateAsync(dto, cancellationToken);
        return this.CreatedAtAction("GetById", new { id = category.Id }, category);
    }

    /// <summary>
    /// Updates an existing budget category.
    /// </summary>
    /// <param name="id">The category identifier.</param>
    /// <param name="dto">The category update data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated category.</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType<BudgetCategoryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateAsync(Guid id, [FromBody] BudgetCategoryUpdateDto dto, CancellationToken cancellationToken)
    {
        string? expectedVersion = null;
        if (this.Request.Headers.TryGetValue("If-Match", out var ifMatch))
        {
            expectedVersion = ifMatch.ToString().Trim('"');
        }

        var category = await _service.UpdateAsync(id, dto, expectedVersion, cancellationToken);
        if (category is null)
        {
            return this.NotFound();
        }

        if (category.Version is not null)
        {
            this.Response.Headers.ETag = $"\"{category.Version}\"";
        }

        return this.Ok(category);
    }

    /// <summary>
    /// Deletes (deactivates) a budget category.
    /// </summary>
    /// <param name="id">The category identifier.</param>
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

    /// <summary>
    /// Activates a deactivated budget category.
    /// </summary>
    /// <param name="id">The category identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content if successful.</returns>
    [HttpPost("{id:guid}/activate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ActivateAsync(Guid id, CancellationToken cancellationToken)
    {
        var activated = await _service.ActivateAsync(id, cancellationToken);
        if (!activated)
        {
            return this.NotFound();
        }

        return this.NoContent();
    }

    /// <summary>
    /// Deactivates a budget category.
    /// </summary>
    /// <param name="id">The category identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content if successful.</returns>
    [HttpPost("{id:guid}/deactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateAsync(Guid id, CancellationToken cancellationToken)
    {
        var deactivated = await _service.DeactivateAsync(id, cancellationToken);
        if (!deactivated)
        {
            return this.NotFound();
        }

        return this.NoContent();
    }
}
