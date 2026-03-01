// <copyright file="AnalysisProgress.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Application.Categorization;

/// <summary>
/// Progress information for analysis operations.
/// </summary>
public sealed record AnalysisProgress
{
    /// <summary>
    /// Gets the current step description.
    /// </summary>
    public string CurrentStep { get; init; } = string.Empty;

    /// <summary>
    /// Gets the percentage complete (0-100).
    /// </summary>
    public int PercentComplete { get; init; }
}
