// <copyright file="KakeiboCalendarService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Application.Calendar;

/// <summary>
/// Provides Kakeibo-enriched calendar analytics using scoped transaction data.
/// All transaction queries are automatically scope-filtered via <see cref="ITransactionRepository"/>.
/// </summary>
public sealed class KakeiboCalendarService : IKakeiboCalendarService
{
    private readonly ITransactionQueryRepository _transactionRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="KakeiboCalendarService"/> class.
    /// </summary>
    /// <param name="transactionRepository">The transaction repository.</param>
    public KakeiboCalendarService(ITransactionQueryRepository transactionRepository)
    {
        _transactionRepository = transactionRepository;
    }

    /// <inheritdoc />
    public async Task<KakeiboBreakdown> GetMonthBreakdownAsync(
        int year,
        int month,
        Guid userId,
        CancellationToken ct = default)
    {
        var startDate = new DateOnly(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);
        var transactions = await _transactionRepository.GetByDateRangeAsync(startDate, endDate, null, ct);
        return ComputeBreakdown(transactions);
    }

    /// <inheritdoc />
    public async Task<KakeiboBreakdown> GetWeekBreakdownAsync(
        DateOnly weekStart,
        Guid userId,
        CancellationToken ct = default)
    {
        var weekEnd = weekStart.AddDays(6);
        var transactions = await _transactionRepository.GetByDateRangeAsync(weekStart, weekEnd, null, ct);
        return ComputeBreakdown(transactions);
    }

    /// <inheritdoc />
    public async Task<KakeiboCategory?> GetDominantCategoryAsync(
        DateOnly date,
        Guid userId,
        CancellationToken ct = default)
    {
        var transactions = await _transactionRepository.GetByDateRangeAsync(date, date, null, ct);
        return ComputeDominantCategory(transactions);
    }

    /// <inheritdoc />
    public async Task<Dictionary<int, HeatmapDayData>> GetMonthHeatmapAsync(
        int year,
        int month,
        Guid userId,
        CancellationToken ct = default)
    {
        var startDate = new DateOnly(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);
        var transactions = await _transactionRepository.GetByDateRangeAsync(startDate, endDate, null, ct);
        return ComputeHeatmap(transactions, year, month);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<int, KakeiboBreakdown>> GetGridWeekBreakdownsAsync(
        DateOnly gridStart,
        int weekCount,
        Guid userId,
        CancellationToken ct = default)
    {
        var gridEnd = gridStart.AddDays((weekCount * 7) - 1);
        var transactions = await _transactionRepository.GetByDateRangeAsync(gridStart, gridEnd, null, ct);

        var result = new Dictionary<int, KakeiboBreakdown>(weekCount);
        for (var weekIdx = 0; weekIdx < weekCount; weekIdx++)
        {
            var weekStart = gridStart.AddDays(weekIdx * 7);
            var weekEnd = weekStart.AddDays(6);
            var weekTransactions = transactions
                .Where(t => t.Date >= weekStart && t.Date <= weekEnd)
                .ToList();
            result[weekIdx] = ComputeBreakdown(weekTransactions);
        }

        return result;
    }

    private static KakeiboBreakdown ComputeBreakdown(IEnumerable<Transaction> transactions)
    {
        var breakdown = new KakeiboBreakdown();
        foreach (var t in transactions)
        {
            if (t.Amount.Amount >= 0m)
            {
                continue;
            }

            var amount = Math.Abs(t.Amount.Amount);
            switch (t.GetEffectiveKakeiboCategory())
            {
                case KakeiboCategory.Essentials:
                    breakdown.EssentialsAmount += amount;
                    break;
                case KakeiboCategory.Wants:
                    breakdown.WantsAmount += amount;
                    break;
                case KakeiboCategory.Culture:
                    breakdown.CultureAmount += amount;
                    break;
                case KakeiboCategory.Unexpected:
                    breakdown.UnexpectedAmount += amount;
                    break;
            }
        }

        return breakdown;
    }

    private static KakeiboCategory? ComputeDominantCategory(IReadOnlyList<Transaction> transactions)
    {
        var expenses = transactions.Where(t => t.Amount.Amount < 0m).ToList();
        if (expenses.Count == 0)
        {
            return null;
        }

        return expenses
            .GroupBy(t => t.GetEffectiveKakeiboCategory())
            .OrderByDescending(g => g.Sum(t => Math.Abs(t.Amount.Amount)))
            .First()
            .Key;
    }

    private static Dictionary<int, HeatmapDayData> ComputeHeatmap(
        IReadOnlyList<Transaction> transactions,
        int year,
        int month)
    {
        var dailySpend = transactions
            .Where(t => t.Amount.Amount < 0m)
            .GroupBy(t => t.Date.Day)
            .ToDictionary(g => g.Key, g => g.Sum(t => Math.Abs(t.Amount.Amount)));

        var daysWithSpend = dailySpend.Count;
        var totalSpend = dailySpend.Values.Sum();
        var dailyAverage = daysWithSpend > 0 ? totalSpend / daysWithSpend : 0m;

        var daysInMonth = DateTime.DaysInMonth(year, month);
        var result = new Dictionary<int, HeatmapDayData>(daysInMonth);
        for (var day = 1; day <= daysInMonth; day++)
        {
            var spend = dailySpend.TryGetValue(day, out var s) ? s : 0m;
            result[day] = new HeatmapDayData
            {
                Spend = spend,
                Intensity = ComputeIntensity(spend, dailyAverage),
            };
        }

        return result;
    }

    private static HeatmapIntensity ComputeIntensity(decimal spend, decimal dailyAverage)
    {
        if (spend == 0m)
        {
            return HeatmapIntensity.None;
        }

        if (dailyAverage == 0m)
        {
            return HeatmapIntensity.Low;
        }

        var ratio = spend / dailyAverage;
        return ratio switch
        {
            < 0.5m => HeatmapIntensity.Low,
            < 1.0m => HeatmapIntensity.Moderate,
            _ => HeatmapIntensity.High,
        };
    }
}
