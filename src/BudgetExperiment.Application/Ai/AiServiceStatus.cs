// <copyright file="AiServiceStatus.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Application.Ai;

/// <summary>
/// Represents the status of the AI service.
/// </summary>
/// <param name="IsAvailable">Whether the AI service is reachable.</param>
/// <param name="CurrentModel">The currently configured model name.</param>
/// <param name="ErrorMessage">Error message if not available.</param>
public sealed record AiServiceStatus(
    bool IsAvailable,
    string? CurrentModel,
    string? ErrorMessage);
