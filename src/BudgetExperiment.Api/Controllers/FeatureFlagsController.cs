// <copyright file="FeatureFlagsController.cs" company="Fortinbra">
// Copyright (c) 2025 Fortinbra (becauseimclever.com). All rights reserved.

using BudgetExperiment.Application.FeatureFlags;
using Microsoft.AspNetCore.Mvc;

namespace BudgetExperiment.Api.Controllers;

/// <summary>
/// API controller for feature flags.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public sealed class FeatureFlagsController : ControllerBase
{
    private readonly IFeatureFlagService featureFlagService;

    /// <summary>
    /// Initializes a new instance of the <see cref="FeatureFlagsController"/> class.
    /// </summary>
    /// <param name="featureFlagService">Feature flag service.</param>
    public FeatureFlagsController(IFeatureFlagService featureFlagService)
    {
        this.featureFlagService = featureFlagService;
    }

    /// <summary>
    /// Gets all feature flags.
    /// </summary>
    /// <returns>Feature flag status.</returns>
    [HttpGet]
    public IActionResult GetFeatureFlags()
    {
        return this.Ok(new
        {
            QuickEntry = this.featureFlagService.IsQuickEntryEnabled()
        });
    }
}
