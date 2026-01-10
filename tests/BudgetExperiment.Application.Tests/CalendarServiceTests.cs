// <copyright file="CalendarServiceTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Application.Services;
using BudgetExperiment.Domain;
using Moq;

namespace BudgetExperiment.Application.Tests;

/// <summary>
/// Unit tests for CalendarService.
/// </summary>
public class CalendarServiceTests
{
    [Fact]
    public async Task GetMonthlySummaryAsync_Returns_DailyTotals()
    {
        // Arrange
        var dailyTotals = new List<DailyTotal>
        {
            new(new DateOnly(2026, 1, 5), MoneyValue.Create("USD", 100.00m), 2),
            new(new DateOnly(2026, 1, 15), MoneyValue.Create("USD", 250.50m), 3),
        };
        var repo = new Mock<ITransactionRepository>();
        repo.Setup(r => r.GetDailyTotalsAsync(2026, 1, null, default)).ReturnsAsync(dailyTotals);
        var service = new CalendarService(repo.Object);

        // Act
        var result = await service.GetMonthlySummaryAsync(2026, 1);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(new DateOnly(2026, 1, 5), result[0].Date);
        Assert.Equal(100.00m, result[0].Total.Amount);
        Assert.Equal(2, result[0].TransactionCount);
        Assert.Equal(new DateOnly(2026, 1, 15), result[1].Date);
        Assert.Equal(250.50m, result[1].Total.Amount);
        Assert.Equal(3, result[1].TransactionCount);
    }

    [Fact]
    public async Task GetMonthlySummaryAsync_Returns_Empty_When_No_Transactions()
    {
        // Arrange
        var repo = new Mock<ITransactionRepository>();
        repo.Setup(r => r.GetDailyTotalsAsync(2026, 2, null, default)).ReturnsAsync(new List<DailyTotal>());
        var service = new CalendarService(repo.Object);

        // Act
        var result = await service.GetMonthlySummaryAsync(2026, 2);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetMonthlySummaryAsync_Filters_By_AccountId()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var dailyTotals = new List<DailyTotal>
        {
            new(new DateOnly(2026, 3, 10), MoneyValue.Create("USD", 75.00m), 1),
        };
        var repo = new Mock<ITransactionRepository>();
        repo.Setup(r => r.GetDailyTotalsAsync(2026, 3, accountId, default)).ReturnsAsync(dailyTotals);
        var service = new CalendarService(repo.Object);

        // Act
        var result = await service.GetMonthlySummaryAsync(2026, 3, accountId);

        // Assert
        Assert.Single(result);
        Assert.Equal(new DateOnly(2026, 3, 10), result[0].Date);
        repo.Verify(r => r.GetDailyTotalsAsync(2026, 3, accountId, default), Times.Once);
    }

    [Fact]
    public async Task GetMonthlySummaryAsync_Maps_All_Properties_Correctly()
    {
        // Arrange
        var date = new DateOnly(2026, 4, 20);
        var money = MoneyValue.Create("EUR", 999.99m);
        var dailyTotals = new List<DailyTotal>
        {
            new(date, money, 5),
        };
        var repo = new Mock<ITransactionRepository>();
        repo.Setup(r => r.GetDailyTotalsAsync(2026, 4, null, default)).ReturnsAsync(dailyTotals);
        var service = new CalendarService(repo.Object);

        // Act
        var result = await service.GetMonthlySummaryAsync(2026, 4);

        // Assert
        Assert.Single(result);
        var dto = result[0];
        Assert.Equal(date, dto.Date);
        Assert.Equal(999.99m, dto.Total.Amount);
        Assert.Equal("EUR", dto.Total.Currency);
        Assert.Equal(5, dto.TransactionCount);
    }

    [Fact]
    public async Task GetMonthlySummaryAsync_Handles_Negative_Totals()
    {
        // Arrange
        var dailyTotals = new List<DailyTotal>
        {
            new(new DateOnly(2026, 5, 1), MoneyValue.Create("USD", -150.00m), 4),
        };
        var repo = new Mock<ITransactionRepository>();
        repo.Setup(r => r.GetDailyTotalsAsync(2026, 5, null, default)).ReturnsAsync(dailyTotals);
        var service = new CalendarService(repo.Object);

        // Act
        var result = await service.GetMonthlySummaryAsync(2026, 5);

        // Assert
        Assert.Single(result);
        Assert.Equal(-150.00m, result[0].Total.Amount);
    }

    [Fact]
    public async Task GetMonthlySummaryAsync_Passes_CancellationToken()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var token = cts.Token;
        var repo = new Mock<ITransactionRepository>();
        repo.Setup(r => r.GetDailyTotalsAsync(2026, 6, null, token)).ReturnsAsync(new List<DailyTotal>());
        var service = new CalendarService(repo.Object);

        // Act
        await service.GetMonthlySummaryAsync(2026, 6, null, token);

        // Assert
        repo.Verify(r => r.GetDailyTotalsAsync(2026, 6, null, token), Times.Once);
    }
}
