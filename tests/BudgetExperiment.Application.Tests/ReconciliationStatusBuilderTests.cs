// <copyright file="ReconciliationStatusBuilderTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain;
using Moq;
using Shouldly;

namespace BudgetExperiment.Application.Tests;

/// <summary>
/// Unit tests for ReconciliationStatusBuilder.
/// </summary>
public class ReconciliationStatusBuilderTests
{
    private readonly Mock<IReconciliationMatchRepository> _matchRepository;
    private readonly Mock<IRecurringTransactionRepository> _recurringRepository;
    private readonly Mock<ITransactionRepository> _transactionRepository;
    private readonly Mock<IRecurringInstanceProjector> _instanceProjector;

    public ReconciliationStatusBuilderTests()
    {
        _matchRepository = new Mock<IReconciliationMatchRepository>();
        _recurringRepository = new Mock<IRecurringTransactionRepository>();
        _transactionRepository = new Mock<ITransactionRepository>();
        _instanceProjector = new Mock<IRecurringInstanceProjector>();

        // Default setups
        _recurringRepository
            .Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecurringTransaction>());
        _instanceProjector
            .Setup(p => p.GetInstancesByDateRangeAsync(
                It.IsAny<IReadOnlyList<RecurringTransaction>>(),
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<DateOnly, List<RecurringInstanceInfoValue>>());
        _matchRepository
            .Setup(r => r.GetByPeriodAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ReconciliationMatch>());
    }

