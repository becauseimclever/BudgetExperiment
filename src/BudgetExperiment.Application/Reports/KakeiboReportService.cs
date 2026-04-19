// <copyright file="KakeiboReportService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Globalization;

using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Application.Reports;

/// <summary>
/// Aggregates expense transactions into Kakeibo bucket totals for daily, weekly, and total views.
/// Income and Transfer category transactions are excluded from all aggregations.
/// </summary>
public sealed class KakeiboReportService : IKakeiboReportService
{
    private static readonly KakeiboCategory[] AllBuckets =
        [KakeiboCategory.Essentials, KakeiboCategory.Wants, KakeiboCategory.Culture, KakeiboCategory.Unexpected];

    private readonly ITransactionQueryRepository _transactionRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="KakeiboReportService"/> class.
    /// </summary>
    /// <param name="transactionRepository">The transaction repository.</param>
    public KakeiboReportService(ITransactionQueryRepository transactionRepository)
    {
        _transactionRepository = transactionRepository;
    }

    /// <inheritdoc />
    public async Task<KakeiboSummary> GetKakeiboSummaryAsync(
        DateOnly from,
        DateOnly to,
        Guid? accountId,
        CancellationToken cancellationToken = default)
    {
        if (from > to)
        {
            throw new ArgumentException("'from' must be on or before 'to'.", nameof(from));
        }

        var transactions = await _transactionRepository.GetByDateRangeAsync(from, to, accountId, cancellationToken);
        var expenses = transactions
            .Where(static t => t.Category?.Type == CategoryType.Expense)
            .ToList();

        return new KakeiboSummary
        {
            DateRange = new KakeiboDateRange { From = from, To = to },
            DailyTotals = BuildDailyTotals(expenses),
            WeeklyTotals = BuildWeeklyTotals(expenses),
            MonthlyTotals = BuildBucketTotals(expenses),
        };
    }

    private static IReadOnlyList<KakeiboDaily> BuildDailyTotals(List<Transaction> expenses)
    {
        return expenses
            .GroupBy(static t => t.Date)
            .Select(static g => new KakeiboDaily
            {
                Date = g.Key,
                BucketTotals = BuildBucketTotals(g),
            })
            .OrderBy(static d => d.Date)
            .ToList();
    }

    private static IReadOnlyList<KakeiboWeekly> BuildWeeklyTotals(List<Transaction> expenses)
    {
        return expenses
            .GroupBy(static t => GetIsoWeekMonday(t.Date))
            .Select(static g => new KakeiboWeekly
            {
                WeekStartDate = g.Key,
                WeekNumber = ISOWeek.GetWeekOfYear(g.Key.ToDateTime(TimeOnly.MinValue)),
                BucketTotals = BuildBucketTotals(g),
            })
            .OrderBy(static w => w.WeekStartDate)
            .ToList();
    }

    private static Dictionary<KakeiboCategory, decimal> BuildBucketTotals(IEnumerable<Transaction> transactions)
    {
        var sums = AllBuckets.ToDictionary(static b => b, static _ => 0m);
        foreach (var t in transactions)
        {
            var bucket = t.GetEffectiveKakeiboCategory();
            sums[bucket] += Math.Abs(t.Amount.Amount);
        }

        return sums;
    }

    private static DateOnly GetIsoWeekMonday(DateOnly date)
    {
        var dayOfWeek = (int)date.DayOfWeek;
        var daysFromMonday = dayOfWeek == 0 ? 6 : dayOfWeek - 1;
        return date.AddDays(-daysFromMonday);
    }
}
