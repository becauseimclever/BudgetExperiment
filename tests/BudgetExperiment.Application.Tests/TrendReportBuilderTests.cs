// <copyright file="TrendReportBuilderTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain.Settings;
using Moq;

namespace BudgetExperiment.Application.Tests;

/// <summary>
/// Unit tests for TrendReportBuilder.
/// </summary>
public class TrendReportBuilderTests
{
    private static readonly Mock<ICurrencyProvider> DefaultCurrencyProvider = CreateCurrencyProviderMock("USD");

    [Fact]
    public async Task GetSpendingTrendsAsync_NoTransactions_ReturnsEmptyMonthlyData()
    {
        var transactionRepo = new Mock<ITransactionRepository>();
        transactionRepo.Setup(r => r.GetByDateRangeAsync(
            It.IsAny<DateOnly>(),
            It.IsAny<DateOnly>(),
            null,
            default)).ReturnsAsync(new List<Transaction>());

        var builder = new TrendReportBuilder(transactionRepo.Object, DefaultCurrencyProvider.Object);

        var result = await builder.GetSpendingTrendsAsync(3, 2026, 3);

        Assert.NotNull(result);
        Assert.Equal(3, result.MonthlyData.Count);
        Assert.All(result.MonthlyData, m => Assert.Equal(0, m.TransactionCount));
        Assert.Equal(0m, result.AverageMonthlySpending.Amount);
        Assert.Equal("stable", result.TrendDirection);
    }

    [Fact]
    public async Task GetSpendingTrendsAsync_WithTransactions_CalculatesMonthlyTotals()
    {
        var transactions = new List<Transaction>
        {
            CreateTransaction(new DateOnly(2026, 1, 10), -100m, null),
            CreateTransaction(new DateOnly(2026, 1, 20), 500m, null),
            CreateTransaction(new DateOnly(2026, 2, 5), -200m, null),
            CreateTransaction(new DateOnly(2026, 3, 15), -150m, null),
        };

        var transactionRepo = new Mock<ITransactionRepository>();
        transactionRepo.Setup(r => r.GetByDateRangeAsync(
            It.IsAny<DateOnly>(),
            It.IsAny<DateOnly>(),
            null,
            default)).ReturnsAsync(transactions);

        var builder = new TrendReportBuilder(transactionRepo.Object, DefaultCurrencyProvider.Object);

        var result = await builder.GetSpendingTrendsAsync(3, 2026, 3);

        Assert.Equal(3, result.MonthlyData.Count);

        // January
        Assert.Equal(100m, result.MonthlyData[0].TotalSpending.Amount);
        Assert.Equal(500m, result.MonthlyData[0].TotalIncome.Amount);
        Assert.Equal(2, result.MonthlyData[0].TransactionCount);

        // February
        Assert.Equal(200m, result.MonthlyData[1].TotalSpending.Amount);
        Assert.Equal(0m, result.MonthlyData[1].TotalIncome.Amount);

        // March
        Assert.Equal(150m, result.MonthlyData[2].TotalSpending.Amount);
    }

    [Fact]
    public async Task GetSpendingTrendsAsync_ExcludesTransfers()
    {
        var transferId = Guid.NewGuid();
        var transactions = new List<Transaction>
        {
            CreateTransaction(new DateOnly(2026, 1, 10), -100m, null),
            CreateTransfer(new DateOnly(2026, 1, 15), -500m, transferId),
        };

        var transactionRepo = new Mock<ITransactionRepository>();
        transactionRepo.Setup(r => r.GetByDateRangeAsync(
            It.IsAny<DateOnly>(),
            It.IsAny<DateOnly>(),
            null,
            default)).ReturnsAsync(transactions);

        var builder = new TrendReportBuilder(transactionRepo.Object, DefaultCurrencyProvider.Object);

        var result = await builder.GetSpendingTrendsAsync(1, 2026, 1);

        Assert.Equal(100m, result.MonthlyData[0].TotalSpending.Amount);
        Assert.Equal(1, result.MonthlyData[0].TransactionCount);
    }

    [Fact]
    public async Task GetSpendingTrendsAsync_WithCategoryFilter_FiltersTransactions()
    {
        var categoryId = Guid.NewGuid();
        var transactions = new List<Transaction>
        {
            CreateTransaction(new DateOnly(2026, 1, 10), -100m, categoryId),
            CreateTransaction(new DateOnly(2026, 1, 15), -200m, null),
            CreateTransaction(new DateOnly(2026, 1, 20), -50m, categoryId),
        };

        var transactionRepo = new Mock<ITransactionRepository>();
        transactionRepo.Setup(r => r.GetByDateRangeAsync(
            It.IsAny<DateOnly>(),
            It.IsAny<DateOnly>(),
            null,
            default)).ReturnsAsync(transactions);

        var builder = new TrendReportBuilder(transactionRepo.Object, DefaultCurrencyProvider.Object);

        var result = await builder.GetSpendingTrendsAsync(1, 2026, 1, categoryId);

        Assert.Equal(150m, result.MonthlyData[0].TotalSpending.Amount);
        Assert.Equal(2, result.MonthlyData[0].TransactionCount);
    }

