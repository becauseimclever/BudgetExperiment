// <copyright file="LinkableInstanceFinderTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Application.Reconciliation;
using BudgetExperiment.Domain;

using Moq;

using Shouldly;

namespace BudgetExperiment.Application.Tests.Services;

/// <summary>
/// Unit tests for <see cref="LinkableInstanceFinder"/>.
/// </summary>
public sealed class LinkableInstanceFinderTests
{
    private readonly Mock<ITransactionRepository> _transactionRepository = new();
    private readonly Mock<IRecurringTransactionRepository> _recurringRepository = new();
    private readonly Mock<IRecurringInstanceProjector> _instanceProjector = new();
    private readonly Mock<ITransactionMatcher> _transactionMatcher = new();
    private readonly Mock<IReconciliationMatchRepository> _matchRepository = new();

    [Fact]
    public async Task GetLinkableInstancesAsync_TransactionNotFound_ReturnsEmptyList()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        _transactionRepository
            .Setup(r => r.GetByIdAsync(transactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction?)null);

        var sut = this.CreateSut();

        // Act
        var result = await sut.GetLinkableInstancesAsync(transactionId);

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetLinkableInstancesAsync_SkippedInstancesFiltered_ExcludesSkippedInstances()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var date = new DateOnly(2026, 3, 15);

        var transaction = CreateTestTransaction(transactionId, accountId, "Netflix", -15.99m, date);

        var activeInstance = new RecurringInstanceInfoValue(
            Guid.NewGuid(),
            date,
            accountId,
            "Checking",
            "Netflix",
            MoneyValue.Create("USD", -15.99m),
            null,
            null,
            false,
            false);

        var skippedInstance = new RecurringInstanceInfoValue(
            Guid.NewGuid(),
            date,
            accountId,
            "Checking",
            "Hulu",
            MoneyValue.Create("USD", -8.99m),
            null,
            null,
            false,
            true);

        this.SetupCommonMocks(transactionId, transaction, date, activeInstance, skippedInstance);

