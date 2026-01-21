// <copyright file="ReconciliationServiceTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>


using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain;
using Moq;
using Shouldly;

namespace BudgetExperiment.Application.Tests;

/// <summary>
/// Unit tests for ReconciliationService.
/// </summary>
public class ReconciliationServiceTests
{
    private readonly Mock<IReconciliationMatchRepository> _matchRepository;
    private readonly Mock<IRecurringTransactionRepository> _recurringRepository;
    private readonly Mock<ITransactionRepository> _transactionRepository;
    private readonly Mock<IRecurringInstanceProjector> _instanceProjector;
    private readonly Mock<ITransactionMatcher> _transactionMatcher;
    private readonly Mock<IUnitOfWork> _unitOfWork;

    public ReconciliationServiceTests()
    {
        _matchRepository = new Mock<IReconciliationMatchRepository>();
        _recurringRepository = new Mock<IRecurringTransactionRepository>();
        _transactionRepository = new Mock<ITransactionRepository>();
        _instanceProjector = new Mock<IRecurringInstanceProjector>();
        _transactionMatcher = new Mock<ITransactionMatcher>();
        _unitOfWork = new Mock<IUnitOfWork>();

        // Default setups
        _matchRepository
            .Setup(r => r.GetPendingMatchesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ReconciliationMatch>());
        _recurringRepository
            .Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecurringTransaction>());
        _instanceProjector
            .Setup(p => p.GetInstancesByDateRangeAsync(
                It.IsAny<IReadOnlyList<RecurringTransaction>>(),
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<DateOnly, List<RecurringInstanceInfo>>());
        _transactionMatcher
            .Setup(m => m.FindMatches(
                It.IsAny<Transaction>(),
                It.IsAny<IEnumerable<RecurringInstanceInfo>>(),
                It.IsAny<MatchingTolerances>()))
            .Returns(new List<TransactionMatchResult>());
        _unitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
    }

    private ReconciliationService CreateService()
    {
        return new ReconciliationService(
            _matchRepository.Object,
            _recurringRepository.Object,
            _transactionRepository.Object,
            _instanceProjector.Object,
            _transactionMatcher.Object,
            _unitOfWork.Object);
    }

    #region FindMatchesAsync Tests

    [Fact]
    public async Task FindMatchesAsync_NoTransactions_ReturnsEmptyResult()
    {
        // Arrange
        var service = CreateService();
        var request = new FindMatchesRequest
        {
            TransactionIds = [],
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 1, 31),
        };

        // Act
        var result = await service.FindMatchesAsync(request);