    [Fact]
    public async Task GetSpendingTrendsAsync_ClampMonthsTo24()
    {
        var transactionRepo = new Mock<ITransactionRepository>();
        transactionRepo.Setup(r => r.GetByDateRangeAsync(
            It.IsAny<DateOnly>(),
            It.IsAny<DateOnly>(),
            null,
            default)).ReturnsAsync(new List<Transaction>());

        var builder = new TrendReportBuilder(transactionRepo.Object, DefaultCurrencyProvider.Object);

        var result = await builder.GetSpendingTrendsAsync(100, 2026, 3);

        Assert.Equal(24, result.MonthlyData.Count);
    }

    [Fact]
    public async Task GetSpendingTrendsAsync_IncreasingTrend_DetectsCorrectly()
    {
        var transactions = new List<Transaction>
        {
            CreateTransaction(new DateOnly(2026, 1, 10), -100m, null),
            CreateTransaction(new DateOnly(2026, 2, 10), -100m, null),
            CreateTransaction(new DateOnly(2026, 3, 10), -200m, null),
            CreateTransaction(new DateOnly(2026, 4, 10), -200m, null),
        };

        var transactionRepo = new Mock<ITransactionRepository>();
        transactionRepo.Setup(r => r.GetByDateRangeAsync(
            It.IsAny<DateOnly>(),
            It.IsAny<DateOnly>(),
            null,
            default)).ReturnsAsync(transactions);

        var builder = new TrendReportBuilder(transactionRepo.Object, DefaultCurrencyProvider.Object);

        var result = await builder.GetSpendingTrendsAsync(4, 2026, 4);

        Assert.Equal("increasing", result.TrendDirection);
    }

    [Fact]
    public async Task GetSpendingTrendsAsync_DecreasingTrend_DetectsCorrectly()
    {
        var transactions = new List<Transaction>
        {
            CreateTransaction(new DateOnly(2026, 1, 10), -200m, null),
            CreateTransaction(new DateOnly(2026, 2, 10), -200m, null),
            CreateTransaction(new DateOnly(2026, 3, 10), -50m, null),
            CreateTransaction(new DateOnly(2026, 4, 10), -50m, null),
        };

        var transactionRepo = new Mock<ITransactionRepository>();
        transactionRepo.Setup(r => r.GetByDateRangeAsync(
            It.IsAny<DateOnly>(),
            It.IsAny<DateOnly>(),
            null,
            default)).ReturnsAsync(transactions);

        var builder = new TrendReportBuilder(transactionRepo.Object, DefaultCurrencyProvider.Object);

        var result = await builder.GetSpendingTrendsAsync(4, 2026, 4);

        Assert.Equal("decreasing", result.TrendDirection);
    }

    [Fact]
    public async Task GetSpendingTrendsAsync_UsesCurrency()
    {
        var transactionRepo = new Mock<ITransactionRepository>();
        transactionRepo.Setup(r => r.GetByDateRangeAsync(
            It.IsAny<DateOnly>(),
            It.IsAny<DateOnly>(),
            null,
            default)).ReturnsAsync(new List<Transaction>());

        var currencyProvider = CreateCurrencyProviderMock("EUR");
        var builder = new TrendReportBuilder(transactionRepo.Object, currencyProvider.Object);

        var result = await builder.GetSpendingTrendsAsync(1, 2026, 1);

        Assert.Equal("EUR", result.AverageMonthlySpending.Currency);
        Assert.Equal("EUR", result.AverageMonthlyIncome.Currency);
    }

    private static Transaction CreateTransaction(DateOnly date, decimal amount, Guid? categoryId)
    {
        var accountId = Guid.NewGuid();
        return Transaction.Create(
            accountId,
            MoneyValue.Create("USD", amount),
            date,
            "Test transaction",
            categoryId);
    }

    private static Transaction CreateTransfer(DateOnly date, decimal amount, Guid transferId)
    {
        var accountId = Guid.NewGuid();
        return Transaction.CreateTransfer(
            accountId,
            MoneyValue.Create("USD", amount),
            date,
            "Transfer",
            transferId,
            TransferDirection.Source);
    }

    private static Mock<ICurrencyProvider> CreateCurrencyProviderMock(string currency)
    {
        var mock = new Mock<ICurrencyProvider>();
        mock.Setup(x => x.GetCurrencyAsync(It.IsAny<CancellationToken>())).ReturnsAsync(currency);
        return mock;
    }
}
