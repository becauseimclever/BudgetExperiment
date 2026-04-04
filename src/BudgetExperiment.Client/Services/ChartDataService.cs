// <copyright file="ChartDataService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Components.Charts.Models;
using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Client.Services;

/// <summary>
/// Computes chart-ready data structures from raw transaction and account balance data.
/// </summary>
public sealed class ChartDataService : IChartDataService
{
    private const int DaysInWeek = 7;
    private const decimal OutlierMultiplier = 1.5m;

    /// <inheritdoc />
    public HeatmapDataPoint[][] BuildSpendingHeatmap(
        IEnumerable<TransactionDto> transactions,
        HeatmapGrouping grouping = HeatmapGrouping.DayOfWeekByWeek)
    {
        var result = InitialiseHeatmapRows();
        var list = transactions.ToList();
        if (list.Count == 0)
        {
            return result;
        }

        var mondayOfFirstWeek = GetMondayOfWeek(list.Min(t => t.Date));
        var pointsByDay = list
            .GroupBy(t => (Day: ToDayIndex(t.Date), Week: ToWeekIndex(t.Date, mondayOfFirstWeek)))
            .Select(g => new HeatmapDataPoint(
                g.Key.Day,
                g.Key.Week,
                g.Sum(t => Math.Abs(t.Amount.Amount)),
                g.Count()))
            .GroupBy(p => p.DayOfWeek)
            .ToDictionary(g => g.Key, g => g.ToArray());

        for (var i = 0; i < DaysInWeek; i++)
        {
            result[i] = pointsByDay.TryGetValue(i, out var pts) ? pts : [];
        }

        return result;
    }

    /// <inheritdoc />
    public WaterfallSegment[] BuildBudgetWaterfall(
        decimal income,
        IEnumerable<CategorySpendingDto> spending)
    {
        var orderedSpending = spending.OrderBy(s => s.Amount.Amount).ToList();
        var segments = new List<WaterfallSegment>(orderedSpending.Count + 2);
        segments.Add(new WaterfallSegment("Income", income, income, false));

        var runningTotal = income;
        foreach (var category in orderedSpending)
        {
            runningTotal += category.Amount.Amount;
            segments.Add(new WaterfallSegment(category.CategoryName, category.Amount.Amount, runningTotal, false));
        }

        segments.Add(new WaterfallSegment("Net", runningTotal, runningTotal, true));
        return [.. segments];
    }

    /// <inheritdoc />
    public CandlestickDataPoint[] BuildBalanceCandlesticks(
        IEnumerable<DailyBalanceDto> balances,
        CandlestickInterval interval = CandlestickInterval.Monthly)
    {
        return [.. balances
            .OrderBy(b => b.Date)
            .GroupBy(b => new DateOnly(b.Date.Year, b.Date.Month, 1))
            .Select(g => new CandlestickDataPoint(
                g.Key,
                g.First().Balance,
                g.Max(b => b.Balance),
                g.Min(b => b.Balance),
                g.Last().Balance))
            .OrderBy(c => c.Date)];
    }

    /// <inheritdoc />
    public BoxPlotSummary[] BuildCategoryDistributions(
        IEnumerable<TransactionDto> transactions,
        int monthsBack = 6)
    {
        var list = transactions.ToList();
        if (list.Count == 0)
        {
            return [];
        }

        var maxDate = list.Max(t => t.Date);
        var cutoff = maxDate.AddMonths(-monthsBack);
        var filtered = list
            .Where(t => t.Date >= cutoff && t.CategoryName is not null)
            .ToList();

        if (filtered.Count == 0)
        {
            return [];
        }

        return [.. filtered
            .GroupBy(t => t.CategoryName!)
            .Select(g => ComputeBoxPlot(g.Key, g.Select(t => Math.Abs(t.Amount.Amount))))];
    }

    private static HeatmapDataPoint[][] InitialiseHeatmapRows()
    {
        var rows = new HeatmapDataPoint[DaysInWeek][];
        for (var i = 0; i < DaysInWeek; i++)
        {
            rows[i] = [];
        }

        return rows;
    }

    private static DateOnly GetMondayOfWeek(DateOnly date)
    {
        var daysToMonday = date.DayOfWeek == DayOfWeek.Sunday ? 6 : (int)date.DayOfWeek - 1;
        return date.AddDays(-daysToMonday);
    }

    private static int ToDayIndex(DateOnly date)
    {
        return date.DayOfWeek == DayOfWeek.Sunday ? 6 : (int)date.DayOfWeek - 1;
    }

    private static int ToWeekIndex(DateOnly date, DateOnly mondayOfFirstWeek)
    {
        var mondayOfThisWeek = GetMondayOfWeek(date);
        return (mondayOfThisWeek.DayNumber - mondayOfFirstWeek.DayNumber) / DaysInWeek;
    }

    private static BoxPlotSummary ComputeBoxPlot(string categoryName, IEnumerable<decimal> values)
    {
        var sorted = values.OrderBy(v => v).ToArray();
        var (q1, median, q3) = ComputeQuartiles(sorted);
        var iqr = q3 - q1;
        var lowerFence = q1 - (OutlierMultiplier * iqr);
        var upperFence = q3 + (OutlierMultiplier * iqr);

        var nonOutliers = sorted.Where(v => v >= lowerFence && v <= upperFence).ToArray();
        var outliers = sorted.Where(v => v < lowerFence || v > upperFence).ToArray();

        return new BoxPlotSummary(
            categoryName,
            nonOutliers.Min(),
            q1,
            median,
            q3,
            nonOutliers.Max(),
            outliers);
    }

    private static (decimal Q1, decimal Median, decimal Q3) ComputeQuartiles(decimal[] sorted)
    {
        var n = sorted.Length;
        if (n % 2 == 1)
        {
            var mid = n / 2;
            return (ComputeMedian(sorted[..mid]), sorted[mid], ComputeMedian(sorted[(mid + 1)..]));
        }

        var half = n / 2;
        return (
            ComputeMedian(sorted[..half]),
            (sorted[half - 1] + sorted[half]) / 2m,
            ComputeMedian(sorted[half..]));
    }

    private static decimal ComputeMedian(decimal[] sorted)
    {
        var n = sorted.Length;
        return n % 2 == 1 ? sorted[n / 2] : (sorted[(n / 2) - 1] + sorted[n / 2]) / 2m;
    }
}
