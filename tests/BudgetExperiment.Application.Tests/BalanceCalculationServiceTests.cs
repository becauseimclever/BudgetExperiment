// <copyright file="BalanceCalculationServiceTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>


using BudgetExperiment.Domain;
using Moq;

namespace BudgetExperiment.Application.Tests;

/// <summary>
/// Unit tests for BalanceCalculationService.
/// </summary>
public class BalanceCalculationServiceTests
{
    private readonly Mock<IAccountRepository> _accountRepo;
    private readonly Mock<ITransactionRepository> _transactionRepo;

    public BalanceCalculationServiceTests()
    {
        _accountRepo = new Mock<IAccountRepository>();
        _transactionRepo = new Mock<ITransactionRepository>();

        // Default setup - return empty collections
        _accountRepo
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Account>());
        _accountRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Account?)null);
        _transactionRepo
            .Setup(r => r.GetByDateRangeAsync(
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                It.IsAny<Guid?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Transaction>());
    }

    private BalanceCalculationService CreateService()
    {
        return new BalanceCalculationService(_accountRepo.Object, _transactionRepo.Object);
    }

    [Fact]
    public async Task GetBalanceBeforeDateAsync_NoAccounts_ReturnsZero()
    {
        // Arrange
        _accountRepo
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Account>());

        var service = CreateService();

        // Act
        var result = await service.GetBalanceBeforeDateAsync(new DateOnly(2026, 1, 15));

        // Assert
        Assert.Equal(0m, result.Amount);
        Assert.Equal("USD", result.Currency);
    }

    [Fact]
    public async Task GetBalanceBeforeDateAsync_SingleAccount_NoTransactions_ReturnsInitialBalance()
    {
        // Arrange
        var account = Account.Create(
            "Checking",
            AccountType.Checking,
            MoneyValue.Create("USD", 1000m),
            new DateOnly(2026, 1, 1));

        _accountRepo
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Account> { account });

        _transactionRepo
            .Setup(r => r.GetByDateRangeAsync(
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                It.IsAny<Guid?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Transaction>());

        var service = CreateService();

        // Act - Get balance before Jan 15, account started Jan 1 with $1000
        var result = await service.GetBalanceBeforeDateAsync(new DateOnly(2026, 1, 15));

        // Assert
        Assert.Equal(1000m, result.Amount);
        Assert.Equal("USD", result.Currency);
    }

    [Fact]
    public async Task GetBalanceBeforeDateAsync_AccountStartsAfterDate_ExcludesInitialBalance()
    {
        // Arrange - Account starts Jan 20, query for balance before Jan 15
        var account = Account.Create(
            "Checking",
            AccountType.Checking,
            MoneyValue.Create("USD", 1000m),
            new DateOnly(2026, 1, 20));

        _accountRepo
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Account> { account });

        var service = CreateService();

        // Act - Account initial balance date (Jan 20) is after query date (Jan 15)
        var result = await service.GetBalanceBeforeDateAsync(new DateOnly(2026, 1, 15));

        // Assert - Initial balance should not be included
        Assert.Equal(0m, result.Amount);
    }

    [Fact]
    public async Task GetBalanceBeforeDateAsync_WithTransactions_SumsCorrectly()
    {
        // Arrange
        var account = Account.Create(
            "Checking",
            AccountType.Checking,
            MoneyValue.Create("USD", 1000m),
            new DateOnly(2026, 1, 1));

        var transactions = new List<Transaction>
        {
            Transaction.Create(account.Id, MoneyValue.Create("USD", 500m), new DateOnly(2026, 1, 5), "Deposit"),
            Transaction.Create(account.Id, MoneyValue.Create("USD", -200m), new DateOnly(2026, 1, 10), "Expense"),
        };

        _accountRepo
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Account> { account });

        _transactionRepo
            .Setup(r => r.GetByDateRangeAsync(
                new DateOnly(2026, 1, 1),
                new DateOnly(2026, 1, 14),
                account.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactions);

        var service = CreateService();

        // Act - Query for balance before Jan 15
        var result = await service.GetBalanceBeforeDateAsync(new DateOnly(2026, 1, 15));

        // Assert - Initial 1000 + 500 deposit - 200 expense = 1300
        Assert.Equal(1300m, result.Amount);
    }

    [Fact]
    public async Task GetBalanceBeforeDateAsync_MultipleAccounts_SumsAllBalances()
    {
        // Arrange
        var checking = Account.Create(
            "Checking",
            AccountType.Checking,
            MoneyValue.Create("USD", 1000m),
            new DateOnly(2026, 1, 1));

        var savings = Account.Create(
            "Savings",
            AccountType.Savings,
            MoneyValue.Create("USD", 5000m),
            new DateOnly(2026, 1, 1));

        var checkingTransactions = new List<Transaction>
        {
            Transaction.Create(checking.Id, MoneyValue.Create("USD", 200m), new DateOnly(2026, 1, 5), "Deposit"),
        };

        var savingsTransactions = new List<Transaction>
        {
            Transaction.Create(savings.Id, MoneyValue.Create("USD", 100m), new DateOnly(2026, 1, 5), "Interest"),
        };

        _accountRepo
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Account> { checking, savings });

        _transactionRepo
            .Setup(r => r.GetByDateRangeAsync(
                new DateOnly(2026, 1, 1),
                new DateOnly(2026, 1, 14),
                checking.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(checkingTransactions);

        _transactionRepo
            .Setup(r => r.GetByDateRangeAsync(
                new DateOnly(2026, 1, 1),
                new DateOnly(2026, 1, 14),
                savings.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(savingsTransactions);

        var service = CreateService();

        // Act
        var result = await service.GetBalanceBeforeDateAsync(new DateOnly(2026, 1, 15));

        // Assert - Checking: 1000 + 200 = 1200, Savings: 5000 + 100 = 5100, Total: 6300
        Assert.Equal(6300m, result.Amount);
    }

    [Fact]
    public async Task GetBalanceBeforeDateAsync_WithAccountFilter_OnlyIncludesSpecifiedAccount()
    {
        // Arrange
        var checking = Account.Create(
            "Checking",
            AccountType.Checking,
            MoneyValue.Create("USD", 1000m),
            new DateOnly(2026, 1, 1));

        var savings = Account.Create(
            "Savings",
            AccountType.Savings,
            MoneyValue.Create("USD", 5000m),
            new DateOnly(2026, 1, 1));

        var checkingTransactions = new List<Transaction>
        {
            Transaction.Create(checking.Id, MoneyValue.Create("USD", 200m), new DateOnly(2026, 1, 5), "Deposit"),
        };

        _accountRepo
            .Setup(r => r.GetByIdAsync(checking.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(checking);

        _transactionRepo
            .Setup(r => r.GetByDateRangeAsync(
                new DateOnly(2026, 1, 1),
                new DateOnly(2026, 1, 14),
                checking.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(checkingTransactions);

        var service = CreateService();

        // Act - Only get balance for checking account
        var result = await service.GetBalanceBeforeDateAsync(
            new DateOnly(2026, 1, 15),
            accountId: checking.Id);

        // Assert - Only checking: 1000 + 200 = 1200 (savings excluded)
        Assert.Equal(1200m, result.Amount);
    }

    [Fact]
    public async Task GetBalanceBeforeDateAsync_AccountFilterNotFound_ReturnsZero()
    {
        // Arrange
        var nonExistentAccountId = Guid.NewGuid();

        _accountRepo
            .Setup(r => r.GetByIdAsync(nonExistentAccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Account?)null);

        var service = CreateService();

        // Act
        var result = await service.GetBalanceBeforeDateAsync(
            new DateOnly(2026, 1, 15),
            accountId: nonExistentAccountId);

        // Assert
        Assert.Equal(0m, result.Amount);
    }

    [Fact]
    public async Task GetBalanceBeforeDateAsync_DateIsAccountInitialBalanceDate_ExcludesInitialBalance()
    {
        // Arrange - Query for exact date of initial balance should exclude it
        var account = Account.Create(
            "Checking",
            AccountType.Checking,
            MoneyValue.Create("USD", 1000m),
            new DateOnly(2026, 1, 15)); // Same as query date

        _accountRepo
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Account> { account });

        var service = CreateService();

        // Act - Balance "before" Jan 15 should not include Jan 15 initial balance
        var result = await service.GetBalanceBeforeDateAsync(new DateOnly(2026, 1, 15));

        // Assert - Initial balance dated Jan 15 should be excluded
        Assert.Equal(0m, result.Amount);
    }

    [Fact]
    public async Task GetBalanceBeforeDateAsync_MultipleAccountsDifferentStartDates_HandlesCorrectly()
    {
        // Arrange
        var oldAccount = Account.Create(
            "Old Account",
            AccountType.Checking,
            MoneyValue.Create("USD", 2000m),
            new DateOnly(2025, 6, 1));

        var newAccount = Account.Create(
            "New Account",
            AccountType.Checking,
            MoneyValue.Create("USD", 500m),
            new DateOnly(2026, 1, 20)); // After query date

        _accountRepo
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Account> { oldAccount, newAccount });

        _transactionRepo
            .Setup(r => r.GetByDateRangeAsync(
                new DateOnly(2025, 6, 1),
                new DateOnly(2026, 1, 14),
                oldAccount.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Transaction>());

        var service = CreateService();

        // Act - Query for balance before Jan 15, 2026
        var result = await service.GetBalanceBeforeDateAsync(new DateOnly(2026, 1, 15));

        // Assert - Only old account's balance (2000), new account started after query date
        Assert.Equal(2000m, result.Amount);
    }

    [Fact]
    public async Task GetBalanceBeforeDateAsync_NegativeBalanceAllowed()
    {
        // Arrange
        var account = Account.Create(
            "Checking",
            AccountType.Checking,
            MoneyValue.Create("USD", 100m),
            new DateOnly(2026, 1, 1));

        var transactions = new List<Transaction>
        {
            Transaction.Create(account.Id, MoneyValue.Create("USD", -500m), new DateOnly(2026, 1, 5), "Big expense"),
        };

        _accountRepo
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Account> { account });

        _transactionRepo
            .Setup(r => r.GetByDateRangeAsync(
                new DateOnly(2026, 1, 1),
                new DateOnly(2026, 1, 14),
                account.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactions);

        var service = CreateService();

        // Act
        var result = await service.GetBalanceBeforeDateAsync(new DateOnly(2026, 1, 15));

        // Assert - Initial 100 - 500 expense = -400
        Assert.Equal(-400m, result.Amount);
    }

    // ========== GetBalanceAsOfDateAsync Tests ==========

    [Fact]
    public async Task GetBalanceAsOfDateAsync_NoAccounts_ReturnsZero()
    {
        // Arrange
        _accountRepo
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Account>());

        var service = CreateService();

        // Act
        var result = await service.GetBalanceAsOfDateAsync(new DateOnly(2026, 1, 15));

        // Assert
        Assert.Equal(0m, result.Amount);
        Assert.Equal("USD", result.Currency);
    }

    [Fact]
    public async Task GetBalanceAsOfDateAsync_AccountStartsOnDate_IncludesInitialBalance()
    {
        // Arrange - Account starts on the same date as query (the key edge case!)
        var account = Account.Create(
            "Checking",
            AccountType.Checking,
            MoneyValue.Create("USD", 1000m),
            new DateOnly(2026, 1, 15)); // Same as query date

        _accountRepo
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Account> { account });

        var service = CreateService();

        // Act - Balance as of Jan 15 should INCLUDE the initial balance
        var result = await service.GetBalanceAsOfDateAsync(new DateOnly(2026, 1, 15));

        // Assert - Initial balance dated Jan 15 should be included
        Assert.Equal(1000m, result.Amount);
    }

    [Fact]
    public async Task GetBalanceAsOfDateAsync_AccountStartsBeforeDate_IncludesInitialBalanceAndTransactions()
    {
        // Arrange
        var account = Account.Create(
            "Checking",
            AccountType.Checking,
            MoneyValue.Create("USD", 1000m),
            new DateOnly(2026, 1, 1));

        var transactions = new List<Transaction>
        {
            Transaction.Create(account.Id, MoneyValue.Create("USD", 500m), new DateOnly(2026, 1, 5), "Deposit"),
            Transaction.Create(account.Id, MoneyValue.Create("USD", -200m), new DateOnly(2026, 1, 10), "Expense"),
            Transaction.Create(account.Id, MoneyValue.Create("USD", 100m), new DateOnly(2026, 1, 15), "Same day tx"),
        };

        _accountRepo
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Account> { account });

        _transactionRepo
            .Setup(r => r.GetByDateRangeAsync(
                new DateOnly(2026, 1, 1),
                new DateOnly(2026, 1, 15),
                account.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactions);

        var service = CreateService();

        // Act - Balance as of Jan 15 includes all transactions up to and including Jan 15
        var result = await service.GetBalanceAsOfDateAsync(new DateOnly(2026, 1, 15));

        // Assert - Initial 1000 + 500 - 200 + 100 = 1400
        Assert.Equal(1400m, result.Amount);
    }

    [Fact]
    public async Task GetBalanceAsOfDateAsync_AccountStartsAfterDate_ExcludesInitialBalance()
    {
        // Arrange - Account starts Jan 20, query for balance as of Jan 15
        var account = Account.Create(
            "Checking",
            AccountType.Checking,
            MoneyValue.Create("USD", 1000m),
            new DateOnly(2026, 1, 20));

        _accountRepo
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Account> { account });

        var service = CreateService();

        // Act - Account initial balance date (Jan 20) is after query date (Jan 15)
        var result = await service.GetBalanceAsOfDateAsync(new DateOnly(2026, 1, 15));

        // Assert - Initial balance should not be included
        Assert.Equal(0m, result.Amount);
    }

    [Fact]
    public async Task GetBalanceAsOfDateAsync_MultipleAccountsDifferentStartDates_HandlesCorrectly()
    {
        // Arrange - One account starts before, one on, one after the query date
        var oldAccount = Account.Create(
            "Old Account",
            AccountType.Checking,
            MoneyValue.Create("USD", 2000m),
            new DateOnly(2025, 12, 1));

        var sameDayAccount = Account.Create(
            "Same Day Account",
            AccountType.Checking,
            MoneyValue.Create("USD", 500m),
            new DateOnly(2025, 12, 28)); // Grid start date for Jan 2026

        var futureAccount = Account.Create(
            "Future Account",
            AccountType.Checking,
            MoneyValue.Create("USD", 300m),
            new DateOnly(2026, 1, 5)); // After query date

        _accountRepo
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Account> { oldAccount, sameDayAccount, futureAccount });

        _transactionRepo
            .Setup(r => r.GetByDateRangeAsync(
                new DateOnly(2025, 12, 1),
                new DateOnly(2025, 12, 28),
                oldAccount.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Transaction>());

        _transactionRepo
            .Setup(r => r.GetByDateRangeAsync(
                new DateOnly(2025, 12, 28),
                new DateOnly(2025, 12, 28),
                sameDayAccount.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Transaction>());

        var service = CreateService();

        // Act - Balance as of Dec 28 (grid start for January calendar)
        var result = await service.GetBalanceAsOfDateAsync(new DateOnly(2025, 12, 28));

        // Assert - Old: 2000 + Same Day: 500 = 2500 (Future excluded)
        Assert.Equal(2500m, result.Amount);
    }

    // ========== GetOpeningBalanceForDateAsync Tests ==========
    // This method is for calendar use: includes initial balances for accounts
    // starting BEFORE the date, plus transactions BEFORE the date.
    // Accounts starting ON the date are handled separately via GetInitialBalancesByDateRangeAsync.

    [Fact]
    public async Task GetOpeningBalanceForDateAsync_NoAccounts_ReturnsZero()
    {
        // Arrange
        _accountRepo
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Account>());

        var service = CreateService();

        // Act
        var result = await service.GetOpeningBalanceForDateAsync(new DateOnly(2026, 1, 15));

        // Assert
        Assert.Equal(0m, result.Amount);
        Assert.Equal("USD", result.Currency);
    }

    [Fact]
    public async Task GetOpeningBalanceForDateAsync_AccountStartsOnDate_ExcludesInitialBalance()
    {
        // Arrange - Account starts on the same date as query
        // Opening balance should EXCLUDE this - it's handled by GetInitialBalancesByDateRangeAsync
        var account = Account.Create(
            "Checking",
            AccountType.Checking,
            MoneyValue.Create("USD", 1000m),
            new DateOnly(2026, 1, 15)); // Same as query date

        _accountRepo
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Account> { account });

        var service = CreateService();

        // Act - Opening balance for Jan 15 should NOT include accounts starting on Jan 15
        var result = await service.GetOpeningBalanceForDateAsync(new DateOnly(2026, 1, 15));

        // Assert - Account starting on same date is excluded (handled separately)
        Assert.Equal(0m, result.Amount);
    }

    [Fact]
    public async Task GetOpeningBalanceForDateAsync_AccountStartsBeforeDate_IncludesPriorTransactionsOnly()
    {
        // Arrange
        var account = Account.Create(
            "Checking",
            AccountType.Checking,
            MoneyValue.Create("USD", 1000m),
            new DateOnly(2026, 1, 1));

        // Transactions before the query date (should be included)
        var priorTransactions = new List<Transaction>
        {
            Transaction.Create(account.Id, MoneyValue.Create("USD", 500m), new DateOnly(2026, 1, 5), "Deposit"),
            Transaction.Create(account.Id, MoneyValue.Create("USD", -200m), new DateOnly(2026, 1, 10), "Expense"),
        };

        _accountRepo
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Account> { account });

        _transactionRepo
            .Setup(r => r.GetByDateRangeAsync(
                new DateOnly(2026, 1, 1),
                new DateOnly(2026, 1, 14),
                account.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(priorTransactions);

        var service = CreateService();

        // Act - Opening balance for Jan 15 includes initial + transactions BEFORE Jan 15
        var result = await service.GetOpeningBalanceForDateAsync(new DateOnly(2026, 1, 15));

        // Assert - Initial 1000 + 500 - 200 = 1300 (no transactions on Jan 15 included)
        Assert.Equal(1300m, result.Amount);
    }

    [Fact]
    public async Task GetOpeningBalanceForDateAsync_AccountStartsAfterDate_ExcludesAccount()
    {
        // Arrange - Account starts Jan 20, query for opening balance on Jan 15
        var account = Account.Create(
            "Checking",
            AccountType.Checking,
            MoneyValue.Create("USD", 1000m),
            new DateOnly(2026, 1, 20));

        _accountRepo
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Account> { account });

        var service = CreateService();

        // Act - Account doesn't exist yet on Jan 15
        var result = await service.GetOpeningBalanceForDateAsync(new DateOnly(2026, 1, 15));

        // Assert - Account excluded
        Assert.Equal(0m, result.Amount);
    }

    [Fact]
    public async Task GetOpeningBalanceForDateAsync_CalendarScenario_AccountStartsOnGridStartDate()
    {
        // Arrange - Account created on grid start date
        // Opening balance should EXCLUDE this - it's handled by GetInitialBalancesByDateRangeAsync
        var gridStartDate = new DateOnly(2025, 12, 28);

        var account = Account.Create(
            "New Year Account",
            AccountType.Checking,
            MoneyValue.Create("USD", 500m),
            gridStartDate); // Created on grid start date

        _accountRepo
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Account> { account });

        var service = CreateService();

        // Act - Opening balance for grid start date
        var result = await service.GetOpeningBalanceForDateAsync(gridStartDate);

        // Assert - Account starting on query date is excluded (handled by GetInitialBalancesByDateRangeAsync)
        Assert.Equal(0m, result.Amount);
    }

    [Fact]
    public async Task GetOpeningBalanceForDateAsync_MultipleAccountsMixedStartDates()
    {
        // Arrange - Mixed scenario with accounts starting before, on, and after the date
        var queryDate = new DateOnly(2025, 12, 28);

        var oldAccount = Account.Create(
            "Old Account",
            AccountType.Checking,
            MoneyValue.Create("USD", 2000m),
            new DateOnly(2025, 12, 1));

        var sameDayAccount = Account.Create(
            "Same Day Account",
            AccountType.Checking,
            MoneyValue.Create("USD", 500m),
            queryDate);

        var futureAccount = Account.Create(
            "Future Account",
            AccountType.Checking,
            MoneyValue.Create("USD", 300m),
            new DateOnly(2026, 1, 5));

        // Transactions for old account before query date
        var oldAccountTransactions = new List<Transaction>
        {
            Transaction.Create(oldAccount.Id, MoneyValue.Create("USD", 100m), new DateOnly(2025, 12, 15), "Deposit"),
        };

        _accountRepo
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Account> { oldAccount, sameDayAccount, futureAccount });

        _transactionRepo
            .Setup(r => r.GetByDateRangeAsync(
                new DateOnly(2025, 12, 1),
                new DateOnly(2025, 12, 27),
                oldAccount.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(oldAccountTransactions);

        var service = CreateService();

        // Act - Opening balance for Dec 28
        var result = await service.GetOpeningBalanceForDateAsync(queryDate);

        // Assert - Old: 2000 + 100 = 2100, Same Day: excluded, Future: excluded
        // Total: 2100
        Assert.Equal(2100m, result.Amount);
    }

    // ========== GetInitialBalancesByDateRangeAsync Tests ==========

    [Fact]
    public async Task GetInitialBalancesByDateRangeAsync_NoAccountsInRange_ReturnsEmptyDictionary()
    {
        // Arrange
        var account = Account.Create(
            "Old Account",
            AccountType.Checking,
            MoneyValue.Create("USD", 1000m),
            new DateOnly(2025, 1, 1));

        _accountRepo
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Account> { account });

        var service = CreateService();

        // Act - Query for accounts starting in Dec 2025
        var result = await service.GetInitialBalancesByDateRangeAsync(
            new DateOnly(2025, 12, 1),
            new DateOnly(2025, 12, 31));

        // Assert - No accounts start in this range
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetInitialBalancesByDateRangeAsync_AccountStartsInRange_ReturnsInitialBalance()
    {
        // Arrange - Account starts on Jan 30 within the grid
        var account = Account.Create(
            "BoA",
            AccountType.Checking,
            MoneyValue.Create("USD", 5000m),
            new DateOnly(2026, 1, 30));

        _accountRepo
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Account> { account });

        var service = CreateService();

        // Act - Query for grid range (Dec 28 - Feb 7 for January 2026)
        var result = await service.GetInitialBalancesByDateRangeAsync(
            new DateOnly(2025, 12, 28),
            new DateOnly(2026, 2, 7));

        // Assert - Account starts on Jan 30 with $5000
        Assert.Single(result);
        Assert.True(result.ContainsKey(new DateOnly(2026, 1, 30)));
        Assert.Equal(5000m, result[new DateOnly(2026, 1, 30)]);
    }

    [Fact]
    public async Task GetInitialBalancesByDateRangeAsync_MultipleAccountsSameDate_SumsBalances()
    {
        // Arrange - Two accounts starting on same date
        var account1 = Account.Create(
            "Account1",
            AccountType.Checking,
            MoneyValue.Create("USD", 1000m),
            new DateOnly(2026, 1, 15));

        var account2 = Account.Create(
            "Account2",
            AccountType.Savings,
            MoneyValue.Create("USD", 2000m),
            new DateOnly(2026, 1, 15));

        _accountRepo
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Account> { account1, account2 });

        var service = CreateService();

        // Act
        var result = await service.GetInitialBalancesByDateRangeAsync(
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 31));

        // Assert - Both accounts starting on Jan 15 should sum to 3000
        Assert.Single(result);
        Assert.Equal(3000m, result[new DateOnly(2026, 1, 15)]);
    }

    [Fact]
    public async Task GetInitialBalancesByDateRangeAsync_MultipleAccountsDifferentDates_ReturnsSeparateEntries()
    {
        // Arrange
        var account1 = Account.Create(
            "Account1",
            AccountType.Checking,
            MoneyValue.Create("USD", 1000m),
            new DateOnly(2026, 1, 10));

        var account2 = Account.Create(
            "Account2",
            AccountType.Savings,
            MoneyValue.Create("USD", 2000m),
            new DateOnly(2026, 1, 20));

        _accountRepo
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Account> { account1, account2 });

        var service = CreateService();

        // Act
        var result = await service.GetInitialBalancesByDateRangeAsync(
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 31));

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(1000m, result[new DateOnly(2026, 1, 10)]);
        Assert.Equal(2000m, result[new DateOnly(2026, 1, 20)]);
    }
}
