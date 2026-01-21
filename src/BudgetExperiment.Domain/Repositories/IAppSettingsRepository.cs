// <copyright file="IAppSettingsRepository.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Repositories;

/// <summary>
/// Repository interface for AppSettings singleton entity.
/// </summary>
public interface IAppSettingsRepository
{
    /// <summary>
    /// Gets the singleton AppSettings instance, creating it with defaults if it doesn't exist.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The AppSettings instance.</returns>
    Task<AppSettings> GetAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves changes to the AppSettings instance.
    /// </summary>
    /// <param name="settings">The settings to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default);
}
