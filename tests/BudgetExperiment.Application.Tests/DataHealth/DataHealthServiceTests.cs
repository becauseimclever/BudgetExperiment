// <copyright file="DataHealthServiceTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Application.DataHealth;
using Moq;

namespace BudgetExperiment.Application.Tests.DataHealth;

/// <summary>
/// Unit tests for <see cref="DataHealthService"/> duplicate and outlier detection.
/// </summary>
public class DataHealthServiceTests
{
    private static readonly Guid AccountId = Guid.NewGuid();

    [Fact]
    public async Task FindDuplicates_Returns_ExactDuplicates()
    {
        // Arrange
        var date = new DateOnly(2026, 3, 1);
        var amount = MoneyValue.Create("USD", -50m);
        var t1 = Transaction.Create(AccountId, amount, date, "Amazon Prime");
        var t2 = Transaction.Create(AccountId, amount, date, "Amazon Prime");

        var txRepo = CreateTransactionRepo([t1, t2]);
        var service = CreateService(txRepo);

        // Act
        var clusters = await service.FindDuplicatesAsync(null, CancellationToken.None);

        // Assert
        Assert.Single(clusters);
        Assert.Equal(2, clusters[0].Transactions.Count);
    }

    [Fact]
    public async Task FindDuplicates_Excludes_Transfers()
    {
        // Arrange
        var date = new DateOnly(2026, 3, 1);
        var amount = MoneyValue.Create("USD", -100m);
        var transferId = Guid.NewGuid();
        var t1 = Transaction.CreateTransfer(AccountId, amount, date, "Transfer", transferId, TransferDirection.Source);
        var t2 = Transaction.CreateTransfer(AccountId, amount, date, "Transfer", transferId, TransferDirection.Destination);

        var txRepo = CreateTransactionRepo([t1, t2]);
        var service = CreateService(txRepo);

        // Act
        var clusters = await service.FindDuplicatesAsync(null, CancellationToken.None);

        // Assert
        Assert.Empty(clusters);
    }

    [Fact]
    public async Task FindDuplicates_Groups_NearDuplicates()
    {
        // Arrange – descriptions differ by one char; Levenshtein similarity = 1 - 1/19 ≈ 0.947
        var date = new DateOnly(2026, 3, 1);
        var amount = MoneyValue.Create("USD", -25m);
        var t1 = Transaction.Create(AccountId, amount, date, "Starbucks Seattle A");
        var t2 = Transaction.Create(AccountId, amount, date, "Starbucks Seattle B");

        var txRepo = CreateTransactionRepo([t1, t2]);
        var service = CreateService(txRepo);

        // Act
        var clusters = await service.FindDuplicatesAsync(null, CancellationToken.None);

        // Assert
        Assert.Single(clusters);
        Assert.Equal(2, clusters[0].Transactions.Count);
    }

    [Fact]
    public async Task FindOutliers_Flags_Transactions_With_3Sigma_Deviation()
    {
        // Arrange — 10 normal at $10 + 1 outlier at $1000 (n=11 ≥ 5); outlier deviates > 3σ
        var date = new DateOnly(2026, 1, 1);
        var transactions = new List<Transaction>();

        for (var i = 0; i < 10; i++)
        {
            transactions.Add(Transaction.Create(
                AccountId,
                MoneyValue.Create("USD", -10m),
                date.AddDays(i),
                "Amazon Prime"));
        }

        transactions.Add(Transaction.Create(
            AccountId,
            MoneyValue.Create("USD", -1000m),
            date.AddDays(10),
            "Amazon Prime"));

        var txRepo = CreateTransactionRepo(transactions);
        var service = CreateService(txRepo);

        // Act
        var outliers = await service.FindOutliersAsync(null, CancellationToken.None);

        // Assert — exactly one outlier (the $1000 transaction)
        Assert.Single(outliers);
        Assert.Equal(1000m, Math.Abs(outliers[0].Transaction.Amount.Amount));
    }

    [Fact]
    public async Task FindOutliers_Ignores_Groups_With_Fewer_Than_5_Transactions()
    {
        // Arrange — 3 normal + 1 potential outlier = 4 total (< 5 minimum group size)
        var date = new DateOnly(2026, 2, 1);
        var transactions = new List<Transaction>
        {
            Transaction.Create(AccountId, MoneyValue.Create("USD", -10m), date, "Netflix"),
            Transaction.Create(AccountId, MoneyValue.Create("USD", -10m), date.AddDays(1), "Netflix"),
            Transaction.Create(AccountId, MoneyValue.Create("USD", -10m), date.AddDays(2), "Netflix"),
            Transaction.Create(AccountId, MoneyValue.Create("USD", -1000m), date.AddDays(3), "Netflix"),
        };

        var txRepo = CreateTransactionRepo(transactions);
        var service = CreateService(txRepo);

        // Act
        var outliers = await service.FindOutliersAsync(null, CancellationToken.None);

        // Assert
        Assert.Empty(outliers);
    }

    /// <summary>Creates a mock transaction repository returning the given transactions.</summary>
    /// <param name="transactions">Transactions to return from health analysis query.</param>
    /// <returns>Configured mock.</returns>
    private static Mock<ITransactionRepository> CreateTransactionRepo(IEnumerable<Transaction> transactions)
    {
        var mock = new Mock<ITransactionRepository>();
        mock.Setup(r => r.GetAllForHealthAnalysisAsync(It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactions.ToList());
        return mock;
    }

    /// <summary>Creates a <see cref="DataHealthService"/> with the given transaction repository mock.</summary>
    /// <param name="txRepo">Transaction repository mock.</param>
    /// <returns>Configured service under test.</returns>
    private static DataHealthService CreateService(Mock<ITransactionRepository> txRepo)
    {
        var dismissedRepo = new Mock<IDismissedOutlierRepository>();
        dismissedRepo
            .Setup(r => r.GetDismissedTransactionIdsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Guid>());

        var accountRepo = new Mock<IAccountRepository>();
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);

        return new DataHealthService(txRepo.Object, accountRepo.Object, dismissedRepo.Object, uow.Object);
    }
}
