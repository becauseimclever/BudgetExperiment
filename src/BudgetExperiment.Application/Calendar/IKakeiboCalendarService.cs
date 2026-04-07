// <copyright file="IKakeiboCalendarService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Application.Calendar;

/// <summary>
/// Service providing Kakeibo-enriched calendar analytics.
/// </summary>
public interface IKakeiboCalendarService
{
    /// <summary>
    /// Gets the Kakeibo spending breakdown for a full calendar month.
    /// </summary>
    /// <param name="year">The year.</param>
    /// <param name="month">The month (1-12).</param>
    /// <param name="userId">The user identifier (used for reflection lookups; transactions are scope-filtered).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The spending breakdown by Kakeibo category.</returns>
    Task<KakeiboBreakdown> GetMonthBreakdownAsync(int year, int month, Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Gets the Kakeibo spending breakdown for a single calendar week.
    /// </summary>
    /// <param name="weekStart">The first day of the week (Sunday-based or Monday-based per user preference).</param>
    /// <param name="userId">The user identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The spending breakdown by Kakeibo category.</returns>
    Task<KakeiboBreakdown> GetWeekBreakdownAsync(DateOnly weekStart, Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Gets the dominant Kakeibo category for a single day (category with highest absolute spend).
    /// </summary>
    /// <param name="date">The date to inspect.</param>
    /// <param name="userId">The user identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The dominant category, or <see langword="null"/> if no expense transactions exist for that day.</returns>
    Task<KakeiboCategory?> GetDominantCategoryAsync(DateOnly date, Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Gets per-day heatmap data for every day in a calendar month.
    /// </summary>
    /// <param name="year">The year.</param>
    /// <param name="month">The month (1-12).</param>
    /// <param name="userId">The user identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A dictionary keyed by day-of-month (1-based) containing the spend and relative intensity for each day.
    /// </returns>
    Task<Dictionary<int, HeatmapDayData>> GetMonthHeatmapAsync(int year, int month, Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Gets Kakeibo spending breakdowns for all weeks in a calendar grid with a single DB query.
    /// More efficient than calling <see cref="GetWeekBreakdownAsync"/> per week.
    /// </summary>
    /// <param name="gridStart">The first date of the calendar grid (may be in the previous month).</param>
    /// <param name="weekCount">Total number of weeks in the grid.</param>
    /// <param name="userId">The user identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Dictionary keyed by 0-based week index.</returns>
    Task<IReadOnlyDictionary<int, KakeiboBreakdown>> GetGridWeekBreakdownsAsync(
        DateOnly gridStart,
        int weekCount,
        Guid userId,
        CancellationToken ct = default);
}
