// <copyright file="SoftDeleteQueryFilterTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;
using BudgetExperiment.Domain.Recurring;
using BudgetExperiment.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace BudgetExperiment.Infrastructure.Tests;

/// <summary>
/// Integration tests for soft-delete query filters across repositories.
/// </summary>
[Collection("PostgreSqlDb")]
public class SoftDeleteQueryFilterTests
{
    private readonly PostgreSqlFixture _fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="SoftDeleteQueryFilterTests"/> class.
    /// </summary>
    /// <param name="fixture">The shared PostgreSQL database fixture.</param>
    public SoftDeleteQueryFilterTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task SoftDeletedTransactions_ExcludedFrom_GetByDateRangeAsync()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var accountRepo = new AccountRepository(context, FakeUserContext.CreateDefault());
        var transactionRepo = new TransactionRepository(context, FakeUserContext.CreateDefault(), NullLogger<TransactionRepository>.Instance);

        var account = Account.Create("Test Account", AccountType.Checking);
        var amount = MoneyValue.Create("USD", 100m);
        var date = DateOnly.FromDateTime(DateTime.UtcNow);
        var activeTransaction = account.AddTransaction(amount, date, "Active transaction");
        var deletedTransaction = account.AddTransaction(amount, date, "Deleted transaction");

        await accountRepo.AddAsync(account);
        await context.SaveChangesAsync();

        // Soft-delete one transaction
        deletedTransaction.SoftDelete();
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = _fixture.CreateSharedContext(context);
        var verifyRepo = new TransactionRepository(verifyContext, FakeUserContext.CreateDefault(), NullLogger<TransactionRepository>.Instance);
        var dateRange = await verifyRepo.GetByDateRangeAsync(date.AddDays(-1), date.AddDays(1));