    [Fact]
    public async Task GetReconciliationStatusAsync_NoRecurringTransactions_ReturnsEmptyStatus()
    {
        var builder = CreateBuilder();

        var result = await builder.GetReconciliationStatusAsync(2026, 1);

        result.ShouldNotBeNull();
        result.Year.ShouldBe(2026);
        result.Month.ShouldBe(1);
        result.TotalExpectedInstances.ShouldBe(0);
        result.MatchedCount.ShouldBe(0);
        result.PendingCount.ShouldBe(0);
        result.MissingCount.ShouldBe(0);
        result.Instances.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetReconciliationStatusAsync_WithMatchedInstance_ReturnsMatchedStatus()
    {
        var recurringId = Guid.NewGuid();
        var transactionId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var instanceDate = new DateOnly(2026, 1, 15);

        var recurring = CreateTestRecurringTransaction(recurringId, accountId, "Netflix", -15.99m, instanceDate);
        var transaction = CreateTestTransaction(transactionId, accountId, "Netflix", -15.99m, instanceDate);

        var instance = new RecurringInstanceInfoValue(
            recurringId,
            instanceDate,
            accountId,
            "Checking",
            "Netflix",
            MoneyValue.Create("USD", -15.99m),
            null,
            null,
            false,
            false);

        var match = ReconciliationMatch.Create(
            transactionId, recurringId, instanceDate, 0.95m, 0m, 0, BudgetScope.Shared, null);
        match.Accept();

        _recurringRepository
            .Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecurringTransaction> { recurring });
        _instanceProjector
            .Setup(p => p.GetInstancesByDateRangeAsync(
                It.IsAny<IReadOnlyList<RecurringTransaction>>(),
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<DateOnly, List<RecurringInstanceInfoValue>>
            {
                { instanceDate, new List<RecurringInstanceInfoValue> { instance } },
            });
        _matchRepository
            .Setup(r => r.GetByPeriodAsync(2026, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ReconciliationMatch> { match });
        _transactionRepository
            .Setup(r => r.GetByIdAsync(transactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction);

        var builder = CreateBuilder();

        var result = await builder.GetReconciliationStatusAsync(2026, 1);

        result.MatchedCount.ShouldBe(1);
        result.PendingCount.ShouldBe(0);
        result.MissingCount.ShouldBe(0);
        result.TotalExpectedInstances.ShouldBe(1);
        result.Instances[0].Status.ShouldBe("Matched");
        result.Instances[0].MatchedTransactionId.ShouldBe(transactionId);
    }

    [Fact]
    public async Task GetReconciliationStatusAsync_WithPendingMatch_ReturnsPendingStatus()
    {
        var recurringId = Guid.NewGuid();
        var transactionId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var instanceDate = new DateOnly(2026, 1, 15);

        var recurring = CreateTestRecurringTransaction(recurringId, accountId, "Netflix", -15.99m, instanceDate);

        var instance = new RecurringInstanceInfoValue(
            recurringId,
            instanceDate,
            accountId,
            "Checking",
            "Netflix",
            MoneyValue.Create("USD", -15.99m),
            null,
            null,
            false,
            false);

        // Suggested match (not accepted)
        var match = ReconciliationMatch.Create(
            transactionId, recurringId, instanceDate, 0.60m, 0m, 0, BudgetScope.Shared, null);

        _recurringRepository
            .Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecurringTransaction> { recurring });
        _instanceProjector
            .Setup(p => p.GetInstancesByDateRangeAsync(
                It.IsAny<IReadOnlyList<RecurringTransaction>>(),
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<DateOnly, List<RecurringInstanceInfoValue>>
            {
                { instanceDate, new List<RecurringInstanceInfoValue> { instance } },
            });
        _matchRepository
            .Setup(r => r.GetByPeriodAsync(2026, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ReconciliationMatch> { match });

        var builder = CreateBuilder();

        var result = await builder.GetReconciliationStatusAsync(2026, 1);

        result.PendingCount.ShouldBe(1);
        result.MatchedCount.ShouldBe(0);
        result.MissingCount.ShouldBe(0);
        result.Instances[0].Status.ShouldBe("Pending");
    }

    [Fact]
    public async Task GetReconciliationStatusAsync_WithMissingInstance_ReturnsMissingStatus()
    {
        var recurringId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var instanceDate = new DateOnly(2026, 1, 15);

        var recurring = CreateTestRecurringTransaction(recurringId, accountId, "Netflix", -15.99m, instanceDate);

        var instance = new RecurringInstanceInfoValue(
            recurringId,
            instanceDate,
            accountId,
            "Checking",
            "Netflix",
            MoneyValue.Create("USD", -15.99m),
            null,
            null,
            false,
            false);

        _recurringRepository
            .Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecurringTransaction> { recurring });
        _instanceProjector
            .Setup(p => p.GetInstancesByDateRangeAsync(
                It.IsAny<IReadOnlyList<RecurringTransaction>>(),
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<DateOnly, List<RecurringInstanceInfoValue>>
            {
                { instanceDate, new List<RecurringInstanceInfoValue> { instance } },
            });

        var builder = CreateBuilder();

        var result = await builder.GetReconciliationStatusAsync(2026, 1);

        result.MissingCount.ShouldBe(1);
        result.MatchedCount.ShouldBe(0);
        result.PendingCount.ShouldBe(0);
        result.Instances[0].Status.ShouldBe("Missing");
    }

    [Fact]
    public async Task GetReconciliationStatusAsync_SkippedInstances_AreExcluded()
    {
        var recurringId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var instanceDate = new DateOnly(2026, 1, 15);

        var recurring = CreateTestRecurringTransaction(recurringId, accountId, "Netflix", -15.99m, instanceDate);

        var skippedInstance = new RecurringInstanceInfoValue(
            recurringId,
            instanceDate,
            accountId,
            "Checking",
            "Netflix",
            MoneyValue.Create("USD", -15.99m),
            null,
            null,
            false,
            true);

        _recurringRepository
            .Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecurringTransaction> { recurring });
        _instanceProjector
            .Setup(p => p.GetInstancesByDateRangeAsync(
                It.IsAny<IReadOnlyList<RecurringTransaction>>(),
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<DateOnly, List<RecurringInstanceInfoValue>>
            {
                { instanceDate, new List<RecurringInstanceInfoValue> { skippedInstance } },
            });

        var builder = CreateBuilder();

        var result = await builder.GetReconciliationStatusAsync(2026, 1);

        result.TotalExpectedInstances.ShouldBe(0);
        result.Instances.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetReconciliationStatusAsync_AutoMatchedInstance_CountsAsMatched()
    {
        var recurringId = Guid.NewGuid();
        var transactionId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var instanceDate = new DateOnly(2026, 1, 15);

        var recurring = CreateTestRecurringTransaction(recurringId, accountId, "Netflix", -15.99m, instanceDate);
        var transaction = CreateTestTransaction(transactionId, accountId, "Netflix", -15.99m, instanceDate);

        var instance = new RecurringInstanceInfoValue(
            recurringId,
            instanceDate,
            accountId,
            "Checking",
            "Netflix",
            MoneyValue.Create("USD", -15.99m),
            null,
            null,
            false,
            false);

        var match = ReconciliationMatch.Create(
            transactionId, recurringId, instanceDate, 0.95m, 0m, 0, BudgetScope.Shared, null);
        match.AutoMatch();

        _recurringRepository
            .Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecurringTransaction> { recurring });
        _instanceProjector
            .Setup(p => p.GetInstancesByDateRangeAsync(
                It.IsAny<IReadOnlyList<RecurringTransaction>>(),
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<DateOnly, List<RecurringInstanceInfoValue>>
            {
                { instanceDate, new List<RecurringInstanceInfoValue> { instance } },
            });
        _matchRepository
            .Setup(r => r.GetByPeriodAsync(2026, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ReconciliationMatch> { match });
        _transactionRepository
            .Setup(r => r.GetByIdAsync(transactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction);

        var builder = CreateBuilder();

        var result = await builder.GetReconciliationStatusAsync(2026, 1);

        result.MatchedCount.ShouldBe(1);
        result.Instances[0].Status.ShouldBe("Matched");
    }

    [Fact]
    public async Task GetReconciliationStatusAsync_MixedStatuses_ReturnsCorrectCounts()
    {
        var accountId = Guid.NewGuid();
        var recurringId1 = Guid.NewGuid();
        var recurringId2 = Guid.NewGuid();
        var recurringId3 = Guid.NewGuid();
        var transactionId1 = Guid.NewGuid();
        var transactionId2 = Guid.NewGuid();
        var date1 = new DateOnly(2026, 1, 5);
        var date2 = new DateOnly(2026, 1, 15);
        var date3 = new DateOnly(2026, 1, 25);

        var recurring1 = CreateTestRecurringTransaction(recurringId1, accountId, "Netflix", -15.99m, date1);
        var recurring2 = CreateTestRecurringTransaction(recurringId2, accountId, "Spotify", -9.99m, date2);
        var recurring3 = CreateTestRecurringTransaction(recurringId3, accountId, "Gym", -45.00m, date3);
        var transaction1 = CreateTestTransaction(transactionId1, accountId, "Netflix", -15.99m, date1);

        var instance1 = new RecurringInstanceInfoValue(
            recurringId1,
            date1,
            accountId,
            "Checking",
            "Netflix",
            MoneyValue.Create("USD", -15.99m),
            null,
            null,
            false,
            false);
        var instance2 = new RecurringInstanceInfoValue(
            recurringId2,
            date2,
            accountId,
            "Checking",
            "Spotify",
            MoneyValue.Create("USD", -9.99m),
            null,
            null,
            false,
            false);
        var instance3 = new RecurringInstanceInfoValue(
            recurringId3,
            date3,
            accountId,
            "Checking",
            "Gym",
            MoneyValue.Create("USD", -45.00m),
            null,
            null,
            false,
            false);

        // Instance 1: Accepted, Instance 2: Suggested (pending), Instance 3: Missing
        var acceptedMatch = ReconciliationMatch.Create(
            transactionId1, recurringId1, date1, 0.95m, 0m, 0, BudgetScope.Shared, null);
        acceptedMatch.Accept();

        var pendingMatch = ReconciliationMatch.Create(
            transactionId2, recurringId2, date2, 0.60m, 0m, 0, BudgetScope.Shared, null);

        _recurringRepository
            .Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecurringTransaction> { recurring1, recurring2, recurring3 });
        _instanceProjector
            .Setup(p => p.GetInstancesByDateRangeAsync(
                It.IsAny<IReadOnlyList<RecurringTransaction>>(),
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<DateOnly, List<RecurringInstanceInfoValue>>
            {
                { date1, new List<RecurringInstanceInfoValue> { instance1 } },
                { date2, new List<RecurringInstanceInfoValue> { instance2 } },
                { date3, new List<RecurringInstanceInfoValue> { instance3 } },
            });
        _matchRepository
            .Setup(r => r.GetByPeriodAsync(2026, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ReconciliationMatch> { acceptedMatch, pendingMatch });
        _transactionRepository
            .Setup(r => r.GetByIdAsync(transactionId1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction1);

        var builder = CreateBuilder();

        var result = await builder.GetReconciliationStatusAsync(2026, 1);

        result.TotalExpectedInstances.ShouldBe(3);
        result.MatchedCount.ShouldBe(1);
        result.PendingCount.ShouldBe(1);
        result.MissingCount.ShouldBe(1);
    }

    [Fact]
    public async Task GetReconciliationStatusAsync_IncludesExpectedAmount()
    {
        var recurringId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var instanceDate = new DateOnly(2026, 1, 15);

        var recurring = CreateTestRecurringTransaction(recurringId, accountId, "Netflix", -15.99m, instanceDate);

        var instance = new RecurringInstanceInfoValue(
            recurringId,
            instanceDate,
            accountId,
            "Checking",
            "Netflix",
            MoneyValue.Create("USD", -15.99m),
            null,
            null,
            false,
            false);

        _recurringRepository
            .Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecurringTransaction> { recurring });
        _instanceProjector
            .Setup(p => p.GetInstancesByDateRangeAsync(
                It.IsAny<IReadOnlyList<RecurringTransaction>>(),
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<DateOnly, List<RecurringInstanceInfoValue>>
            {
                { instanceDate, new List<RecurringInstanceInfoValue> { instance } },
            });

        var builder = CreateBuilder();

        var result = await builder.GetReconciliationStatusAsync(2026, 1);

        result.Instances[0].ExpectedAmount.ShouldNotBeNull();
        result.Instances[0].ExpectedAmount!.Amount.ShouldBe(-15.99m);
        result.Instances[0].ExpectedAmount!.Currency.ShouldBe("USD");
    }

    private static Transaction CreateTestTransaction(Guid id, Guid accountId, string description, decimal amount, DateOnly date)
    {
        var transaction = Transaction.Create(
            accountId,
            MoneyValue.Create("USD", amount),
            date,
            description);
        typeof(Transaction).GetProperty(nameof(Transaction.Id))!.SetValue(transaction, id);
        return transaction;
    }

    private static RecurringTransaction CreateTestRecurringTransaction(Guid id, Guid accountId, string description, decimal amount, DateOnly startDate)
    {
        var recurring = RecurringTransaction.Create(
            accountId,
            description,
            MoneyValue.Create("USD", amount),
            RecurrencePatternValue.CreateMonthly(1, startDate.Day),
            startDate);
        typeof(RecurringTransaction).GetProperty(nameof(RecurringTransaction.Id))!.SetValue(recurring, id);
        return recurring;
    }

    private ReconciliationStatusBuilder CreateBuilder()
    {
        return new ReconciliationStatusBuilder(
            _matchRepository.Object,
            _recurringRepository.Object,
            _transactionRepository.Object,
            _instanceProjector.Object);
    }
}
