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
}
