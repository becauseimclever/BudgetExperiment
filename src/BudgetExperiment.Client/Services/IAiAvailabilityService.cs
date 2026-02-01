// <copyright file="IAiAvailabilityService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Client.Services;

/// <summary>
/// Service for checking AI feature availability with caching.
/// </summary>
public interface IAiAvailabilityService
{
    /// <summary>
    /// Gets the current AI availability state.
    /// </summary>
    AiAvailabilityState State { get; }

    /// <summary>
    /// Gets a value indicating whether the AI feature flag is enabled.
    /// When true, AI UI should be shown (possibly in warning state).
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Gets a value indicating whether the Ollama connection is available.
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>
    /// Gets a value indicating whether AI is fully operational (enabled AND available).
    /// </summary>
    bool IsFullyOperational { get; }

    /// <summary>
    /// Gets the error message when AI is unavailable (for tooltip display).
    /// </summary>
    string? ErrorMessage { get; }

    /// <summary>
    /// Event raised when the AI availability status changes.
    /// </summary>
    event Action? StatusChanged;

    /// <summary>
    /// Refreshes the AI availability status from the API.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    Task RefreshAsync();
}
