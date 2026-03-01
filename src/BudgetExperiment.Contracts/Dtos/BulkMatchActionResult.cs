// <copyright file="BulkMatchActionResult.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// Result of a bulk match action.
/// </summary>
public sealed record BulkMatchActionResult
{
    /// <summary>
    /// Gets the number of matches successfully accepted.
    /// </summary>
    public int AcceptedCount { get; init; }

    /// <summary>
    /// Gets the number of matches that failed.
    /// </summary>
    public int FailedCount { get; init; }
}
