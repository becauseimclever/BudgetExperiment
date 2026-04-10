// <copyright file="LocationReportBuilderTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;

using Moq;

namespace BudgetExperiment.Application.Tests;

/// <summary>
/// Unit tests for LocationReportBuilder.
/// </summary>
public class LocationReportBuilderTests
{
    [Fact]
    public async Task GetSpendingByLocationAsync_NoTransactions_ReturnsEmptyReport()
    {
        var transactionRepo = new Mock<ITransactionRepository>();
        transactionRepo.Setup(r => r.GetByDateRangeAsync(
            It.IsAny<DateOnly>(),
            It.IsAny<DateOnly>(),
            null,
            default)).ReturnsAsync(new List<Transaction>());

        var builder = new LocationReportBuilder(transactionRepo.Object);

        var result = await builder.GetSpendingByLocationAsync(
            new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31));

        Assert.NotNull(result);
        Assert.Equal(0m, result.TotalSpending);
        Assert.Equal(0, result.TotalTransactions);
        Assert.Equal(0, result.TransactionsWithLocation);
        Assert.Empty(result.Regions);
    }

    [Fact]
    public async Task GetSpendingByLocationAsync_WithLocations_GroupsByRegion()
    {
        var transactions = new List<Transaction>
        {
            CreateTransactionWithLocation(new DateOnly(2026, 1, 10), -100m, "New York", "NY", "US"),
            CreateTransactionWithLocation(new DateOnly(2026, 1, 15), -50m, "Buffalo", "NY", "US"),
            CreateTransactionWithLocation(new DateOnly(2026, 1, 20), -200m, "Los Angeles", "CA", "US"),
        };

        var transactionRepo = new Mock<ITransactionRepository>();
        transactionRepo.Setup(r => r.GetByDateRangeAsync(
            It.IsAny<DateOnly>(),
            It.IsAny<DateOnly>(),
            null,
            default)).ReturnsAsync(transactions);

        var builder = new LocationReportBuilder(transactionRepo.Object);

        var result = await builder.GetSpendingByLocationAsync(
            new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31));

        Assert.Equal(350m, result.TotalSpending);
        Assert.Equal(2, result.Regions.Count);

        // CA should be first (200 > 150)
        var caRegion = result.Regions.First(r => r.RegionName == "CA");
        Assert.Equal(200m, caRegion.TotalSpending);
        Assert.Equal(1, caRegion.TransactionCount);

        var nyRegion = result.Regions.First(r => r.RegionName == "NY");
        Assert.Equal(150m, nyRegion.TotalSpending);
        Assert.Equal(2, nyRegion.TransactionCount);
    }

    [Fact]
    public async Task GetSpendingByLocationAsync_ExcludesTransfers()
    {
        var transferId = Guid.NewGuid();
        var transactions = new List<Transaction>
        {
            CreateTransactionWithLocation(new DateOnly(2026, 1, 10), -100m, "New York", "NY", "US"),
            CreateTransferWithLocation(new DateOnly(2026, 1, 15), -500m, transferId, "Buffalo", "NY", "US"),
        };

        var transactionRepo = new Mock<ITransactionRepository>();
        transactionRepo.Setup(r => r.GetByDateRangeAsync(
            It.IsAny<DateOnly>(),
            It.IsAny<DateOnly>(),
            null,
            default)).ReturnsAsync(transactions);

        var builder = new LocationReportBuilder(transactionRepo.Object);

        var result = await builder.GetSpendingByLocationAsync(
            new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31));

        Assert.Equal(100m, result.TotalSpending);
        Assert.Equal(1, result.TransactionsWithLocation);
    }

    [Fact]
    public async Task GetSpendingByLocationAsync_CalculatesPercentages()
    {
        var transactions = new List<Transaction>
        {
            CreateTransactionWithLocation(new DateOnly(2026, 1, 10), -300m, "New York", "NY", "US"),
            CreateTransactionWithLocation(new DateOnly(2026, 1, 15), -100m, "Los Angeles", "CA", "US"),
        };

        var transactionRepo = new Mock<ITransactionRepository>();
        transactionRepo.Setup(r => r.GetByDateRangeAsync(
            It.IsAny<DateOnly>(),
            It.IsAny<DateOnly>(),
            null,
            default)).ReturnsAsync(transactions);

        var builder = new LocationReportBuilder(transactionRepo.Object);

        var result = await builder.GetSpendingByLocationAsync(
            new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31));

        var nyRegion = result.Regions.First(r => r.RegionName == "NY");
        Assert.Equal(75m, nyRegion.Percentage);

        var caRegion = result.Regions.First(r => r.RegionName == "CA");
        Assert.Equal(25m, caRegion.Percentage);
    }

    [Fact]
    public async Task GetSpendingByLocationAsync_GroupsCitiesWithinRegion()
    {
        var transactions = new List<Transaction>
        {
            CreateTransactionWithLocation(new DateOnly(2026, 1, 10), -100m, "New York", "NY", "US"),
            CreateTransactionWithLocation(new DateOnly(2026, 1, 15), -50m, "Buffalo", "NY", "US"),
            CreateTransactionWithLocation(new DateOnly(2026, 1, 20), -75m, "New York", "NY", "US"),
        };

        var transactionRepo = new Mock<ITransactionRepository>();
        transactionRepo.Setup(r => r.GetByDateRangeAsync(
            It.IsAny<DateOnly>(),
            It.IsAny<DateOnly>(),
            null,
            default)).ReturnsAsync(transactions);

        var builder = new LocationReportBuilder(transactionRepo.Object);

        var result = await builder.GetSpendingByLocationAsync(
            new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31));

        var nyRegion = result.Regions.First(r => r.RegionName == "NY");
        Assert.NotNull(nyRegion.Cities);
        Assert.Equal(2, nyRegion.Cities!.Count);

        // New York city should be first (175m > 50m)
        var nycCity = nyRegion.Cities!.First();
        Assert.Equal("New York", nycCity.City);
        Assert.Equal(175m, nycCity.TotalSpending);
        Assert.Equal(2, nycCity.TransactionCount);
    }

    [Fact]
    public async Task GetSpendingByLocationAsync_FormatsRegionCode()
    {
        var transactions = new List<Transaction>
        {
            CreateTransactionWithLocation(new DateOnly(2026, 1, 10), -100m, "New York", "NY", "US"),
        };

        var transactionRepo = new Mock<ITransactionRepository>();
        transactionRepo.Setup(r => r.GetByDateRangeAsync(
            It.IsAny<DateOnly>(),
            It.IsAny<DateOnly>(),
            null,
            default)).ReturnsAsync(transactions);

        var builder = new LocationReportBuilder(transactionRepo.Object);

        var result = await builder.GetSpendingByLocationAsync(
            new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31));

        Assert.Equal("US-NY", result.Regions[0].RegionCode);
        Assert.Equal("US", result.Regions[0].Country);
    }

    [Fact]
    public async Task GetSpendingByLocationAsync_WithAccountFilter_PassesFilter()
    {
        var accountId = Guid.NewGuid();
        var transactionRepo = new Mock<ITransactionRepository>();
        transactionRepo.Setup(r => r.GetByDateRangeAsync(
            It.IsAny<DateOnly>(),
            It.IsAny<DateOnly>(),
            accountId,
            default)).ReturnsAsync(new List<Transaction>());

        var builder = new LocationReportBuilder(transactionRepo.Object);

        await builder.GetSpendingByLocationAsync(
            new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31), accountId);

        transactionRepo.Verify(
            r => r.GetByDateRangeAsync(
                new DateOnly(2026, 1, 1),
                new DateOnly(2026, 1, 31),
                accountId,
                default),
            Times.Once);
    }

    [Fact]
    public async Task GetSpendingByLocationAsync_OnlyExpenses_IgnoresIncome()
    {
        var transactions = new List<Transaction>
        {
            CreateTransactionWithLocation(new DateOnly(2026, 1, 10), -100m, "New York", "NY", "US"),
            CreateTransactionWithLocation(new DateOnly(2026, 1, 15), 500m, "New York", "NY", "US"),
        };

        var transactionRepo = new Mock<ITransactionRepository>();
        transactionRepo.Setup(r => r.GetByDateRangeAsync(
            It.IsAny<DateOnly>(),
            It.IsAny<DateOnly>(),
            null,
            default)).ReturnsAsync(transactions);

        var builder = new LocationReportBuilder(transactionRepo.Object);

        var result = await builder.GetSpendingByLocationAsync(
            new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31));

        Assert.Equal(100m, result.TotalSpending);
        Assert.Equal(1, result.Regions[0].TransactionCount);
    }

    private static Transaction CreateTransactionWithLocation(
        DateOnly date, decimal amount, string city, string state, string country)
    {
        var accountId = Guid.NewGuid();
        var transaction = TransactionFactory.Create(
            accountId,
            MoneyValue.Create("USD", amount),
            date,
            "Test transaction");
        transaction.SetLocation(TransactionLocationValue.Create(
            city, state, country, null, null, LocationSource.Manual));
        return transaction;
    }

    private static Transaction CreateTransferWithLocation(
        DateOnly date, decimal amount, Guid transferId, string city, string state, string country)
    {
        var accountId = Guid.NewGuid();
        var transfer = TransactionFactory.CreateTransfer(
            accountId,
            MoneyValue.Create("USD", amount),
            date,
            "Transfer",
            transferId,
            TransferDirection.Source);
        transfer.SetLocation(TransactionLocationValue.Create(
            city, state, country, null, null, LocationSource.Manual));
        return transfer;
    }
}
