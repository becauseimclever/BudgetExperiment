// <copyright file="ReconciliationMatchActionHandlerTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain;
using Moq;
using Shouldly;

namespace BudgetExperiment.Application.Tests;

/// <summary>
/// Unit tests for ReconciliationMatchActionHandler.
/// </summary>
public class ReconciliationMatchActionHandlerTests
{
    private readonly Mock<IReconciliationMatchRepository> _matchRepository;
    private readonly Mock<IRecurringTransactionRepository> _recurringRepository;
    private readonly Mock<ITransactionRepository> _transactionRepository;
    private readonly Mock<IUnitOfWork> _unitOfWork;

    public ReconciliationMatchActionHandlerTests()
    {
        _matchRepository = new Mock<IReconciliationMatchRepository>();
        _recurringRepository = new Mock<IRecurringTransactionRepository>();
        _transactionRepository = new Mock<ITransactionRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();

        _unitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
    }

    [Fact]
    public async Task AcceptMatchAsync_MatchNotFound_ReturnsNull()
    {
        _matchRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ReconciliationMatch?)null);

        var handler = CreateHandler();

        var result = await handler.AcceptMatchAsync(Guid.NewGuid());

        result.ShouldBeNull();
    }

    [Fact]
    public async Task AcceptMatchAsync_ValidMatch_AcceptsAndLinksTransaction()
    {
        var matchId = Guid.NewGuid();
        var transactionId = Guid.NewGuid();
        var recurringId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var instanceDate = new DateOnly(2026, 1, 15);

        var match = CreateTestMatch(matchId, transactionId, recurringId, instanceDate);
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

        var handler = CreateHandler();

        var result = await handler.AcceptMatchAsync(matchId);

        result.ShouldNotBeNull();
        result!.Status.ShouldBe("Accepted");
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RejectMatchAsync_MatchNotFound_ReturnsNull()
    {
        _matchRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ReconciliationMatch?)null);

        var handler = CreateHandler();

        var result = await handler.RejectMatchAsync(Guid.NewGuid());

        result.ShouldBeNull();
    }

    [Fact]
    public async Task RejectMatchAsync_ValidMatch_RejectsMatch()
    {
        var matchId = Guid.NewGuid();
        var transactionId = Guid.NewGuid();
        var recurringId = Guid.NewGuid();
        var instanceDate = new DateOnly(2026, 1, 15);

        var match = CreateTestMatch(matchId, transactionId, recurringId, instanceDate);

        _matchRepository
            .Setup(r => r.GetByIdAsync(matchId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(match);

        var handler = CreateHandler();

        var result = await handler.RejectMatchAsync(matchId);

        result.ShouldNotBeNull();
        result!.Status.ShouldBe("Rejected");
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BulkAcceptMatchesAsync_AcceptsMultipleMatches()
    {
        var matchId1 = Guid.NewGuid();
        var matchId2 = Guid.NewGuid();
        var transactionId1 = Guid.NewGuid();
        var transactionId2 = Guid.NewGuid();
        var recurringId1 = Guid.NewGuid();
        var recurringId2 = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var date1 = new DateOnly(2026, 1, 5);
        var date2 = new DateOnly(2026, 1, 15);

        var match1 = CreateTestMatch(matchId1, transactionId1, recurringId1, date1);
        var match2 = CreateTestMatch(matchId2, transactionId2, recurringId2, date2);
        var transaction1 = CreateTestTransaction(transactionId1, accountId, "Netflix", -15.99m, date1);
        var transaction2 = CreateTestTransaction(transactionId2, accountId, "Spotify", -9.99m, date2);
        var recurring1 = CreateTestRecurringTransaction(recurringId1, accountId, "Netflix", -15.99m, date1);
        var recurring2 = CreateTestRecurringTransaction(recurringId2, accountId, "Spotify", -9.99m, date2);

        _matchRepository.Setup(r => r.GetByIdAsync(matchId1, It.IsAny<CancellationToken>())).ReturnsAsync(match1);
        _matchRepository.Setup(r => r.GetByIdAsync(matchId2, It.IsAny<CancellationToken>())).ReturnsAsync(match2);
        _transactionRepository.Setup(r => r.GetByIdAsync(transactionId1, It.IsAny<CancellationToken>())).ReturnsAsync(transaction1);
        _transactionRepository.Setup(r => r.GetByIdAsync(transactionId2, It.IsAny<CancellationToken>())).ReturnsAsync(transaction2);
        _recurringRepository.Setup(r => r.GetByIdAsync(recurringId1, It.IsAny<CancellationToken>())).ReturnsAsync(recurring1);
        _recurringRepository.Setup(r => r.GetByIdAsync(recurringId2, It.IsAny<CancellationToken>())).ReturnsAsync(recurring2);

        var handler = CreateHandler();

        var result = await handler.BulkAcceptMatchesAsync(
            new BulkMatchActionRequest { MatchIds = [matchId1, matchId2] });

        result.Count.ShouldBe(2);
    }

    [Fact]
    public async Task UnlinkMatchAsync_MatchNotFound_ReturnsNull()
    {
        _matchRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ReconciliationMatch?)null);

        var handler = CreateHandler();

        var result = await handler.UnlinkMatchAsync(Guid.NewGuid());

        result.ShouldBeNull();
    }

    [Fact]
    public async Task UnlinkMatchAsync_ValidMatch_UnlinksAndRejects()
    {
        var matchId = Guid.NewGuid();
        var transactionId = Guid.NewGuid();
        var recurringId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var instanceDate = new DateOnly(2026, 1, 15);

        var match = CreateTestMatch(matchId, transactionId, recurringId, instanceDate);
        match.Accept();

        var transaction = CreateTestTransaction(transactionId, accountId, "Netflix", -15.99m, instanceDate);
        transaction.LinkToRecurringInstance(recurringId, instanceDate);

        _matchRepository
            .Setup(r => r.GetByIdAsync(matchId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(match);
        _transactionRepository
            .Setup(r => r.GetByIdAsync(transactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction);

        var handler = CreateHandler();

        var result = await handler.UnlinkMatchAsync(matchId);

        result.ShouldNotBeNull();
        result!.Status.ShouldBe("Rejected");
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateManualMatchAsync_TransactionNotFound_ReturnsNull()
    {
        _transactionRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction?)null);

        var handler = CreateHandler();

        var result = await handler.CreateManualMatchAsync(
            new ManualMatchRequest
            {
                TransactionId = Guid.NewGuid(),
                RecurringTransactionId = Guid.NewGuid(),
                InstanceDate = new DateOnly(2026, 1, 15),
            });

        result.ShouldBeNull();
    }

    [Fact]
    public async Task CreateManualMatchAsync_RecurringNotFound_ReturnsNull()
    {
        var transactionId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var transaction = CreateTestTransaction(transactionId, accountId, "Netflix", -15.99m, new DateOnly(2026, 1, 15));

        _transactionRepository
            .Setup(r => r.GetByIdAsync(transactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction);
        _recurringRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RecurringTransaction?)null);

        var handler = CreateHandler();

        var result = await handler.CreateManualMatchAsync(
            new ManualMatchRequest
            {
                TransactionId = transactionId,
                RecurringTransactionId = Guid.NewGuid(),
                InstanceDate = new DateOnly(2026, 1, 15),
            });

        result.ShouldBeNull();
    }

    [Fact]
    public async Task CreateManualMatchAsync_ValidRequest_CreatesAcceptedMatch()
    {
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

        var handler = CreateHandler();

        var result = await handler.CreateManualMatchAsync(
            new ManualMatchRequest
            {
                TransactionId = transactionId,
                RecurringTransactionId = recurringId,
                InstanceDate = instanceDate,
            });

        result.ShouldNotBeNull();
        result!.Status.ShouldBe("Accepted");
        result.Source.ShouldBe("Manual");
        _matchRepository.Verify(
            r => r.AddAsync(It.IsAny<ReconciliationMatch>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateManualMatchAsync_ExistingMatch_ReturnsExisting()
    {
        var transactionId = Guid.NewGuid();
        var recurringId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var instanceDate = new DateOnly(2026, 1, 15);

        var transaction = CreateTestTransaction(transactionId, accountId, "Netflix", -15.99m, instanceDate);
        var recurring = CreateTestRecurringTransaction(recurringId, accountId, "Netflix", -15.99m, instanceDate);
        var existingMatch = CreateTestMatch(Guid.NewGuid(), transactionId, recurringId, instanceDate);

        _transactionRepository
            .Setup(r => r.GetByIdAsync(transactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction);
        _recurringRepository
            .Setup(r => r.GetByIdAsync(recurringId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(recurring);
        _matchRepository
            .Setup(r => r.ExistsAsync(transactionId, recurringId, instanceDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _matchRepository
            .Setup(r => r.GetByTransactionIdAsync(transactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ReconciliationMatch> { existingMatch });

        var handler = CreateHandler();

        var result = await handler.CreateManualMatchAsync(
            new ManualMatchRequest
            {
                TransactionId = transactionId,
                RecurringTransactionId = recurringId,
                InstanceDate = instanceDate,
            });

        result.ShouldNotBeNull();

        // Should not create a new match
        _matchRepository.Verify(
            r => r.AddAsync(It.IsAny<ReconciliationMatch>(), It.IsAny<CancellationToken>()),
            Times.Never);
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

    private ReconciliationMatchActionHandler CreateHandler()
    {
        return new ReconciliationMatchActionHandler(
            _matchRepository.Object,
            _recurringRepository.Object,
            _transactionRepository.Object,
            _unitOfWork.Object);
    }
}
