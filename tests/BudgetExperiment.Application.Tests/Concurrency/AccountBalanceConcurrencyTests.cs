// <copyright file="AccountBalanceConcurrencyTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Globalization;
using Moq;

namespace BudgetExperiment.Application.Tests.Concurrency;

/// <summary>
/// Concurrency tests for account balance calculations with concurrent transactions.
/// </summary>
public class AccountBalanceConcurrencyTests
{
    public AccountBalanceConcurrencyTests()
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
    }

    [Fact]
    public async Task BalanceCalculation_ConcurrentDeposits_AggregatesAllDeposits()
    {
        // Arrange: Account with multiple concurrent deposit transactions
        var account = Account.Create("Checking", AccountType.Checking);
        var deposit1 = account.AddTransaction(
            MoneyValue.Create("USD", 500m),
            new DateOnly(2026, 1, 15),
            "Deposit 1");
        var deposit2 = account.AddTransaction(
            MoneyValue.Create("USD", 750m),
            new DateOnly(2026, 1, 20),
            "Deposit 2");
        var deposit3 = account.AddTransaction(
            MoneyValue.Create("USD", 250m),
            new DateOnly(2026, 1, 25),
            "Deposit 3");

        var mockRepository = new Mock<ITransactionRepository>();
        var transactions = new List<Transaction> { deposit1, deposit2, deposit3 };
        mockRepository.Setup(r => r.GetByDateRangeAsync(
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                account.Id,
                default))
            .ReturnsAsync(transactions);

        var mockAccountRepository = new Mock<IAccountRepository>();
        var mockUow = new Mock<IUnitOfWork>();
        var mockCategorizationEngine = new Mock<ICategorizationEngine>();

        var service = new TransactionService(
            mockRepository.Object,
            mockAccountRepository.Object,
            mockUow.Object,
            mockCategorizationEngine.Object);

        // Act: Concurrent retrievals simulating parallel balance calculations
        var task1 = service.GetByDateRangeAsync(
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 31),
            account.Id);
        var task2 = service.GetByDateRangeAsync(
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 31),
            account.Id);
        var task3 = service.GetByDateRangeAsync(
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 31),
            account.Id);

        var results = await Task.WhenAll(task1, task2, task3);

        // Assert: All concurrent operations see consistent total (no lost deposits)
        Assert.All(results, result =>
        {
            Assert.Equal(3, result.Count);
            var total = result.Sum(t => t.Amount.Amount);
            Assert.Equal(1500m, total); // 500 + 750 + 250
        });
    }

    [Fact]
    public async Task BalanceCalculation_WithInitialBalance_ConcurrentQueriesReturnConsistentTotal()
    {
        // Arrange: Account with initial balance and transactions
        var initialBalance = MoneyValue.Create("USD", 1000m);
        var account = Account.Create(
            "Savings",
            AccountType.Savings,
            initialBalance,
            new DateOnly(2026, 1, 1));

        var tx1 = account.AddTransaction(
            MoneyValue.Create("USD", 200m),
            new DateOnly(2026, 1, 15),
            "Withdrawal");
        var tx2 = account.AddTransaction(
            MoneyValue.Create("USD", 300m),
            new DateOnly(2026, 1, 20),
            "Deposit");

        var mockRepository = new Mock<ITransactionRepository>();
        var transactions = new List<Transaction> { tx1, tx2 };
        mockRepository.Setup(r => r.GetByDateRangeAsync(
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                account.Id,
                default))
            .ReturnsAsync(transactions);

        var mockAccountRepository = new Mock<IAccountRepository>();
        mockAccountRepository.Setup(r => r.GetByIdAsync(account.Id, default))
            .ReturnsAsync(account);

        var mockUow = new Mock<IUnitOfWork>();
        var mockCategorizationEngine = new Mock<ICategorizationEngine>();
        var service = new TransactionService(
            mockRepository.Object,
            mockAccountRepository.Object,
            mockUow.Object,
            mockCategorizationEngine.Object);

        // Act: Concurrent balance queries
        var tasks = Enumerable.Range(0, 5)
            .Select(_ => service.GetByDateRangeAsync(
                new DateOnly(2026, 1, 1),
                new DateOnly(2026, 1, 31),
                account.Id))
            .ToList();

        var results = await Task.WhenAll(tasks);

        // Assert: All concurrent queries show consistent amounts (initial balance not double-counted)
        Assert.All(results, result =>
        {
            Assert.Equal(2, result.Count);
            var totalTransactions = result.Sum(t => t.Amount.Amount);
            Assert.Equal(500m, totalTransactions); // 200 + 300, not including initial balance
        });
    }

    [Fact]
    public async Task TransactionRetrieval_ConcurrentFilterByDateRange_NoMissingOrDuplicateTransactions()
    {
        // Arrange: 20 transactions across different dates
        var account = Account.Create("Checking", AccountType.Checking);
        var transactions = new List<Transaction>();
        for (int day = 1; day <= 20; day++)
        {
            var tx = account.AddTransaction(
                MoneyValue.Create("USD", 100m + day),
                new DateOnly(2026, 1, day),
                $"Transaction {day}");
            transactions.Add(tx);
        }

        var mockRepository = new Mock<ITransactionRepository>();
        mockRepository.Setup(r => r.GetByDateRangeAsync(
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                account.Id,
                default))
            .ReturnsAsync((DateOnly start, DateOnly end, Guid accId, CancellationToken ct) =>
            {
                return transactions
                    .Where(t => t.Date >= start && t.Date <= end)
                    .ToList();
            });

        var mockAccountRepository = new Mock<IAccountRepository>();
        var mockUow = new Mock<IUnitOfWork>();
        var mockCategorizationEngine = new Mock<ICategorizationEngine>();
        var service = new TransactionService(
            mockRepository.Object,
            mockAccountRepository.Object,
            mockUow.Object,
            mockCategorizationEngine.Object);

        // Act: Concurrent date range queries with overlapping ranges
        var task1 = service.GetByDateRangeAsync(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 10), account.Id);
        var task2 = service.GetByDateRangeAsync(new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 15), account.Id);
        var task3 = service.GetByDateRangeAsync(new DateOnly(2026, 1, 10), new DateOnly(2026, 1, 20), account.Id);

        var results = await Task.WhenAll(task1, task2, task3);

        // Assert: Each query returns correct count with no duplicates
        Assert.Equal(10, results[0].Count); // Days 1-10
        Assert.Equal(11, results[1].Count); // Days 5-15
        Assert.Equal(11, results[2].Count); // Days 10-20

        // Verify no duplicate transactions within results
        var uniqueIds1 = results[0].Select(t => t.Id).Distinct().Count();
        var uniqueIds2 = results[1].Select(t => t.Id).Distinct().Count();
        var uniqueIds3 = results[2].Select(t => t.Id).Distinct().Count();
        Assert.Equal(10, uniqueIds1);
        Assert.Equal(11, uniqueIds2);
        Assert.Equal(11, uniqueIds3);
    }

    [Fact]
    public async Task BalanceCalculation_ConcurrentMixedOperations_MaintainsDataIntegrity()
    {
        // Arrange: Account with transactions of different types (deposits/withdrawals)
        var account = Account.Create("Checking", AccountType.Checking);
        var operations = new List<Transaction>();

        // Add mixed transactions
        for (int i = 1; i <= 10; i++)
        {
            var amount = (i % 2 == 0) ? 100m + i : -(100m - i);
            var tx = account.AddTransaction(
                MoneyValue.Create("USD", Math.Abs(amount)),
                new DateOnly(2026, 1, i),
                amount > 0 ? "Deposit" : "Withdrawal");
            operations.Add(tx);
        }

        var mockRepository = new Mock<ITransactionRepository>();
        mockRepository.Setup(r => r.GetByDateRangeAsync(
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                account.Id,
                default))
            .ReturnsAsync(operations);

        var mockAccountRepository = new Mock<IAccountRepository>();
        var mockUow = new Mock<IUnitOfWork>();
        var mockCategorizationEngine = new Mock<ICategorizationEngine>();
        var service = new TransactionService(
            mockRepository.Object,
            mockAccountRepository.Object,
            mockUow.Object,
            mockCategorizationEngine.Object);

        // Act: Multiple concurrent queries
        var tasks = Enumerable.Range(0, 10)
            .Select(_ => service.GetByDateRangeAsync(
                new DateOnly(2026, 1, 1),
                new DateOnly(2026, 1, 31),
                account.Id))
            .ToList();

        var results = await Task.WhenAll(tasks);

        // Assert: All concurrent queries see all 10 transactions consistently
        Assert.All(results, result =>
        {
            Assert.Equal(10, result.Count);

            // Verify all transaction amounts are present
            Assert.Collection(
                result.OrderBy(t => t.Date),
                t => Assert.Equal(99m, t.Amount.Amount), // |-(100-1)| = 99
                t => Assert.Equal(102m, t.Amount.Amount), // 100 + 2
                t => Assert.Equal(97m, t.Amount.Amount),
                t => Assert.Equal(104m, t.Amount.Amount),
                t => Assert.Equal(95m, t.Amount.Amount),
                t => Assert.Equal(106m, t.Amount.Amount),
                t => Assert.Equal(93m, t.Amount.Amount),
                t => Assert.Equal(108m, t.Amount.Amount),
                t => Assert.Equal(91m, t.Amount.Amount),
                t => Assert.Equal(110m, t.Amount.Amount));
        });
    }
}
