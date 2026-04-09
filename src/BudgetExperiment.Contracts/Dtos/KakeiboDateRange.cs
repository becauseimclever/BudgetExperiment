// <copyright file="KakeiboDateRange.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// Represents the inclusive date range used for a Kakeibo summary report.
/// </summary>
public record KakeiboDateRange
{
    /// <summary>Gets the start date (inclusive).</summary>
    public DateOnly From { get; init; }

    /// <summary>Gets the end date (inclusive).</summary>
    public DateOnly To { get; init; }
}
