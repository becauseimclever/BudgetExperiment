// <copyright file="DayDetailServiceTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Application.Services;
using BudgetExperiment.Domain;
using Moq;

namespace BudgetExperiment.Application.Tests;

/// <summary>
/// Unit tests for DayDetailService.
/// </summary>
public class DayDetailServiceTests
{
    private readonly Mock<ITransactionRepository> _transactionRepo;
    private readonly Mock<IRecurringTransactionRepository> _recurringRepo;
    private readonly Mock<IRecurringTransferRepository> _recurringTransferRepo;
    private readonly Mock<IAccountRepository> _accountRepo;
    private readonly Mock<IRecurringInstanceProjector> _recurringInstanceProjector;
    private readonly Mock<IRecurringTransferInstanceProjector> _recurringTransferInstanceProjector;

    public DayDetailServiceTests()
    {
        _transactionRepo = new Mock<ITransactionRepository>();
        _recurringRepo = new Mock<IRecurringTransactionRepository>();
        _recurringTransferRepo = new Mock<IRecurringTransferRepository>();
        _accountRepo = new Mock<IAccountRepository>();
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

        // Set up projector mocks for this test
        _recurringInstanceProjector
            .Setup(p => p.GetInstancesForDateAsync(
                It.IsAny<IReadOnlyList<RecurringTransaction>>(),
                date,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecurringInstanceInfo>
            {
                new RecurringInstanceInfo(
                    recurringTransactionId,
                    date,
                    checkingId,
                    "Checking",
                    "Netflix Subscription",
                    MoneyValue.Create("USD", -15.99m),
                    null,
                    false,
                    false),
            });

        _recurringTransferInstanceProjector
            .Setup(p => p.GetInstancesForDateAsync(
                It.IsAny<IReadOnlyList<RecurringTransfer>>(),
                date,
                It.IsAny<Guid?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecurringTransferInstanceInfo>
            {
                new RecurringTransferInstanceInfo(
                    recurringTransferId,
                    date,
                    checkingId,
                    "Checking",
                    "Transfer to Savings: Monthly Savings",
                    MoneyValue.Create("USD", -100.00m),
                    false,
                    false,
                    "Source"),
                new RecurringTransferInstanceInfo(
                    recurringTransferId,
                    date,
                    savingsId,
                    "Savings",
                    "Transfer from Checking: Monthly Savings",
                    MoneyValue.Create("USD", 100.00m),
                    false,
                    false,
                    "Destination"),
            });

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

        // Set up projector mocks for this test
        _recurringTransferInstanceProjector
            .Setup(p => p.GetInstancesForDateAsync(
                It.IsAny<IReadOnlyList<RecurringTransfer>>(),
                date,
                It.IsAny<Guid?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecurringTransferInstanceInfo>
            {
                new RecurringTransferInstanceInfo(
                    recurringTransferId,
                    date,
                    checkingId,
                    "Checking",
                    "Transfer to Savings: Monthly Savings",
                    MoneyValue.Create("USD", -100.00m),
                    false,
                    false,
                    "Source"),
                new RecurringTransferInstanceInfo(
                    recurringTransferId,
                    date,
                    savingsId,
                    "Savings",
                    "Transfer from Checking: Monthly Savings",
                    MoneyValue.Create("USD", 100.00m),
                    false,
                    false,
                    "Destination"),
            });

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

        // Set up projector mock for this test
        _recurringInstanceProjector
            .Setup(p => p.GetInstancesForDateAsync(
                It.IsAny<IReadOnlyList<RecurringTransaction>>(),
                date,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecurringInstanceInfo>
            {
                new RecurringInstanceInfo(
                    recurringTransactionId,
                    date,
                    accountId,
                    "Checking",
                    "Netflix",
                    MoneyValue.Create("USD", -15.99m),
                    null,
                    false,
                    false),
            });

        var service = CreateService();

        // Act
        var result = await service.GetDayDetailAsync(date);

        // Assert
        Assert.Equal(50.00m, result.Summary.TotalActual.Amount); // -50 + 100
        Assert.Equal(-15.99m, result.Summary.TotalProjected.Amount); // Netflix
        Assert.Equal(34.01m, result.Summary.CombinedTotal.Amount); // 50 + (-15.99)
        Assert.Equal(3, result.Summary.ItemCount);
    }

    #endregion

    private DayDetailService CreateService()
    {
        return new DayDetailService(
            _transactionRepo.Object,
            _recurringRepo.Object,
            _recurringTransferRepo.Object,
            _accountRepo.Object,
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

    private static void SetEntityId<T>(T entity, Guid id)
    {
        var idProperty = typeof(T).GetProperty("Id");
        idProperty?.SetValue(entity, id);
    }
}
