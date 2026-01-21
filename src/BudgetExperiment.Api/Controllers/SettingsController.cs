// <copyright file="SettingsController.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>


using BudgetExperiment.Contracts.Dtos;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetExperiment.Api.Controllers;

/// <summary>
/// REST API controller for application settings operations.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public sealed class SettingsController : ControllerBase
{
    private readonly IAppSettingsService _settingsService;

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsController"/> class.
    /// </summary>
    /// <param name="settingsService">The settings service.</param>
    public SettingsController(IAppSettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    /// <summary>
    /// Gets the current application settings.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The application settings.</returns>
    [HttpGet]
    [ProducesResponseType<AppSettingsDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSettingsAsync(CancellationToken cancellationToken)
    {
        var settings = await _settingsService.GetSettingsAsync(cancellationToken);
        return Ok(settings);
    }

    /// <summary>
    /// Updates the application settings.
    /// </summary>
    /// <param name="dto">The settings update data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated application settings.</returns>
    [HttpPut]
    [ProducesResponseType<AppSettingsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateSettingsAsync(
        [FromBody] AppSettingsUpdateDto dto,
        CancellationToken cancellationToken)
    {
        var settings = await _settingsService.UpdateSettingsAsync(dto, cancellationToken);
        return Ok(settings);
    }
}
