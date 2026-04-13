// <copyright file="KakeiboWeekly.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// Kakeibo spending totals for an ISO week (Monday–Sunday), broken down by the four buckets.
/// All four buckets are always present; zero-spend buckets are included.
/// </summary>
public record KakeiboWeekly
{
    /// <summary>Gets the Monday date that starts this ISO week.</summary>
    public DateOnly WeekStartDate
    {
        get; init;
    }

    /// <summary>Gets the ISO week number within the year (1–53).</summary>
    public int WeekNumber
    {
        get; init;
    }

    /// <summary>Gets per-bucket absolute spending totals for this week.</summary>
    public Dictionary<KakeiboCategory, decimal> BucketTotals { get; init; } = [];
}
