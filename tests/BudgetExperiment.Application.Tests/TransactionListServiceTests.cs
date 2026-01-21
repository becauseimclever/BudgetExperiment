// <copyright file="TransactionListServiceTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>


using BudgetExperiment.Domain;
using Moq;

namespace BudgetExperiment.Application.Tests;

/// <summary>
/// Unit tests for TransactionListService.
/// </summary>
public class TransactionListServiceTests
{
    private readonly Mock<ITransactionRepository> _transactionRepo;
    private readonly Mock<IRecurringTransactionRepository> _recurringRepo;
    private readonly Mock<IRecurringTransferRepository> _recurringTransferRepo;
    private readonly Mock<IAccountRepository> _accountRepo;
    private readonly Mock<IBalanceCalculationService> _balanceService;
    private readonly Mock<IRecurringInstanceProjector> _recurringInstanceProjector;
    private readonly Mock<IRecurringTransferInstanceProjector> _recurringTransferInstanceProjector;

    public TransactionListServiceTests()
    {
        _transactionRepo = new Mock<ITransactionRepository>();
        _recurringRepo = new Mock<IRecurringTransactionRepository>();
        _recurringTransferRepo = new Mock<IRecurringTransferRepository>();
        _accountRepo = new Mock<IAccountRepository>();
        _balanceService = new Mock<IBalanceCalculationService>();
        _recurringInstanceProjector = new Mock<IRecurringInstanceProjector>();
        _recurringTransferInstanceProjector = new Mock<IRecurringTransferInstanceProjector>();

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

    #region Unified Transaction Display Tests (Feature 014)

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

        // Set up projector mocks for this test
        _recurringInstanceProjector
            .Setup(p => p.GetInstancesByDateRangeAsync(
                It.IsAny<IReadOnlyList<RecurringTransaction>>(),
                startDate,
                endDate,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<DateOnly, List<RecurringInstanceInfo>>
            {
                {
                    targetDate, new List<RecurringInstanceInfo>
                    {
                        new RecurringInstanceInfo(
                            recurringTransactionId,
                            targetDate,
                            checkingId,
                            "Checking",
                            "Netflix Subscription",
                            MoneyValue.Create("USD", -15.99m),
                            null,
                            null,
                            false,
                            false),
                    }
                },
            });

        _recurringTransferInstanceProjector
            .Setup(p => p.GetInstancesByDateRangeAsync(
                It.IsAny<IReadOnlyList<RecurringTransfer>>(),
                startDate,
                endDate,
                checkingId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<DateOnly, List<RecurringTransferInstanceInfo>>
            {
                {
                    targetDate, new List<RecurringTransferInstanceInfo>
                    {
                        new RecurringTransferInstanceInfo(
                            recurringTransferId,
                            targetDate,
                            checkingId,
                            "Checking",
                            "Transfer to Savings: Monthly Savings",
                            MoneyValue.Create("USD", -100.00m),
                            false,
                            false,
                            "Source"),
                    }
                },
            });

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

    #endregion

    #region Running Balance Tests

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
            CreateTestTransactionWithDate(accountId, 500m, new DateOnly(2026, 1, 10), "Deposit"),
            CreateTestTransactionWithDate(accountId, -200m, new DateOnly(2026, 1, 12), "Expense"),
            CreateTestTransactionWithDate(accountId, -100m, new DateOnly(2026, 1, 12), "Another expense"),
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
            CreateTestTransactionWithDate(accountId, 500m, new DateOnly(2026, 1, 10), "Deposit"),
            CreateTestTransactionWithDate(accountId, -200m, new DateOnly(2026, 1, 12), "Expense 1"),
            CreateTestTransactionWithDate(accountId, -100m, new DateOnly(2026, 1, 12), "Expense 2"),
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
            CreateTestTransactionWithDate(accountId, 100m, new DateOnly(2026, 1, 10), "Day 1"),
            CreateTestTransactionWithDate(accountId, 100m, new DateOnly(2026, 1, 15), "Day 2"),
            CreateTestTransactionWithDate(accountId, 100m, new DateOnly(2026, 1, 20), "Day 3"),
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
            CreateTestTransactionWithDate(accountId, 500m, new DateOnly(2026, 1, 10), "Deposit"),
        };

        _transactionRepo
            .Setup(r => r.GetByDateRangeAsync(
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                accountId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactions);

        // One recurring transaction on Jan 15
        var recurring = CreateTestRecurringTransactionForDate(accountId, new DateOnly(2026, 1, 15));
        _recurringRepo
            .Setup(r => r.GetByAccountIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecurringTransaction> { recurring });

        _recurringTransferRepo
            .Setup(r => r.GetByAccountIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecurringTransfer>());

        // Set up projector mock for this test
        var jan15 = new DateOnly(2026, 1, 15);
        _recurringInstanceProjector
            .Setup(p => p.GetInstancesByDateRangeAsync(
                It.IsAny<IReadOnlyList<RecurringTransaction>>(),
                new DateOnly(2026, 1, 10),
                new DateOnly(2026, 1, 20),
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
                            "Checking",
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

    private TransactionListService CreateService()
    {
        return new TransactionListService(
            _transactionRepo.Object,
            _recurringRepo.Object,
            _recurringTransferRepo.Object,
            _accountRepo.Object,
            _balanceService.Object,
            _recurringInstanceProjector.Object,
            _recurringTransferInstanceProjector.Object);
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

    private static Account CreateTestAccountWithInitialBalance(Guid id, string name, decimal initialBalance, DateOnly initialBalanceDate)
    {
        var account = Account.Create(name, AccountType.Checking, MoneyValue.Create("USD", initialBalance), initialBalanceDate);
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

    private static Transaction CreateTestTransactionWithDate(Guid accountId, decimal amount, DateOnly date, string description)
    {
        return Transaction.Create(accountId, MoneyValue.Create("USD", amount), date, description);
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

    private static RecurringTransaction CreateTestRecurringTransactionForDate(Guid accountId, DateOnly startDate)
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

    private static void SetEntityId<T>(T entity, Guid id)
    {
        var idProperty = typeof(T).GetProperty("Id");
        idProperty?.SetValue(entity, id);
    }
}
