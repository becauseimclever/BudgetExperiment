// <copyright file="ReportServiceLocationTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain.Settings;

using Moq;

namespace BudgetExperiment.Application.Tests;

/// <summary>
/// Unit tests for ReportService location spending aggregation.
/// </summary>
public class ReportServiceLocationTests
{
    private static readonly Mock<ICurrencyProvider> DefaultCurrencyProvider = CreateCurrencyProviderMock("USD");

    /// <summary>
    /// Returns an empty report when no transactions exist in the date range.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetSpendingByLocation_NoTransactions_ReturnsEmptyReport()
    {
        // Arrange
        var startDate = new DateOnly(2026, 1, 1);
        var endDate = new DateOnly(2026, 1, 31);
        var transactionRepo = new Mock<ITransactionRepository>();
        var categoryRepo = new Mock<IBudgetCategoryRepository>();
        transactionRepo.Setup(r => r.GetByDateRangeAsync(startDate, endDate, null, default))
            .ReturnsAsync(new List<Transaction>());
        var service = new ReportService(transactionRepo.Object, categoryRepo.Object, DefaultCurrencyProvider.Object);

        // Act
        var result = await service.GetSpendingByLocationAsync(startDate, endDate);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(startDate, result.StartDate);
        Assert.Equal(endDate, result.EndDate);
        Assert.Equal(0m, result.TotalSpending);
        Assert.Equal(0, result.TotalTransactions);
        Assert.Equal(0, result.TransactionsWithLocation);
        Assert.Empty(result.Regions);
    }

    /// <summary>
    /// Groups transactions by state/region and returns region-level aggregation.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetSpendingByLocation_GroupsByState_ReturnsRegions()
    {
        // Arrange
        var startDate = new DateOnly(2026, 1, 1);
        var endDate = new DateOnly(2026, 1, 31);

        var t1 = CreateTransactionWithLocation(new DateOnly(2026, 1, 5), -50m, "Seattle", "WA", "US");
        var t2 = CreateTransactionWithLocation(new DateOnly(2026, 1, 10), -30m, "Spokane", "WA", "US");
        var t3 = CreateTransactionWithLocation(new DateOnly(2026, 1, 15), -100m, "Portland", "OR", "US");

        var transactionRepo = new Mock<ITransactionRepository>();
        var categoryRepo = new Mock<IBudgetCategoryRepository>();
        transactionRepo.Setup(r => r.GetByDateRangeAsync(startDate, endDate, null, default))
            .ReturnsAsync(new List<Transaction> { t1, t2, t3 });
        var service = new ReportService(transactionRepo.Object, categoryRepo.Object, DefaultCurrencyProvider.Object);

        // Act
        var result = await service.GetSpendingByLocationAsync(startDate, endDate);

        // Assert
        Assert.Equal(2, result.Regions.Count);
        Assert.Equal(3, result.TransactionsWithLocation);
        Assert.Equal(180m, result.TotalSpending);

        var wa = result.Regions.First(r => r.RegionCode == "US-WA");
        Assert.Equal("WA", wa.RegionName);
        Assert.Equal("US", wa.Country);
        Assert.Equal(80m, wa.TotalSpending);
        Assert.Equal(2, wa.TransactionCount);

        var or = result.Regions.First(r => r.RegionCode == "US-OR");
        Assert.Equal("OR", or.RegionName);
        Assert.Equal("US", or.Country);
        Assert.Equal(100m, or.TotalSpending);
        Assert.Equal(1, or.TransactionCount);
    }

