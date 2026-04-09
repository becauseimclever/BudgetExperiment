// <copyright file="TransferDeletionAccuracyTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Infrastructure.Persistence.Repositories;

using Microsoft.Extensions.Logging.Abstractions;

namespace BudgetExperiment.Infrastructure.Tests.Accuracy;

/// <summary>
/// Integration accuracy tests for atomic transfer deletion backed by a real PostgreSQL
/// Testcontainer. These tests prove that INV-2 (Transfer Net-Zero) holds after deletion:
/// removing a transfer leaves no orphaned legs, and the total sum of account balances
/// is exactly equal to the pre-transfer value.
/// </summary>
[Collection("PostgreSqlDb")]
[Trait("Category", "Accuracy")]
public sealed class TransferDeletionAccuracyTests
{
    private readonly PostgreSqlFixture _fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="TransferDeletionAccuracyTests"/> class.
    /// </summary>
    /// <param name="fixture">The shared PostgreSQL Testcontainer fixture.</param>
    public TransferDeletionAccuracyTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Create a transfer (-100 from A, +100 to B), delete it, then verify:
    /// <list type="bullet">
    ///   <item>Both transaction legs are gone.</item>
    ///   <item>Account A balance returns to its pre-transfer value.</item>
    ///   <item>Account B balance returns to its pre-transfer value.</item>
    /// </list>
    /// Proves INV-1 (account balance integrity) and INV-2 (transfer net-zero).
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task TransferDeletion_BothLegsDeleted_AccountBalancesExact()
    {
        // Arrange — two accounts, no prior transactions
        await using var context = _fixture.CreateContext();
        var accountRepo = new AccountRepository(context, FakeUserContext.CreateDefault());
        var transactionRepo = new TransactionRepository(context, FakeUserContext.CreateDefault(), NullLogger<TransactionRepository>.Instance);

        var accountA = Account.Create("Del-AccA", AccountType.Checking);
        var accountB = Account.Create("Del-AccB", AccountType.Savings);
        await accountRepo.AddAsync(accountA);
        await accountRepo.AddAsync(accountB);
        await context.SaveChangesAsync();

        // Record initial balances (both 0 — no transactions yet)
        var initialBalanceA = await GetAccountBalanceSumAsync(transactionRepo, accountA.Id);
        var initialBalanceB = await GetAccountBalanceSumAsync(transactionRepo, accountB.Id);

        // Create transfer: -$100 from A, +$100 to B
        var transferId = Guid.NewGuid();
        var sourceTx = Transaction.CreateTransfer(
            accountA.Id,
            MoneyValue.Create("USD", -100m),
            new DateOnly(2061, 6, 1),
            "Transfer to Del-AccB: Accuracy test",
            transferId,
            TransferDirection.Source);
        var destTx = Transaction.CreateTransfer(
            accountB.Id,
            MoneyValue.Create("USD", 100m),
            new DateOnly(2061, 6, 1),
            "Transfer from Del-AccA: Accuracy test",
            transferId,
            TransferDirection.Destination);

        await transactionRepo.AddAsync(sourceTx);
        await transactionRepo.AddAsync(destTx);
        await context.SaveChangesAsync();

        // Confirm both legs are present before deletion
        await using var readContext = _fixture.CreateSharedContext(context);
        var readRepo = new TransactionRepository(readContext, FakeUserContext.CreateDefault(), NullLogger<TransactionRepository>.Instance);
        var legsBeforeDelete = await readRepo.GetByTransferIdAsync(transferId);
        Assert.Equal(2, legsBeforeDelete.Count);

        // Act — atomically delete the transfer
        await readRepo.DeleteTransferAsync(transferId);

        // Assert — no legs remain
        await using var verifyContext = _fixture.CreateSharedContext(readContext);
        var verifyRepo = new TransactionRepository(verifyContext, FakeUserContext.CreateDefault(), NullLogger<TransactionRepository>.Instance);

        var legsAfterDelete = await verifyRepo.GetByTransferIdAsync(transferId);
        Assert.Empty(legsAfterDelete);

        // Assert — account balances are exactly restored to pre-transfer values
        var finalBalanceA = await GetAccountBalanceSumAsync(verifyRepo, accountA.Id);
        var finalBalanceB = await GetAccountBalanceSumAsync(verifyRepo, accountB.Id);

        Assert.Equal(initialBalanceA, finalBalanceA);
        Assert.Equal(initialBalanceB, finalBalanceB);
    }

