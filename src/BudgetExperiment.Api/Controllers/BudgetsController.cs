// <copyright file="BudgetsController.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Application.Services;
using BudgetExperiment.Contracts.Dtos;

using Microsoft.AspNetCore.Mvc;

namespace BudgetExperiment.Api.Controllers;

/// <summary>
/// REST API controller for budget goal and progress operations.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public sealed class BudgetsController : ControllerBase
{
    private readonly IBudgetGoalService _goalService;
    private readonly IBudgetProgressService _progressService;

    /// <summary>
    /// Initializes a new instance of the <see cref="BudgetsController"/> class.
    /// </summary>
    /// <param name="goalService">The budget goal service.</param>
    /// <param name="progressService">The budget progress service.</param>
    public BudgetsController(IBudgetGoalService goalService, IBudgetProgressService progressService)
    {
        this._goalService = goalService;
        this._progressService = progressService;
    }

    /// <summary>
    /// Gets all budget goals for a specific month.
    /// </summary>
    /// <param name="year">The year.</param>
    /// <param name="month">The month (1-12).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of budget goals for the month.</returns>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<BudgetGoalDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetGoalsByMonthAsync(
        [FromQuery] int year,
        [FromQuery] int month,
        CancellationToken cancellationToken)
    {
        if (month < 1 || month > 12)
        {
            return this.BadRequest("Month must be between 1 and 12.");
        }

        var goals = await this._goalService.GetByMonthAsync(year, month, cancellationToken);
        return this.Ok(goals);
    }

    /// <summary>
    /// Gets all budget goals for a specific category.
    /// </summary>
    /// <param name="categoryId">The category identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of budget goals for the category.</returns>
    [HttpGet("category/{categoryId:guid}")]
    [ProducesResponseType<IReadOnlyList<BudgetGoalDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGoalsByCategoryAsync(Guid categoryId, CancellationToken cancellationToken)
    {
        var goals = await this._goalService.GetByCategoryAsync(categoryId, cancellationToken);
        return this.Ok(goals);
    }

    /// <summary>
    /// Sets or updates a budget goal for a category.
    /// </summary>
    /// <param name="categoryId">The category identifier.</param>
    /// <param name="dto">The goal data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created or updated budget goal.</returns>
    [HttpPut("{categoryId:guid}")]
    [ProducesResponseType<BudgetGoalDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetGoalAsync(
        Guid categoryId,
        [FromBody] BudgetGoalSetDto dto,
        CancellationToken cancellationToken)
    {
        if (dto.Month < 1 || dto.Month > 12)
        {
            return this.BadRequest("Month must be between 1 and 12.");
        }

        var goal = await this._goalService.SetGoalAsync(categoryId, dto, cancellationToken);
        if (goal is null)
        {
            return this.NotFound("Category not found.");
        }

        return this.Ok(goal);
    }

    /// <summary>
    /// Deletes a budget goal for a category and month.
    /// </summary>
    /// <param name="categoryId">The category identifier.</param>
    /// <param name="year">The year.</param>
    /// <param name="month">The month (1-12).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content if successful.</returns>
    [HttpDelete("{categoryId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteGoalAsync(
        Guid categoryId,
        [FromQuery] int year,
        [FromQuery] int month,
        CancellationToken cancellationToken)
    {
        if (month < 1 || month > 12)
        {
            return this.BadRequest("Month must be between 1 and 12.");
        }

        var deleted = await this._goalService.DeleteGoalAsync(categoryId, year, month, cancellationToken);
        if (!deleted)
        {
            return this.NotFound();
        }

        return this.NoContent();
    }

    /// <summary>
    /// Gets the budget progress for all categories for a specific month.
    /// </summary>
    /// <param name="year">The year.</param>
    /// <param name="month">The month (1-12).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The budget summary with category progress.</returns>
    [HttpGet("progress")]
    [ProducesResponseType<BudgetSummaryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetProgressAsync(
        [FromQuery] int year,
        [FromQuery] int month,
        CancellationToken cancellationToken)
    {
        if (month < 1 || month > 12)
        {
            return this.BadRequest("Month must be between 1 and 12.");
        }

        var summary = await this._progressService.GetMonthlySummaryAsync(year, month, cancellationToken);
        return this.Ok(summary);
    }

    /// <summary>
    /// Gets the budget progress for a specific category and month.
    /// </summary>
    /// <param name="categoryId">The category identifier.</param>
    /// <param name="year">The year.</param>
    /// <param name="month">The month (1-12).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The budget progress for the category.</returns>
    [HttpGet("progress/{categoryId:guid}")]
    [ProducesResponseType<BudgetProgressDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCategoryProgressAsync(
        Guid categoryId,
        [FromQuery] int year,
        [FromQuery] int month,
        CancellationToken cancellationToken)
    {
        if (month < 1 || month > 12)
        {
            return this.BadRequest("Month must be between 1 and 12.");
        }

        var progress = await this._progressService.GetProgressAsync(categoryId, year, month, cancellationToken);
        if (progress is null)
        {
            return this.NotFound();
        }

        return this.Ok(progress);
    }

    /// <summary>
    /// Gets the overall budget summary for a specific month.
    /// </summary>
    /// <param name="year">The year.</param>
    /// <param name="month">The month (1-12).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The budget summary.</returns>
    [HttpGet("summary")]
    [ProducesResponseType<BudgetSummaryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetSummaryAsync(
        [FromQuery] int year,
        [FromQuery] int month,
        CancellationToken cancellationToken)
    {
        if (month < 1 || month > 12)
        {
            return this.BadRequest("Month must be between 1 and 12.");
        }

        var summary = await this._progressService.GetMonthlySummaryAsync(year, month, cancellationToken);
        return this.Ok(summary);
    }
}
