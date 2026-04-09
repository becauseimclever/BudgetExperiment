// <copyright file="KakeiboSummary.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// Full Kakeibo bucket summary for a date range.
/// Includes daily, weekly (ISO), and total (monthly) bucket aggregations.
/// Income and Transfer transactions are excluded; all four buckets are always present.
/// </summary>
public record KakeiboSummary
{
    /// <summary>Gets the queried date range.</summary>
    public KakeiboDateRange DateRange { get; init; } = null!;

    /// <summary>Gets per-day bucket totals, ordered chronologically.</summary>
    public IReadOnlyList<KakeiboDaily> DailyTotals { get; init; } = [];

    /// <summary>Gets per-ISO-week bucket totals, ordered chronologically by week start.</summary>
    public IReadOnlyList<KakeiboWeekly> WeeklyTotals { get; init; } = [];

    /// <summary>Gets total bucket spending across the entire date range.</summary>
    public Dictionary<KakeiboCategory, decimal> MonthlyTotals { get; init; } = [];
}
