// <copyright file="CalendarGridServiceTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Application.Services;
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
    private readonly Mock<IAccountRepository> _accountRepo;

    public CalendarGridServiceTests()
    {
        _transactionRepo = new Mock<ITransactionRepository>();
        _recurringRepo = new Mock<IRecurringTransactionRepository>();
        _recurringTransferRepo = new Mock<IRecurringTransferRepository>();
        _accountRepo = new Mock<IAccountRepository>();

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

        // Default setup for accounts - return empty list
        _accountRepo
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Account>());
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

    [Fact]
    public async Task GetDayDetailAsync_Returns_Merged_Transactions_And_Recurring()
    {
        // Arrange
        var date = new DateOnly(2026, 1, 15);
        var accountId = Guid.NewGuid();
        var account = CreateTestAccount(accountId, "Checking");

        var transaction = CreateTestTransaction(accountId, date, -85.50m, "Grocery Store");
        _transactionRepo
            .Setup(r => r.GetByDateRangeAsync(date, date, null, default))
            .ReturnsAsync(new List<Transaction> { transaction });

        _accountRepo
            .Setup(r => r.GetAllAsync(default))
            .ReturnsAsync(new List<Account> { account });

        _recurringRepo
            .Setup(r => r.GetActiveAsync(default))
            .ReturnsAsync(new List<RecurringTransaction>());

        var service = CreateService();

        // Act
        var result = await service.GetDayDetailAsync(date);

        // Assert
        Assert.Equal(date, result.Date);
        Assert.Single(result.Items);
        Assert.Equal("transaction", result.Items[0].Type);
        Assert.Equal("Grocery Store", result.Items[0].Description);
        Assert.Equal(-85.50m, result.Items[0].Amount.Amount);
        Assert.Equal("Checking", result.Items[0].AccountName);
    }

    [Fact]
    public async Task GetDayDetailAsync_Calculates_Summary_Correctly()
    {
        // Arrange
        var date = new DateOnly(2026, 1, 15);
        var accountId = Guid.NewGuid();
        var account = CreateTestAccount(accountId, "Checking");

        var transactions = new List<Transaction>
        {
            CreateTestTransaction(accountId, date, -50.00m, "Transaction 1"),
            CreateTestTransaction(accountId, date, -30.00m, "Transaction 2"),
        };
        _transactionRepo
            .Setup(r => r.GetByDateRangeAsync(date, date, null, default))
            .ReturnsAsync(transactions);

        _accountRepo
            .Setup(r => r.GetAllAsync(default))
            .ReturnsAsync(new List<Account> { account });

        _recurringRepo
            .Setup(r => r.GetActiveAsync(default))
            .ReturnsAsync(new List<RecurringTransaction>());

        var service = CreateService();

        // Act
        var result = await service.GetDayDetailAsync(date);

        // Assert
        Assert.Equal(-80.00m, result.Summary.TotalActual.Amount);
        Assert.Equal(0m, result.Summary.TotalProjected.Amount);
        Assert.Equal(-80.00m, result.Summary.CombinedTotal.Amount);
        Assert.Equal(2, result.Summary.ItemCount);
    }

    private CalendarGridService CreateService()
    {
        return new CalendarGridService(
            _transactionRepo.Object,
            _recurringRepo.Object,
            _recurringTransferRepo.Object,
            _accountRepo.Object);
    }

    private static Account CreateTestAccount(Guid id, string name)
    {
        // Use reflection to create account for testing since factory may not allow setting ID
        var account = Account.Create(name, AccountType.Checking);

        // Use reflection to set the Id for testing
        var idProperty = typeof(Account).GetProperty(nameof(Account.Id));
        idProperty?.SetValue(account, id);

        return account;
    }

    private static Transaction CreateTestTransaction(Guid accountId, DateOnly date, decimal amount, string description)
    {
        // Create account first to add transaction
        var account = Account.Create("Test", AccountType.Checking);

        // Use reflection to set account ID
        var accountIdProperty = typeof(Account).GetProperty(nameof(Account.Id));
        accountIdProperty?.SetValue(account, accountId);

        // Add transaction through domain
        var money = MoneyValue.Create("USD", amount);
        return account.AddTransaction(money, date, description);
    }
}