        _matchRepository
            .Setup(r => r.IsInstanceMatchedAsync(
                It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _transactionMatcher
            .Setup(m => m.FindMatches(
                It.IsAny<Transaction>(),
                It.IsAny<IEnumerable<RecurringInstanceInfoValue>>(),
                It.IsAny<MatchingTolerancesValue>()))
            .Returns(new List<TransactionMatchResultValue>());

        var sut = this.CreateSut();

        // Act
        var result = await sut.GetLinkableInstancesAsync(transactionId);

        // Assert
        result.Count.ShouldBe(1);
        result[0].Description.ShouldBe("Netflix");
    }

    [Fact]
    public async Task GetLinkableInstancesAsync_AlreadyMatched_SetsIsAlreadyMatchedTrue()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var recurringId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var date = new DateOnly(2026, 3, 15);

        var transaction = CreateTestTransaction(transactionId, accountId, "Netflix", -15.99m, date);

        var instance = new RecurringInstanceInfoValue(
            recurringId,
            date,
            accountId,
            "Checking",
            "Netflix",
            MoneyValue.Create("USD", -15.99m),
            null,
            null,
            false,
            false);

        this.SetupCommonMocks(transactionId, transaction, date, instance);

        _matchRepository
            .Setup(r => r.IsInstanceMatchedAsync(recurringId, date, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var sut = this.CreateSut();

        // Act
        var result = await sut.GetLinkableInstancesAsync(transactionId);

        // Assert
        result.Count.ShouldBe(1);
        result[0].IsAlreadyMatched.ShouldBeTrue();
        result[0].SuggestedConfidence.ShouldBeNull();
    }

    [Fact]
    public async Task GetLinkableInstancesAsync_NotMatched_CalculatesSuggestedConfidence()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var recurringId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var date = new DateOnly(2026, 3, 15);

        var transaction = CreateTestTransaction(transactionId, accountId, "Netflix", -15.99m, date);

        var instance = new RecurringInstanceInfoValue(
            recurringId,
            date,
            accountId,
            "Checking",
            "Netflix",
            MoneyValue.Create("USD", -15.99m),
            null,
            null,
            false,
            false);

        this.SetupCommonMocks(transactionId, transaction, date, instance);

        _matchRepository
            .Setup(r => r.IsInstanceMatchedAsync(recurringId, date, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var matchResult = new TransactionMatchResultValue(
            recurringId,
            date,
            0.92m,
            MatchConfidenceLevel.High,
            0m,
            0,
            1.0m);

        _transactionMatcher
            .Setup(m => m.FindMatches(
                It.IsAny<Transaction>(),
                It.IsAny<IEnumerable<RecurringInstanceInfoValue>>(),
                It.IsAny<MatchingTolerancesValue>()))
            .Returns(new List<TransactionMatchResultValue> { matchResult });

        var sut = this.CreateSut();

        // Act
        var result = await sut.GetLinkableInstancesAsync(transactionId);

        // Assert
        result.Count.ShouldBe(1);
        result[0].IsAlreadyMatched.ShouldBeFalse();
        result[0].SuggestedConfidence.ShouldBe(0.92m);
    }

    [Fact]
    public async Task GetLinkableInstancesAsync_MultipleInstances_OrdersByDateThenConfidenceDescending()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var date = new DateOnly(2026, 3, 15);

        var transaction = CreateTestTransaction(transactionId, accountId, "Netflix", -15.99m, date);

        var earlierDate = new DateOnly(2026, 3, 10);
        var laterDate = new DateOnly(2026, 3, 20);

        var instanceA = new RecurringInstanceInfoValue(
            Guid.NewGuid(),
            laterDate,
            accountId,
            "Checking",
            "Instance A",
            MoneyValue.Create("USD", -10m),
            null,
            null,
            false,
            false);

        var instanceB = new RecurringInstanceInfoValue(
            Guid.NewGuid(),
            earlierDate,
            accountId,
            "Checking",
            "Instance B",
            MoneyValue.Create("USD", -15m),
            null,
            null,
            false,
            false);

        _transactionRepository
            .Setup(r => r.GetByIdAsync(transactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction);

        _recurringRepository
            .Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecurringTransaction>());

        _instanceProjector
            .Setup(p => p.GetInstancesByDateRangeAsync(
                It.IsAny<IReadOnlyList<RecurringTransaction>>(),
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                It.IsAny<ISet<DateOnly>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<DateOnly, List<RecurringInstanceInfoValue>>
            {
                { laterDate, new List<RecurringInstanceInfoValue> { instanceA } },
                { earlierDate, new List<RecurringInstanceInfoValue> { instanceB } },
            });

        _matchRepository
            .Setup(r => r.IsInstanceMatchedAsync(
                It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _transactionMatcher
            .Setup(m => m.FindMatches(
                It.IsAny<Transaction>(),
                It.IsAny<IEnumerable<RecurringInstanceInfoValue>>(),
                It.IsAny<MatchingTolerancesValue>()))
            .Returns(new List<TransactionMatchResultValue>());

        var sut = this.CreateSut();

        // Act
        var result = await sut.GetLinkableInstancesAsync(transactionId);

        // Assert
        result.Count.ShouldBe(2);
        result[0].InstanceDate.ShouldBe(earlierDate);
        result[1].InstanceDate.ShouldBe(laterDate);
    }

    [Fact]
    public async Task GetLinkableInstancesAsync_ProjectsDateRange_UsesPlusMinus30Days()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var date = new DateOnly(2026, 6, 15);

        var transaction = CreateTestTransaction(transactionId, accountId, "Test", -10m, date);

        _transactionRepository
            .Setup(r => r.GetByIdAsync(transactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction);

        _recurringRepository
            .Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecurringTransaction>());

        DateOnly capturedStart = default;
        DateOnly capturedEnd = default;

        _instanceProjector
            .Setup(p => p.GetInstancesByDateRangeAsync(
                It.IsAny<IReadOnlyList<RecurringTransaction>>(),
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                It.IsAny<ISet<DateOnly>?>(),
                It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyList<RecurringTransaction>, DateOnly, DateOnly, ISet<DateOnly>?, CancellationToken>(
                (_, start, end, _, _) =>
                {
                    capturedStart = start;
                    capturedEnd = end;
                })
            .ReturnsAsync(new Dictionary<DateOnly, List<RecurringInstanceInfoValue>>());

        var sut = this.CreateSut();

        // Act
        await sut.GetLinkableInstancesAsync(transactionId);

        // Assert
        capturedStart.ShouldBe(new DateOnly(2026, 5, 16));
        capturedEnd.ShouldBe(new DateOnly(2026, 7, 15));
    }

    private static Transaction CreateTestTransaction(
        Guid id,
        Guid accountId,
        string description,
        decimal amount,
        DateOnly date)
    {
        var transaction = TransactionFactory.Create(
            accountId,
            MoneyValue.Create("USD", amount),
            date,
            description);
        typeof(Transaction).GetProperty(nameof(Transaction.Id))!.SetValue(transaction, id);
        return transaction;
    }

    private void SetupCommonMocks(
        Guid transactionId,
        Transaction transaction,
        DateOnly date,
        params RecurringInstanceInfoValue[] instances)
    {
        _transactionRepository
            .Setup(r => r.GetByIdAsync(transactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction);

        _recurringRepository
            .Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecurringTransaction>());

        _instanceProjector
            .Setup(p => p.GetInstancesByDateRangeAsync(
                It.IsAny<IReadOnlyList<RecurringTransaction>>(),
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                It.IsAny<ISet<DateOnly>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<DateOnly, List<RecurringInstanceInfoValue>>
            {
                { date, instances.ToList() },
            });
    }

    private LinkableInstanceFinder CreateSut()
    {
        return new LinkableInstanceFinder(
            _transactionRepository.Object,
            _recurringRepository.Object,
            _instanceProjector.Object,
            _transactionMatcher.Object,
            _matchRepository.Object);
    }
}
