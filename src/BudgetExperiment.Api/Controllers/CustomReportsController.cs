// <copyright file="CustomReportsController.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Application.Reports;
using BudgetExperiment.Contracts.Dtos;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetExperiment.Api.Controllers;

/// <summary>
/// REST API controller for custom report layouts.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/custom-reports")]
[Produces("application/json")]
public sealed class CustomReportsController : ControllerBase
{
    private readonly ICustomReportLayoutService _layoutService;

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomReportsController"/> class.
    /// </summary>
    /// <param name="layoutService">Layout service.</param>
    public CustomReportsController(ICustomReportLayoutService layoutService)
    {
        this._layoutService = layoutService;
    }

    /// <summary>
    /// Gets all custom report layouts.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Layouts.</returns>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<CustomReportLayoutDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllAsync(CancellationToken cancellationToken)
    {
        var layouts = await this._layoutService.GetAllAsync(cancellationToken);
        return this.Ok(layouts);
    }

    /// <summary>
    /// Gets a custom report layout by id.
    /// </summary>
    /// <param name="id">Layout id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Layout.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<CustomReportLayoutDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var layout = await this._layoutService.GetByIdAsync(id, cancellationToken);
        return layout is null ? this.NotFound() : this.Ok(layout);
    }

    /// <summary>
    /// Creates a new custom report layout.
    /// </summary>
    /// <param name="dto">Create DTO.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Created layout.</returns>
    [HttpPost]
    [ProducesResponseType<CustomReportLayoutDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateAsync(
        [FromBody] CustomReportLayoutCreateDto dto,
        CancellationToken cancellationToken)
    {
        var layout = await this._layoutService.CreateAsync(dto, cancellationToken);
        return this.CreatedAtAction(nameof(GetByIdAsync), new { id = layout.Id }, layout);
    }

    /// <summary>
    /// Updates a custom report layout.
    /// </summary>
    /// <param name="id">Layout id.</param>
    /// <param name="dto">Update DTO.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated layout.</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType<CustomReportLayoutDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateAsync(
        Guid id,
        [FromBody] CustomReportLayoutUpdateDto dto,
        CancellationToken cancellationToken)
    {
        var layout = await this._layoutService.UpdateAsync(id, dto, cancellationToken);
        return layout is null ? this.NotFound() : this.Ok(layout);
    }

    /// <summary>
    /// Deletes a custom report layout.
    /// </summary>
    /// <param name="id">Layout id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content if deleted.</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await this._layoutService.DeleteAsync(id, cancellationToken);
        return deleted ? this.NoContent() : this.NotFound();
    }
}
