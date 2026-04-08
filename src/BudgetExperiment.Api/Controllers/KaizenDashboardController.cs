// <copyright file="KaizenDashboardController.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Application.Reports;
using BudgetExperiment.Contracts.Dtos;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetExperiment.Api.Controllers;

/// <summary>
/// REST API controller for the Kaizen Dashboard report.
/// Returns 12-week rolling Kakeibo spending aggregations with Kaizen micro-goal outcomes.
/// </summary>
[ApiVersion("1.0")]
[ApiController]
[Authorize]
[Route("api/v{version:apiVersion}/reports/kaizen-dashboard")]
[Produces("application/json")]
public sealed class KaizenDashboardController : ControllerBase
{
    private const string FeatureFlagName = "Kaizen:Dashboard";
    private const int MinWeeks = 1;
    private const int MaxWeeks = 52;

    private readonly IKaizenDashboardService _dashboardService;
    private readonly IFeatureFlagService _featureFlagService;
    private readonly IUserContext _userContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="KaizenDashboardController"/> class.
    /// </summary>
    /// <param name="dashboardService">The Kaizen dashboard service.</param>
    /// <param name="featureFlagService">The feature flag service.</param>
    /// <param name="userContext">The current user context.</param>
    public KaizenDashboardController(
        IKaizenDashboardService dashboardService,
        IFeatureFlagService featureFlagService,
        IUserContext userContext)
    {
        _dashboardService = dashboardService;
        _featureFlagService = featureFlagService;
        _userContext = userContext;
    }

    /// <summary>
    /// Gets the Kaizen Dashboard report for the current user.
    /// </summary>
    /// <param name="weeks">Number of rolling ISO weeks to include (default 12, min 1, max 52).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A <see cref="KaizenDashboardDto"/> with one <see cref="WeeklyKakeiboSummaryDto"/> per week,
    /// or 404 if the feature flag is disabled.
    /// </returns>
    [HttpGet]
    [ProducesResponseType<KaizenDashboardDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAsync(
        [FromQuery] int weeks = 12,
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

        if (weeks < MinWeeks || weeks > MaxWeeks)
        {
            return this.BadRequest($"weeks must be between {MinWeeks} and {MaxWeeks}.");
        }

        var dashboard = await _dashboardService.GetDashboardAsync(userId, weeks, cancellationToken);
        return this.Ok(dashboard);
    }

    private bool TryGetUserId(out Guid userId)
    {
        var parsed = _userContext.UserIdAsGuid;
        userId = parsed ?? Guid.Empty;
        return parsed.HasValue;
    }
}