    /// <summary>
    /// Calculates percentage of total spending for each region.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetSpendingByLocation_CalculatesPercentages()
    {
        // Arrange
        var startDate = new DateOnly(2026, 1, 1);
        var endDate = new DateOnly(2026, 1, 31);

        var t1 = CreateTransactionWithLocation(new DateOnly(2026, 1, 5), -25m, "Seattle", "WA", "US");
        var t2 = CreateTransactionWithLocation(new DateOnly(2026, 1, 10), -75m, "Portland", "OR", "US");

        var transactionRepo = new Mock<ITransactionRepository>();
        var categoryRepo = new Mock<IBudgetCategoryRepository>();
        transactionRepo.Setup(r => r.GetByDateRangeAsync(startDate, endDate, null, default))
            .ReturnsAsync(new List<Transaction> { t1, t2 });
        var service = new ReportService(transactionRepo.Object, categoryRepo.Object, DefaultCurrencyProvider.Object);

        // Act
        var result = await service.GetSpendingByLocationAsync(startDate, endDate);

        // Assert
        var wa = result.Regions.First(r => r.RegionCode == "US-WA");
        Assert.Equal(25m, wa.Percentage);

        var or = result.Regions.First(r => r.RegionCode == "US-OR");
        Assert.Equal(75m, or.Percentage);
    }

    /// <summary>
    /// Excludes transfers from the location spending report.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetSpendingByLocation_ExcludesTransfers()
    {
        // Arrange
        var startDate = new DateOnly(2026, 1, 1);
        var endDate = new DateOnly(2026, 1, 31);

        var spending = CreateTransactionWithLocation(new DateOnly(2026, 1, 5), -50m, "Seattle", "WA", "US");
        var transfer = CreateTransferWithLocation(new DateOnly(2026, 1, 10), -100m, "Portland", "OR", "US");

        var transactionRepo = new Mock<ITransactionRepository>();
        var categoryRepo = new Mock<IBudgetCategoryRepository>();
        transactionRepo.Setup(r => r.GetByDateRangeAsync(startDate, endDate, null, default))
            .ReturnsAsync(new List<Transaction> { spending, transfer });
        var service = new ReportService(transactionRepo.Object, categoryRepo.Object, DefaultCurrencyProvider.Object);

        // Act
        var result = await service.GetSpendingByLocationAsync(startDate, endDate);

        // Assert
        Assert.Equal(50m, result.TotalSpending);
        Assert.Equal(1, result.TotalTransactions);
        Assert.Single(result.Regions);
    }

    /// <summary>
    /// Respects the date range by passing it to the repository.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetSpendingByLocation_RespectsDateRange()
    {
        // Arrange
        var startDate = new DateOnly(2026, 3, 1);
        var endDate = new DateOnly(2026, 3, 31);
        var accountId = Guid.NewGuid();

        var transactionRepo = new Mock<ITransactionRepository>();
        var categoryRepo = new Mock<IBudgetCategoryRepository>();
        transactionRepo.Setup(r => r.GetByDateRangeAsync(startDate, endDate, accountId, default))
            .ReturnsAsync(new List<Transaction>());
        var service = new ReportService(transactionRepo.Object, categoryRepo.Object, DefaultCurrencyProvider.Object);

        // Act
        var result = await service.GetSpendingByLocationAsync(startDate, endDate, accountId);

        // Assert
        Assert.NotNull(result);
        transactionRepo.Verify(r => r.GetByDateRangeAsync(startDate, endDate, accountId, default), Times.Once);
    }

