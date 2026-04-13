// <copyright file="KakeiboDaily.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// Kakeibo spending totals for a single day, broken down by the four buckets.
/// All four buckets are always present; zero-spend buckets are included.
/// </summary>
public record KakeiboDaily
{
    /// <summary>Gets the date for this daily summary.</summary>
    public DateOnly Date
    {
        get; init;
    }

    /// <summary>Gets per-bucket absolute spending totals for this day.</summary>
    public Dictionary<KakeiboCategory, decimal> BucketTotals { get; init; } = [];
}
