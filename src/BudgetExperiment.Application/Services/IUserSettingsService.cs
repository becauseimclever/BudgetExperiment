// <copyright file="IUserSettingsService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Application.Services;

/// <summary>
/// Service interface for user settings operations.
/// </summary>
public interface IUserSettingsService
{
    /// <summary>
    /// Gets the current user's profile.
    /// </summary>
    /// <returns>The user profile DTO.</returns>
    UserProfileDto GetCurrentUserProfile();

    /// <summary>
    /// Gets the current user's settings, creating defaults if they don't exist.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user settings DTO.</returns>
    Task<UserSettingsDto> GetCurrentUserSettingsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the current user's settings.
    /// </summary>
    /// <param name="dto">The settings update DTO.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated user settings DTO.</returns>
    Task<UserSettingsDto> UpdateCurrentUserSettingsAsync(UserSettingsUpdateDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current session scope.
    /// </summary>
    /// <returns>The scope DTO.</returns>
    ScopeDto GetCurrentScope();

    /// <summary>
    /// Sets the current session scope.
    /// </summary>
    /// <param name="dto">The scope DTO.</param>
    void SetCurrentScope(ScopeDto dto);
}