    /// <summary>
    /// Includes city-level drill-down within each region.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetSpendingByLocation_IncludesCityDrillDown()
    {
        // Arrange
        var startDate = new DateOnly(2026, 1, 1);
        var endDate = new DateOnly(2026, 1, 31);

        var t1 = CreateTransactionWithLocation(new DateOnly(2026, 1, 5), -50m, "Seattle", "WA", "US");
        var t2 = CreateTransactionWithLocation(new DateOnly(2026, 1, 10), -30m, "Spokane", "WA", "US");
        var t3 = CreateTransactionWithLocation(new DateOnly(2026, 1, 15), -20m, "Seattle", "WA", "US");

        var transactionRepo = new Mock<ITransactionRepository>();
        var categoryRepo = new Mock<IBudgetCategoryRepository>();
        transactionRepo.Setup(r => r.GetByDateRangeAsync(startDate, endDate, null, default))
            .ReturnsAsync(new List<Transaction> { t1, t2, t3 });
        var service = new ReportService(transactionRepo.Object, categoryRepo.Object, DefaultCurrencyProvider.Object);

        // Act
        var result = await service.GetSpendingByLocationAsync(startDate, endDate);

        // Assert
        var wa = result.Regions.First(r => r.RegionCode == "US-WA");
        Assert.NotNull(wa.Cities);
        Assert.Equal(2, wa.Cities.Count);

        var seattle = wa.Cities.First(c => c.City == "Seattle");
        Assert.Equal(70m, seattle.TotalSpending);
        Assert.Equal(2, seattle.TransactionCount);
        Assert.Equal(70m, seattle.Percentage); // 70 of 100 region spending

        var spokane = wa.Cities.First(c => c.City == "Spokane");
        Assert.Equal(30m, spokane.TotalSpending);
        Assert.Equal(1, spokane.TransactionCount);
        Assert.Equal(30m, spokane.Percentage);
    }

    /// <summary>
    /// Skips transactions without location data (they still count in totals but not regions).
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetSpendingByLocation_SkipsTransactionsWithoutLocation()
    {
        // Arrange
        var startDate = new DateOnly(2026, 1, 1);
        var endDate = new DateOnly(2026, 1, 31);

        var withLocation = CreateTransactionWithLocation(new DateOnly(2026, 1, 5), -50m, "Seattle", "WA", "US");
        var withoutLocation = CreateTransaction(new DateOnly(2026, 1, 10), -30m);

        var transactionRepo = new Mock<ITransactionRepository>();
        var categoryRepo = new Mock<IBudgetCategoryRepository>();
        transactionRepo.Setup(r => r.GetByDateRangeAsync(startDate, endDate, null, default))
            .ReturnsAsync(new List<Transaction> { withLocation, withoutLocation });
        var service = new ReportService(transactionRepo.Object, categoryRepo.Object, DefaultCurrencyProvider.Object);

        // Act
        var result = await service.GetSpendingByLocationAsync(startDate, endDate);

        // Assert
        Assert.Equal(80m, result.TotalSpending);
        Assert.Equal(2, result.TotalTransactions);
        Assert.Equal(1, result.TransactionsWithLocation);
        Assert.Single(result.Regions);
        Assert.Equal(50m, result.Regions[0].TotalSpending);
    }

    private static Transaction CreateTransaction(DateOnly date, decimal amount)
    {
        var accountId = Guid.NewGuid();
        return Transaction.Create(
            accountId,
            MoneyValue.Create("USD", amount),
            date,
            "Test transaction");
    }

    private static Transaction CreateTransactionWithLocation(
        DateOnly date,
        decimal amount,
        string city,
        string state,
        string country)
    {
        var transaction = Transaction.Create(
            Guid.NewGuid(),
            MoneyValue.Create("USD", amount),
            date,
            "Test transaction");
        transaction.SetLocation(TransactionLocationValue.Create(city, state, country, null, null, LocationSource.Manual));
        return transaction;
    }

    private static Transaction CreateTransferWithLocation(
        DateOnly date,
        decimal amount,
        string city,
        string state,
        string country)
    {
        var transfer = Transaction.CreateTransfer(
            Guid.NewGuid(),
            MoneyValue.Create("USD", amount),
            date,
            "Transfer",
            Guid.NewGuid(),
            TransferDirection.Source);
        transfer.SetLocation(TransactionLocationValue.Create(city, state, country, null, null, LocationSource.Manual));
        return transfer;
    }

    private static Mock<ICurrencyProvider> CreateCurrencyProviderMock(string currency)
    {
        var mock = new Mock<ICurrencyProvider>();
        mock.Setup(x => x.GetCurrencyAsync(It.IsAny<CancellationToken>())).ReturnsAsync(currency);
        return mock;
    }
}
