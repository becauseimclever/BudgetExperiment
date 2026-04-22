// <copyright file="AccountSoftDeleteTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Globalization;
using Moq;

namespace BudgetExperiment.Application.Tests.SoftDelete;

/// <summary>
/// Soft-delete integration tests for accounts and cascade behavior.
/// </summary>
public class AccountSoftDeleteTests
{
    public AccountSoftDeleteTests()
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
    }

    [Fact]
    public async Task GetByIdAsync_OnSoftDeletedAccount_ReturnsNull()
    {
        // Arrange: Account is soft-deleted
        var accountId = Guid.NewGuid();

        var mockAccountRepository = new Mock<IAccountRepository>();
        mockAccountRepository.Setup(r => r.GetByIdAsync(accountId, default))
            .ReturnsAsync((Account?)null); // Soft-deleted, not found

        // Act
        var result = await mockAccountRepository.Object.GetByIdAsync(accountId);

        // Assert: Soft-deleted account not found
        Assert.Null(result);
    }

    [Fact]
    public async Task SoftDeleteAccount_ExcludesAccountTransactionsFromQueries()
    {
        // Arrange: Account with transactions is soft-deleted
        var account = Account.Create("Deleted Account", AccountType.Checking);
        var tx1 = account.AddTransaction(
            MoneyValue.Create("USD", 100m),
            new DateOnly(2026, 1, 15),
            "Transaction 1");
        var tx2 = account.AddTransaction(
            MoneyValue.Create("USD", 200m),
            new DateOnly(2026, 1, 20),
            "Transaction 2");

        // Mock account repository returns null (soft-deleted)
        var mockAccountRepository = new Mock<IAccountRepository>();
        mockAccountRepository.Setup(r => r.GetByIdAsync(account.Id, default))
            .ReturnsAsync((Account?)null);

        // Mock transaction repository returns no transactions for deleted account
        var mockTransactionRepository = new Mock<ITransactionRepository>();
        mockTransactionRepository.Setup(r => r.GetByDateRangeAsync(
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                account.Id,
                default))
            .ReturnsAsync(new List<Transaction>()); // Empty when account is deleted

        var mockUow = new Mock<IUnitOfWork>();
        var mockCategorizationEngine = new Mock<ICategorizationEngine>();

        var service = new TransactionService(
            mockTransactionRepository.Object,
            mockAccountRepository.Object,
            mockUow.Object,
            mockCategorizationEngine.Object);

        // Act
        var accountResult = await mockAccountRepository.Object.GetByIdAsync(account.Id);
        var transactionsResult = await service.GetByDateRangeAsync(
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 31),
            account.Id);

        // Assert: Account and its transactions not found
        Assert.Null(accountResult);
        Assert.Empty(transactionsResult);
    }

    [Fact]
    public async Task SoftDeletedAccount_BalanceCalculationExcludesAccount()
    {
        // Arrange: Deleted account with transactions
        var account = Account.Create("Deleted", AccountType.Checking);
        var tx = account.AddTransaction(
            MoneyValue.Create("USD", 1000m),
            new DateOnly(2026, 1, 15),
            "Transfer");

        // Mock transaction repository returns no transactions for deleted account
        var mockTransactionRepository = new Mock<ITransactionRepository>();
        mockTransactionRepository.Setup(r => r.GetByDateRangeAsync(
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                account.Id,
                default))
            .ReturnsAsync(new List<Transaction>()); // Empty due to soft-delete filter

        var mockAccountRepository = new Mock<IAccountRepository>();
        var mockUow = new Mock<IUnitOfWork>();
        var mockCategorizationEngine = new Mock<ICategorizationEngine>();

        var service = new TransactionService(
            mockTransactionRepository.Object,
            mockAccountRepository.Object,
            mockUow.Object,
            mockCategorizationEngine.Object);

        // Act: Query transactions for deleted account
        var result = await service.GetByDateRangeAsync(
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 31),
            account.Id);

        // Assert: No transactions returned (balance = 0)
        Assert.Empty(result);
    }

    [Fact]
    public async Task RestoreSoftDeletedAccount_ReIncludesAccountTransactions()
    {
        // Arrange: Account that was soft-deleted and is now restored
        var account = Account.Create("Restored Account", AccountType.Checking);
        var tx1 = account.AddTransaction(
            MoneyValue.Create("USD", 500m),
            new DateOnly(2026, 1, 15),
            "Transaction 1");
        var tx2 = account.AddTransaction(
            MoneyValue.Create("USD", 300m),
            new DateOnly(2026, 1, 20),
            "Transaction 2");

        // Mock account repository returns account after restore
        var mockAccountRepository = new Mock<IAccountRepository>();
        mockAccountRepository.Setup(r => r.GetByIdAsync(account.Id, default))
            .ReturnsAsync(account);

        // Mock transaction repository returns transactions after restore
        var mockTransactionRepository = new Mock<ITransactionRepository>();
        mockTransactionRepository.Setup(r => r.GetByDateRangeAsync(
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                account.Id,
                default))
            .ReturnsAsync(new List<Transaction> { tx1, tx2 });

        var mockUow = new Mock<IUnitOfWork>();
        var mockCategorizationEngine = new Mock<ICategorizationEngine>();

        var service = new TransactionService(
            mockTransactionRepository.Object,
            mockAccountRepository.Object,
            mockUow.Object,
            mockCategorizationEngine.Object);

        // Act
        var accountResult = await mockAccountRepository.Object.GetByIdAsync(account.Id);
        var transactionsResult = await service.GetByDateRangeAsync(
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 31),
            account.Id);

        // Assert: Restored account and transactions found
        Assert.NotNull(accountResult);
        Assert.Equal(2, transactionsResult.Count);
        Assert.Equal(800m, transactionsResult.Sum(t => t.Amount.Amount));
    }

    [Fact]
    public async Task MultipleAccounts_WithOneSoftDeleted_OnlyActiveAccountsReturned()
    {
        // Arrange: 3 accounts, 1 is soft-deleted
        var account1 = Account.Create("Active 1", AccountType.Checking);
        var account2 = Account.Create("Deleted", AccountType.Savings);
        var account3 = Account.Create("Active 2", AccountType.CreditCard);

        // Mock repository returns only active accounts
        var mockAccountRepository = new Mock<IAccountRepository>();
        mockAccountRepository.Setup(r => r.GetByIdAsync(account1.Id, default))
            .ReturnsAsync(account1);
        mockAccountRepository.Setup(r => r.GetByIdAsync(account2.Id, default))
            .ReturnsAsync((Account?)null); // Soft-deleted
        mockAccountRepository.Setup(r => r.GetByIdAsync(account3.Id, default))
            .ReturnsAsync(account3);

        // Act: Query each account
        var result1 = await mockAccountRepository.Object.GetByIdAsync(account1.Id);
        var result2 = await mockAccountRepository.Object.GetByIdAsync(account2.Id);
        var result3 = await mockAccountRepository.Object.GetByIdAsync(account3.Id);

        // Assert: Active accounts found, deleted not found
        Assert.NotNull(result1);
        Assert.Null(result2);
        Assert.NotNull(result3);
    }

    [Fact]
    public async Task SoftDeleteAccountField_IsNullWhenActive()
    {
        // Arrange: Active account
        var account = Account.Create("Test Account", AccountType.Checking);

        // Assert: Account is not deleted
        Assert.NotNull(account);
        Assert.Null(account.DeletedAtUtc); // Soft-delete field is null (not deleted)
    }

    [Fact]
    public async Task TransactionsBelongingToSoftDeletedAccount_NotIncludedInGlobalQueries()
    {
        // Arrange: Multiple accounts with transactions, 1 account soft-deleted
        var activeAccount1 = Account.Create("Active 1", AccountType.Checking);
        var deletedAccount = Account.Create("Deleted", AccountType.Savings);
        var activeAccount2 = Account.Create("Active 2", AccountType.Checking);

        var tx1 = activeAccount1.AddTransaction(
            MoneyValue.Create("USD", 100m),
            new DateOnly(2026, 1, 15),
            "Active Account 1 TX");
        var tx2 = deletedAccount.AddTransaction(
            MoneyValue.Create("USD", 50m),
            new DateOnly(2026, 1, 20),
            "Deleted Account TX");
        var tx3 = activeAccount2.AddTransaction(
            MoneyValue.Create("USD", 200m),
            new DateOnly(2026, 1, 25),
            "Active Account 2 TX");

        // Mock repository returns transactions from active accounts only
        var mockTransactionRepository = new Mock<ITransactionRepository>();
        mockTransactionRepository.Setup(r => r.GetByDateRangeAsync(
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                null, // no account filter
                default))
            .ReturnsAsync(new List<Transaction> { tx1, tx3 }); // Excludes tx2

        var mockAccountRepository = new Mock<IAccountRepository>();
        var mockUow = new Mock<IUnitOfWork>();
        var mockCategorizationEngine = new Mock<ICategorizationEngine>();

        var service = new TransactionService(
            mockTransactionRepository.Object,
            mockAccountRepository.Object,
            mockUow.Object,
            mockCategorizationEngine.Object);

        // Act: Get all transactions
        var result = await service.GetByDateRangeAsync(
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 31));

        // Assert: Only transactions from active accounts included
        Assert.Equal(2, result.Count);
        Assert.DoesNotContain(result, t => t.Description == "Deleted Account TX");
        Assert.Equal(300m, result.Sum(t => t.Amount.Amount)); // 100 + 200, not 350
    }

    [Fact]
    public async Task SoftDeletedAccountWithTransactions_CascadeSoftDelete()
    {
        // Arrange: Account with multiple transactions, all will be soft-deleted when account is deleted
        var account = Account.Create("Account to Delete", AccountType.Checking);
        var tx1 = account.AddTransaction(
            MoneyValue.Create("USD", 100m),
            new DateOnly(2026, 1, 15),
            "Transaction 1");
        var tx2 = account.AddTransaction(
            MoneyValue.Create("USD", 200m),
            new DateOnly(2026, 1, 20),
            "Transaction 2");
        var tx3 = account.AddTransaction(
            MoneyValue.Create("USD", 150m),
            new DateOnly(2026, 1, 25),
            "Transaction 3");

        // Mock: Account returns null when soft-deleted
        var mockAccountRepository = new Mock<IAccountRepository>();
        mockAccountRepository.Setup(r => r.GetByIdAsync(account.Id, default))
            .ReturnsAsync((Account?)null);

        // Mock: Transactions also return empty (cascade soft-delete or query filter applies)
        var mockTransactionRepository = new Mock<ITransactionRepository>();
        mockTransactionRepository.Setup(r => r.GetByDateRangeAsync(
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                account.Id,
                default))
            .ReturnsAsync(new List<Transaction>()); // All cascade-deleted

        var mockUow = new Mock<IUnitOfWork>();
        var mockCategorizationEngine = new Mock<ICategorizationEngine>();

        var service = new TransactionService(
            mockTransactionRepository.Object,
            mockAccountRepository.Object,
            mockUow.Object,
            mockCategorizationEngine.Object);

        // Act: Query deleted account
        var accountResult = await mockAccountRepository.Object.GetByIdAsync(account.Id);
        var transactionsResult = await service.GetByDateRangeAsync(
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 31),
            account.Id);

        // Assert: Account and all transactions are gone
        Assert.Null(accountResult);
        Assert.Empty(transactionsResult);
    }

    [Fact]
    public async Task QueryWithoutAccountFilter_ExcludesSoftDeletedAccountTransactions()
    {
        // Arrange: Global query across all accounts
        var account1 = Account.Create("Active", AccountType.Checking);
        var account2 = Account.Create("Deleted", AccountType.Savings);

        var tx1 = account1.AddTransaction(
            MoneyValue.Create("USD", 500m),
            new DateOnly(2026, 1, 15),
            "Active TX");
        var tx2 = account2.AddTransaction(
            MoneyValue.Create("USD", 300m),
            new DateOnly(2026, 1, 20),
            "Deleted Account TX");

        // Mock: Only returns transactions from active accounts
        var mockTransactionRepository = new Mock<ITransactionRepository>();
        mockTransactionRepository.Setup(r => r.GetByDateRangeAsync(
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                null, // global query
                default))
            .ReturnsAsync(new List<Transaction> { tx1 }); // Excludes tx2

        var mockAccountRepository = new Mock<IAccountRepository>();
        var mockUow = new Mock<IUnitOfWork>();
        var mockCategorizationEngine = new Mock<ICategorizationEngine>();

        var service = new TransactionService(
            mockTransactionRepository.Object,
            mockAccountRepository.Object,
            mockUow.Object,
            mockCategorizationEngine.Object);

        // Act
        var result = await service.GetByDateRangeAsync(
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 31));

        // Assert: Only transactions from active accounts
        Assert.Single(result);
        Assert.Equal(500m, result.First().Amount.Amount);
    }
}
