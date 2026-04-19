// <copyright file="IUserSettingsService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Application.Settings;

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
    /// Completes the onboarding wizard for the current user.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated user settings DTO.</returns>
    Task<UserSettingsDto> CompleteOnboardingAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks the Kakeibo category setup wizard as complete for the current user.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    Task MarkKakeiboSetupCompleteAsync(CancellationToken cancellationToken = default);
}
