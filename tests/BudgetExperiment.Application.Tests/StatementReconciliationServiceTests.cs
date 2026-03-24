// <copyright file="StatementReconciliationServiceTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain;

using Moq;

using Shouldly;

namespace BudgetExperiment.Application.Tests;

/// <summary>
/// Unit tests for <see cref="StatementReconciliationService"/>.
/// </summary>
public class StatementReconciliationServiceTests
{
    private readonly Mock<ITransactionRepository> _txRepo;
    private readonly Mock<IAccountRepository> _accountRepo;
    private readonly Mock<IStatementBalanceRepository> _sbRepo;
    private readonly Mock<IReconciliationRecordRepository> _rrRepo;
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly Mock<IUserContext> _userContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="StatementReconciliationServiceTests"/> class.
    /// </summary>
    public StatementReconciliationServiceTests()
    {
        _txRepo = new Mock<ITransactionRepository>();
        _accountRepo = new Mock<IAccountRepository>();
        _sbRepo = new Mock<IStatementBalanceRepository>();
        _rrRepo = new Mock<IReconciliationRecordRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _userContext = new Mock<IUserContext>();

        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);
        _userContext.Setup(u => u.UserIdAsGuid).Returns(Guid.NewGuid());
        _userContext.Setup(u => u.CurrentScope).Returns(BudgetScope.Shared);
    }

    // AC-125b-08: cleared balance = InitialBalance + sum(cleared transactions ≤ statementDate)
    [Fact]
    public async Task GetClearedBalanceAsync_ReturnsInitialPlusClearedSum()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var account = CreateAccount(100m);
        var statementDate = new DateOnly(2026, 1, 31);

        _accountRepo.Setup(r => r.GetByIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);
        _txRepo.Setup(r => r.GetClearedBalanceSumAsync(accountId, statementDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MoneyValue.Create("USD", -450m));

        var svc = CreateService();

        // Act
        var result = await svc.GetClearedBalanceAsync(accountId, statementDate, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.AccountId.ShouldBe(accountId);
        result.InitialBalance.ShouldBe(100m);
        result.ClearedBalance.ShouldBe(-350m); // 100 + (-450)
        result.UpToDate.ShouldBe(statementDate);
    }

    // AC-125b-09: bulk clear marks all specified transactions and returns updated DTOs
    [Fact]
    public async Task BulkMarkClearedAsync_MarksAllTransactionsCleared()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var clearedDate = new DateOnly(2026, 1, 20);
        var ids = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        var transactions = ids.Select(id => CreateTransaction(id, accountId)).ToList();

        _txRepo.Setup(r => r.GetByIdsAsync(ids, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactions);

        var svc = CreateService();

        // Act
        var result = await svc.BulkMarkClearedAsync(ids, clearedDate, CancellationToken.None);

        // Assert
        result.Count.ShouldBe(2);
        transactions.ShouldAllBe(t => t.IsCleared);
        transactions.ShouldAllBe(t => t.ClearedDate == clearedDate);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // AC-125b-10: bulk unclear skips reconciled (locked) transactions and returns only affected DTOs
    [Fact]
    public async Task BulkMarkUnclearedAsync_SkipsReconciledTransactions()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var clearedDate = new DateOnly(2026, 1, 20);
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var ids = new List<Guid> { id1, id2 };

        var tx1 = CreateTransaction(id1, accountId);
        tx1.MarkCleared(clearedDate);

        var tx2 = CreateTransaction(id2, accountId);
        tx2.MarkCleared(clearedDate);
        tx2.LockToReconciliation(Guid.NewGuid());

        _txRepo.Setup(r => r.GetByIdsAsync(ids, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Transaction> { tx1, tx2 });

        var svc = CreateService();

        // Act
        var result = await svc.BulkMarkUnclearedAsync(ids, CancellationToken.None);

        // Assert
        result.Count.ShouldBe(1);
        result[0].Id.ShouldBe(id1);
        tx1.IsCleared.ShouldBeFalse();
        tx2.IsCleared.ShouldBeTrue();
    }

    // AC-125b-07: CompleteReconciliationAsync creates record and locks all cleared transactions
    [Fact]
    public async Task CompleteReconciliationAsync_CreatesRecordAndLocksTransactions()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var statementDate = new DateOnly(2026, 1, 31);
        var balanceAmount = 1000m;
        var currency = "USD";

        var account = Account.CreateShared(
            "Test Account",
            AccountType.Checking,
            Guid.NewGuid(),
            MoneyValue.Create(currency, 100m));

        var sb = StatementBalance.Create(accountId, statementDate, MoneyValue.Create(currency, balanceAmount));

        var tx1 = CreateTransaction(Guid.NewGuid(), accountId);
        tx1.MarkCleared(new DateOnly(2026, 1, 10));
        var tx2 = CreateTransaction(Guid.NewGuid(), accountId);
        tx2.MarkCleared(new DateOnly(2026, 1, 20));

        _sbRepo.Setup(r => r.GetActiveByAccountAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sb);
        _accountRepo.Setup(r => r.GetByIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);
        _txRepo.Setup(r => r.GetClearedBalanceSumAsync(accountId, statementDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MoneyValue.Create(currency, 900m)); // 100 initial + 900 = 1000
        _txRepo.Setup(r => r.GetClearedByAccountAsync(accountId, statementDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Transaction> { tx1, tx2 });
        _rrRepo.Setup(r => r.AddAsync(It.IsAny<ReconciliationRecord>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var svc = CreateService();

        // Act
        var result = await svc.CompleteReconciliationAsync(accountId, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.AccountId.ShouldBe(accountId);
        result.StatementDate.ShouldBe(statementDate);
        result.TransactionCount.ShouldBe(2);

        tx1.ReconciliationRecordId.ShouldNotBeNull();
        tx2.ReconciliationRecordId.ShouldNotBeNull();
        tx1.ReconciliationRecordId.ShouldBe(tx2.ReconciliationRecordId);

        sb.IsCompleted.ShouldBeTrue();
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private static Account CreateAccount(decimal initialBalance)
    {
        return Account.Create(
            "Test Account",
            AccountType.Checking,
            MoneyValue.Create("USD", initialBalance),
            new DateOnly(2025, 1, 1));
    }

    private static Transaction CreateTransaction(Guid id, Guid accountId)
    {
        var tx = Transaction.Create(
            accountId,
            MoneyValue.Create("USD", -50m),
            new DateOnly(2026, 1, 10),
            "Test Transaction");

        // Use reflection to set the Id since it's private set
        typeof(Transaction)
            .GetProperty("Id")!
            .SetValue(tx, id);

        return tx;
    }

    private StatementReconciliationService CreateService()
    {
        return new StatementReconciliationService(
            _txRepo.Object,
            _accountRepo.Object,
            _sbRepo.Object,
            _rrRepo.Object,
            _unitOfWork.Object,
            _userContext.Object);
    }
}