    /// <summary>
    /// Create a single-leg "orphaned" transfer state (destination already gone),
    /// call <c>DeleteTransferAsync</c>, and verify no exception is thrown and the
    /// remaining leg is removed cleanly.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task TransferDeletion_OrphanedLeg_DeletedWithoutError()
    {
        // Arrange — only the source leg is created (simulating a previously orphaned state)
        await using var context = _fixture.CreateContext();
        var accountRepo = new AccountRepository(context, FakeUserContext.CreateDefault());
        var transactionRepo = new TransactionRepository(context, FakeUserContext.CreateDefault(), NullLogger<TransactionRepository>.Instance);

        var accountA = Account.Create("Orphan-AccA", AccountType.Checking);
        await accountRepo.AddAsync(accountA);
        await context.SaveChangesAsync();

        var transferId = Guid.NewGuid();
        var orphanedLeg = Transaction.CreateTransfer(
            accountA.Id,
            MoneyValue.Create("USD", -50m),
            new DateOnly(2061, 7, 1),
            "Transfer to unknown: Orphan test",
            transferId,
            TransferDirection.Source);

        await transactionRepo.AddAsync(orphanedLeg);
        await context.SaveChangesAsync();

        // Act — deleting orphan should not throw
        await using var deleteContext = _fixture.CreateSharedContext(context);
        var deleteRepo = new TransactionRepository(deleteContext, FakeUserContext.CreateDefault(), NullLogger<TransactionRepository>.Instance);

        var exception = await Record.ExceptionAsync(() => deleteRepo.DeleteTransferAsync(transferId));

        // Assert — no exception was thrown
        Assert.Null(exception);

        // Assert — the orphaned leg is gone
        await using var verifyContext = _fixture.CreateSharedContext(deleteContext);
        var verifyRepo = new TransactionRepository(verifyContext, FakeUserContext.CreateDefault(), NullLogger<TransactionRepository>.Instance);
        var remaining = await verifyRepo.GetByTransferIdAsync(transferId);
        Assert.Empty(remaining);
    }

    /// <summary>
    /// Create a transfer, delete it, then verify that the sum of all account balances
    /// is identical to the sum before the transfer was created (INV-2: net-zero preserved).
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task TransferDeletion_AfterDeletion_NetZeroRestored()
    {
        // Arrange — two accounts with seeded non-transfer transactions
        await using var context = _fixture.CreateContext();
        var accountRepo = new AccountRepository(context, FakeUserContext.CreateDefault());
        var transactionRepo = new TransactionRepository(context, FakeUserContext.CreateDefault(), NullLogger<TransactionRepository>.Instance);

        var accountA = Account.Create("NetZero-AccA", AccountType.Checking);
        var accountB = Account.Create("NetZero-AccB", AccountType.Savings);

        // Seed existing transactions so balances are non-zero
        accountA.AddTransaction(MoneyValue.Create("USD", 2000m), new DateOnly(2062, 1, 1), "Initial deposit A");
        accountA.AddTransaction(MoneyValue.Create("USD", -200m), new DateOnly(2062, 2, 1), "Rent");
        accountB.AddTransaction(MoneyValue.Create("USD", 500m), new DateOnly(2062, 1, 1), "Initial deposit B");

        await accountRepo.AddAsync(accountA);
        await accountRepo.AddAsync(accountB);
        await context.SaveChangesAsync();

        // Record combined balance before transfer
        await using var beforeContext = _fixture.CreateSharedContext(context);
        var beforeRepo = new TransactionRepository(beforeContext, FakeUserContext.CreateDefault(), NullLogger<TransactionRepository>.Instance);
        var combinedBefore =
            await GetAccountBalanceSumAsync(beforeRepo, accountA.Id)
            + await GetAccountBalanceSumAsync(beforeRepo, accountB.Id);

        // Create a transfer of $300 from A to B
        var transferId = Guid.NewGuid();
        var sourceTx = Transaction.CreateTransfer(
            accountA.Id,
            MoneyValue.Create("USD", -300m),
            new DateOnly(2062, 3, 1),
            "Transfer to NetZero-AccB: Net-zero test",
            transferId,
            TransferDirection.Source);
        var destTx = Transaction.CreateTransfer(
            accountB.Id,
            MoneyValue.Create("USD", 300m),
            new DateOnly(2062, 3, 1),
            "Transfer from NetZero-AccA: Net-zero test",
            transferId,
            TransferDirection.Destination);

        await beforeRepo.AddAsync(sourceTx);
        await beforeRepo.AddAsync(destTx);
        await beforeContext.SaveChangesAsync();

        // Act — delete the transfer
        await using var deleteContext = _fixture.CreateSharedContext(beforeContext);
        var deleteRepo = new TransactionRepository(deleteContext, FakeUserContext.CreateDefault(), NullLogger<TransactionRepository>.Instance);
        await deleteRepo.DeleteTransferAsync(transferId);

        // Assert — combined balance equals pre-transfer combined balance (INV-2)
        await using var afterContext = _fixture.CreateSharedContext(deleteContext);
        var afterRepo = new TransactionRepository(afterContext, FakeUserContext.CreateDefault(), NullLogger<TransactionRepository>.Instance);
        var combinedAfter =
            await GetAccountBalanceSumAsync(afterRepo, accountA.Id)
            + await GetAccountBalanceSumAsync(afterRepo, accountB.Id);

        Assert.Equal(combinedBefore, combinedAfter);

        // Sanity: neither leg survives
        var remainingLegs = await afterRepo.GetByTransferIdAsync(transferId);
        Assert.Empty(remainingLegs);
    }

    // ===== Helpers =====

    /// <summary>
    /// Computes the account balance as the sum of all transaction amounts.
    /// Uses a wide date range to capture all transactions in the Testcontainer database.
    /// </summary>
    /// <param name="repo">The transaction repository.</param>
    /// <param name="accountId">The account to sum.</param>
    /// <returns>The sum of all transaction amounts for the account.</returns>
    private static async Task<decimal> GetAccountBalanceSumAsync(
        TransactionRepository repo,
        Guid accountId)
    {
        var transactions = await repo.GetByDateRangeAsync(
            new DateOnly(2000, 1, 1),
            new DateOnly(2099, 12, 31),
            accountId);
        return transactions.Sum(t => t.Amount.Amount);
    }
}
