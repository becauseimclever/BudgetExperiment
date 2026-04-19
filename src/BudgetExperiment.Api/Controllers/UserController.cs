// <copyright file="UserController.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetExperiment.Api.Controllers;

/// <summary>
/// REST API controller for user-related operations.
/// </summary>
[ApiVersion("1.0")]
[ApiController]
[Authorize]
[Route("api/v{version:apiVersion}/user")]
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
        _service = service;
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
        var profile = _service.GetCurrentUserProfile();
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
        var settings = await _service.GetCurrentUserSettingsAsync(cancellationToken);
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
        var settings = await _service.UpdateCurrentUserSettingsAsync(dto, cancellationToken);
        return this.Ok(settings);
    }

    /// <summary>
    /// Completes the onboarding wizard for the current user.
    /// Sets IsOnboarded to true so the wizard does not reappear.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated user settings.</returns>
    [HttpPost("settings/complete-onboarding")]
    [ProducesResponseType<UserSettingsDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> CompleteOnboardingAsync(CancellationToken cancellationToken)
    {
        var settings = await _service.CompleteOnboardingAsync(cancellationToken);
        return this.Ok(settings);
    }

    /// <summary>
    /// Marks the Kakeibo category setup wizard as complete for the current user.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content.</returns>
    [HttpPost("onboarding/kakeibo-setup/complete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> CompleteKakeiboSetupAsync(CancellationToken cancellationToken)
    {
        await _service.MarkKakeiboSetupCompleteAsync(cancellationToken);
        return this.NoContent();
    }
}
