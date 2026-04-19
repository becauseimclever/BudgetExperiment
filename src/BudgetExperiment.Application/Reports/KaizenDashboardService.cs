// <copyright file="KaizenDashboardService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain.Kaizen;
using BudgetExperiment.Shared.Budgeting;

using Microsoft.Extensions.Caching.Memory;

namespace BudgetExperiment.Application.Reports;

/// <summary>
/// Builds the 12-week rolling Kaizen Dashboard by combining weekly Kakeibo spending
/// aggregations with Kaizen micro-goal outcomes. Results are cached for one hour.
/// </summary>
public sealed class KaizenDashboardService : IKaizenDashboardService
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(1);

    private readonly ITransactionQueryRepository _transactionRepository;
    private readonly IKaizenGoalRepository _goalRepository;
    private readonly IMemoryCache _cache;

    /// <summary>
    /// Initializes a new instance of the <see cref="KaizenDashboardService"/> class.
    /// </summary>
    /// <param name="transactionRepository">The transaction repository.</param>
    /// <param name="goalRepository">The Kaizen goal repository.</param>
    /// <param name="cache">The in-process memory cache.</param>
    public KaizenDashboardService(
        ITransactionQueryRepository transactionRepository,
        IKaizenGoalRepository goalRepository,
        IMemoryCache cache)
    {
        _transactionRepository = transactionRepository;
        _goalRepository = goalRepository;
        _cache = cache;
    }

    /// <inheritdoc />
    public async Task<KaizenDashboardDto> GetDashboardAsync(
        Guid userId,
        int weeks = 12,
        CancellationToken ct = default)
    {
        var cacheKey = $"kaizen-dashboard:{userId}:{weeks}";
        if (_cache.TryGetValue(cacheKey, out KaizenDashboardDto? cached) && cached is not null)
        {
            return cached;
        }

        var weekStarts = ComputeWeekStarts(weeks);
        var rangeStart = weekStarts[0];
        var rangeEnd = weekStarts[^1].AddDays(6);

        var transactions = await _transactionRepository.GetByDateRangeAsync(rangeStart, rangeEnd, null, ct);
        var goals = await _goalRepository.GetRangeAsync(userId, rangeStart, weekStarts[^1], ct);
        var goalsByWeek = goals.ToDictionary(static g => g.WeekStartDate);

        var weekDtos = weekStarts
            .Select(weekStart => BuildWeekSummary(weekStart, transactions, goalsByWeek))
            .ToList();

        var dto = new KaizenDashboardDto { Weeks = weekDtos };
        _cache.Set(cacheKey, dto, CacheTtl);
        return dto;
    }

    private static IReadOnlyList<DateOnly> ComputeWeekStarts(int weeks)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var dayOfWeek = (int)today.DayOfWeek;
        var daysFromMonday = dayOfWeek == 0 ? 6 : dayOfWeek - 1;
        var currentWeekMonday = today.AddDays(-daysFromMonday);
        var firstWeekStart = currentWeekMonday.AddDays(-(weeks - 1) * 7);

        return Enumerable.Range(0, weeks)
            .Select(i => firstWeekStart.AddDays(i * 7))
            .ToArray();
    }

    private static WeeklyKakeiboSummaryDto BuildWeekSummary(
        DateOnly weekStart,
        IReadOnlyList<Domain.Accounts.Transaction> transactions,
        Dictionary<DateOnly, KaizenGoal> goalsByWeek)
    {
        var weekEnd = weekStart.AddDays(6);

        var expenses = transactions
            .Where(t => t.Date >= weekStart && t.Date <= weekEnd && !t.IsTransfer && t.Amount.Amount < 0)
            .ToList();

        goalsByWeek.TryGetValue(weekStart, out var goal);

        return new WeeklyKakeiboSummaryDto
        {
            WeekStart = weekStart,
            WeekLabel = $"{weekStart:MMM d}\u2013{weekStart.AddDays(6).Day}",
            Essentials = SumByCategory(expenses, KakeiboCategory.Essentials),
            Wants = SumByCategory(expenses, KakeiboCategory.Wants),
            Culture = SumByCategory(expenses, KakeiboCategory.Culture),
            Unexpected = SumByCategory(expenses, KakeiboCategory.Unexpected),
            KaizenGoalDescription = goal?.Description,
            KaizenGoalAchieved = goal is not null ? goal.IsAchieved : null,
        };
    }

    private static decimal SumByCategory(
        IEnumerable<Domain.Accounts.Transaction> transactions,
        KakeiboCategory category)
    {
        return transactions
            .Where(t => t.GetEffectiveKakeiboCategory() == category)
            .Sum(static t => Math.Abs(t.Amount.Amount));
    }
}
