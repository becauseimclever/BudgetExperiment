// <copyright file="IUserSettingsRepository.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain;

/// <summary>
/// Repository interface for UserSettings entities.
/// </summary>
public interface IUserSettingsRepository
{
    /// <summary>
    /// Gets the settings for a specific user, creating defaults if they don't exist.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user's settings.</returns>
    Task<UserSettings> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves changes to the user settings.
    /// </summary>
    /// <param name="settings">The settings to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    Task SaveAsync(UserSettings settings, CancellationToken cancellationToken = default);
}
