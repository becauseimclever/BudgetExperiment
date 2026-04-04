// <copyright file="IChartDataService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Components.Charts.Models;
using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Client.Services;

/// <summary>
/// Service for computing chart-ready data structures from raw transaction and account balance data.
/// </summary>
public interface IChartDataService
{
    /// <summary>
    /// Builds a spending heatmap matrix (7 rows = Mon–Sun, N columns = weeks or months).
    /// </summary>
    /// <param name="transactions">The transactions to aggregate into heatmap cells.</param>
    /// <param name="grouping">Controls how columns are calculated.</param>
    /// <returns>
    /// A jagged array where the outer index is day-of-week (0 = Monday, 6 = Sunday)
    /// and each inner array contains one <see cref="HeatmapDataPoint"/> per column.
    /// </returns>
    HeatmapDataPoint[][] BuildSpendingHeatmap(
        IEnumerable<TransactionDto> transactions,
        HeatmapGrouping grouping = HeatmapGrouping.DayOfWeekByWeek);

    /// <summary>
    /// Builds waterfall segments from income and per-category spending totals.
    /// The first segment represents income (positive), one segment per category (negative),
    /// and the final segment represents the net total.
    /// </summary>
    /// <param name="income">The total income for the period.</param>
    /// <param name="spending">Per-category spending data.</param>
    /// <returns>An ordered array of waterfall segments suitable for rendering.</returns>
    WaterfallSegment[] BuildBudgetWaterfall(
        decimal income,
        IEnumerable<CategorySpendingDto> spending);

    /// <summary>
    /// Builds OHLC candlestick data points from daily account balance data.
    /// </summary>
    /// <param name="balances">The daily balance readings to group into candles.</param>
    /// <param name="interval">Controls whether candlesticks span a month or a week.</param>
    /// <returns>An ordered array of candlestick data points.</returns>
    CandlestickDataPoint[] BuildBalanceCandlesticks(
        IEnumerable<DailyBalanceDto> balances,
        CandlestickInterval interval = CandlestickInterval.Monthly);

    /// <summary>
    /// Builds box plot statistical summaries for each category, covering the specified number of months.
    /// Uses the 1.5×IQR method to identify outliers.
    /// </summary>
    /// <param name="transactions">The transactions to analyse.</param>
    /// <param name="monthsBack">The number of past months to include (default 6).</param>
    /// <returns>An array of box plot summaries, one per category.</returns>
    BoxPlotSummary[] BuildCategoryDistributions(
        IEnumerable<TransactionDto> transactions,
        int monthsBack = 6);
}
