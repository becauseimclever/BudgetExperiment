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

    [Fact]
    public async Task GetAccountTransactionListAsync_Returns_Pre_Merged_List()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var account = CreateTestAccount(accountId, "Checking");

        _accountRepo
            .Setup(r => r.GetByIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        var transactions = new List<Transaction>
        {
            CreateTestTransaction(accountId, new DateOnly(2026, 1, 10), -50.00m, "Groceries"),
            CreateTestTransaction(accountId, new DateOnly(2026, 1, 15), 1000.00m, "Paycheck"),
        };

        _transactionRepo
            .Setup(r => r.GetByDateRangeAsync(
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                accountId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactions);

        _recurringRepo
            .Setup(r => r.GetByAccountIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecurringTransaction>());

        _recurringTransferRepo
            .Setup(r => r.GetByAccountIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecurringTransfer>());

        var service = CreateService();

        // Act
        var result = await service.GetAccountTransactionListAsync(
            accountId,
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 31));

        // Assert
        Assert.Equal(accountId, result.AccountId);
        Assert.Equal("Checking", result.AccountName);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal(2, result.Summary.TransactionCount);
        Assert.Equal(0, result.Summary.RecurringCount);
        Assert.Equal(950.00m, result.Summary.TotalAmount.Amount);
        Assert.Equal(1000.00m, result.Summary.TotalIncome.Amount);
        Assert.Equal(-50.00m, result.Summary.TotalExpenses.Amount);
    }

    [Fact]
    public async Task GetAccountTransactionListAsync_Excludes_Recurring_When_Disabled()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var account = CreateTestAccount(accountId, "Checking");

        _accountRepo
            .Setup(r => r.GetByIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        _transactionRepo
            .Setup(r => r.GetByDateRangeAsync(
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                accountId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Transaction>());

        var service = CreateService();

        // Act
        var result = await service.GetAccountTransactionListAsync(
            accountId,
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 31),
            includeRecurring: false);

        // Assert
        Assert.Empty(result.Items);
        Assert.Equal(0, result.Summary.RecurringCount);

        // Verify recurring repositories were NOT called
        _recurringRepo.Verify(
            r => r.GetByAccountIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetAccountTransactionListAsync_Throws_When_Account_Not_Found()
    {
        // Arrange
        var accountId = Guid.NewGuid();

        _accountRepo
            .Setup(r => r.GetByIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Account?)null);

        var service = CreateService();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.GetAccountTransactionListAsync(
                accountId,
                new DateOnly(2026, 1, 1),
                new DateOnly(2026, 1, 31)));

        Assert.Contains("not found", ex.Message);
    }

    [Fact]
    public async Task GetAccountTransactionListAsync_Calculates_Current_Balance()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var account = CreateTestAccount(accountId, "Savings");

        // Set initial balance using reflection
        var initialBalanceProperty = typeof(Account).GetProperty(nameof(Account.InitialBalance));
        initialBalanceProperty?.SetValue(account, MoneyValue.Create("USD", 1000.00m));

        _accountRepo
            .Setup(r => r.GetByIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        var transactions = new List<Transaction>
        {
            CreateTestTransaction(accountId, new DateOnly(2026, 1, 10), 500.00m, "Deposit"),
            CreateTestTransaction(accountId, new DateOnly(2026, 1, 20), -200.00m, "Withdrawal"),
        };

        _transactionRepo
            .Setup(r => r.GetByDateRangeAsync(
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                accountId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactions);

        _recurringRepo
            .Setup(r => r.GetByAccountIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecurringTransaction>());

        _recurringTransferRepo
            .Setup(r => r.GetByAccountIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecurringTransfer>());

        var service = CreateService();

        // Act
        var result = await service.GetAccountTransactionListAsync(
            accountId,
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 31));

        // Assert
        Assert.Equal(1000.00m, result.InitialBalance.Amount);
        Assert.Equal(300.00m, result.Summary.TotalAmount.Amount); // 500 - 200
        Assert.Equal(1300.00m, result.Summary.CurrentBalance.Amount); // 1000 + 300
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

    private static Transaction CreateTestTransferTransaction(
        Guid accountId,
        DateOnly date,
        decimal amount,
        string description,
        Guid transferId,
        TransferDirection direction)
    {
        var money = MoneyValue.Create("USD", amount);
        return Transaction.CreateTransfer(accountId, money, date, description, transferId, direction);
    }

    private static Transaction CreateTestRealizedRecurringTransaction(
        Guid accountId,
        DateOnly date,
        decimal amount,
        string description,
        Guid recurringTransactionId)
    {
        var money = MoneyValue.Create("USD", amount);
        return Transaction.CreateFromRecurring(accountId, money, date, description, recurringTransactionId, date);
    }

    private static Transaction CreateTestRealizedRecurringTransfer(
        Guid accountId,
        DateOnly date,
        decimal amount,
        string description,
        Guid transferId,
        TransferDirection direction,
        Guid recurringTransferId)
    {
        var money = MoneyValue.Create("USD", amount);
        return Transaction.CreateFromRecurringTransfer(
            accountId,
            money,
            date,
            description,
            transferId,
            direction,
            recurringTransferId,
            date);
    }

    private static RecurringTransaction CreateTestRecurringTransaction(
        Guid accountId,
        decimal amount,
        string description,
        DateOnly startDate)
    {
        var money = MoneyValue.Create("USD", amount);
        var pattern = RecurrencePattern.CreateMonthly(1, startDate.Day);
        return RecurringTransaction.Create(accountId, description, money, pattern, startDate);
    }

    private static RecurringTransfer CreateTestRecurringTransfer(
        Guid sourceAccountId,
        Guid destinationAccountId,
        decimal amount,
        string description,
        DateOnly startDate)
    {
        var money = MoneyValue.Create("USD", amount);
        var pattern = RecurrencePattern.CreateMonthly(1, startDate.Day);
        return RecurringTransfer.Create(sourceAccountId, destinationAccountId, description, money, pattern, startDate);
    }

    #region Unified Transaction Display Tests (Feature 014)

    [Fact]
    public async Task GetDayDetailAsync_Shows_All_Four_Item_Types_On_Same_Day()
    {
        // Arrange - Test 1 from Feature 014: Calendar Day Detail with Mixed Items
        var date = new DateOnly(2026, 1, 15);
        var checkingId = Guid.NewGuid();
        var savingsId = Guid.NewGuid();
        var checking = CreateTestAccount(checkingId, "Checking");
        var savings = CreateTestAccount(savingsId, "Savings");
        var transferId = Guid.NewGuid();
        var recurringTransactionId = Guid.NewGuid();
        var recurringTransferId = Guid.NewGuid();

        // 1. Regular transaction
        var regularTransaction = CreateTestTransaction(checkingId, date, -50.00m, "Grocery Store");

        // 2. Recurring transaction (create and set up for occurrence on date)
        var recurringTransaction = CreateTestRecurringTransaction(checkingId, -15.99m, "Netflix Subscription", date);
        SetEntityId(recurringTransaction, recurringTransactionId);

        // 3. Transfer (source side - outgoing)
        var transferTransaction = CreateTestTransferTransaction(
            checkingId,
            date,
            -200.00m,
            "Transfer to Savings",
            transferId,
            TransferDirection.Source);

        // 4. Recurring transfer (will be projected)
        var recurringTransfer = CreateTestRecurringTransfer(checkingId, savingsId, 100.00m, "Monthly Savings", date);
        SetEntityId(recurringTransfer, recurringTransferId);

        // Set up mocks
        _transactionRepo
            .Setup(r => r.GetByDateRangeAsync(date, date, null, default))
            .ReturnsAsync(new List<Transaction> { regularTransaction, transferTransaction });

        _accountRepo
            .Setup(r => r.GetAllAsync(default))
            .ReturnsAsync(new List<Account> { checking, savings });

        _recurringRepo
            .Setup(r => r.GetActiveAsync(default))
            .ReturnsAsync(new List<RecurringTransaction> { recurringTransaction });

        _recurringRepo
            .Setup(r => r.GetExceptionAsync(recurringTransactionId, date, default))
            .ReturnsAsync((RecurringTransactionException?)null);

        _recurringTransferRepo
            .Setup(r => r.GetActiveAsync(default))
            .ReturnsAsync(new List<RecurringTransfer> { recurringTransfer });

        _recurringTransferRepo
            .Setup(r => r.GetExceptionAsync(recurringTransferId, date, default))
            .ReturnsAsync((RecurringTransferException?)null);

        var service = CreateService();

        // Act
        var result = await service.GetDayDetailAsync(date);

        // Assert - should have all 4 item types
        Assert.Equal(date, result.Date);

        // Verify we have items of each type
        var transactionItems = result.Items.Where(i => i.Type == "transaction").ToList();
        var recurringItems = result.Items.Where(i => i.Type == "recurring").ToList();
        var recurringTransferItems = result.Items.Where(i => i.Type == "recurring-transfer").ToList();

        Assert.Equal(2, transactionItems.Count); // regular + transfer (realized)
        Assert.Single(recurringItems); // Netflix (projected)

        // When no account filter, recurring transfer shows both source and destination
        Assert.Equal(2, recurringTransferItems.Count); // Monthly Savings source + destination (projected)

        // Verify transfer badges
        var transferItem = transactionItems.First(i => i.IsTransfer);
        Assert.True(transferItem.IsTransfer);
        Assert.Equal(transferId, transferItem.TransferId);
        Assert.Equal("Source", transferItem.TransferDirection);

        // Verify recurring transaction
        var recurringItem = recurringItems.First();
        Assert.Equal("Netflix Subscription", recurringItem.Description);
        Assert.Equal(-15.99m, recurringItem.Amount.Amount);

        // Verify recurring transfers show as transfers with proper directions
        Assert.All(recurringTransferItems, item =>
        {
            Assert.True(item.IsTransfer);
            Assert.NotNull(item.TransferDirection);
        });
        Assert.Single(recurringTransferItems, i => i.TransferDirection == "Source");
        Assert.Single(recurringTransferItems, i => i.TransferDirection == "Destination");
    }

    [Fact]
    public async Task GetAccountTransactionListAsync_Shows_All_Four_Item_Types()
    {
        // Arrange - Test 2 from Feature 014: Account Transactions List with Mixed Items
        var startDate = new DateOnly(2026, 1, 1);
        var endDate = new DateOnly(2026, 1, 31);
        var targetDate = new DateOnly(2026, 1, 15);
        var checkingId = Guid.NewGuid();
        var savingsId = Guid.NewGuid();
        var checking = CreateTestAccount(checkingId, "Checking");
        var savings = CreateTestAccount(savingsId, "Savings");
        var transferId = Guid.NewGuid();
        var recurringTransactionId = Guid.NewGuid();
        var recurringTransferId = Guid.NewGuid();

        // 1. Regular transaction
        var regularTransaction = CreateTestTransaction(checkingId, targetDate, -50.00m, "Grocery Store");

        // 2. Recurring transaction
        var recurringTransaction = CreateTestRecurringTransaction(checkingId, -15.99m, "Netflix Subscription", targetDate);
        SetEntityId(recurringTransaction, recurringTransactionId);

        // 3. Transfer (source side)
        var transferTransaction = CreateTestTransferTransaction(
            checkingId,
            targetDate,
            -200.00m,
            "Transfer to Savings",
            transferId,
            TransferDirection.Source);

        // 4. Recurring transfer
        var recurringTransfer = CreateTestRecurringTransfer(checkingId, savingsId, 100.00m, "Monthly Savings", targetDate);
        SetEntityId(recurringTransfer, recurringTransferId);

        // Set up mocks
        _accountRepo
            .Setup(r => r.GetByIdAsync(checkingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(checking);

        _transactionRepo
            .Setup(r => r.GetByDateRangeAsync(startDate, endDate, checkingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Transaction> { regularTransaction, transferTransaction });

        _accountRepo
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Account> { checking, savings });

        _recurringRepo
            .Setup(r => r.GetByAccountIdAsync(checkingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecurringTransaction> { recurringTransaction });

        _recurringTransferRepo
            .Setup(r => r.GetByAccountIdAsync(checkingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecurringTransfer> { recurringTransfer });

        var service = CreateService();

        // Act
        var result = await service.GetAccountTransactionListAsync(checkingId, startDate, endDate);

        // Assert
        var transactionItems = result.Items.Where(i => i.Type == "transaction").ToList();
        var recurringItems = result.Items.Where(i => i.Type == "recurring").ToList();
        var recurringTransferItems = result.Items.Where(i => i.Type == "recurring-transfer").ToList();

        Assert.Equal(2, transactionItems.Count); // regular + transfer
        Assert.Single(recurringItems); // Netflix
        Assert.Single(recurringTransferItems); // Monthly Savings

        // Verify summary
        Assert.Equal(2, result.Summary.TransactionCount); // Only realized transactions
        Assert.Equal(2, result.Summary.RecurringCount); // recurring + recurring-transfer
    }

    [Fact]
    public async Task GetDayDetailAsync_Deduplicates_Realized_Recurring_Transaction()
    {
        // Arrange - Test 3: Deduplication - Realized Recurring Transaction
        var date = new DateOnly(2026, 1, 15);
        var accountId = Guid.NewGuid();
        var account = CreateTestAccount(accountId, "Checking");
        var recurringTransactionId = Guid.NewGuid();

        // Create recurring transaction
        var recurringTransaction = CreateTestRecurringTransaction(accountId, -15.99m, "Netflix Subscription", date);
        SetEntityId(recurringTransaction, recurringTransactionId);

        // Create realized transaction (linked to the recurring)
        var realizedTransaction = CreateTestRealizedRecurringTransaction(
            accountId,
            date,
            -15.99m,
            "Netflix Subscription",
            recurringTransactionId);

        // Set up mocks
        _transactionRepo
            .Setup(r => r.GetByDateRangeAsync(date, date, null, default))
            .ReturnsAsync(new List<Transaction> { realizedTransaction });

        _accountRepo
            .Setup(r => r.GetAllAsync(default))
            .ReturnsAsync(new List<Account> { account });

        _recurringRepo
            .Setup(r => r.GetActiveAsync(default))
            .ReturnsAsync(new List<RecurringTransaction> { recurringTransaction });

        _recurringRepo
            .Setup(r => r.GetExceptionAsync(recurringTransactionId, date, default))
            .ReturnsAsync((RecurringTransactionException?)null);

        _recurringTransferRepo
            .Setup(r => r.GetActiveAsync(default))
            .ReturnsAsync(new List<RecurringTransfer>());

        var service = CreateService();

        // Act
        var result = await service.GetDayDetailAsync(date);

        // Assert - should only show realized transaction, not projected instance
        Assert.Single(result.Items);
        Assert.Equal("transaction", result.Items[0].Type);
        Assert.Equal(recurringTransactionId, result.Items[0].RecurringTransactionId);

        // Summary should show as actual, not projected
        Assert.Equal(-15.99m, result.Summary.TotalActual.Amount);
        Assert.Equal(0m, result.Summary.TotalProjected.Amount);
    }

    [Fact]
    public async Task GetDayDetailAsync_Deduplicates_Realized_Recurring_Transfer()
    {
        // Arrange - Test 4: Deduplication - Realized Recurring Transfer
        var date = new DateOnly(2026, 1, 15);
        var checkingId = Guid.NewGuid();
        var savingsId = Guid.NewGuid();
        var checking = CreateTestAccount(checkingId, "Checking");
        var savings = CreateTestAccount(savingsId, "Savings");
        var transferId = Guid.NewGuid();
        var recurringTransferId = Guid.NewGuid();

        // Create recurring transfer
        var recurringTransfer = CreateTestRecurringTransfer(checkingId, savingsId, 100.00m, "Monthly Savings", date);
        SetEntityId(recurringTransfer, recurringTransferId);

        // Create realized transfer transactions (linked to the recurring)
        var sourceTransaction = CreateTestRealizedRecurringTransfer(
            checkingId,
            date,
            -100.00m,
            "Transfer to Savings: Monthly Savings",
            transferId,
            TransferDirection.Source,
            recurringTransferId);

        var destTransaction = CreateTestRealizedRecurringTransfer(
            savingsId,
            date,
            100.00m,
            "Transfer from Checking: Monthly Savings",
            transferId,
            TransferDirection.Destination,
            recurringTransferId);

        // Set up mocks - no account filter means we see both sides
        _transactionRepo
            .Setup(r => r.GetByDateRangeAsync(date, date, null, default))
            .ReturnsAsync(new List<Transaction> { sourceTransaction, destTransaction });

        _accountRepo
            .Setup(r => r.GetAllAsync(default))
            .ReturnsAsync(new List<Account> { checking, savings });

        _recurringRepo
            .Setup(r => r.GetActiveAsync(default))
            .ReturnsAsync(new List<RecurringTransaction>());

        _recurringTransferRepo
            .Setup(r => r.GetActiveAsync(default))
            .ReturnsAsync(new List<RecurringTransfer> { recurringTransfer });

        _recurringTransferRepo
            .Setup(r => r.GetExceptionAsync(recurringTransferId, date, default))
            .ReturnsAsync((RecurringTransferException?)null);

        var service = CreateService();

        // Act
        var result = await service.GetDayDetailAsync(date);

        // Assert - should only show realized transfer transactions, not projected instances
        Assert.Equal(2, result.Items.Count); // source + destination realized transactions

        // Both should be type "transaction" (realized), not "recurring-transfer" (projected)
        Assert.All(result.Items, i => Assert.Equal("transaction", i.Type));

        // Both should have the recurring transfer ID set
        Assert.All(result.Items, i => Assert.Equal(recurringTransferId, i.RecurringTransferId));

        // Summary should show in actual, not projected
        Assert.Equal(0m, result.Summary.TotalActual.Amount); // -100 + 100 = 0 (transfers cancel)
        Assert.Equal(0m, result.Summary.TotalProjected.Amount);
    }

    [Fact]
    public async Task GetAccountTransactionListAsync_Deduplicates_Realized_Recurring_Transaction()
    {
        // Arrange
        var startDate = new DateOnly(2026, 1, 1);
        var endDate = new DateOnly(2026, 1, 31);
        var targetDate = new DateOnly(2026, 1, 15);
        var accountId = Guid.NewGuid();
        var account = CreateTestAccount(accountId, "Checking");
        var recurringTransactionId = Guid.NewGuid();

        // Create recurring transaction
        var recurringTransaction = CreateTestRecurringTransaction(accountId, -15.99m, "Netflix Subscription", targetDate);
        SetEntityId(recurringTransaction, recurringTransactionId);

        // Create realized transaction (linked to the recurring)
        var realizedTransaction = CreateTestRealizedRecurringTransaction(
            accountId,
            targetDate,
            -15.99m,
            "Netflix Subscription",
            recurringTransactionId);

        // Set up mocks
        _accountRepo
            .Setup(r => r.GetByIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        _transactionRepo
            .Setup(r => r.GetByDateRangeAsync(startDate, endDate, accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Transaction> { realizedTransaction });

        _accountRepo
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Account> { account });

        _recurringRepo
            .Setup(r => r.GetByAccountIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecurringTransaction> { recurringTransaction });

        _recurringTransferRepo
            .Setup(r => r.GetByAccountIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecurringTransfer>());

        var service = CreateService();

        // Act
        var result = await service.GetAccountTransactionListAsync(accountId, startDate, endDate);

        // Assert - should only show realized transaction, not projected instance
        Assert.Single(result.Items);
        Assert.Equal("transaction", result.Items[0].Type);
        Assert.Equal(recurringTransactionId, result.Items[0].RecurringTransactionId);

        // Summary should count as transaction, not recurring
        Assert.Equal(1, result.Summary.TransactionCount);
        Assert.Equal(0, result.Summary.RecurringCount);
    }

    [Fact]
    public async Task GetAccountTransactionListAsync_Deduplicates_Realized_Recurring_Transfer()
    {
        // Arrange
        var startDate = new DateOnly(2026, 1, 1);
        var endDate = new DateOnly(2026, 1, 31);
        var targetDate = new DateOnly(2026, 1, 15);
        var checkingId = Guid.NewGuid();
        var savingsId = Guid.NewGuid();
        var checking = CreateTestAccount(checkingId, "Checking");
        var savings = CreateTestAccount(savingsId, "Savings");
        var transferId = Guid.NewGuid();
        var recurringTransferId = Guid.NewGuid();

        // Create recurring transfer
        var recurringTransfer = CreateTestRecurringTransfer(checkingId, savingsId, 100.00m, "Monthly Savings", targetDate);
        SetEntityId(recurringTransfer, recurringTransferId);

        // Create realized transfer transaction (source side - for checking account)
        var realizedTransaction = CreateTestRealizedRecurringTransfer(
            checkingId,
            targetDate,
            -100.00m,
            "Transfer to Savings: Monthly Savings",
            transferId,
            TransferDirection.Source,
            recurringTransferId);

        // Set up mocks
        _accountRepo
            .Setup(r => r.GetByIdAsync(checkingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(checking);

        _transactionRepo
            .Setup(r => r.GetByDateRangeAsync(startDate, endDate, checkingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Transaction> { realizedTransaction });

        _accountRepo
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Account> { checking, savings });

        _recurringRepo
            .Setup(r => r.GetByAccountIdAsync(checkingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecurringTransaction>());

        _recurringTransferRepo
            .Setup(r => r.GetByAccountIdAsync(checkingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecurringTransfer> { recurringTransfer });

        var service = CreateService();

        // Act
        var result = await service.GetAccountTransactionListAsync(checkingId, startDate, endDate);

        // Assert - should only show realized transfer, not projected instance
        Assert.Single(result.Items);
        Assert.Equal("transaction", result.Items[0].Type);
        Assert.Equal(recurringTransferId, result.Items[0].RecurringTransferId);
        Assert.True(result.Items[0].IsTransfer);

        // Summary
        Assert.Equal(1, result.Summary.TransactionCount);
        Assert.Equal(0, result.Summary.RecurringCount);
    }

    [Fact]
    public async Task GetDayDetailAsync_Shows_Recurring_Transfer_With_Both_Icons()
    {
        // REQ-004: Visual Indicators - recurring transfers should show both icons
        var date = new DateOnly(2026, 1, 15);
        var checkingId = Guid.NewGuid();
        var savingsId = Guid.NewGuid();
        var checking = CreateTestAccount(checkingId, "Checking");
        var savings = CreateTestAccount(savingsId, "Savings");
        var recurringTransferId = Guid.NewGuid();

        // Create recurring transfer
        var recurringTransfer = CreateTestRecurringTransfer(checkingId, savingsId, 100.00m, "Monthly Savings", date);
        SetEntityId(recurringTransfer, recurringTransferId);

        // Set up mocks
        _transactionRepo
            .Setup(r => r.GetByDateRangeAsync(date, date, null, default))
            .ReturnsAsync(new List<Transaction>());

        _accountRepo
            .Setup(r => r.GetAllAsync(default))
            .ReturnsAsync(new List<Account> { checking, savings });

        _recurringRepo
            .Setup(r => r.GetActiveAsync(default))
            .ReturnsAsync(new List<RecurringTransaction>());

        _recurringTransferRepo
            .Setup(r => r.GetActiveAsync(default))
            .ReturnsAsync(new List<RecurringTransfer> { recurringTransfer });

        _recurringTransferRepo
            .Setup(r => r.GetExceptionAsync(recurringTransferId, date, default))
            .ReturnsAsync((RecurringTransferException?)null);

        var service = CreateService();

        // Act
        var result = await service.GetDayDetailAsync(date);

        // Assert - should show recurring-transfer type with transfer flag
        var items = result.Items.Where(i => i.Type == "recurring-transfer").ToList();
        Assert.Equal(2, items.Count); // source + destination

        // All recurring-transfer items should have IsTransfer = true
        Assert.All(items, i =>
        {
            Assert.True(i.IsTransfer);
            Assert.Equal(recurringTransferId, i.RecurringTransferId);
            Assert.NotNull(i.TransferDirection);
        });

        // Verify directions
        Assert.Single(items, i => i.TransferDirection == "Source");
        Assert.Single(items, i => i.TransferDirection == "Destination");
    }

    [Fact]
    public async Task GetDayDetailAsync_Summary_Separates_Actual_And_Projected_Correctly()
    {
        // REQ-005: Correct Summary Calculations
        var date = new DateOnly(2026, 1, 15);
        var accountId = Guid.NewGuid();
        var account = CreateTestAccount(accountId, "Checking");
        var recurringTransactionId = Guid.NewGuid();

        // Actual transactions
        var actualTransaction1 = CreateTestTransaction(accountId, date, -50.00m, "Grocery");
        var actualTransaction2 = CreateTestTransaction(accountId, date, 100.00m, "Deposit");

        // Recurring transaction (projected)
        var recurringTransaction = CreateTestRecurringTransaction(accountId, -15.99m, "Netflix", date);
        SetEntityId(recurringTransaction, recurringTransactionId);

        // Set up mocks
        _transactionRepo
            .Setup(r => r.GetByDateRangeAsync(date, date, null, default))
            .ReturnsAsync(new List<Transaction> { actualTransaction1, actualTransaction2 });

        _accountRepo
            .Setup(r => r.GetAllAsync(default))
            .ReturnsAsync(new List<Account> { account });

        _recurringRepo
            .Setup(r => r.GetActiveAsync(default))
            .ReturnsAsync(new List<RecurringTransaction> { recurringTransaction });

        _recurringRepo
            .Setup(r => r.GetExceptionAsync(recurringTransactionId, date, default))
            .ReturnsAsync((RecurringTransactionException?)null);

        _recurringTransferRepo
            .Setup(r => r.GetActiveAsync(default))
            .ReturnsAsync(new List<RecurringTransfer>());

        var service = CreateService();

        // Act
        var result = await service.GetDayDetailAsync(date);

        // Assert
        Assert.Equal(50.00m, result.Summary.TotalActual.Amount); // -50 + 100
        Assert.Equal(-15.99m, result.Summary.TotalProjected.Amount); // Netflix
        Assert.Equal(34.01m, result.Summary.CombinedTotal.Amount); // 50 + (-15.99)
        Assert.Equal(3, result.Summary.ItemCount);
    }

    private static void SetEntityId<T>(T entity, Guid id)
    {
        var idProperty = typeof(T).GetProperty("Id");
        idProperty?.SetValue(entity, id);
    }

    #endregion
}
