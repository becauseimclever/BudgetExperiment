// <copyright file="UserController.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Application.Services;
using BudgetExperiment.Contracts.Dtos;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetExperiment.Api.Controllers;

/// <summary>
/// REST API controller for user-related operations.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/user")]
[Produces("application/json")]
public sealed class UserController : ControllerBase
{
    private readonly IUserSettingsService _service;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserController"/> class.
    /// </summary>
    /// <param name="service">The user settings service.</param>
    public UserController(IUserSettingsService service)
    {
        this._service = service;
    }

    /// <summary>
    /// Gets the current user's profile.
    /// Provisions user settings on first call (auto-creates if not exists).
    /// </summary>
    /// <returns>The user profile.</returns>
    [HttpGet("me")]
    [ProducesResponseType<UserProfileDto>(StatusCodes.Status200OK)]
    public IActionResult GetProfile()
    {
        var profile = this._service.GetCurrentUserProfile();
        return this.Ok(profile);
    }

    /// <summary>
    /// Gets the current user's settings.
    /// Creates default settings if they don't exist (user provisioning).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user settings.</returns>
    [HttpGet("settings")]
    [ProducesResponseType<UserSettingsDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSettingsAsync(CancellationToken cancellationToken)
    {
        var settings = await this._service.GetCurrentUserSettingsAsync(cancellationToken);
        return this.Ok(settings);
    }

    /// <summary>
    /// Updates the current user's settings.
    /// </summary>
    /// <param name="dto">The settings update data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated user settings.</returns>
    [HttpPut("settings")]
    [ProducesResponseType<UserSettingsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateSettingsAsync(
        [FromBody] UserSettingsUpdateDto dto,
        CancellationToken cancellationToken)
    {
        var settings = await this._service.UpdateCurrentUserSettingsAsync(dto, cancellationToken);
        return this.Ok(settings);
    }

    /// <summary>
    /// Gets the current session's budget scope.
    /// </summary>
    /// <returns>The current scope selection.</returns>
    [HttpGet("scope")]
    [ProducesResponseType<ScopeDto>(StatusCodes.Status200OK)]
    public IActionResult GetScope()
    {
        var scope = this._service.GetCurrentScope();
        return this.Ok(scope);
    }

    /// <summary>
    /// Sets the current session's budget scope.
    /// This affects which data is visible for the current request/session.
    /// </summary>
    /// <param name="dto">The scope selection.</param>
    /// <returns>The updated scope.</returns>
    [HttpPut("scope")]
    [ProducesResponseType<ScopeDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult SetScope([FromBody] ScopeDto dto)
    {
        this._service.SetCurrentScope(dto);
        return this.Ok(dto);
    }
}