        // Assert
        Assert.Single(dateRange);
        Assert.Contains(dateRange, t => t.Id == activeTransaction.Id);
        Assert.DoesNotContain(dateRange, t => t.Id == deletedTransaction.Id);
    }

    [Fact]
    public async Task SoftDeletedAccount_NotReturnedBy_GetByIdAsync()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var repository = new AccountRepository(context, FakeUserContext.CreateDefault());

        var account = Account.Create("Test Account", AccountType.Checking);
        await repository.AddAsync(account);
        await context.SaveChangesAsync();

        var accountId = account.Id;

        // Soft-delete the account
        account.SoftDelete();
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = _fixture.CreateSharedContext(context);
        var verifyRepo = new AccountRepository(verifyContext, FakeUserContext.CreateDefault());
        var retrieved = await verifyRepo.GetByIdAsync(accountId);

        // Assert
        Assert.Null(retrieved);
    }

    [Fact]
    public async Task QueryFilters_ApplyTransparently_WithoutExplicitFilter()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var categoryRepo = new BudgetCategoryRepository(context, FakeUserContext.CreateDefault());

        var activeCategory = BudgetCategory.Create("Active Category", CategoryType.Expense);
        var deletedCategory = BudgetCategory.Create("Deleted Category", CategoryType.Expense);

        await categoryRepo.AddAsync(activeCategory);
        await categoryRepo.AddAsync(deletedCategory);
        await context.SaveChangesAsync();

        // Soft-delete one category
        deletedCategory.SoftDelete();
        await context.SaveChangesAsync();

        // Act - standard GetAllAsync should only return active categories
        await using var verifyContext = _fixture.CreateSharedContext(context);
        var verifyRepo = new BudgetCategoryRepository(verifyContext, FakeUserContext.CreateDefault());
        var allCategories = await verifyRepo.GetAllAsync();

        // Assert
        Assert.Single(allCategories);
        Assert.Equal(activeCategory.Id, allCategories.Single().Id);
    }

    [Fact]
    public async Task IgnoreQueryFilters_RetrievesSoftDeletedRecords()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var repository = new AccountRepository(context, FakeUserContext.CreateDefault());

        var account = Account.Create("Deleted Account", AccountType.Savings);
        await repository.AddAsync(account);
        await context.SaveChangesAsync();

        var accountId = account.Id;

        // Soft-delete the account
        account.SoftDelete();
        await context.SaveChangesAsync();

        // Act - explicitly ignore query filters
        await using var verifyContext = _fixture.CreateSharedContext(context);
        var deletedAccount = await verifyContext.Accounts
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(a => a.Id == accountId);

        // Assert
        Assert.NotNull(deletedAccount);
        Assert.NotNull(deletedAccount.DeletedAtUtc);
    }

    [Fact]
    public async Task SoftDeletedBudgetGoals_ExcludedFrom_GetByMonthAsync()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var categoryRepo = new BudgetCategoryRepository(context, FakeUserContext.CreateDefault());
        var goalRepo = new BudgetGoalRepository(context, FakeUserContext.CreateDefault());

        var category = BudgetCategory.Create("Test Category", CategoryType.Expense);
        await categoryRepo.AddAsync(category);
        await context.SaveChangesAsync();

        var activeGoal = BudgetGoal.Create(category.Id, 2026, 1, MoneyValue.Create("USD", 500m));
        var deletedGoal = BudgetGoal.Create(category.Id, 2026, 2, MoneyValue.Create("USD", 600m));

        await goalRepo.AddAsync(activeGoal);
        await goalRepo.AddAsync(deletedGoal);
        await context.SaveChangesAsync();

        // Soft-delete one goal
        deletedGoal.SoftDelete();
        await context.SaveChangesAsync();

        // Act - query for Feb goals, should return 0 (deleted goal is filtered out)
        await using var verifyContext = _fixture.CreateSharedContext(context);
        var verifyRepo = new BudgetGoalRepository(verifyContext, FakeUserContext.CreateDefault());
        var febGoals = await verifyRepo.GetByMonthAsync(2026, 2);

        // Assert - deleted goal should not appear
        Assert.Empty(febGoals);

        // Act again - query for Jan goals, should return 1 (active goal)
        var janGoals = await verifyRepo.GetByMonthAsync(2026, 1);

        // Assert - active goal should appear
        Assert.Single(janGoals);
        Assert.Equal(activeGoal.Id, janGoals.Single().Id);
    }

    [Fact]
    public async Task SoftDeletedRecurringTransaction_ExcludedFrom_GetAllAsync()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var accountRepo = new AccountRepository(context, FakeUserContext.CreateDefault());
        var recurringRepo = new RecurringTransactionRepository(context, FakeUserContext.CreateDefault());

        var account = Account.Create("Test Account", AccountType.Checking);
        await accountRepo.AddAsync(account);
        await context.SaveChangesAsync();

        var activeRecurring = RecurringTransaction.Create(
            account.Id,
            "Active subscription",
            MoneyValue.Create("USD", 50m),
            RecurrencePatternValue.CreateMonthly(1, 1),
            DateOnly.FromDateTime(DateTime.UtcNow));

        var deletedRecurring = RecurringTransaction.Create(
            account.Id,
            "Deleted subscription",
            MoneyValue.Create("USD", 75m),
            RecurrencePatternValue.CreateMonthly(1, 1),
            DateOnly.FromDateTime(DateTime.UtcNow));

        await recurringRepo.AddAsync(activeRecurring);
        await recurringRepo.AddAsync(deletedRecurring);
        await context.SaveChangesAsync();

        // Soft-delete one recurring transaction
        deletedRecurring.SoftDelete();
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = _fixture.CreateSharedContext(context);
        var verifyRepo = new RecurringTransactionRepository(verifyContext, FakeUserContext.CreateDefault());
        var allRecurring = await verifyRepo.GetAllAsync();

        // Assert
        Assert.Single(allRecurring);
        Assert.Equal(activeRecurring.Id, allRecurring.Single().Id);
    }

    [Fact]
    public async Task SoftDeletedRecurringTransfer_ExcludedFrom_GetAllAsync()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var accountRepo = new AccountRepository(context, FakeUserContext.CreateDefault());
        var recurringTransferRepo = new RecurringTransferRepository(context, FakeUserContext.CreateDefault());

        var sourceAccount = Account.Create("Source Account", AccountType.Checking);
        var destAccount = Account.Create("Dest Account", AccountType.Savings);
        await accountRepo.AddAsync(sourceAccount);
        await accountRepo.AddAsync(destAccount);
        await context.SaveChangesAsync();

        var activeTransfer = RecurringTransfer.Create(
            sourceAccount.Id,
            destAccount.Id,
            "Active transfer",
            MoneyValue.Create("USD", 500m),
            RecurrencePatternValue.CreateMonthly(1, 1),
            DateOnly.FromDateTime(DateTime.UtcNow));

        var deletedTransfer = RecurringTransfer.Create(
            sourceAccount.Id,
            destAccount.Id,
            "Deleted transfer",
            MoneyValue.Create("USD", 750m),
            RecurrencePatternValue.CreateMonthly(1, 1),
            DateOnly.FromDateTime(DateTime.UtcNow));

        await recurringTransferRepo.AddAsync(activeTransfer);
        await recurringTransferRepo.AddAsync(deletedTransfer);
        await context.SaveChangesAsync();

        // Soft-delete one recurring transfer
        deletedTransfer.SoftDelete();
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = _fixture.CreateSharedContext(context);
        var verifyRepo = new RecurringTransferRepository(verifyContext, FakeUserContext.CreateDefault());
        var allTransfers = await verifyRepo.GetAllAsync();

        // Assert
        Assert.Single(allTransfers);
        Assert.Equal(activeTransfer.Id, allTransfers.Single().Id);
    }

    [Fact]
    public async Task RestoredEntity_ReappearsIn_Queries()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var repository = new AccountRepository(context, FakeUserContext.CreateDefault());

        var account = Account.Create("Test Account", AccountType.Checking);
        await repository.AddAsync(account);
        await context.SaveChangesAsync();

        var accountId = account.Id;

        // Soft-delete then restore
        account.SoftDelete();
        await context.SaveChangesAsync();
        account.Restore();
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = _fixture.CreateSharedContext(context);
        var verifyRepo = new AccountRepository(verifyContext, FakeUserContext.CreateDefault());
        var retrieved = await verifyRepo.GetByIdAsync(accountId);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Null(retrieved.DeletedAtUtc);
    }

    [Fact]
    public async Task SoftDeletedAccount_DoesNotAffectChildTransactions()
    {
        // Arrange: soft-delete is NON-cascading at domain level
        await using var context = _fixture.CreateContext();
        var accountRepo = new AccountRepository(context, FakeUserContext.CreateDefault());
        var transactionRepo = new TransactionRepository(context, FakeUserContext.CreateDefault(), NullLogger<TransactionRepository>.Instance);

        var account = Account.Create("Test Account", AccountType.Checking);
        var amount = MoneyValue.Create("USD", 100m);
        var date = DateOnly.FromDateTime(DateTime.UtcNow);
        var transaction = account.AddTransaction(amount, date, "Test transaction");

        await accountRepo.AddAsync(account);
        await context.SaveChangesAsync();

        var transactionId = transaction.Id;

        // Soft-delete the account
        account.SoftDelete();
        await context.SaveChangesAsync();

        // Act - transaction should still be queryable (non-cascading design)
        await using var verifyContext = _fixture.CreateSharedContext(context);
        var verifyRepo = new TransactionRepository(verifyContext, FakeUserContext.CreateDefault(), NullLogger<TransactionRepository>.Instance);
        var retrievedTransaction = await verifyRepo.GetByIdAsync(transactionId);

        // Assert - transaction remains visible (account soft-delete does not cascade)
        Assert.NotNull(retrievedTransaction);
        Assert.Equal(transactionId, retrievedTransaction.Id);
    }

    [Fact]
    public async Task SoftDeletedBudgetCategory_DoesNotCascadeToTransactions()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var accountRepo = new AccountRepository(context, FakeUserContext.CreateDefault());
        var categoryRepo = new BudgetCategoryRepository(context, FakeUserContext.CreateDefault());
        var transactionRepo = new TransactionRepository(context, FakeUserContext.CreateDefault(), NullLogger<TransactionRepository>.Instance);

        var account = Account.Create("Test Account", AccountType.Checking);
        var category = BudgetCategory.Create("Test Category", CategoryType.Expense);

        await accountRepo.AddAsync(account);
        await categoryRepo.AddAsync(category);
        await context.SaveChangesAsync();

        var amount = MoneyValue.Create("USD", 50m);
        var date = DateOnly.FromDateTime(DateTime.UtcNow);
        var transaction = account.AddTransaction(amount, date, "Categorized transaction", category.Id);
        await context.SaveChangesAsync();

        var transactionId = transaction.Id;

        // Soft-delete the category
        category.SoftDelete();
        await context.SaveChangesAsync();

        // Act - transaction should still be queryable (category soft-delete does not cascade)
        await using var verifyContext = _fixture.CreateSharedContext(context);
        var verifyRepo = new TransactionRepository(verifyContext, FakeUserContext.CreateDefault(), NullLogger<TransactionRepository>.Instance);
        var retrievedTransaction = await verifyRepo.GetByIdAsync(transactionId);

        // Assert - transaction remains visible (category soft-delete does not cascade)
        Assert.NotNull(retrievedTransaction);
        Assert.Equal(transactionId, retrievedTransaction.Id);
    }
}
