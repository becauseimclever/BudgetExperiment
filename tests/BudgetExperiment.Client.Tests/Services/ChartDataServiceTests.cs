// <copyright file="ChartDataServiceTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Components.Charts.Models;
using BudgetExperiment.Client.Services;
using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Client.Tests.Services;

/// <summary>
/// Unit tests for <see cref="ChartDataService"/> — TDD RED phase.
/// Tests describe expected behavior; implementation is pending (Feature 127, Slice 2).
/// </summary>
/// <remarks>
/// Amount conventions used throughout these tests:
/// - Income: positive decimal (e.g., 1000m)
/// - Expenses: negative decimal in Amount.Amount (e.g., -300m), matching the domain model.
/// - Heatmap TotalAmount and BoxPlot values use absolute values (spending intensity).
/// </remarks>
public sealed class ChartDataServiceTests
{
    private readonly IChartDataService _sut = new ChartDataService();

    // ─────────────────────────────────────────────────────────────────────────
    // BuildSpendingHeatmap
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Empty input produces a 7-element outer array (Mon–Sun rows) with no data points.
    /// </summary>
    [Fact]
    public void BuildSpendingHeatmap_EmptyTransactions_Returns7RowArrayEachEmpty()
    {
        // Arrange
        var transactions = Array.Empty<TransactionDto>();

        // Act
        var result = _sut.BuildSpendingHeatmap(transactions);

        // Assert
        Assert.Equal(7, result.Length);
        foreach (var row in result)
        {
            Assert.Empty(row);
        }
    }

    /// <summary>
    /// A transaction on a Monday appears in row index 0 (Mon=0); all other day rows are empty.
    /// </summary>
    [Fact]
    public void BuildSpendingHeatmap_SingleTransactionOnMonday_AppearsInRowIndexZero()
    {
        // Arrange — January 5, 2026 is a Monday
        var transactions = new[]
        {
            MakeTransaction("Groceries", -50m, new DateOnly(2026, 1, 5)),
        };

        // Act
        var result = _sut.BuildSpendingHeatmap(transactions);

        // Assert
        Assert.Single(result[0]);
        for (var i = 1; i <= 6; i++)
        {
            Assert.Empty(result[i]);
        }
    }

    /// <summary>
    /// Two transactions on the same day of the same week are aggregated into one HeatmapDataPoint.
    /// </summary>
    [Fact]
    public void BuildSpendingHeatmap_TwoTransactionsSameDayAndWeek_AggregatedIntoSinglePoint()
    {
        // Arrange — both on Monday January 5, 2026 (same day-of-week, same week)
        var monday = new DateOnly(2026, 1, 5);
        var transactions = new[]
        {
            MakeTransaction("Groceries", -100m, monday),
            MakeTransaction("Dining", -50m, monday),
        };

        // Act
        var result = _sut.BuildSpendingHeatmap(transactions);

        // Assert — one aggregated data point in the Monday row
        Assert.Single(result[0]);
        var point = result[0][0];
        Assert.Equal(2, point.TransactionCount);
        Assert.Equal(150m, point.TotalAmount);
    }

    /// <summary>
    /// Transactions in two non-adjacent weeks produce two separate data points in the Monday row.
    /// </summary>
    [Fact]
    public void BuildSpendingHeatmap_TransactionsInTwoSeparateWeeks_ProduceTwoDistinctDataPoints()
    {
        // Arrange — January 5 (week 1) and January 19 (week 3) are both Mondays; week 2 has no data
        var transactions = new[]
        {
            MakeTransaction("Groceries", -80m, new DateOnly(2026, 1, 5)),
            MakeTransaction("Groceries", -120m, new DateOnly(2026, 1, 19)),
        };

        // Act
        var result = _sut.BuildSpendingHeatmap(transactions);

        // Assert — Monday row contains exactly 2 data points with different WeekIndex values
        Assert.Equal(2, result[0].Length);
        Assert.NotEqual(result[0][0].WeekIndex, result[0][1].WeekIndex);
    }