        // Assert
        result.ShouldNotBeNull();
        result.MatchesByTransaction.ShouldBeEmpty();
        result.TotalMatchesFound.ShouldBe(0);
        result.HighConfidenceCount.ShouldBe(0);
    }

    [Fact]
    public async Task FindMatchesAsync_TransactionNotFound_SkipsTransaction()
    {
        // Arrange
        _transactionRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction?)null);

        var service = CreateService();
        var request = new FindMatchesRequest
        {
            TransactionIds = [Guid.NewGuid()],
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 1, 31),
        };

        // Act
        var result = await service.FindMatchesAsync(request);

        // Assert
        result.MatchesByTransaction.ShouldBeEmpty();
        result.TotalMatchesFound.ShouldBe(0);
    }

    [Fact]
    public async Task FindMatchesAsync_WithMatchingTransactions_CreatesMatches()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var recurringId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var instanceDate = new DateOnly(2026, 1, 15);

        var transaction = CreateTestTransaction(transactionId, accountId, "Netflix", -15.99m, instanceDate);
        var recurring = CreateTestRecurringTransaction(recurringId, accountId, "Netflix", -15.99m, instanceDate);

        var instance = new RecurringInstanceInfo(
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

        var matchResult = new TransactionMatchResult(
            recurringId,
            instanceDate,
            0.95m,
            MatchConfidenceLevel.High,
            0m,
            0,
            1.0m);

        _transactionRepository
            .Setup(r => r.GetByIdAsync(transactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction);
        _recurringRepository
            .Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecurringTransaction> { recurring });
        _recurringRepository
            .Setup(r => r.GetByIdAsync(recurringId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(recurring);
        _instanceProjector
            .Setup(p => p.GetInstancesByDateRangeAsync(
                It.IsAny<IReadOnlyList<RecurringTransaction>>(),
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<DateOnly, List<RecurringInstanceInfo>>
            {
                { instanceDate, new List<RecurringInstanceInfo> { instance } },
            });
        _transactionMatcher
            .Setup(m => m.FindMatches(
                transaction,
                It.IsAny<IEnumerable<RecurringInstanceInfo>>(),
                It.IsAny<MatchingTolerances>()))
            .Returns(new List<TransactionMatchResult> { matchResult });
        _matchRepository
            .Setup(r => r.ExistsAsync(
                transactionId,
                recurringId,
                instanceDate,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var service = CreateService();
        var request = new FindMatchesRequest
        {
            TransactionIds = [transactionId],
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 1, 31),
        };

        // Act
        var result = await service.FindMatchesAsync(request);

        // Assert
        result.TotalMatchesFound.ShouldBe(1);
        result.HighConfidenceCount.ShouldBe(1); // 0.95 > 0.85 auto-match threshold
        result.MatchesByTransaction.ShouldContainKey(transactionId);
        result.MatchesByTransaction[transactionId].Count.ShouldBe(1);

        var match = result.MatchesByTransaction[transactionId][0];
        match.RecurringTransactionId.ShouldBe(recurringId);
        match.RecurringInstanceDate.ShouldBe(instanceDate);
        match.ConfidenceScore.ShouldBe(0.95m);
        match.Status.ShouldBe("AutoMatched");

        _matchRepository.Verify(r => r.AddAsync(
            It.Is<ReconciliationMatch>(m => m.ImportedTransactionId == transactionId),
            It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FindMatchesAsync_ExistingMatch_SkipsCreatingDuplicate()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var recurringId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var instanceDate = new DateOnly(2026, 1, 15);

        var transaction = CreateTestTransaction(transactionId, accountId, "Netflix", -15.99m, instanceDate);
        var recurring = CreateTestRecurringTransaction(recurringId, accountId, "Netflix", -15.99m, instanceDate);

        var instance = new RecurringInstanceInfo(
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

        var matchResult = new TransactionMatchResult(
            recurringId,
            instanceDate,
            0.95m,
            MatchConfidenceLevel.High,
            0m,
            0,
            1.0m);

        _transactionRepository
            .Setup(r => r.GetByIdAsync(transactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction);
        _recurringRepository
            .Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecurringTransaction> { recurring });
        _instanceProjector
            .Setup(p => p.GetInstancesByDateRangeAsync(
                It.IsAny<IReadOnlyList<RecurringTransaction>>(),
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<DateOnly, List<RecurringInstanceInfo>>
            {
                { instanceDate, new List<RecurringInstanceInfo> { instance } },
            });
        _transactionMatcher
            .Setup(m => m.FindMatches(
                transaction,
                It.IsAny<IEnumerable<RecurringInstanceInfo>>(),
                It.IsAny<MatchingTolerances>()))
            .Returns(new List<TransactionMatchResult> { matchResult });
        _matchRepository
            .Setup(r => r.ExistsAsync(
                transactionId,
                recurringId,
                instanceDate,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true); // Match already exists

        var service = CreateService();
        var request = new FindMatchesRequest
        {
            TransactionIds = [transactionId],
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 1, 31),
        };

        // Act
        var result = await service.FindMatchesAsync(request);

        // Assert
        result.TotalMatchesFound.ShouldBe(0);
        _matchRepository.Verify(r => r.AddAsync(
            It.IsAny<ReconciliationMatch>(),
            It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region AcceptMatchAsync Tests

    [Fact]
    public async Task AcceptMatchAsync_MatchNotFound_ReturnsNull()
    {
        // Arrange
        _matchRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ReconciliationMatch?)null);

        var service = CreateService();

        // Act
        var result = await service.AcceptMatchAsync(Guid.NewGuid());

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task AcceptMatchAsync_ValidMatch_AcceptsAndLinksTransaction()
    {
        // Arrange
        var matchId = Guid.NewGuid();
        var transactionId = Guid.NewGuid();
        var recurringId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var instanceDate = new DateOnly(2026, 1, 15);

        var match = ReconciliationMatch.Create(
            transactionId,
            recurringId,
            instanceDate,
            0.85m,
            0m,
            0,
            BudgetScope.Shared,
            null);
        typeof(ReconciliationMatch).GetProperty(nameof(ReconciliationMatch.Id))!
            .SetValue(match, matchId);

        var transaction = CreateTestTransaction(transactionId, accountId, "Netflix", -15.99m, instanceDate);
        var recurring = CreateTestRecurringTransaction(recurringId, accountId, "Netflix", -15.99m, instanceDate);

        _matchRepository
            .Setup(r => r.GetByIdAsync(matchId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(match);
        _transactionRepository
            .Setup(r => r.GetByIdAsync(transactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction);
        _recurringRepository
            .Setup(r => r.GetByIdAsync(recurringId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(recurring);

        var service = CreateService();

        // Act
        var result = await service.AcceptMatchAsync(matchId);

        // Assert
        result.ShouldNotBeNull();
        result.Status.ShouldBe("Accepted");
        transaction.RecurringTransactionId.ShouldBe(recurringId);
        transaction.RecurringInstanceDate.ShouldBe(instanceDate);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region RejectMatchAsync Tests

    [Fact]
    public async Task RejectMatchAsync_MatchNotFound_ReturnsNull()
    {
        // Arrange
        _matchRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ReconciliationMatch?)null);

        var service = CreateService();

        // Act
        var result = await service.RejectMatchAsync(Guid.NewGuid());

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task RejectMatchAsync_ValidMatch_RejectsMatch()
    {
        // Arrange
        var matchId = Guid.NewGuid();
        var match = ReconciliationMatch.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateOnly(2026, 1, 15),
            0.75m,
            0m,
            0,
            BudgetScope.Shared,
            null);
        typeof(ReconciliationMatch).GetProperty(nameof(ReconciliationMatch.Id))!
            .SetValue(match, matchId);

        _matchRepository
            .Setup(r => r.GetByIdAsync(matchId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(match);

        var service = CreateService();

        // Act
        var result = await service.RejectMatchAsync(matchId);

        // Assert
        result.ShouldNotBeNull();
        result.Status.ShouldBe("Rejected");
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region GetPendingMatchesAsync Tests

    [Fact]
    public async Task GetPendingMatchesAsync_ReturnsEnrichedMatches()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var recurringId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var instanceDate = new DateOnly(2026, 1, 15);

        var match = ReconciliationMatch.Create(
            transactionId,
            recurringId,
            instanceDate,
            0.75m,
            0m,
            0,
            BudgetScope.Shared,
            null);

        var transaction = CreateTestTransaction(transactionId, accountId, "Netflix", -15.99m, instanceDate);
        var recurring = CreateTestRecurringTransaction(recurringId, accountId, "Netflix", -15.99m, instanceDate);

        _matchRepository
            .Setup(r => r.GetPendingMatchesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ReconciliationMatch> { match });
        _transactionRepository
            .Setup(r => r.GetByIdAsync(transactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction);
        _recurringRepository
            .Setup(r => r.GetByIdAsync(recurringId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(recurring);

        var service = CreateService();

        // Act
        var result = await service.GetPendingMatchesAsync();

        // Assert
        result.ShouldNotBeEmpty();
        result.Count.ShouldBe(1);
        result[0].RecurringTransactionDescription.ShouldBe("Netflix");
        result[0].ExpectedAmount.ShouldNotBeNull();
        result[0].ImportedTransaction.ShouldNotBeNull();
    }

    #endregion

    #region GetReconciliationStatusAsync Tests

    [Fact]
    public async Task GetReconciliationStatusAsync_NoRecurringTransactions_ReturnsEmptyStatus()
    {
        // Arrange
        _recurringRepository
            .Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RecurringTransaction>());
        _matchRepository
            .Setup(r => r.GetByPeriodAsync(2026, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ReconciliationMatch>());

        var service = CreateService();

        // Act
        var result = await service.GetReconciliationStatusAsync(2026, 1);

        // Assert
        result.Year.ShouldBe(2026);
        result.Month.ShouldBe(1);
        result.TotalExpectedInstances.ShouldBe(0);
        result.MatchedCount.ShouldBe(0);
        result.PendingCount.ShouldBe(0);
        result.MissingCount.ShouldBe(0);
    }

    [Fact]
    public async Task GetReconciliationStatusAsync_WithInstances_ReturnsCorrectCounts()
    {
        // Arrange
        var recurringId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var jan15 = new DateOnly(2026, 1, 15);

        var recurring = CreateTestRecurringTransaction(recurringId, accountId, "Netflix", -15.99m, jan15);

        var instance = new RecurringInstanceInfo(
            recurringId,
            jan15,
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
            .ReturnsAsync(new Dictionary<DateOnly, List<RecurringInstanceInfo>>
            {
                { jan15, new List<RecurringInstanceInfo> { instance } },
            });
        _matchRepository
            .Setup(r => r.GetByPeriodAsync(2026, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ReconciliationMatch>());

        var service = CreateService();

        // Act
        var result = await service.GetReconciliationStatusAsync(2026, 1);

        // Assert
        result.TotalExpectedInstances.ShouldBe(1);
        result.MissingCount.ShouldBe(1);
        result.MatchedCount.ShouldBe(0);
        result.PendingCount.ShouldBe(0);
        result.Instances.Count.ShouldBe(1);
        result.Instances[0].Status.ShouldBe("Missing");
    }

    #endregion

    #region CreateManualMatchAsync Tests

    [Fact]
    public async Task CreateManualMatchAsync_TransactionNotFound_ReturnsNull()
    {
        // Arrange
        _transactionRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction?)null);

        var service = CreateService();
        var request = new ManualMatchRequest
        {
            TransactionId = Guid.NewGuid(),
            RecurringTransactionId = Guid.NewGuid(),
            InstanceDate = new DateOnly(2026, 1, 15),
        };

        // Act
        var result = await service.CreateManualMatchAsync(request);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task CreateManualMatchAsync_RecurringNotFound_ReturnsNull()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var transaction = CreateTestTransaction(transactionId, accountId, "Netflix", -15.99m, new DateOnly(2026, 1, 15));

        _transactionRepository
            .Setup(r => r.GetByIdAsync(transactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction);
        _recurringRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RecurringTransaction?)null);

        var service = CreateService();
        var request = new ManualMatchRequest
        {
            TransactionId = transactionId,
            RecurringTransactionId = Guid.NewGuid(),
            InstanceDate = new DateOnly(2026, 1, 15),
        };

        // Act
        var result = await service.CreateManualMatchAsync(request);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task CreateManualMatchAsync_ValidRequest_CreatesAcceptedMatch()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var recurringId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var instanceDate = new DateOnly(2026, 1, 15);

        var transaction = CreateTestTransaction(transactionId, accountId, "Netflix", -15.99m, instanceDate);
        var recurring = CreateTestRecurringTransaction(recurringId, accountId, "Netflix", -15.99m, instanceDate);

        _transactionRepository
            .Setup(r => r.GetByIdAsync(transactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction);
        _recurringRepository
            .Setup(r => r.GetByIdAsync(recurringId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(recurring);
        _matchRepository
            .Setup(r => r.ExistsAsync(transactionId, recurringId, instanceDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var service = CreateService();
        var request = new ManualMatchRequest
        {
            TransactionId = transactionId,
            RecurringTransactionId = recurringId,
            InstanceDate = instanceDate,
        };

        // Act
        var result = await service.CreateManualMatchAsync(request);

        // Assert
        result.ShouldNotBeNull();
        result.Status.ShouldBe("Accepted");
        result.ConfidenceScore.ShouldBe(1.0m);
        transaction.RecurringTransactionId.ShouldBe(recurringId);
        transaction.RecurringInstanceDate.ShouldBe(instanceDate);

        _matchRepository.Verify(r => r.AddAsync(
            It.Is<ReconciliationMatch>(m => m.Status == ReconciliationMatchStatus.Accepted),
            It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region BulkAcceptMatchesAsync Tests

    [Fact]
    public async Task BulkAcceptMatchesAsync_AcceptsMultipleMatches()
    {
        // Arrange
        var matchIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        var transactionId1 = Guid.NewGuid();
        var transactionId2 = Guid.NewGuid();
        var recurringId = Guid.NewGuid();
        var accountId = Guid.NewGuid();

        var match1 = CreateTestMatch(matchIds[0], transactionId1, recurringId, new DateOnly(2026, 1, 15));
        var match2 = CreateTestMatch(matchIds[1], transactionId2, recurringId, new DateOnly(2026, 2, 15));

        var transaction1 = CreateTestTransaction(transactionId1, accountId, "Netflix", -15.99m, new DateOnly(2026, 1, 15));
        var transaction2 = CreateTestTransaction(transactionId2, accountId, "Netflix", -15.99m, new DateOnly(2026, 2, 15));
        var recurring = CreateTestRecurringTransaction(recurringId, accountId, "Netflix", -15.99m, new DateOnly(2026, 1, 15));

        _matchRepository
            .Setup(r => r.GetByIdAsync(matchIds[0], It.IsAny<CancellationToken>()))
            .ReturnsAsync(match1);
        _matchRepository
            .Setup(r => r.GetByIdAsync(matchIds[1], It.IsAny<CancellationToken>()))
            .ReturnsAsync(match2);
        _transactionRepository
            .Setup(r => r.GetByIdAsync(transactionId1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction1);
        _transactionRepository
            .Setup(r => r.GetByIdAsync(transactionId2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction2);
        _recurringRepository
            .Setup(r => r.GetByIdAsync(recurringId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(recurring);

        var service = CreateService();
        var request = new BulkMatchActionRequest { MatchIds = matchIds };

        // Act
        var result = await service.BulkAcceptMatchesAsync(request);

        // Assert
        result.Count.ShouldBe(2);
        result.All(m => m.Status == "Accepted").ShouldBeTrue();
    }

    #endregion

    #region Helper Methods

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
            RecurrencePattern.CreateMonthly(1, startDate.Day),
            startDate);
        typeof(RecurringTransaction).GetProperty(nameof(RecurringTransaction.Id))!.SetValue(recurring, id);
        return recurring;
    }

    private static ReconciliationMatch CreateTestMatch(Guid id, Guid transactionId, Guid recurringId, DateOnly instanceDate)
    {
        var match = ReconciliationMatch.Create(
            transactionId,
            recurringId,
            instanceDate,
            0.85m,
            0m,
            0,
            BudgetScope.Shared,
            null);
        typeof(ReconciliationMatch).GetProperty(nameof(ReconciliationMatch.Id))!.SetValue(match, id);
        return match;
    }

    #endregion
}
