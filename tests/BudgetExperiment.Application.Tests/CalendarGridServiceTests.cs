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
    private readonly Mock<IAppSettingsRepository> _settingsRepo;
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly Mock<IBalanceCalculationService> _balanceService;

    public CalendarGridServiceTests()
    {
        _transactionRepo = new Mock<ITransactionRepository>();
        _recurringRepo = new Mock<IRecurringTransactionRepository>();
        _recurringTransferRepo = new Mock<IRecurringTransferRepository>();
        _accountRepo = new Mock<IAccountRepository>();
        _settingsRepo = new Mock<IAppSettingsRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _balanceService = new Mock<IBalanceCalculationService>();

        // Default setup for settings - auto-realize disabled
        _settingsRepo
            .Setup(r => r.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(AppSettings.CreateDefault());

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

        // Default setup for balance calculation - return zero
        _balanceService
            .Setup(s => s.GetBalanceBeforeDateAsync(
                It.IsAny<DateOnly>(),
                It.IsAny<Guid?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(MoneyValue.Zero("USD"));
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
            _accountRepo.Object,
            _settingsRepo.Object,
            _unitOfWork.Object,
            _balanceService.Object);
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

    #region Auto-Realize Tests

    [Fact]
    public async Task GetCalendarGridAsync_DoesNotAutoRealize_WhenSettingDisabled()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var pastDueDate = today.AddDays(-5);

        var settings = AppSettings.CreateDefault();
        // AutoRealizePastDueItems is false by default
        _settingsRepo.Setup(r => r.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(settings);

        var recurringTransaction = CreateTestRecurringTransaction(accountId, pastDueDate);
        _recurringRepo.Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecurringTransaction> { recurringTransaction });
        _transactionRepo.Setup(r => r.GetDailyTotalsAsync(It.IsAny<int>(), It.IsAny<int>(), null, default))
            .ReturnsAsync(new List<DailyTotal>());

        var service = CreateService();

        // Act
        await service.GetCalendarGridAsync(today.Year, today.Month);

        // Assert - no transaction should be added
        _transactionRepo.Verify(r => r.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetCalendarGridAsync_AutoRealizesPastDueItems_WhenSettingEnabled()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var pastDueDate = today.AddDays(-5);

        var settings = AppSettings.CreateDefault();
        settings.UpdateAutoRealize(true);
        _settingsRepo.Setup(r => r.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(settings);

        var recurringTransaction = CreateTestRecurringTransaction(accountId, pastDueDate);
        _recurringRepo.Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecurringTransaction> { recurringTransaction });
        _recurringRepo.Setup(r => r.GetExceptionAsync(It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RecurringTransactionException?)null);
        _transactionRepo.Setup(r => r.GetByRecurringInstanceAsync(It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction?)null);
        _transactionRepo.Setup(r => r.GetDailyTotalsAsync(It.IsAny<int>(), It.IsAny<int>(), null, default))
            .ReturnsAsync(new List<DailyTotal>());

        var service = CreateService();

        // Act
        await service.GetCalendarGridAsync(today.Year, today.Month);

        // Assert - transaction should be added
        _transactionRepo.Verify(r => r.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetCalendarGridAsync_SkipsAlreadyRealizedItems_WhenAutoRealizeEnabled()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var pastDueDate = today.AddDays(-5);

        var settings = AppSettings.CreateDefault();
        settings.UpdateAutoRealize(true);
        _settingsRepo.Setup(r => r.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(settings);

        var recurringTransaction = CreateTestRecurringTransaction(accountId, pastDueDate);
        _recurringRepo.Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecurringTransaction> { recurringTransaction });
        _recurringRepo.Setup(r => r.GetExceptionAsync(It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RecurringTransactionException?)null);

        // Already realized
        var existingTransaction = CreateTestTransaction(accountId, pastDueDate, -50m, "Already realized");
        _transactionRepo.Setup(r => r.GetByRecurringInstanceAsync(It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTransaction);
        _transactionRepo.Setup(r => r.GetDailyTotalsAsync(It.IsAny<int>(), It.IsAny<int>(), null, default))
            .ReturnsAsync(new List<DailyTotal>());

        var service = CreateService();

        // Act
        await service.GetCalendarGridAsync(today.Year, today.Month);

        // Assert - no new transaction should be added
        _transactionRepo.Verify(r => r.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetCalendarGridAsync_SkipsSkippedItems_WhenAutoRealizeEnabled()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var pastDueDate = today.AddDays(-5);

        var settings = AppSettings.CreateDefault();
        settings.UpdateAutoRealize(true);
        _settingsRepo.Setup(r => r.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(settings);

        var recurringTransaction = CreateTestRecurringTransaction(accountId, pastDueDate);
        var skippedException = RecurringTransactionException.CreateSkipped(recurringTransaction.Id, pastDueDate);

        _recurringRepo.Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecurringTransaction> { recurringTransaction });
        _recurringRepo.Setup(r => r.GetExceptionAsync(It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(skippedException);
        _transactionRepo.Setup(r => r.GetByRecurringInstanceAsync(It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction?)null);
        _transactionRepo.Setup(r => r.GetDailyTotalsAsync(It.IsAny<int>(), It.IsAny<int>(), null, default))
            .ReturnsAsync(new List<DailyTotal>());

        var service = CreateService();

        // Act
        await service.GetCalendarGridAsync(today.Year, today.Month);

        // Assert - no transaction should be added for skipped item
        _transactionRepo.Verify(r => r.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetCalendarGridAsync_RespectsLookbackDays_WhenAutoRealizeEnabled()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var withinLookback = today.AddDays(-10);

        var settings = AppSettings.CreateDefault();
        settings.UpdateAutoRealize(true);
        settings.UpdatePastDueLookbackDays(15); // Set lookback to 15 days
        _settingsRepo.Setup(r => r.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(settings);

        // Create a recurring transaction that started 10 days ago (within 15-day lookback)
        var recurringWithinLookback = CreateTestRecurringTransaction(accountId, withinLookback);

        _recurringRepo.Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecurringTransaction> { recurringWithinLookback });
        _recurringRepo.Setup(r => r.GetExceptionAsync(It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RecurringTransactionException?)null);
        _transactionRepo.Setup(r => r.GetByRecurringInstanceAsync(It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction?)null);
        _transactionRepo.Setup(r => r.GetDailyTotalsAsync(It.IsAny<int>(), It.IsAny<int>(), null, default))
            .ReturnsAsync(new List<DailyTotal>());

        var service = CreateService();

        // Act
        await service.GetCalendarGridAsync(today.Year, today.Month);

        // Assert - should be realized (within lookback)
        _transactionRepo.Verify(r => r.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetCalendarGridAsync_SkipsItemsOutsideLookback_WhenAutoRealizeEnabled()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Set lookback to 5 days: [today-5, yesterday]
        var settings = AppSettings.CreateDefault();
        settings.UpdateAutoRealize(true);
        settings.UpdatePastDueLookbackDays(5);
        _settingsRepo.Setup(r => r.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(settings);

        // Create a recurring transaction that started 10 days ago
        // Day of month = (today - 10).Day, which is outside the 5-day lookback
        var outsideLookback = today.AddDays(-10);
        var recurringOutsideLookback = CreateTestRecurringTransaction(accountId, outsideLookback);

        _recurringRepo.Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecurringTransaction> { recurringOutsideLookback });
        _recurringRepo.Setup(r => r.GetExceptionAsync(It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RecurringTransactionException?)null);
        _transactionRepo.Setup(r => r.GetByRecurringInstanceAsync(It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction?)null);
        _transactionRepo.Setup(r => r.GetDailyTotalsAsync(It.IsAny<int>(), It.IsAny<int>(), null, default))
            .ReturnsAsync(new List<DailyTotal>());

        var service = CreateService();

        // Act
        await service.GetCalendarGridAsync(today.Year, today.Month);

        // Assert - should NOT be realized (occurrence on day (today-10) is outside 5-day lookback)
        _transactionRepo.Verify(r => r.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetCalendarGridAsync_AutoRealizesRecurringTransfers_WhenSettingEnabled()
    {
        // Arrange
        var fromAccountId = Guid.NewGuid();
        var toAccountId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var pastDueDate = today.AddDays(-5);

        var settings = AppSettings.CreateDefault();
        settings.UpdateAutoRealize(true);
        _settingsRepo.Setup(r => r.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(settings);

        var recurringTransfer = CreateTestRecurringTransfer(fromAccountId, toAccountId, pastDueDate);
        _recurringTransferRepo.Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecurringTransfer> { recurringTransfer });
        _recurringTransferRepo.Setup(r => r.GetExceptionAsync(It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RecurringTransferException?)null);
        _transactionRepo.Setup(r => r.GetByRecurringTransferInstanceAsync(It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Transaction>());
        _transactionRepo.Setup(r => r.GetDailyTotalsAsync(It.IsAny<int>(), It.IsAny<int>(), null, default))
            .ReturnsAsync(new List<DailyTotal>());
        _recurringRepo.Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecurringTransaction>());

        var service = CreateService();

        // Act
        await service.GetCalendarGridAsync(today.Year, today.Month);

        // Assert - two transactions should be added (from and to)
        _transactionRepo.Verify(r => r.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetCalendarGridAsync_DoesNotRealizeToday_WhenAutoRealizeEnabled()
    {
        // Arrange - recurring scheduled for today should NOT be auto-realized
        var accountId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var settings = AppSettings.CreateDefault();
        settings.UpdateAutoRealize(true);
        _settingsRepo.Setup(r => r.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(settings);

        var recurringTransaction = CreateTestRecurringTransaction(accountId, today);
        _recurringRepo.Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecurringTransaction> { recurringTransaction });
        _transactionRepo.Setup(r => r.GetDailyTotalsAsync(It.IsAny<int>(), It.IsAny<int>(), null, default))
            .ReturnsAsync(new List<DailyTotal>());

        var service = CreateService();

        // Act
        await service.GetCalendarGridAsync(today.Year, today.Month);

        // Assert - today's item should not be auto-realized
        _transactionRepo.Verify(r => r.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

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

    #region Transaction List Running Balance Tests

    [Fact]
    public async Task GetAccountTransactionListAsync_IncludesStartingBalance()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var account = CreateTestAccountWithInitialBalance(accountId, "Checking", 1000m, new DateOnly(2026, 1, 1));

        _accountRepo
            .Setup(r => r.GetByIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        _balanceService
            .Setup(s => s.GetBalanceBeforeDateAsync(
                new DateOnly(2026, 1, 10),
                accountId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(MoneyValue.Create("USD", 1500m));

        _transactionRepo
            .Setup(r => r.GetByDateRangeAsync(
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                accountId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Transaction>());

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
            new DateOnly(2026, 1, 10),
            new DateOnly(2026, 1, 20));

        // Assert
        Assert.Equal(1500m, result.StartingBalance.Amount);
        Assert.Equal("USD", result.StartingBalance.Currency);
    }

    [Fact]
    public async Task GetAccountTransactionListAsync_CalculatesRunningBalanceForEachItem()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var account = CreateTestAccountWithInitialBalance(accountId, "Checking", 1000m, new DateOnly(2026, 1, 1));

        _accountRepo
            .Setup(r => r.GetByIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        _balanceService
            .Setup(s => s.GetBalanceBeforeDateAsync(
                new DateOnly(2026, 1, 10),
                accountId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(MoneyValue.Create("USD", 1000m));

        var transactions = new List<Transaction>
        {
            CreateTestTransaction(accountId, 500m, new DateOnly(2026, 1, 10), "Deposit"),
            CreateTestTransaction(accountId, -200m, new DateOnly(2026, 1, 12), "Expense"),
            CreateTestTransaction(accountId, -100m, new DateOnly(2026, 1, 12), "Another expense"),
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
            new DateOnly(2026, 1, 10),
            new DateOnly(2026, 1, 20));

        // Assert - Items are sorted by date ascending for running balance calculation
        // Starting: 1000
        // After +500 on Jan 10: 1500
        // After -200 on Jan 12: 1300
        // After -100 on Jan 12: 1200

        // Note: Items are returned descending for display, but running balance should reflect chronological order
        var itemsByDateAsc = result.Items.OrderBy(i => i.Date).ThenBy(i => i.CreatedAt).ToList();
        Assert.Equal(1500m, itemsByDateAsc[0].RunningBalance.Amount); // After first txn
        Assert.Equal(1300m, itemsByDateAsc[1].RunningBalance.Amount); // After second txn
        Assert.Equal(1200m, itemsByDateAsc[2].RunningBalance.Amount); // After third txn
    }

    [Fact]
    public async Task GetAccountTransactionListAsync_CalculatesDailyBalanceSummaries()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var account = CreateTestAccountWithInitialBalance(accountId, "Checking", 1000m, new DateOnly(2026, 1, 1));

        _accountRepo
            .Setup(r => r.GetByIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        _balanceService
            .Setup(s => s.GetBalanceBeforeDateAsync(
                new DateOnly(2026, 1, 10),
                accountId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(MoneyValue.Create("USD", 1000m));

        var transactions = new List<Transaction>
        {
            CreateTestTransaction(accountId, 500m, new DateOnly(2026, 1, 10), "Deposit"),
            CreateTestTransaction(accountId, -200m, new DateOnly(2026, 1, 12), "Expense 1"),
            CreateTestTransaction(accountId, -100m, new DateOnly(2026, 1, 12), "Expense 2"),
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
            new DateOnly(2026, 1, 10),
            new DateOnly(2026, 1, 20));

        // Assert
        Assert.Equal(2, result.DailyBalances.Count);

        // Day 1: Jan 10 - starts at 1000, +500, ends at 1500
        var day1 = result.DailyBalances.First(d => d.Date == new DateOnly(2026, 1, 10));
        Assert.Equal(1000m, day1.StartingBalance.Amount);
        Assert.Equal(1500m, day1.EndingBalance.Amount);
        Assert.Equal(500m, day1.DayTotal.Amount);
        Assert.Equal(1, day1.TransactionCount);

        // Day 2: Jan 12 - starts at 1500, -200 -100 = -300, ends at 1200
        var day2 = result.DailyBalances.First(d => d.Date == new DateOnly(2026, 1, 12));
        Assert.Equal(1500m, day2.StartingBalance.Amount);
        Assert.Equal(1200m, day2.EndingBalance.Amount);
        Assert.Equal(-300m, day2.DayTotal.Amount);
        Assert.Equal(2, day2.TransactionCount);
    }

    [Fact]
    public async Task GetAccountTransactionListAsync_DailyBalances_SortedByDateDescending()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var account = CreateTestAccountWithInitialBalance(accountId, "Checking", 1000m, new DateOnly(2026, 1, 1));

        _accountRepo
            .Setup(r => r.GetByIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        _balanceService
            .Setup(s => s.GetBalanceBeforeDateAsync(
                It.IsAny<DateOnly>(),
                accountId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(MoneyValue.Create("USD", 1000m));

        var transactions = new List<Transaction>
        {
            CreateTestTransaction(accountId, 100m, new DateOnly(2026, 1, 10), "Day 1"),
            CreateTestTransaction(accountId, 100m, new DateOnly(2026, 1, 15), "Day 2"),
            CreateTestTransaction(accountId, 100m, new DateOnly(2026, 1, 20), "Day 3"),
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
            new DateOnly(2026, 1, 10),
            new DateOnly(2026, 1, 20));

        // Assert - Daily balances should be sorted descending (most recent first)
        Assert.Equal(new DateOnly(2026, 1, 20), result.DailyBalances[0].Date);
        Assert.Equal(new DateOnly(2026, 1, 15), result.DailyBalances[1].Date);
        Assert.Equal(new DateOnly(2026, 1, 10), result.DailyBalances[2].Date);
    }

    [Fact]
    public async Task GetAccountTransactionListAsync_IncludesRecurringInRunningBalance()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var account = CreateTestAccountWithInitialBalance(accountId, "Checking", 1000m, new DateOnly(2026, 1, 1));

        _accountRepo
            .Setup(r => r.GetByIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        _balanceService
            .Setup(s => s.GetBalanceBeforeDateAsync(
                new DateOnly(2026, 1, 10),
                accountId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(MoneyValue.Create("USD", 1000m));

        // One actual transaction
        var transactions = new List<Transaction>
        {
            CreateTestTransaction(accountId, 500m, new DateOnly(2026, 1, 10), "Deposit"),
        };

        _transactionRepo
            .Setup(r => r.GetByDateRangeAsync(
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                accountId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactions);

        // One recurring transaction on Jan 15
        var recurring = CreateTestRecurringTransaction(accountId, new DateOnly(2026, 1, 15));
        _recurringRepo
            .Setup(r => r.GetByAccountIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecurringTransaction> { recurring });

        _recurringTransferRepo
            .Setup(r => r.GetByAccountIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecurringTransfer>());

        var service = CreateService();

        // Act
        var result = await service.GetAccountTransactionListAsync(
            accountId,
            new DateOnly(2026, 1, 10),
            new DateOnly(2026, 1, 20));

        // Assert - Should include both actual and recurring in running balance
        // Starting: 1000
        // After +500 on Jan 10: 1500
        // After -50 recurring on Jan 15: 1450
        var itemsByDateAsc = result.Items.OrderBy(i => i.Date).ToList();
        Assert.Equal(2, itemsByDateAsc.Count);
        Assert.Equal(1500m, itemsByDateAsc[0].RunningBalance.Amount); // After deposit
        Assert.Equal(1450m, itemsByDateAsc[1].RunningBalance.Amount); // After recurring
    }

    [Fact]
    public async Task GetAccountTransactionListAsync_EmptyDateRange_ReturnsEmptyWithStartingBalance()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var account = CreateTestAccountWithInitialBalance(accountId, "Checking", 1000m, new DateOnly(2026, 1, 1));

        _accountRepo
            .Setup(r => r.GetByIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        _balanceService
            .Setup(s => s.GetBalanceBeforeDateAsync(
                It.IsAny<DateOnly>(),
                accountId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(MoneyValue.Create("USD", 2500m));

        _transactionRepo
            .Setup(r => r.GetByDateRangeAsync(
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                accountId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Transaction>());

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
            new DateOnly(2026, 1, 10),
            new DateOnly(2026, 1, 20));

        // Assert
        Assert.Empty(result.Items);
        Assert.Empty(result.DailyBalances);
        Assert.Equal(2500m, result.StartingBalance.Amount);
    }

    #endregion

    private static Account CreateTestAccountWithInitialBalance(Guid id, string name, decimal initialBalance, DateOnly initialBalanceDate)
    {
        var account = Account.Create(name, AccountType.Checking, MoneyValue.Create("USD", initialBalance), initialBalanceDate);
        var idProperty = typeof(Account).GetProperty(nameof(Account.Id));
        idProperty?.SetValue(account, id);
        return account;
    }

    private static Transaction CreateTestTransaction(Guid accountId, decimal amount, DateOnly date, string description)
    {
        return Transaction.Create(accountId, MoneyValue.Create("USD", amount), date, description);
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

    private static RecurringTransfer CreateTestRecurringTransfer(Guid fromAccountId, Guid toAccountId, DateOnly startDate)
    {
        var amount = MoneyValue.Create("USD", 100.00m);
        var pattern = RecurrencePattern.CreateMonthly(1, startDate.Day);
        var transfer = RecurringTransfer.Create(fromAccountId, toAccountId, "Test Transfer", amount, pattern, startDate);

        // Set the Id for testing
        var id = Guid.NewGuid();
        var idProperty = typeof(RecurringTransfer).GetProperty(nameof(RecurringTransfer.Id));
        idProperty?.SetValue(transfer, id);

        return transfer;
    }
}
