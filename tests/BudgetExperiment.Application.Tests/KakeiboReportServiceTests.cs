// <copyright file="KakeiboReportServiceTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Reflection;

using BudgetExperiment.Contracts.Dtos;

using Moq;

namespace BudgetExperiment.Application.Tests.Reports;

/// <summary>
/// Unit tests for <see cref="KakeiboReportService"/>.
/// Verifies bucket mapping, override precedence, income/transfer exclusion,
/// aggregation correctness, and date-boundary inclusion.
///
/// NOTE: <see cref="Transaction.Category"/> is a private-set EF Core navigation property.
/// Tests set it via reflection to simulate what EF Core does when loading entities.
/// </summary>
public sealed class KakeiboReportServiceTests
{
    private static readonly Guid TestAccountId = Guid.NewGuid();

    private readonly Mock<ITransactionRepository> _transactionRepo = new();

    [Fact]
    public async Task GetKakeiboSummaryAsync_FromGreaterThanTo_ThrowsArgumentException()
    {
        // Arrange
        var from = new DateOnly(2026, 3, 31);
        var to = new DateOnly(2026, 3, 1);
        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => service.GetKakeiboSummaryAsync(from, to, null, default));
    }

    [Fact]
    public async Task GetKakeiboSummaryAsync_SingleEssentialsExpense_MapsToEssentialsBucket()
    {
        // Arrange
        var from = new DateOnly(2026, 3, 1);
        var to = new DateOnly(2026, 3, 31);
        var category = CreateExpenseCategory(KakeiboCategory.Essentials);
        SetupTransactionRepo(from, to, CreateExpenseTransaction(new DateOnly(2026, 3, 15), 75m, category));
        var service = CreateService();

        // Act
        var result = await service.GetKakeiboSummaryAsync(from, to, null, default);

        // Assert
        Assert.Equal(75m, result.MonthlyTotals[KakeiboCategory.Essentials]);
        Assert.Equal(0m, result.MonthlyTotals[KakeiboCategory.Wants]);
        Assert.Equal(0m, result.MonthlyTotals[KakeiboCategory.Culture]);
        Assert.Equal(0m, result.MonthlyTotals[KakeiboCategory.Unexpected]);
    }

    [Fact]
    public async Task GetKakeiboSummaryAsync_SingleWantsExpense_MapsToWantsBucket()
    {
        // Arrange
        var from = new DateOnly(2026, 3, 1);
        var to = new DateOnly(2026, 3, 31);
        var category = CreateExpenseCategory(KakeiboCategory.Wants);
        SetupTransactionRepo(from, to, CreateExpenseTransaction(new DateOnly(2026, 3, 10), 40m, category));
        var service = CreateService();

        // Act
        var result = await service.GetKakeiboSummaryAsync(from, to, null, default);

        // Assert
        Assert.Equal(0m, result.MonthlyTotals[KakeiboCategory.Essentials]);
        Assert.Equal(40m, result.MonthlyTotals[KakeiboCategory.Wants]);
        Assert.Equal(0m, result.MonthlyTotals[KakeiboCategory.Culture]);
        Assert.Equal(0m, result.MonthlyTotals[KakeiboCategory.Unexpected]);
    }

    [Fact]
    public async Task GetKakeiboSummaryAsync_SingleCultureExpense_MapsToCultureBucket()
    {
        // Arrange
        var from = new DateOnly(2026, 3, 1);
        var to = new DateOnly(2026, 3, 31);
        var category = CreateExpenseCategory(KakeiboCategory.Culture);
        SetupTransactionRepo(from, to, CreateExpenseTransaction(new DateOnly(2026, 3, 20), 30m, category));
        var service = CreateService();

        // Act
        var result = await service.GetKakeiboSummaryAsync(from, to, null, default);

        // Assert
        Assert.Equal(0m, result.MonthlyTotals[KakeiboCategory.Essentials]);
        Assert.Equal(0m, result.MonthlyTotals[KakeiboCategory.Wants]);
        Assert.Equal(30m, result.MonthlyTotals[KakeiboCategory.Culture]);
        Assert.Equal(0m, result.MonthlyTotals[KakeiboCategory.Unexpected]);
    }

    [Fact]
    public async Task GetKakeiboSummaryAsync_SingleUnexpectedExpense_MapsToUnexpectedBucket()
    {
        // Arrange
        var from = new DateOnly(2026, 3, 1);
        var to = new DateOnly(2026, 3, 31);
        var category = CreateExpenseCategory(KakeiboCategory.Unexpected);
        SetupTransactionRepo(from, to, CreateExpenseTransaction(new DateOnly(2026, 3, 25), 150m, category));
        var service = CreateService();

        // Act
        var result = await service.GetKakeiboSummaryAsync(from, to, null, default);

        // Assert
        Assert.Equal(0m, result.MonthlyTotals[KakeiboCategory.Essentials]);
        Assert.Equal(0m, result.MonthlyTotals[KakeiboCategory.Wants]);
        Assert.Equal(0m, result.MonthlyTotals[KakeiboCategory.Culture]);
        Assert.Equal(150m, result.MonthlyTotals[KakeiboCategory.Unexpected]);
    }

    [Fact]
    public async Task GetKakeiboSummaryAsync_KakeiboOverride_TakesPrecedenceOverCategoryDefault()
    {
        // Arrange — category says Essentials, override says Wants
        var from = new DateOnly(2026, 3, 1);
        var to = new DateOnly(2026, 3, 31);
        var category = CreateExpenseCategory(KakeiboCategory.Essentials);
        var transaction = CreateExpenseTransaction(new DateOnly(2026, 3, 10), 50m, category);
        transaction.SetKakeiboOverride(KakeiboCategory.Wants);
        SetupTransactionRepo(from, to, transaction);
        var service = CreateService();

        // Act
        var result = await service.GetKakeiboSummaryAsync(from, to, null, default);

        // Assert — override wins: in Wants, NOT in Essentials
        Assert.Equal(0m, result.MonthlyTotals[KakeiboCategory.Essentials]);
        Assert.Equal(50m, result.MonthlyTotals[KakeiboCategory.Wants]);
    }

    [Fact]
    public async Task GetKakeiboSummaryAsync_IncomeTransaction_IsExcludedFromTotals()
    {
        // Arrange — income category transaction must not appear in any bucket
        var from = new DateOnly(2026, 3, 1);
        var to = new DateOnly(2026, 3, 31);
        var incomeCategory = BudgetCategory.Create("Salary", CategoryType.Income);
        var incomeTransaction = CreateTransactionWithCategory(new DateOnly(2026, 3, 5), 3000m, incomeCategory);
        SetupTransactionRepo(from, to, incomeTransaction);
        var service = CreateService();

        // Act
        var result = await service.GetKakeiboSummaryAsync(from, to, null, default);

        // Assert — all buckets are zero; income excluded
        Assert.All(result.MonthlyTotals.Values, v => Assert.Equal(0m, v));
        Assert.Empty(result.DailyTotals);
        Assert.Empty(result.WeeklyTotals);
    }

    [Fact]
    public async Task GetKakeiboSummaryAsync_TransferTransaction_IsExcludedFromTotals()
    {
        // Arrange — transfer has no expense category; Category?.Type != Expense so filtered out
        var from = new DateOnly(2026, 3, 1);
        var to = new DateOnly(2026, 3, 31);
        var transferTx = TransactionFactory.CreateTransfer(
            TestAccountId,
            MoneyValue.Create("USD", -500m),
            new DateOnly(2026, 3, 10),
            "Transfer Out",
            Guid.NewGuid(),
            TransferDirection.Source);
        SetupTransactionRepo(from, to, transferTx);
        var service = CreateService();

        // Act
        var result = await service.GetKakeiboSummaryAsync(from, to, null, default);

        // Assert
        Assert.All(result.MonthlyTotals.Values, v => Assert.Equal(0m, v));
        Assert.Empty(result.DailyTotals);
    }

    [Fact]
    public async Task GetKakeiboSummaryAsync_EmptyDateRange_AllBucketsReturnZero()
    {
        // Arrange
        var from = new DateOnly(2026, 12, 1);
        var to = new DateOnly(2026, 12, 31);
        _transactionRepo
            .Setup(r => r.GetByDateRangeAsync(from, to, It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Transaction>());
        var service = CreateService();

        // Act
        var result = await service.GetKakeiboSummaryAsync(from, to, null, default);

        // Assert
        Assert.Equal(4, result.MonthlyTotals.Count);
        Assert.All(result.MonthlyTotals.Values, v => Assert.Equal(0m, v));
        Assert.Empty(result.DailyTotals);
        Assert.Empty(result.WeeklyTotals);
    }

    [Fact]
    public async Task GetKakeiboSummaryAsync_ZeroBuckets_StillReturnedNotOmitted()
    {
        // Arrange — only Essentials has spend; other buckets must still appear at zero
        var from = new DateOnly(2026, 3, 1);
        var to = new DateOnly(2026, 3, 31);
        var category = CreateExpenseCategory(KakeiboCategory.Essentials);
        SetupTransactionRepo(from, to, CreateExpenseTransaction(new DateOnly(2026, 3, 15), 100m, category));
        var service = CreateService();

        // Act
        var result = await service.GetKakeiboSummaryAsync(from, to, null, default);

        // Assert — all four buckets present; zero-spend buckets not omitted
        Assert.True(result.MonthlyTotals.ContainsKey(KakeiboCategory.Essentials));
        Assert.True(result.MonthlyTotals.ContainsKey(KakeiboCategory.Wants));
        Assert.True(result.MonthlyTotals.ContainsKey(KakeiboCategory.Culture));
        Assert.True(result.MonthlyTotals.ContainsKey(KakeiboCategory.Unexpected));
        Assert.Equal(0m, result.MonthlyTotals[KakeiboCategory.Culture]);
        Assert.Equal(0m, result.MonthlyTotals[KakeiboCategory.Unexpected]);
    }

    [Fact]
    public async Task GetKakeiboSummaryAsync_WeeklyTotals_SumToMonthlyTotal()
    {
        // Arrange — transactions across multiple ISO weeks
        var from = new DateOnly(2026, 3, 1);
        var to = new DateOnly(2026, 3, 31);
        var category = CreateExpenseCategory(KakeiboCategory.Wants);
        SetupTransactionRepo(
            from,
            to,
            new[]
            {
                CreateExpenseTransaction(new DateOnly(2026, 3, 2), 20m, category),
                CreateExpenseTransaction(new DateOnly(2026, 3, 5), 30m, category),
                CreateExpenseTransaction(new DateOnly(2026, 3, 10), 50m, category),
                CreateExpenseTransaction(new DateOnly(2026, 3, 17), 100m, category),
            });
        var service = CreateService();

        // Act
        var result = await service.GetKakeiboSummaryAsync(from, to, null, default);

        // Assert — sum of all weekly Wants totals == monthly Wants total
        var weeklyWantsSum = result.WeeklyTotals.Sum(w => w.BucketTotals[KakeiboCategory.Wants]);
        Assert.Equal(result.MonthlyTotals[KakeiboCategory.Wants], weeklyWantsSum);
    }

    [Fact]
    public async Task GetKakeiboSummaryAsync_DailyTotals_SumToMonthlyTotal()
    {
        // Arrange
        var from = new DateOnly(2026, 3, 1);
        var to = new DateOnly(2026, 3, 31);
        var category = CreateExpenseCategory(KakeiboCategory.Essentials);
        SetupTransactionRepo(
            from,
            to,
            new[]
            {
                CreateExpenseTransaction(new DateOnly(2026, 3, 1), 100m, category),
                CreateExpenseTransaction(new DateOnly(2026, 3, 5), 50m, category),
                CreateExpenseTransaction(new DateOnly(2026, 3, 20), 75m, category),
            });
        var service = CreateService();

        // Act
        var result = await service.GetKakeiboSummaryAsync(from, to, null, default);

        // Assert — sum of all daily Essentials totals == monthly Essentials total
        var dailyEssentialsSum = result.DailyTotals.Sum(d => d.BucketTotals[KakeiboCategory.Essentials]);
        Assert.Equal(result.MonthlyTotals[KakeiboCategory.Essentials], dailyEssentialsSum);
    }

    [Fact]
    public async Task GetKakeiboSummaryAsync_MultipleTransactionsSameDay_AggregatedCorrectly()
    {
        // Arrange — three transactions on same day in two buckets
        var from = new DateOnly(2026, 3, 1);
        var to = new DateOnly(2026, 3, 31);
        var date = new DateOnly(2026, 3, 15);
        var essentialsCategory = CreateExpenseCategory(KakeiboCategory.Essentials);
        var wantsCategory = CreateExpenseCategory(KakeiboCategory.Wants);
        SetupTransactionRepo(
            from,
            to,
            new[]
            {
                CreateExpenseTransaction(date, 80m, essentialsCategory),
                CreateExpenseTransaction(date, 20m, essentialsCategory),
                CreateExpenseTransaction(date, 45m, wantsCategory),
            });
        var service = CreateService();

        // Act
        var result = await service.GetKakeiboSummaryAsync(from, to, null, default);

        // Assert — single DailyTotals entry; Essentials=100, Wants=45
        Assert.Single(result.DailyTotals);
        var dayEntry = result.DailyTotals[0];
        Assert.Equal(date, dayEntry.Date);
        Assert.Equal(100m, dayEntry.BucketTotals[KakeiboCategory.Essentials]);
        Assert.Equal(45m, dayEntry.BucketTotals[KakeiboCategory.Wants]);
    }

    [Fact]
    public async Task GetKakeiboSummaryAsync_TransactionsOnBoundaryDates_Included()
    {
        // Arrange — one transaction exactly on 'from', one exactly on 'to'
        var from = new DateOnly(2026, 3, 1);
        var to = new DateOnly(2026, 3, 31);
        var category = CreateExpenseCategory(KakeiboCategory.Culture);
        SetupTransactionRepo(
            from,
            to,
            new[]
            {
                CreateExpenseTransaction(from, 11m, category),
                CreateExpenseTransaction(to, 22m, category),
            });
        var service = CreateService();

        // Act
        var result = await service.GetKakeiboSummaryAsync(from, to, null, default);

        // Assert — both boundary transactions are included
        Assert.Equal(33m, result.MonthlyTotals[KakeiboCategory.Culture]);
        Assert.Equal(2, result.DailyTotals.Count);
    }

    private static BudgetCategory CreateExpenseCategory(KakeiboCategory kakeibo)
    {
        var category = BudgetCategory.Create($"Test-{kakeibo}", CategoryType.Expense);
        category.SetKakeiboCategory(kakeibo);
        return category;
    }

    private static Transaction CreateExpenseTransaction(DateOnly date, decimal amount, BudgetCategory category)
    {
        var tx = TransactionFactory.Create(
            TestAccountId,
            MoneyValue.Create("USD", -Math.Abs(amount)),
            date,
            $"Expense-{category.Name}",
            category.Id);
        SetCategoryNavigation(tx, category);
        return tx;
    }

    private static Transaction CreateTransactionWithCategory(DateOnly date, decimal amount, BudgetCategory category)
    {
        var tx = TransactionFactory.Create(
            TestAccountId,
            MoneyValue.Create("USD", amount),
            date,
            $"Tx-{category.Name}",
            category.Id);
        SetCategoryNavigation(tx, category);
        return tx;
    }

    private static void SetCategoryNavigation(Transaction transaction, BudgetCategory category)
    {
        var prop = typeof(Transaction).GetProperty("Category", BindingFlags.Public | BindingFlags.Instance);
        prop?.SetValue(transaction, category);
    }

    private KakeiboReportService CreateService() => new(_transactionRepo.Object);

    private void SetupTransactionRepo(DateOnly from, DateOnly to, params Transaction[] transactions)
    {
        var list = transactions.ToList();
        _transactionRepo
            .Setup(r => r.GetByDateRangeAsync(from, to, It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(list);
    }

    private void SetupTransactionRepo(DateOnly from, DateOnly to, IEnumerable<Transaction> transactions)
    {
        var list = transactions.ToList();
        _transactionRepo
            .Setup(r => r.GetByDateRangeAsync(from, to, It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(list);
    }
}
