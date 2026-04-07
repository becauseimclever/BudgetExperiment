// <copyright file="ReflectionsController.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetExperiment.Api.Controllers;

/// <summary>
/// REST API controller for monthly Kakeibo reflection operations.
/// </summary>
[ApiVersion("1.0")]
[ApiController]
[Authorize]
[Route("api/v{version:apiVersion}")]
[Produces("application/json")]
public sealed class ReflectionsController : ControllerBase
{
    private const string FeatureFlagName = "Kakeibo:MonthlyReflectionPrompts";

    private readonly IReflectionService _service;
    private readonly IFeatureFlagService _featureFlagService;
    private readonly IUserContext _userContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReflectionsController"/> class.
    /// </summary>
    /// <param name="service">The reflection service.</param>
    /// <param name="featureFlagService">The feature flag service.</param>
    /// <param name="userContext">The current user context.</param>
    public ReflectionsController(
        IReflectionService service,
        IFeatureFlagService featureFlagService,
        IUserContext userContext)
    {
        _service = service;
        _featureFlagService = featureFlagService;
        _userContext = userContext;
    }

    /// <summary>
    /// Gets the reflection for the current user for a specific month.
    /// </summary>
    /// <param name="year">The year.</param>
    /// <param name="month">The month (1-12).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The reflection if found, or 204 No Content if not yet created.</returns>
    [HttpGet("reflections/month/{year:int}/{month:int}")]
    [ProducesResponseType<MonthlyReflectionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByMonthAsync(int year, int month, CancellationToken cancellationToken)
    {
        if (!await _featureFlagService.IsEnabledAsync(FeatureFlagName, cancellationToken))
        {
            return this.NotFound();
        }

        if (!this.TryGetUserId(out var userId))
        {
            return this.Unauthorized();
        }

        var reflection = await _service.GetByMonthAsync(year, month, userId, cancellationToken);
        if (reflection is null)
        {
            return this.NoContent();
        }

        return this.Ok(reflection);
    }

    /// <summary>
    /// Creates or updates the reflection for the current user for a specific month.
    /// </summary>
    /// <param name="year">The year.</param>
    /// <param name="month">The month (1-12).</param>
    /// <param name="dto">The create/update data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created or updated reflection.</returns>
    [HttpPost("reflections/month/{year:int}/{month:int}")]
    [ProducesResponseType<MonthlyReflectionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateOrUpdateAsync(
        int year,
        int month,
        [FromBody] CreateOrUpdateMonthlyReflectionDto dto,
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

        try
        {
            var reflection = await _service.CreateOrUpdateAsync(year, month, dto, userId, cancellationToken);
            return this.Ok(reflection);
        }
        catch (DomainException ex)
        {
            return this.BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Gets a paginated history of reflections for the current user.
    /// </summary>
    /// <param name="limit">Maximum number of items to return (default 12).</param>
    /// <param name="offset">Number of items to skip (default 0).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The paginated list of reflections with total count.</returns>
    [HttpGet("reflections")]
    [ProducesResponseType<object>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetHistoryAsync(
        [FromQuery] int limit = 12,
        [FromQuery] int offset = 0,
        CancellationToken cancellationToken = default)
    {
        if (!await _featureFlagService.IsEnabledAsync(FeatureFlagName, cancellationToken))
        {
            return this.NotFound();
        }

        if (!this.TryGetUserId(out var userId))
        {
            return this.Unauthorized();
        }

        var (items, total) = await _service.GetHistoryAsync(userId, limit, offset, cancellationToken);

        this.Response.Headers["X-Pagination-TotalCount"] = total.ToString();

        return this.Ok(new
        {
            items,
            total,
            limit,
            offset,
        });
    }

    /// <summary>
    /// Gets the complete financial summary for a month, including the reflection.
    /// </summary>
    /// <param name="year">The year.</param>
    /// <param name="month">The month (1-12).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The monthly financial summary.</returns>
    [HttpGet("calendar/month/{year:int}/{month:int}/summary")]
    [ProducesResponseType<MonthFinancialSummaryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMonthSummaryAsync(
        int year,
        int month,
        CancellationToken cancellationToken)
    {
        if (!await _featureFlagService.IsEnabledAsync(FeatureFlagName, cancellationToken))
        {
            return this.NotFound();
        }

        if (month < 1 || month > 12)
        {
            return this.BadRequest("Month must be between 1 and 12.");
        }

        if (!this.TryGetUserId(out var userId))
        {
            return this.Unauthorized();
        }

        var summary = await _service.GetMonthSummaryAsync(year, month, userId, cancellationToken);
        return this.Ok(summary);
    }

    /// <summary>
    /// Deletes a reflection by its identifier.
    /// </summary>
    /// <param name="reflectionId">The reflection identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content if deleted.</returns>
    [HttpDelete("reflections/{reflectionId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(Guid reflectionId, CancellationToken cancellationToken)
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
            await _service.DeleteAsync(reflectionId, userId, cancellationToken);
            return this.NoContent();
        }
        catch (DomainException)
        {
            return this.NotFound();
        }
    }

    private bool TryGetUserId(out Guid userId)
    {
        var parsed = _userContext.UserIdAsGuid;
        userId = parsed ?? Guid.Empty;
        return parsed.HasValue;
    }
}
