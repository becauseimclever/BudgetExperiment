// <copyright file="AiSetupState.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Client.Components.AI;

/// <summary>
/// Represents the current state of AI setup for the setup banner.
/// </summary>
public enum AiSetupState
{
    /// <summary>
    /// AI is not configured (not available or not enabled).
    /// </summary>
    NotConfigured,

    /// <summary>
    /// AI is configured but no suggestions exist yet.
    /// </summary>
    NoSuggestions,

    /// <summary>
    /// All suggestions have been handled.
    /// </summary>
    AllCaughtUp,
}
