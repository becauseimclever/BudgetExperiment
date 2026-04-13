// <copyright file="DataHealthServiceTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Reflection;

using BudgetExperiment.Application.DataHealth;
using BudgetExperiment.Application.FeatureFlags;
using BudgetExperiment.Domain.DataHealth;

using Moq;

namespace BudgetExperiment.Application.Tests.DataHealth;

/// <summary>
/// Unit tests for <see cref="DataHealthService"/> duplicate and outlier detection.
/// </summary>
public class DataHealthServiceTests
{
    private static readonly Guid AccountId = Guid.NewGuid();

    [Fact]
    public async Task AnalyzeAsync_WithFeatureFlagEnabled_UsesProjectionQueriesOnceEach()
    {
        // Arrange
        var txRepo = new Mock<ITransactionRepository>();
        txRepo.Setup(r => r.GetUncategorizedAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Transaction>());
        txRepo.Setup(r => r.GetTransactionProjectionsForDuplicateDetectionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<DuplicateDetectionProjection>());
        txRepo.Setup(r => r.GetTransactionAmountsForOutlierAnalysisAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<OutlierProjection>());
        txRepo.Setup(r => r.GetTransactionDatesForGapAnalysisAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<DateGapProjection>());

        var featureFlags = new Mock<IFeatureFlagService>();
        featureFlags.Setup(f => f.IsEnabledAsync("feature-data-health-optimized-analysis", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var dismissed = new Mock<IDismissedOutlierRepository>();
        dismissed.Setup(r => r.GetDismissedTransactionIdsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Guid>());

        var accounts = new Mock<IAccountRepository>();
        accounts.Setup(a => a.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Account>());

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var service = new DataHealthService(
            txRepo.Object,
            accounts.Object,
            dismissed.Object,
            uow.Object,
            featureFlags.Object);

        // Act
        _ = await service.AnalyzeAsync(null, CancellationToken.None);

        // Assert
        txRepo.Verify(r => r.GetTransactionProjectionsForDuplicateDetectionAsync(It.IsAny<CancellationToken>()), Times.Once);
        txRepo.Verify(r => r.GetTransactionAmountsForOutlierAnalysisAsync(It.IsAny<CancellationToken>()), Times.Once);
        txRepo.Verify(r => r.GetTransactionDatesForGapAnalysisAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AnalyzeAsync_WithFeatureFlagDisabled_BehavesAsOriginal()
    {
        // Arrange
        var txRepo = new Mock<ITransactionRepository>();
        txRepo.Setup(r => r.GetUncategorizedAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Transaction>());
        txRepo.Setup(r => r.GetTransactionProjectionsForDuplicateDetectionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<DuplicateDetectionProjection>());
        txRepo.Setup(r => r.GetTransactionAmountsForOutlierAnalysisAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<OutlierProjection>());
        txRepo.Setup(r => r.GetTransactionDatesForGapAnalysisAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<DateGapProjection>());

        var featureFlags = new Mock<IFeatureFlagService>();
        featureFlags.Setup(f => f.IsEnabledAsync("feature-data-health-optimized-analysis", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var dismissed = new Mock<IDismissedOutlierRepository>();
        dismissed.Setup(r => r.GetDismissedTransactionIdsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Guid>());

        var accounts = new Mock<IAccountRepository>();
        accounts.Setup(a => a.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Account>());

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var service = new DataHealthService(
            txRepo.Object,
            accounts.Object,
            dismissed.Object,
            uow.Object,
            featureFlags.Object);

        // Act
        _ = await service.AnalyzeAsync(null, CancellationToken.None);

        // Assert
        txRepo.Verify(r => r.GetTransactionProjectionsForDuplicateDetectionAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
        txRepo.Verify(r => r.GetTransactionAmountsForOutlierAnalysisAsync(It.IsAny<CancellationToken>()), Times.Exactly(1));
        txRepo.Verify(r => r.GetTransactionDatesForGapAnalysisAsync(It.IsAny<CancellationToken>()), Times.Exactly(1));
    }

    [Fact]
    public void FindNearDuplicateClusters_WithLargeInput_DoesNotExceedLinearWindowComparisons()
    {
        // Arrange
        const int itemCount = 1000;
        var accountId = Guid.NewGuid();
        var candidates = new List<Transaction>();

        for (var i = 0; i < itemCount; i++)
        {
            candidates.Add(TransactionFactory.Create(
                accountId,
                MoneyValue.Create("USD", -25m),
                new DateOnly(2026, 1, 1).AddDays(i),
                $"Merchant {i}"));
        }

        var comparisons = 0;
        Func<string, string, bool> comparer = (_, _) =>
        {
            comparisons++;
            return false;
        };

        var method = typeof(DataHealthService)
            .GetMethod(
                "FindNearDuplicateClusters",
                BindingFlags.NonPublic | BindingFlags.Static,
                null,
                [typeof(List<Transaction>), typeof(Func<string, string, bool>)],
                null);
        Assert.NotNull(method);

        // Act
        _ = method!.Invoke(null, new object?[] { candidates, comparer });

        // Assert
        var maxExpected = itemCount * 6;
        Assert.True(comparisons <= maxExpected, $"Expected <= {maxExpected} comparisons, got {comparisons}.");
    }

    [Fact]
    public async Task FindDuplicates_Returns_ExactDuplicates()
    {
        // Arrange
        var date = new DateOnly(2026, 3, 1);
        var amount = MoneyValue.Create("USD", -50m);
        var t1 = TransactionFactory.Create(AccountId, amount, date, "Amazon Prime");
        var t2 = TransactionFactory.Create(AccountId, amount, date, "Amazon Prime");

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
        var t1 = TransactionFactory.CreateTransfer(AccountId, amount, date, "Transfer", transferId, TransferDirection.Source);
        var t2 = TransactionFactory.CreateTransfer(AccountId, amount, date, "Transfer", transferId, TransferDirection.Destination);

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
        var t1 = TransactionFactory.Create(AccountId, amount, date, "Starbucks Seattle A");
        var t2 = TransactionFactory.Create(AccountId, amount, date, "Starbucks Seattle B");

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
            transactions.Add(TransactionFactory.Create(
                AccountId,
                MoneyValue.Create("USD", -10m),
                date.AddDays(i),
                "Amazon Prime"));
        }

        transactions.Add(TransactionFactory.Create(
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
            TransactionFactory.Create(AccountId, MoneyValue.Create("USD", -10m), date, "Netflix"),
            TransactionFactory.Create(AccountId, MoneyValue.Create("USD", -10m), date.AddDays(1), "Netflix"),
            TransactionFactory.Create(AccountId, MoneyValue.Create("USD", -10m), date.AddDays(2), "Netflix"),
            TransactionFactory.Create(AccountId, MoneyValue.Create("USD", -1000m), date.AddDays(3), "Netflix"),
        };

        var txRepo = CreateTransactionRepo(transactions);
        var service = CreateService(txRepo);

        // Act
        var outliers = await service.FindOutliersAsync(null, CancellationToken.None);

        // Assert
        Assert.Empty(outliers);
    }

    // --- AC-125a-06: Date gap detection ---

    /// <summary>AC-125a-06: FindDateGapsAsync returns gaps exceeding the threshold.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task FindDateGaps_Returns_Gaps_Exceeding_Threshold()
    {
        // Arrange — two transactions 59 days apart in an account with > 30 days of history
        var t1 = TransactionFactory.Create(AccountId, MoneyValue.Create("USD", -10m), new DateOnly(2026, 1, 1), "Coffee");
        var t2 = TransactionFactory.Create(AccountId, MoneyValue.Create("USD", -10m), new DateOnly(2026, 3, 1), "Coffee");
        var t3 = TransactionFactory.Create(AccountId, MoneyValue.Create("USD", -10m), new DateOnly(2026, 3, 15), "Coffee");

        var txRepo = CreateTransactionRepo([t1, t2, t3]);
        var service = CreateService(txRepo);

        // Act
        var gaps = await service.FindDateGapsAsync(null, minGapDays: 7, CancellationToken.None);

        // Assert — gap between Jan 1 and Mar 1 = 59 days > 7
        Assert.NotEmpty(gaps);
        Assert.Equal(AccountId, gaps[0].AccountId);
        Assert.True(gaps[0].DurationDays > 7);
    }

    // --- AC-125a-07: Skip accounts with < 30 days history ---

    /// <summary>AC-125a-07: FindDateGapsAsync skips accounts with less than 30 days of history.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task FindDateGaps_Skips_Accounts_With_Less_Than_30_Day_History()
    {
        // Arrange — 14 days of history (< 30 threshold)
        var t1 = TransactionFactory.Create(AccountId, MoneyValue.Create("USD", -10m), new DateOnly(2026, 1, 1), "Coffee");
        var t2 = TransactionFactory.Create(AccountId, MoneyValue.Create("USD", -10m), new DateOnly(2026, 1, 15), "Coffee");

        var txRepo = CreateTransactionRepo([t1, t2]);
        var service = CreateService(txRepo);

        // Act
        var gaps = await service.FindDateGapsAsync(null, minGapDays: 7, CancellationToken.None);

        // Assert
        Assert.Empty(gaps);
    }

    // --- AC-125a-08: Uncategorized summary ---

    /// <summary>AC-125a-08: GetUncategorizedSummaryAsync returns correct counts and totals.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetUncategorizedSummary_Returns_Correct_Counts_And_Totals()
    {
        // Arrange — 2 uncategorized transactions totalling $50
        var t1 = TransactionFactory.Create(AccountId, MoneyValue.Create("USD", -30m), new DateOnly(2026, 1, 1), "Unknown A");
        var t2 = TransactionFactory.Create(AccountId, MoneyValue.Create("USD", -20m), new DateOnly(2026, 1, 2), "Unknown B");

        var txRepo = new Mock<ITransactionRepository>();
        txRepo.Setup(r => r.GetUncategorizedAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(new List<Transaction> { t1, t2 });

        var service = CreateService(txRepo);

        // Act
        var summary = await service.GetUncategorizedSummaryAsync(CancellationToken.None);

        // Assert
        Assert.Equal(2, summary.TotalCount);
        Assert.Equal(50m, summary.TotalAmount);
        Assert.Single(summary.ByAccount);
        Assert.Equal(AccountId, summary.ByAccount[0].AccountId);
    }

    // --- AC-125a-09: MergeDuplicatesAsync transfers category ---

    /// <summary>AC-125a-09: MergeDuplicatesAsync transfers category from duplicate to primary.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task MergeDuplicates_Transfers_Category_From_Duplicate_To_Primary()
    {
        // Arrange
        var category = Guid.NewGuid();
        var primary = TransactionFactory.Create(AccountId, MoneyValue.Create("USD", -50m), new DateOnly(2026, 1, 1), "Amazon");
        var duplicate = TransactionFactory.Create(AccountId, MoneyValue.Create("USD", -50m), new DateOnly(2026, 1, 1), "Amazon");
        duplicate.UpdateCategory(category);

        var txRepo = new Mock<ITransactionRepository>();
        txRepo.Setup(r => r.GetByIdAsync(primary.Id, It.IsAny<CancellationToken>())).ReturnsAsync(primary);
        txRepo.Setup(r => r.GetByIdsAsync(It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(new List<Transaction> { duplicate });
        txRepo.Setup(r => r.RemoveAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);

        var dismissed = new Mock<IDismissedOutlierRepository>();
        var accounts = new Mock<IAccountRepository>();
        accounts.Setup(a => a.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Account>());

        var featureFlags = new Mock<IFeatureFlagService>();
        featureFlags.Setup(f => f.IsEnabledAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var service = new DataHealthService(
            txRepo.Object,
            accounts.Object,
            dismissed.Object,
            uow.Object,
            featureFlags.Object);

        // Act
        await service.MergeDuplicatesAsync(primary.Id, [duplicate.Id], CancellationToken.None);

        // Assert: primary now has the category
        Assert.Equal(category, primary.CategoryId);
        txRepo.Verify(r => r.RemoveAsync(duplicate, It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>Creates a mock transaction repository returning the given transactions.</summary>
    /// <param name="transactions">Transactions to return from health analysis query.</param>
    /// <returns>Configured mock.</returns>
    private static Mock<ITransactionRepository> CreateTransactionRepo(IEnumerable<Transaction> transactions)
    {
        var transactionList = transactions.ToList();
        var mock = new Mock<ITransactionRepository>();
        mock.Setup(r => r.GetTransactionProjectionsForDuplicateDetectionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactionList
                .Where(t => !t.IsTransfer)
                .Select(t => new DuplicateDetectionProjection(
                    t.Id,
                    t.AccountId,
                    t.Date,
                    t.Amount.Amount,
                    t.Description))
                .ToList());
        mock.Setup(r => r.GetTransactionAmountsForOutlierAnalysisAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactionList
                .Select(t => new OutlierProjection(
                    t.Id,
                    t.Description,
                    t.Amount.Amount))
                .ToList());
        mock.Setup(r => r.GetTransactionDatesForGapAnalysisAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactionList
                .Select(t => new DateGapProjection(t.AccountId, t.Date))
                .Distinct()
                .ToList());
        return mock;
    }

    /// <summary>Creates a <see cref="DataHealthService"/> with the given transaction repository mock.</summary>
    /// <param name="txRepo">Transaction repository mock.</param>
    /// <param name="featureFlags">Optional feature flag service override.</param>
    /// <returns>Configured service under test.</returns>
    private static DataHealthService CreateService(
        Mock<ITransactionRepository> txRepo,
        IFeatureFlagService? featureFlags = null)
    {
        var dismissedRepo = new Mock<IDismissedOutlierRepository>();
        dismissedRepo
            .Setup(r => r.GetDismissedTransactionIdsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Guid>());

        var accountRepo = new Mock<IAccountRepository>();
        accountRepo
            .Setup(a => a.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Account>());

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);

        if (featureFlags is null)
        {
            var flags = new Mock<IFeatureFlagService>();
            flags.Setup(f => f.IsEnabledAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            featureFlags = flags.Object;
        }

        return new DataHealthService(txRepo.Object, accountRepo.Object, dismissedRepo.Object, uow.Object, featureFlags);
    }
}
