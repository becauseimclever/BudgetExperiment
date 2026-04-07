// <copyright file="KaizenGoalsController.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetExperiment.Api.Controllers;

/// <summary>
/// REST API controller for Kaizen micro-goal operations.
/// </summary>
[ApiVersion("1.0")]
[ApiController]
[Authorize]
[Route("api/v{version:apiVersion}/goals/kaizen")]
[Produces("application/json")]
public sealed class KaizenGoalsController : ControllerBase
{
    private const string FeatureFlagName = "Kaizen:MicroGoals";

    private readonly IKaizenGoalService _service;
    private readonly IFeatureFlagService _featureFlagService;
    private readonly IUserContext _userContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="KaizenGoalsController"/> class.
    /// </summary>
    /// <param name="service">The Kaizen goal service.</param>
    /// <param name="featureFlagService">The feature flag service.</param>
    /// <param name="userContext">The current user context.</param>
    public KaizenGoalsController(
        IKaizenGoalService service,
        IFeatureFlagService featureFlagService,
        IUserContext userContext)
    {
        _service = service;
        _featureFlagService = featureFlagService;
        _userContext = userContext;
    }

    /// <summary>
    /// Gets the Kaizen goal for the current user for a specific week.
    /// </summary>
    /// <param name="weekStart">The Monday of the ISO week (yyyy-MM-dd).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The goal if found, or 204 No Content if not yet created.</returns>
    [HttpGet("week/{weekStart}")]
    [ProducesResponseType<KaizenGoalDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByWeekAsync(DateOnly weekStart, CancellationToken cancellationToken)
    {
        if (!await _featureFlagService.IsEnabledAsync(FeatureFlagName, cancellationToken))
        {
            return this.NotFound();
        }

        if (!this.TryGetUserId(out var userId))
        {
            return this.Unauthorized();
        }

        var goal = await _service.GetByWeekAsync(weekStart, userId, cancellationToken);
        if (goal is null)
        {
            return this.NoContent();
        }

        return this.Ok(goal);
    }

    /// <summary>
    /// Creates a new Kaizen goal for the specified week.
    /// </summary>
    /// <param name="weekStart">The Monday of the ISO week (yyyy-MM-dd).</param>
    /// <param name="dto">The goal creation data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created goal.</returns>
    [HttpPost("week/{weekStart}")]
    [ProducesResponseType<KaizenGoalDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateAsync(DateOnly weekStart, [FromBody] CreateKaizenGoalDto dto, CancellationToken cancellationToken)
    {
        if (!await _featureFlagService.IsEnabledAsync(FeatureFlagName, cancellationToken))
        {
            return this.NotFound();
        }

        if (!this.TryGetUserId(out var userId))
        {
            return this.Unauthorized();
        }

        try
        {
            var goal = await _service.CreateAsync(weekStart, dto, userId, cancellationToken);
            return this.CreatedAtAction(nameof(this.GetByWeekAsync), new { weekStart = goal.WeekStartDate.ToString("yyyy-MM-dd") }, goal);
        }
        catch (DomainException ex)
        {
            return this.BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Updates an existing Kaizen goal.
    /// </summary>
    /// <param name="goalId">The goal identifier.</param>
    /// <param name="dto">The goal update data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated goal, or 404 if not found.</returns>
    [HttpPut("{goalId:guid}")]
    [ProducesResponseType<KaizenGoalDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateAsync(Guid goalId, [FromBody] UpdateKaizenGoalDto dto, CancellationToken cancellationToken)
    {
        if (!await _featureFlagService.IsEnabledAsync(FeatureFlagName, cancellationToken))
        {
            return this.NotFound();
        }

        if (!this.TryGetUserId(out var userId))
        {
            return this.Unauthorized();
        }

        try
        {
            var goal = await _service.UpdateAsync(goalId, dto, userId, cancellationToken);
            if (goal is null)
            {
                return this.NotFound();
            }

            return this.Ok(goal);
        }
        catch (DomainException ex)
        {
            return this.BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Deletes a Kaizen goal.
    /// </summary>
    /// <param name="goalId">The goal identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content if deleted, 404 if not found.</returns>
    [HttpDelete("{goalId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(Guid goalId, CancellationToken cancellationToken)
    {
        if (!await _featureFlagService.IsEnabledAsync(FeatureFlagName, cancellationToken))
        {
            return this.NotFound();
        }

        if (!this.TryGetUserId(out var userId))
        {
            return this.Unauthorized();
        }

        var deleted = await _service.DeleteAsync(goalId, userId, cancellationToken);
        if (!deleted)
        {
            return this.NotFound();
        }

        return this.NoContent();
    }

    /// <summary>
    /// Gets all Kaizen goals for the current user within an inclusive week range.
    /// </summary>
    /// <param name="from">The earliest week start date (inclusive, yyyy-MM-dd).</param>
    /// <param name="to">The latest week start date (inclusive, yyyy-MM-dd).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of goals ordered by week descending.</returns>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<KaizenGoalDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRangeAsync(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        CancellationToken cancellationToken)
    {
        if (!await _featureFlagService.IsEnabledAsync(FeatureFlagName, cancellationToken))
        {
            return this.NotFound();
        }

        if (!this.TryGetUserId(out var userId))
        {
            return this.Unauthorized();
        }

        var goals = await _service.GetRangeAsync(from, to, userId, cancellationToken);
        return this.Ok(goals);
    }

    private bool TryGetUserId(out Guid userId)
    {
        var parsed = _userContext.UserIdAsGuid;
        userId = parsed ?? Guid.Empty;
        return parsed.HasValue;
    }
}
