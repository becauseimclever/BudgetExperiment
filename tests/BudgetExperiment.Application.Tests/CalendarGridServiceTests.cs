// <copyright file="CalendarGridServiceTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>


using BudgetExperiment.Domain;
using Moq;

namespace BudgetExperiment.Application.Tests;

/// <summary>
/// Unit tests for CalendarGridService.
/// </summary>
public class CalendarGridServiceTests
{
    private readonly Mock<ITransactionRepository> _transactionRepo;
    private readonly Mock<IRecurringTransactionRepository> _recurringRepo;
    private readonly Mock<IRecurringTransferRepository> _recurringTransferRepo;
    private readonly Mock<IBalanceCalculationService> _balanceService;
    private readonly Mock<IRecurringInstanceProjector> _recurringInstanceProjector;
    private readonly Mock<IRecurringTransferInstanceProjector> _recurringTransferInstanceProjector;
    private readonly Mock<IAutoRealizeService> _autoRealizeService;

    public CalendarGridServiceTests()
    {
        _transactionRepo = new Mock<ITransactionRepository>();
        _recurringRepo = new Mock<IRecurringTransactionRepository>();
        _recurringTransferRepo = new Mock<IRecurringTransferRepository>();
        _balanceService = new Mock<IBalanceCalculationService>();
        _recurringInstanceProjector = new Mock<IRecurringInstanceProjector>();
        _recurringTransferInstanceProjector = new Mock<IRecurringTransferInstanceProjector>();
        _autoRealizeService = new Mock<IAutoRealizeService>();

        // Default setup for exceptions - return empty list
        _recurringRepo
            .Setup(r => r.GetExceptionsByDateRangeAsync(
                It.IsAny<Guid>(),
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecurringTransactionException>());

        // Default setup for recurring transfer exceptions - return empty list
        _recurringTransferRepo
            .Setup(r => r.GetExceptionsByDateRangeAsync(
                It.IsAny<Guid>(),
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecurringTransferException>());

        // Default setup for active recurring transfers - return empty list
        _recurringTransferRepo
            .Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecurringTransfer>());

        // Default setup for recurring transfers by account id - return empty list
        _recurringTransferRepo
            .Setup(r => r.GetByAccountIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecurringTransfer>());

        // Default setup for balance calculation - return zero
        _balanceService
            .Setup(s => s.GetBalanceBeforeDateAsync(
                It.IsAny<DateOnly>(),
                It.IsAny<Guid?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(MoneyValue.Zero("USD"));

        // Default setup for projectors - return empty dictionaries/lists
        _recurringInstanceProjector
            .Setup(p => p.GetInstancesByDateRangeAsync(
                It.IsAny<IReadOnlyList<RecurringTransaction>>(),
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<DateOnly, List<RecurringInstanceInfo>>());

        _recurringInstanceProjector
            .Setup(p => p.GetInstancesForDateAsync(
                It.IsAny<IReadOnlyList<RecurringTransaction>>(),
                It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecurringInstanceInfo>());

        _recurringTransferInstanceProjector
            .Setup(p => p.GetInstancesByDateRangeAsync(
                It.IsAny<IReadOnlyList<RecurringTransfer>>(),
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                It.IsAny<Guid?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<DateOnly, List<RecurringTransferInstanceInfo>>());

        _recurringTransferInstanceProjector
            .Setup(p => p.GetInstancesForDateAsync(
                It.IsAny<IReadOnlyList<RecurringTransfer>>(),
                It.IsAny<DateOnly>(),
                It.IsAny<Guid?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecurringTransferInstanceInfo>());
    }

    [Fact]
    public async Task GetCalendarGridAsync_Returns_42_Days_Grid()
    {
        // Arrange
        _transactionRepo
            .Setup(r => r.GetDailyTotalsAsync(It.IsAny<int>(), It.IsAny<int>(), null, default))
            .ReturnsAsync(new List<DailyTotal>());
        _recurringRepo
            .Setup(r => r.GetActiveAsync(default))
            .ReturnsAsync(new List<RecurringTransaction>());

        var service = CreateService();

        // Act
        var result = await service.GetCalendarGridAsync(2026, 1);

        // Assert
        Assert.Equal(2026, result.Year);
        Assert.Equal(1, result.Month);
        Assert.Equal(42, result.Days.Count); // 6 weeks * 7 days
    }

    [Fact]
    public async Task GetCalendarGridAsync_Marks_Current_Month_Days_Correctly()
    {
        // Arrange
        _transactionRepo
            .Setup(r => r.GetDailyTotalsAsync(It.IsAny<int>(), It.IsAny<int>(), null, default))
            .ReturnsAsync(new List<DailyTotal>());
        _recurringRepo
            .Setup(r => r.GetActiveAsync(default))
            .ReturnsAsync(new List<RecurringTransaction>());

        var service = CreateService();

        // Act
        var result = await service.GetCalendarGridAsync(2026, 1);

        // Assert - January 2026 has 31 days
        var currentMonthDays = result.Days.Where(d => d.IsCurrentMonth).ToList();
        Assert.Equal(31, currentMonthDays.Count);

        // All current month days should be in January 2026
        Assert.All(currentMonthDays, d =>
        {
            Assert.Equal(2026, d.Date.Year);
            Assert.Equal(1, d.Date.Month);
        });
    }

    [Fact]
    public async Task GetCalendarGridAsync_Includes_Previous_Month_Days()
    {
        // Arrange
        _transactionRepo
            .Setup(r => r.GetDailyTotalsAsync(It.IsAny<int>(), It.IsAny<int>(), null, default))
            .ReturnsAsync(new List<DailyTotal>());
        _recurringRepo
            .Setup(r => r.GetActiveAsync(default))
            .ReturnsAsync(new List<RecurringTransaction>());

        var service = CreateService();

        // Act - January 2026 starts on Thursday, so we need 4 days from December
        var result = await service.GetCalendarGridAsync(2026, 1);

        // Assert
        var firstDay = result.Days[0];
        Assert.Equal(12, firstDay.Date.Month); // December
        Assert.Equal(2025, firstDay.Date.Year);
        Assert.False(firstDay.IsCurrentMonth);
    }

    [Fact]
    public async Task GetCalendarGridAsync_Includes_Next_Month_Days()
    {
        // Arrange
        _transactionRepo
            .Setup(r => r.GetDailyTotalsAsync(It.IsAny<int>(), It.IsAny<int>(), null, default))
            .ReturnsAsync(new List<DailyTotal>());
        _recurringRepo
            .Setup(r => r.GetActiveAsync(default))
            .ReturnsAsync(new List<RecurringTransaction>());

        var service = CreateService();

        // Act
        var result = await service.GetCalendarGridAsync(2026, 1);

        // Assert
        var lastDay = result.Days[^1];
        Assert.Equal(2, lastDay.Date.Month); // February
        Assert.Equal(2026, lastDay.Date.Year);
        Assert.False(lastDay.IsCurrentMonth);
    }

    [Fact]
    public async Task GetCalendarGridAsync_Includes_ActualTotal_From_Transactions()
    {
        // Arrange
        var dailyTotals = new List<DailyTotal>
        {
            new(new DateOnly(2026, 1, 15), MoneyValue.Create("USD", -150.00m), 3),
        };
        _transactionRepo
            .Setup(r => r.GetDailyTotalsAsync(2026, 1, null, default))
            .ReturnsAsync(dailyTotals);
        _recurringRepo
            .Setup(r => r.GetActiveAsync(default))
            .ReturnsAsync(new List<RecurringTransaction>());

        var service = CreateService();

        // Act
        var result = await service.GetCalendarGridAsync(2026, 1);

        // Assert
        var jan15 = result.Days.Single(d => d.Date == new DateOnly(2026, 1, 15));
        Assert.Equal(-150.00m, jan15.ActualTotal.Amount);
        Assert.Equal(3, jan15.TransactionCount);
    }

    [Fact]
    public async Task GetCalendarGridAsync_Filters_By_AccountId()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        _transactionRepo
            .Setup(r => r.GetDailyTotalsAsync(2026, 1, accountId, default))
            .ReturnsAsync(new List<DailyTotal>());
        _recurringRepo
            .Setup(r => r.GetByAccountIdAsync(accountId, default))
            .ReturnsAsync(new List<RecurringTransaction>());

        var service = CreateService();

        // Act
        await service.GetCalendarGridAsync(2026, 1, accountId);

        // Assert
        _transactionRepo.Verify(r => r.GetDailyTotalsAsync(2026, 1, accountId, default), Times.Once);
        _recurringRepo.Verify(r => r.GetByAccountIdAsync(accountId, default), Times.Once);
    }

    [Fact]
    public async Task GetCalendarGridAsync_Calculates_MonthSummary_Income_Expenses()
    {
        // Arrange
        var dailyTotals = new List<DailyTotal>
        {
            new(new DateOnly(2026, 1, 1), MoneyValue.Create("USD", 5000.00m), 1), // Income
            new(new DateOnly(2026, 1, 5), MoneyValue.Create("USD", -1500.00m), 2), // Expense
            new(new DateOnly(2026, 1, 20), MoneyValue.Create("USD", -500.00m), 1), // Expense
        };
        _transactionRepo
            .Setup(r => r.GetDailyTotalsAsync(2026, 1, null, default))
            .ReturnsAsync(dailyTotals);
        _recurringRepo
            .Setup(r => r.GetActiveAsync(default))
            .ReturnsAsync(new List<RecurringTransaction>());

        var service = CreateService();

        // Act
        var result = await service.GetCalendarGridAsync(2026, 1);

        // Assert
        Assert.Equal(5000.00m, result.MonthSummary.TotalIncome.Amount);
        Assert.Equal(-2000.00m, result.MonthSummary.TotalExpenses.Amount);
        Assert.Equal(3000.00m, result.MonthSummary.NetChange.Amount);
    }

    private CalendarGridService CreateService()
    {
        return new CalendarGridService(
            _transactionRepo.Object,
            _recurringRepo.Object,
            _recurringTransferRepo.Object,
            _balanceService.Object,
            _recurringInstanceProjector.Object,
            _recurringTransferInstanceProjector.Object,
            _autoRealizeService.Object);
    }

    private static RecurringTransaction CreateTestRecurringTransaction(Guid accountId, DateOnly startDate)
    {
        var amount = MoneyValue.Create("USD", -50.00m);
        var pattern = RecurrencePattern.CreateMonthly(1, startDate.Day);
        var recurring = RecurringTransaction.Create(accountId, "Test Recurring", amount, pattern, startDate);

        // Set the Id for testing
        var id = Guid.NewGuid();
        var idProperty = typeof(RecurringTransaction).GetProperty(nameof(RecurringTransaction.Id));
        idProperty?.SetValue(recurring, id);

        return recurring;
    }

    #region Running Balance Tests

    [Fact]
    public async Task GetCalendarGridAsync_IncludesStartingBalance()
    {
        // Arrange
        var startingBalance = MoneyValue.Create("USD", 5000m);
        _balanceService
            .Setup(s => s.GetBalanceBeforeDateAsync(
                It.IsAny<DateOnly>(),
                It.IsAny<Guid?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(startingBalance);

        _transactionRepo
            .Setup(r => r.GetDailyTotalsAsync(It.IsAny<int>(), It.IsAny<int>(), null, default))
            .ReturnsAsync(new List<DailyTotal>());
        _recurringRepo
            .Setup(r => r.GetActiveAsync(default))
            .ReturnsAsync(new List<RecurringTransaction>());

        var service = CreateService();

        // Act
        var result = await service.GetCalendarGridAsync(2026, 1);

        // Assert
        Assert.Equal(5000m, result.StartingBalance.Amount);
        Assert.Equal("USD", result.StartingBalance.Currency);
    }

    [Fact]
    public async Task GetCalendarGridAsync_CalculatesEndOfDayBalance_FromStartingBalance()
    {
        // Arrange
        var startingBalance = MoneyValue.Create("USD", 1000m);
        _balanceService
            .Setup(s => s.GetBalanceBeforeDateAsync(
                It.IsAny<DateOnly>(),
                It.IsAny<Guid?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(startingBalance);

        _transactionRepo
            .Setup(r => r.GetDailyTotalsAsync(It.IsAny<int>(), It.IsAny<int>(), null, default))
            .ReturnsAsync(new List<DailyTotal>());
        _recurringRepo
            .Setup(r => r.GetActiveAsync(default))
            .ReturnsAsync(new List<RecurringTransaction>());

        var service = CreateService();

        // Act
        var result = await service.GetCalendarGridAsync(2026, 1);

        // Assert - First day should have starting balance as end-of-day (no transactions)
        Assert.Equal(1000m, result.Days[0].EndOfDayBalance.Amount);
    }

    [Fact]
    public async Task GetCalendarGridAsync_AccumulatesEndOfDayBalance_AcrossDays()
    {
        // Arrange - Starting balance of 1000, transaction of +500 on first day
        var startingBalance = MoneyValue.Create("USD", 1000m);
        _balanceService
            .Setup(s => s.GetBalanceBeforeDateAsync(
                It.IsAny<DateOnly>(),
                It.IsAny<Guid?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(startingBalance);

        // January 2026 grid starts on December 28, 2025 (Sunday)
        var gridStartDate = new DateOnly(2025, 12, 28);

        var dailyTotals = new List<DailyTotal>
        {
            new(gridStartDate, MoneyValue.Create("USD", 500m), 1), // +500 on first grid day
            new(gridStartDate.AddDays(2), MoneyValue.Create("USD", -200m), 1), // -200 on third grid day
        };

        _transactionRepo
            .Setup(r => r.GetDailyTotalsAsync(It.IsAny<int>(), It.IsAny<int>(), null, default))
            .ReturnsAsync(dailyTotals);
        _recurringRepo
            .Setup(r => r.GetActiveAsync(default))
            .ReturnsAsync(new List<RecurringTransaction>());

        var service = CreateService();

        // Act
        var result = await service.GetCalendarGridAsync(2026, 1);

        // Assert
        // Day 0: 1000 + 500 = 1500
        Assert.Equal(1500m, result.Days[0].EndOfDayBalance.Amount);
        // Day 1: 1500 + 0 = 1500
        Assert.Equal(1500m, result.Days[1].EndOfDayBalance.Amount);
        // Day 2: 1500 - 200 = 1300
        Assert.Equal(1300m, result.Days[2].EndOfDayBalance.Amount);
        // Day 3: 1300 + 0 = 1300
        Assert.Equal(1300m, result.Days[3].EndOfDayBalance.Amount);
    }

    [Fact]
    public async Task GetCalendarGridAsync_IncludesRecurringInEndOfDayBalance()
    {
        // Arrange - Starting balance of 1000, recurring transaction of -50 on Jan 15
        var startingBalance = MoneyValue.Create("USD", 1000m);
        _balanceService
            .Setup(s => s.GetBalanceBeforeDateAsync(
                It.IsAny<DateOnly>(),
                It.IsAny<Guid?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(startingBalance);

        _transactionRepo
            .Setup(r => r.GetDailyTotalsAsync(It.IsAny<int>(), It.IsAny<int>(), null, default))
            .ReturnsAsync(new List<DailyTotal>());

        var accountId = Guid.NewGuid();
        var recurringTransaction = CreateTestRecurringTransaction(accountId, new DateOnly(2026, 1, 15));
        _recurringRepo
            .Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecurringTransaction> { recurringTransaction });

        // Set up projector mock for this test - return recurring instance on Jan 15
        var jan15 = new DateOnly(2026, 1, 15);
        var gridStartDate = new DateOnly(2025, 12, 28);
        var gridEndDate = gridStartDate.AddDays(41);
        _recurringInstanceProjector
            .Setup(p => p.GetInstancesByDateRangeAsync(
                It.IsAny<IReadOnlyList<RecurringTransaction>>(),
                gridStartDate,
                gridEndDate,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<DateOnly, List<RecurringInstanceInfo>>
            {
                {
                    jan15, new List<RecurringInstanceInfo>
                    {
                        new RecurringInstanceInfo(
                            Guid.NewGuid(),
                            jan15,
                            accountId,
                            "Test",
                            "Recurring Transaction",
                            MoneyValue.Create("USD", -50.00m),
                            null,
                            null,
                            false,
                            false),
                    }
                },
            });

        var service = CreateService();

        // Act
        var result = await service.GetCalendarGridAsync(2026, 1);

        // Assert - Jan 15, 2026 is at index 18 (grid starts Dec 28)
        // Days 0-17: 1000 (no transactions/recurring)
        // Jan 15 (index 18): 1000 - 50 = 950
        var jan15Index = 18; // December has 4 days in grid (28-31), then Jan 1-15 = 18 days
        Assert.Equal(950m, result.Days[jan15Index].EndOfDayBalance.Amount);
    }

    [Fact]
    public async Task GetCalendarGridAsync_SetsIsBalanceNegative_WhenBalanceGoesNegative()
    {
        // Arrange - Starting balance of 100, transaction of -200 on first day
        var startingBalance = MoneyValue.Create("USD", 100m);
        _balanceService
            .Setup(s => s.GetBalanceBeforeDateAsync(
                It.IsAny<DateOnly>(),
                It.IsAny<Guid?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(startingBalance);

        var gridStartDate = new DateOnly(2025, 12, 28);
        var dailyTotals = new List<DailyTotal>
        {
            new(gridStartDate, MoneyValue.Create("USD", -200m), 1), // -200 on first grid day
        };

        _transactionRepo
            .Setup(r => r.GetDailyTotalsAsync(It.IsAny<int>(), It.IsAny<int>(), null, default))
            .ReturnsAsync(dailyTotals);
        _recurringRepo
            .Setup(r => r.GetActiveAsync(default))
            .ReturnsAsync(new List<RecurringTransaction>());

        var service = CreateService();

        // Act
        var result = await service.GetCalendarGridAsync(2026, 1);

        // Assert - First day should be negative: 100 - 200 = -100
        Assert.Equal(-100m, result.Days[0].EndOfDayBalance.Amount);
        Assert.True(result.Days[0].IsBalanceNegative);
    }

    [Fact]
    public async Task GetCalendarGridAsync_IsBalanceNegative_IsFalse_WhenBalancePositive()
    {
        // Arrange
        var startingBalance = MoneyValue.Create("USD", 1000m);
        _balanceService
            .Setup(s => s.GetBalanceBeforeDateAsync(
                It.IsAny<DateOnly>(),
                It.IsAny<Guid?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(startingBalance);

        _transactionRepo
            .Setup(r => r.GetDailyTotalsAsync(It.IsAny<int>(), It.IsAny<int>(), null, default))
            .ReturnsAsync(new List<DailyTotal>());
        _recurringRepo
            .Setup(r => r.GetActiveAsync(default))
            .ReturnsAsync(new List<RecurringTransaction>());

        var service = CreateService();

        // Act
        var result = await service.GetCalendarGridAsync(2026, 1);

        // Assert
        Assert.False(result.Days[0].IsBalanceNegative);
        Assert.All(result.Days, d => Assert.False(d.IsBalanceNegative));
    }

    [Fact]
    public async Task GetCalendarGridAsync_PassesAccountIdToBalanceService()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        _balanceService
            .Setup(s => s.GetBalanceBeforeDateAsync(
                It.IsAny<DateOnly>(),
                accountId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(MoneyValue.Create("USD", 2000m));

        _transactionRepo
            .Setup(r => r.GetDailyTotalsAsync(It.IsAny<int>(), It.IsAny<int>(), accountId, default))
            .ReturnsAsync(new List<DailyTotal>());
        _recurringRepo
            .Setup(r => r.GetByAccountIdAsync(accountId, default))
            .ReturnsAsync(new List<RecurringTransaction>());

        var service = CreateService();

        // Act
        var result = await service.GetCalendarGridAsync(2026, 1, accountId);

        // Assert
        _balanceService.Verify(
            s => s.GetBalanceBeforeDateAsync(
                It.IsAny<DateOnly>(),
                accountId,
                It.IsAny<CancellationToken>()),
            Times.Once);
        Assert.Equal(2000m, result.StartingBalance.Amount);
    }

    #endregion
}
