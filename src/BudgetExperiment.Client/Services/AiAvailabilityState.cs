// <copyright file="AiAvailabilityState.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Client.Services;

/// <summary>
/// Represents the availability state of AI features.
/// </summary>
public enum AiAvailabilityState
{
    /// <summary>
    /// AI feature flag is disabled - hide all AI UI.
    /// </summary>
    Disabled,

    /// <summary>
    /// AI feature flag is enabled, but Ollama connection is unavailable - show warning state.
    /// </summary>
    Unavailable,

    /// <summary>
    /// AI is fully operational - normal display.
    /// </summary>
    Available,
}