    /// <summary>
    /// Expense transactions (negative amounts) produce positive TotalAmount in the heatmap
    /// because heatmap shows spending intensity (absolute value).
    /// </summary>
    [Fact]
    public void BuildSpendingHeatmap_ExpenseTransactions_TotalAmountIsAbsoluteValue()
    {
        // Arrange — expense has negative Amount in the domain
        var transactions = new[]
        {
            MakeTransaction("Dining", -200m, new DateOnly(2026, 1, 5)),
        };

        // Act
        var result = _sut.BuildSpendingHeatmap(transactions);

        // Assert — TotalAmount is the absolute value of spending (positive intensity)
        var point = result[0][0];
        Assert.Equal(200m, point.TotalAmount);
        Assert.True(point.TotalAmount > 0, "Heatmap TotalAmount must be positive (absolute spending intensity).");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // BuildBudgetWaterfall
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// No spending produces exactly 2 segments: Income and Net, both equal to income.
    /// </summary>
    [Fact]
    public void BuildBudgetWaterfall_NoSpending_ReturnsIncomeAndNetSegmentsOnly()
    {
        // Arrange
        var spending = Array.Empty<CategorySpendingDto>();

        // Act
        var result = _sut.BuildBudgetWaterfall(1000m, spending);

        // Assert
        Assert.Equal(2, result.Length);

        var income = result[0];
        Assert.Equal("Income", income.Label);
        Assert.Equal(1000m, income.Amount);
        Assert.True(income.IsPositive);
        Assert.Equal(1000m, income.RunningTotal);

        var net = result[^1];
        Assert.Equal(1000m, net.Amount);
        Assert.True(net.IsTotal);
    }

    /// <summary>
    /// Two categories produce 4 segments: Income → Groceries → Utilities → Net,
    /// each with correct Amount and running totals.
    /// </summary>
    [Fact]
    public void BuildBudgetWaterfall_TwoCategories_ProducesFourSegmentsInOrder()
    {
        // Arrange
        var spending = new[]
        {
            new CategorySpendingDto { CategoryName = "Groceries", Amount = new MoneyDto { Amount = -300m } },
            new CategorySpendingDto { CategoryName = "Utilities", Amount = new MoneyDto { Amount = -200m } },
        };

        // Act
        var result = _sut.BuildBudgetWaterfall(2000m, spending);

        // Assert
        Assert.Equal(4, result.Length);

        Assert.Equal("Income", result[0].Label);
        Assert.Equal(2000m, result[0].Amount);
        Assert.Equal(2000m, result[0].RunningTotal);

        Assert.Equal("Groceries", result[1].Label);
        Assert.Equal(-300m, result[1].Amount);
        Assert.Equal(1700m, result[1].RunningTotal);

        Assert.Equal("Utilities", result[2].Label);
        Assert.Equal(-200m, result[2].Amount);
        Assert.Equal(1500m, result[2].RunningTotal);

        Assert.True(result[3].IsTotal);
        Assert.Equal(1500m, result[3].Amount);
    }

    /// <summary>
    /// When spending exceeds income the Net segment Amount is negative.
    /// </summary>
    [Fact]
    public void BuildBudgetWaterfall_SpendingExceedsIncome_NetSegmentAmountIsNegative()
    {
        // Arrange
        var spending = new[]
        {
            new CategorySpendingDto { CategoryName = "Rent", Amount = new MoneyDto { Amount = -800m } },
        };

        // Act
        var result = _sut.BuildBudgetWaterfall(500m, spending);

        // Assert — net = 500 - 800 = -300
        var net = result[^1];
        Assert.True(net.IsTotal);
        Assert.Equal(-300m, net.Amount);
    }

    /// <summary>
    /// Spending segments are ordered by absolute amount descending (largest spend first).
    /// </summary>
    [Fact]
    public void BuildBudgetWaterfall_MultipleCategories_SpendingSegmentsOrderedByAbsoluteAmountDescending()
    {
        // Arrange — unordered input: Groceries(-500), Utilities(-200), Entertainment(-800)
        var spending = new[]
        {
            new CategorySpendingDto { CategoryName = "Groceries", Amount = new MoneyDto { Amount = -500m } },
            new CategorySpendingDto { CategoryName = "Utilities", Amount = new MoneyDto { Amount = -200m } },
            new CategorySpendingDto { CategoryName = "Entertainment", Amount = new MoneyDto { Amount = -800m } },
        };

        // Act
        var result = _sut.BuildBudgetWaterfall(3000m, spending);

        // Assert — middle segments (between Income and Net) ordered Entertainment, Groceries, Utilities
        var spendingSegments = result[1..^1];
        Assert.Equal("Entertainment", spendingSegments[0].Label);
        Assert.Equal("Groceries", spendingSegments[1].Label);
        Assert.Equal("Utilities", spendingSegments[2].Label);
    }

    /// <summary>
    /// The last segment is always the Net total, regardless of input order.
    /// </summary>
    [Fact]
    public void BuildBudgetWaterfall_AnyInput_LastSegmentIsAlwaysTotal()
    {
        // Arrange
        var spending = new[]
        {
            new CategorySpendingDto { CategoryName = "Misc", Amount = new MoneyDto { Amount = -100m } },
        };

        // Act
        var result = _sut.BuildBudgetWaterfall(600m, spending);

        // Assert — last segment is total; first (Income) is not total
        Assert.True(result[^1].IsTotal);
        Assert.False(result[0].IsTotal);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // BuildBalanceCandlesticks
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Empty input returns an empty array.
    /// </summary>
    [Fact]
    public void BuildBalanceCandlesticks_EmptyInput_ReturnsEmptyArray()
    {
        // Arrange
        var balances = Array.Empty<DailyBalanceDto>();

        // Act
        var result = _sut.BuildBalanceCandlesticks(balances);

        // Assert
        Assert.Empty(result);
    }

    /// <summary>
    /// Three daily balances in one month produce a single candlestick with correct OHLC values.
    /// Open = first day balance, High = max, Low = min, Close = last day balance.
    /// </summary>
    [Fact]
    public void BuildBalanceCandlesticks_SingleMonthThreeBalances_CorrectOhlcValues()
    {
        // Arrange — January 2026: starts at 1000, peaks at 1500, ends at 800
        var balances = new[]
        {
            new DailyBalanceDto(new DateOnly(2026, 1, 1), 1000m),
            new DailyBalanceDto(new DateOnly(2026, 1, 15), 1500m),
            new DailyBalanceDto(new DateOnly(2026, 1, 31), 800m),
        };

        // Act
        var result = _sut.BuildBalanceCandlesticks(balances);

        // Assert
        Assert.Single(result);
        var candle = result[0];
        Assert.Equal(1000m, candle.Open);
        Assert.Equal(1500m, candle.High);
        Assert.Equal(800m, candle.Low);
        Assert.Equal(800m, candle.Close);
    }

    /// <summary>
    /// Balances spanning two months produce two candlesticks ordered by date ascending.
    /// </summary>
    [Fact]
    public void BuildBalanceCandlesticks_TwoMonthsOfBalances_ProducesTwoCandlesticksAscending()
    {
        // Arrange — January and February 2026
        var balances = new[]
        {
            new DailyBalanceDto(new DateOnly(2026, 1, 1), 1000m),
            new DailyBalanceDto(new DateOnly(2026, 1, 31), 1200m),
            new DailyBalanceDto(new DateOnly(2026, 2, 1), 1200m),
            new DailyBalanceDto(new DateOnly(2026, 2, 28), 1400m),
        };

        // Act
        var result = _sut.BuildBalanceCandlesticks(balances);

        // Assert
        Assert.Equal(2, result.Length);
        Assert.True(result[0].Date < result[1].Date, "Candlesticks must be ordered by date ascending.");
    }

    /// <summary>
    /// When the closing balance is greater than or equal to the opening balance, IsBullish is true.
    /// </summary>
    [Fact]
    public void BuildBalanceCandlesticks_CloseGreaterThanOpen_IsBullishTrue()
    {
        // Arrange — open=1000, close=1200 (account balance grew)
        var balances = new[]
        {
            new DailyBalanceDto(new DateOnly(2026, 1, 1), 1000m),
            new DailyBalanceDto(new DateOnly(2026, 1, 31), 1200m),
        };

        // Act
        var result = _sut.BuildBalanceCandlesticks(balances);

        // Assert
        Assert.Single(result);
        Assert.True(result[0].IsBullish);
    }

    /// <summary>
    /// When the closing balance is less than the opening balance, IsBullish is false.
    /// </summary>
    [Fact]
    public void BuildBalanceCandlesticks_CloseLessThanOpen_IsBullishFalse()
    {
        // Arrange — open=1000, close=900 (account balance shrank)
        var balances = new[]
        {
            new DailyBalanceDto(new DateOnly(2026, 1, 1), 1000m),
            new DailyBalanceDto(new DateOnly(2026, 1, 31), 900m),
        };

        // Act
        var result = _sut.BuildBalanceCandlesticks(balances);

        // Assert
        Assert.Single(result);
        Assert.False(result[0].IsBullish);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // BuildCategoryDistributions
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Empty transaction list returns an empty BoxPlotSummary array.
    /// </summary>
    [Fact]
    public void BuildCategoryDistributions_EmptyInput_ReturnsEmptyArray()
    {
        // Arrange
        var transactions = Array.Empty<TransactionDto>();

        // Act
        var result = _sut.BuildCategoryDistributions(transactions);

        // Assert
        Assert.Empty(result);
    }

    /// <summary>
    /// Five expense transactions for a single category produce correct quartile values.
    /// Absolute values sorted: [100, 200, 300, 400, 500].
    /// Using the hinge (Tukey) method: Q1 = median([100,200]) = 150, Q3 = median([400,500]) = 450.
    /// No outliers because all values are within (Q1−1.5·IQR, Q3+1.5·IQR) = (−300, 900).
    /// </summary>
    [Fact]
    public void BuildCategoryDistributions_FiveExpensesOneCategoryNoOutliers_CorrectQuartiles()
    {
        // Arrange — amounts deliberately un-sorted to verify the service sorts internally
        var reference = new DateOnly(2026, 1, 31);
        var transactions = new[]
        {
            MakeTransaction("Groceries", -300m, reference),
            MakeTransaction("Groceries", -100m, reference),
            MakeTransaction("Groceries", -500m, reference),
            MakeTransaction("Groceries", -200m, reference),
            MakeTransaction("Groceries", -400m, reference),
        };

        // Act
        var result = _sut.BuildCategoryDistributions(transactions, 12);

        // Assert
        Assert.Single(result);
        var summary = result[0];
        Assert.Equal("Groceries", summary.CategoryName);
        Assert.Equal(100m, summary.Minimum);
        Assert.Equal(150m, summary.Q1);
        Assert.Equal(300m, summary.Median);
        Assert.Equal(450m, summary.Q3);
        Assert.Equal(500m, summary.Maximum);
        Assert.Empty(summary.Outliers);
    }

    /// <summary>
    /// A value far above the upper fence (Q3 + 1.5·IQR) is classified as an outlier.
    /// The whisker maximum (Maximum) is the highest non-outlier value.
    /// Input (abs): [100, 200, 300, 400, 500, 2000].
    /// Q1=200, Q3=500, IQR=300, upper fence=950 → 2000 is an outlier; Maximum=500.
    /// </summary>
    [Fact]
    public void BuildCategoryDistributions_ValueFarAboveUpperFence_ClassifiedAsOutlier()
    {
        // Arrange
        var reference = new DateOnly(2026, 1, 31);
        var transactions = new[]
        {
            MakeTransaction("Dining", -100m, reference),
            MakeTransaction("Dining", -200m, reference),
            MakeTransaction("Dining", -300m, reference),
            MakeTransaction("Dining", -400m, reference),
            MakeTransaction("Dining", -500m, reference),
            MakeTransaction("Dining", -2000m, reference), // extreme outlier
        };

        // Act
        var result = _sut.BuildCategoryDistributions(transactions, 12);

        // Assert
        Assert.Single(result);
        var summary = result[0];
        Assert.Single(summary.Outliers);
        Assert.Equal(2000m, summary.Outliers[0]);
        Assert.Equal(500m, summary.Maximum); // whisker stops at 500; outlier excluded from Max
    }

    /// <summary>
    /// Transactions in multiple categories produce one BoxPlotSummary per category.
    /// </summary>
    [Fact]
    public void BuildCategoryDistributions_TwoCategories_ProducesSeparateSummaryPerCategory()
    {
        // Arrange
        var reference = new DateOnly(2026, 1, 31);
        var transactions = new[]
        {
            MakeTransaction("Groceries", -100m, reference),
            MakeTransaction("Groceries", -200m, reference),
            MakeTransaction("Groceries", -300m, reference),
            MakeTransaction("Utilities", -50m, reference),
            MakeTransaction("Utilities", -75m, reference),
            MakeTransaction("Utilities", -90m, reference),
        };

        // Act
        var result = _sut.BuildCategoryDistributions(transactions, 12);

        // Assert — two summaries, one per category
        Assert.Equal(2, result.Length);
        var names = result.Select(s => s.CategoryName).ToHashSet(StringComparer.Ordinal);
        Assert.Contains("Groceries", names);
        Assert.Contains("Utilities", names);
    }

    /// <summary>
    /// Transactions older than monthsBack months are excluded from the distribution calculations.
    /// Reference date is derived from the most recent transaction in the dataset.
    /// </summary>
    [Fact]
    public void BuildCategoryDistributions_OlderTransactions_ExcludedByMonthsBack()
    {
        // Arrange — most recent = Jan 31 2026; monthsBack=3 means include Oct 31 2025 onward.
        // Transactions 7 months ago are outside the window and must NOT influence statistics.
        var recentDate = new DateOnly(2026, 1, 31);
        var oldDate = recentDate.AddMonths(-7); // July 31, 2025 — outside the 3-month window

        var transactions = new[]
        {
            // Recent transactions (within last 3 months) — max abs value = 300
            MakeTransaction("Groceries", -100m, recentDate),
            MakeTransaction("Groceries", -200m, recentDate.AddMonths(-1)),
            MakeTransaction("Groceries", -300m, recentDate.AddMonths(-2)),

            // Old transactions (7 months ago) — extreme values that must be ignored
            MakeTransaction("Groceries", -5000m, oldDate),
            MakeTransaction("Groceries", -5000m, oldDate.AddDays(1)),
        };

        // Act
        var result = _sut.BuildCategoryDistributions(transactions, 3);

        // Assert — old transactions must NOT inflate the statistics
        Assert.Single(result);
        var summary = result[0];
        Assert.True(
            summary.Maximum <= 300m,
            $"Expected Maximum ≤ 300 but was {summary.Maximum}. Old transactions must be excluded.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────
    private static TransactionDto MakeTransaction(string categoryName, decimal amount, DateOnly date) =>
        new()
        {
            Date = date,
            Amount = new MoneyDto { Amount = amount },
            CategoryName = categoryName,
        };
}
