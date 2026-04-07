// <copyright file="FeaturesController.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Application.FeatureFlags;
using BudgetExperiment.Contracts.Dtos;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetExperiment.Api.Controllers;

/// <summary>
/// REST API controller for feature flag management.
/// </summary>
[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
public sealed class FeaturesController : ControllerBase
{
    private readonly IFeatureFlagService _featureFlagService;

    /// <summary>
    /// Initializes a new instance of the <see cref="FeaturesController"/> class.
    /// </summary>
    /// <param name="featureFlagService">The feature flag service.</param>
    public FeaturesController(IFeatureFlagService featureFlagService)
    {
        _featureFlagService = featureFlagService;
    }

    /// <summary>
    /// Gets all feature flags as a name-to-enabled dictionary.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A dictionary of feature flag names and their enabled states.</returns>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType<Dictionary<string, bool>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<Dictionary<string, bool>>> GetFeaturesAsync(CancellationToken cancellationToken)
    {
        var flags = await _featureFlagService.GetAllAsync(cancellationToken);
        return this.Ok(flags);
    }

    /// <summary>
    /// Updates a feature flag's enabled state. Requires authentication.
    /// </summary>
    /// <param name="flagName">The feature flag name.</param>
    /// <param name="request">The update request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated feature flag state.</returns>
    [HttpPut("{flagName}")]
    [Authorize]
    [ProducesResponseType<UpdateFeatureFlagResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UpdateFeatureFlagResponse>> UpdateFeatureFlagAsync(
        string flagName,
        [FromBody] UpdateFeatureFlagRequest request,
        CancellationToken cancellationToken)
    {
        await _featureFlagService.SetFlagAsync(flagName, request.Enabled, cancellationToken);
        var isEnabled = await _featureFlagService.IsEnabledAsync(flagName, cancellationToken);
        return this.Ok(new UpdateFeatureFlagResponse
        {
            Name = flagName,
            Enabled = isEnabled,
            UpdatedAtUtc = DateTime.UtcNow,
        });
    }
}
