// <copyright file="RecurringTransactionRepositoryTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;
using BudgetExperiment.Infrastructure.Persistence.Repositories;

namespace BudgetExperiment.Infrastructure.Tests;

/// <summary>
/// Integration tests for <see cref="RecurringTransactionRepository"/>.
/// </summary>
[Collection("InMemoryDb")]
public class RecurringTransactionRepositoryTests
{
    private readonly InMemoryDbFixture _fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecurringTransactionRepositoryTests"/> class.
    /// </summary>
    /// <param name="fixture">The shared in-memory database fixture.</param>
    public RecurringTransactionRepositoryTests(InMemoryDbFixture fixture)
    {
        this._fixture = fixture;
    }

    [Fact]
    public async Task AddAsync_And_GetByIdAsync_Persists_RecurringTransaction()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var account = Account.Create("Checking", AccountType.Checking);
        context.Accounts.Add(account);
        await context.SaveChangesAsync();

        var repository = new RecurringTransactionRepository(context, FakeUserContext.CreateDefault());
        var recurring = RecurringTransaction.Create(
            account.Id,
            "Monthly Rent",
            MoneyValue.Create("USD", 1500m),
            RecurrencePatternValue.CreateMonthly(1, 1),
            new DateOnly(2026, 1, 1));

        // Act
        await repository.AddAsync(recurring);
        await context.SaveChangesAsync();

        // Assert
        await using var verifyContext = this._fixture.CreateSharedContext(context);
        var verifyRepo = new RecurringTransactionRepository(verifyContext, FakeUserContext.CreateDefault());
        var retrieved = await verifyRepo.GetByIdAsync(recurring.Id);

        Assert.NotNull(retrieved);
        Assert.Equal("Monthly Rent", retrieved.Description);
        Assert.Equal(1500m, retrieved.Amount.Amount);
    }

    [Fact]
    public async Task GetByAccountIdAsync_Returns_Only_Transactions_For_Account()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var account1 = Account.Create("Checking", AccountType.Checking);
        var account2 = Account.Create("Savings", AccountType.Savings);
        context.Accounts.AddRange(account1, account2);
        await context.SaveChangesAsync();

        var repository = new RecurringTransactionRepository(context, FakeUserContext.CreateDefault());

        var tx1 = RecurringTransaction.Create(account1.Id, "Rent", MoneyValue.Create("USD", 1500m), RecurrencePatternValue.CreateMonthly(1, 1), new DateOnly(2026, 1, 1));
        var tx2 = RecurringTransaction.Create(account1.Id, "Utilities", MoneyValue.Create("USD", 200m), RecurrencePatternValue.CreateMonthly(1, 15), new DateOnly(2026, 1, 15));
        var tx3 = RecurringTransaction.Create(account2.Id, "Transfer", MoneyValue.Create("USD", 500m), RecurrencePatternValue.CreateMonthly(1, 1), new DateOnly(2026, 1, 1));

        await repository.AddAsync(tx1);
        await repository.AddAsync(tx2);
        await repository.AddAsync(tx3);
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = this._fixture.CreateSharedContext(context);
        var verifyRepo = new RecurringTransactionRepository(verifyContext, FakeUserContext.CreateDefault());
        var results = await verifyRepo.GetByAccountIdAsync(account1.Id);

        // Assert
        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.Equal(account1.Id, r.AccountId));
    }

    [Fact]
    public async Task GetActiveAsync_Returns_Only_Active_Ordered_By_NextOccurrence()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var account = Account.Create("Checking", AccountType.Checking);
        context.Accounts.Add(account);
        await context.SaveChangesAsync();

        var repository = new RecurringTransactionRepository(context, FakeUserContext.CreateDefault());

        var active1 = RecurringTransaction.Create(account.Id, "B-Active", MoneyValue.Create("USD", 100m), RecurrencePatternValue.CreateMonthly(1, 15), new DateOnly(2026, 1, 15));
        var active2 = RecurringTransaction.Create(account.Id, "A-Active", MoneyValue.Create("USD", 200m), RecurrencePatternValue.CreateMonthly(1, 1), new DateOnly(2026, 1, 1));
        var paused = RecurringTransaction.Create(account.Id, "Paused", MoneyValue.Create("USD", 300m), RecurrencePatternValue.CreateMonthly(1, 10), new DateOnly(2026, 1, 10));
        paused.Pause();

        await repository.AddAsync(active1);
        await repository.AddAsync(active2);
        await repository.AddAsync(paused);
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = this._fixture.CreateSharedContext(context);
        var verifyRepo = new RecurringTransactionRepository(verifyContext, FakeUserContext.CreateDefault());
        var results = await verifyRepo.GetActiveAsync();

        // Assert — paused excluded, ordered by NextOccurrence
        Assert.Equal(2, results.Count);
        Assert.DoesNotContain(results, r => r.Description == "Paused");
        Assert.True(results[0].NextOccurrence <= results[1].NextOccurrence);
    }

    [Fact]
    public async Task GetAllAsync_Returns_All_Ordered_By_Description()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var account = Account.Create("Checking", AccountType.Checking);
        context.Accounts.Add(account);
        await context.SaveChangesAsync();

        var repository = new RecurringTransactionRepository(context, FakeUserContext.CreateDefault());

        var txC = RecurringTransaction.Create(account.Id, "C-Item", MoneyValue.Create("USD", 100m), RecurrencePatternValue.CreateMonthly(1, 1), new DateOnly(2026, 1, 1));
        var txA = RecurringTransaction.Create(account.Id, "A-Item", MoneyValue.Create("USD", 200m), RecurrencePatternValue.CreateMonthly(1, 1), new DateOnly(2026, 1, 1));
        var txB = RecurringTransaction.Create(account.Id, "B-Item", MoneyValue.Create("USD", 300m), RecurrencePatternValue.CreateMonthly(1, 1), new DateOnly(2026, 1, 1));

        await repository.AddAsync(txC);
        await repository.AddAsync(txA);
        await repository.AddAsync(txB);
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = this._fixture.CreateSharedContext(context);
        var verifyRepo = new RecurringTransactionRepository(verifyContext, FakeUserContext.CreateDefault());
        var results = await verifyRepo.GetAllAsync();

        // Assert
        Assert.Equal(3, results.Count);
        Assert.Equal("A-Item", results[0].Description);
        Assert.Equal("B-Item", results[1].Description);
        Assert.Equal("C-Item", results[2].Description);
    }

    [Fact]
    public async Task AddExceptionAsync_And_GetExceptionAsync_Persists_Exception()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var account = Account.Create("Checking", AccountType.Checking);
        context.Accounts.Add(account);
        await context.SaveChangesAsync();

        var repository = new RecurringTransactionRepository(context, FakeUserContext.CreateDefault());
        var recurring = RecurringTransaction.Create(account.Id, "Rent", MoneyValue.Create("USD", 1500m), RecurrencePatternValue.CreateMonthly(1, 1), new DateOnly(2026, 1, 1));
        await repository.AddAsync(recurring);
        await context.SaveChangesAsync();

        var exception = RecurringTransactionException.CreateModified(
            recurring.Id,
            new DateOnly(2026, 2, 1),
            MoneyValue.Create("USD", 1600m),
            "Rent increase",
            null);

        // Act
        await repository.AddExceptionAsync(exception);
        await context.SaveChangesAsync();

        // Assert
        await using var verifyContext = this._fixture.CreateSharedContext(context);
        var verifyRepo = new RecurringTransactionRepository(verifyContext, FakeUserContext.CreateDefault());
        var retrieved = await verifyRepo.GetExceptionAsync(recurring.Id, new DateOnly(2026, 2, 1));

        Assert.NotNull(retrieved);
        Assert.Equal(ExceptionType.Modified, retrieved.ExceptionType);
        Assert.Equal(1600m, retrieved.ModifiedAmount!.Amount);
        Assert.Equal("Rent increase", retrieved.ModifiedDescription);
    }

    [Fact]
    public async Task GetExceptionsByDateRangeAsync_Returns_Only_Exceptions_In_Range()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var account = Account.Create("Checking", AccountType.Checking);
        context.Accounts.Add(account);
        await context.SaveChangesAsync();

        var repository = new RecurringTransactionRepository(context, FakeUserContext.CreateDefault());
        var recurring = RecurringTransaction.Create(account.Id, "Rent", MoneyValue.Create("USD", 1500m), RecurrencePatternValue.CreateMonthly(1, 1), new DateOnly(2026, 1, 1));
        await repository.AddAsync(recurring);
        await context.SaveChangesAsync();

        var jan = RecurringTransactionException.CreateSkipped(recurring.Id, new DateOnly(2026, 1, 1));
        var feb = RecurringTransactionException.CreateModified(recurring.Id, new DateOnly(2026, 2, 1), MoneyValue.Create("USD", 1600m), null, null);
        var mar = RecurringTransactionException.CreateSkipped(recurring.Id, new DateOnly(2026, 3, 1));
        var apr = RecurringTransactionException.CreateModified(recurring.Id, new DateOnly(2026, 4, 1), MoneyValue.Create("USD", 1700m), null, null);

        await repository.AddExceptionAsync(jan);
        await repository.AddExceptionAsync(feb);
        await repository.AddExceptionAsync(mar);
        await repository.AddExceptionAsync(apr);
        await context.SaveChangesAsync();

        // Act — query Feb to Mar only
        await using var verifyContext = this._fixture.CreateSharedContext(context);
        var verifyRepo = new RecurringTransactionRepository(verifyContext, FakeUserContext.CreateDefault());
        var results = await verifyRepo.GetExceptionsByDateRangeAsync(
            recurring.Id,
            new DateOnly(2026, 2, 1),
            new DateOnly(2026, 3, 31));

        // Assert — only Feb and Mar exceptions, ordered by date
        Assert.Equal(2, results.Count);
        Assert.Equal(new DateOnly(2026, 2, 1), results[0].OriginalDate);
        Assert.Equal(new DateOnly(2026, 3, 1), results[1].OriginalDate);
    }

    [Fact]
    public async Task RemoveExceptionAsync_Deletes_Exception()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var account = Account.Create("Checking", AccountType.Checking);
        context.Accounts.Add(account);
        await context.SaveChangesAsync();

        var repository = new RecurringTransactionRepository(context, FakeUserContext.CreateDefault());
        var recurring = RecurringTransaction.Create(account.Id, "Rent", MoneyValue.Create("USD", 1500m), RecurrencePatternValue.CreateMonthly(1, 1), new DateOnly(2026, 1, 1));
        await repository.AddAsync(recurring);
        await context.SaveChangesAsync();

        var exception = RecurringTransactionException.CreateSkipped(recurring.Id, new DateOnly(2026, 2, 1));
        await repository.AddExceptionAsync(exception);
        await context.SaveChangesAsync();

        // Act
        await repository.RemoveExceptionAsync(exception);
        await context.SaveChangesAsync();

        // Assert
        await using var verifyContext = this._fixture.CreateSharedContext(context);
        var verifyRepo = new RecurringTransactionRepository(verifyContext, FakeUserContext.CreateDefault());
        var retrieved = await verifyRepo.GetExceptionAsync(recurring.Id, new DateOnly(2026, 2, 1));
        Assert.Null(retrieved);
    }

    [Fact]
    public async Task RemoveExceptionsFromDateAsync_Removes_Only_Future_Exceptions()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var account = Account.Create("Checking", AccountType.Checking);
        context.Accounts.Add(account);
        await context.SaveChangesAsync();

        var repository = new RecurringTransactionRepository(context, FakeUserContext.CreateDefault());
        var recurring = RecurringTransaction.Create(account.Id, "Rent", MoneyValue.Create("USD", 1500m), RecurrencePatternValue.CreateMonthly(1, 1), new DateOnly(2026, 1, 1));
        await repository.AddAsync(recurring);
        await context.SaveChangesAsync();

        var jan = RecurringTransactionException.CreateSkipped(recurring.Id, new DateOnly(2026, 1, 1));
        var feb = RecurringTransactionException.CreateSkipped(recurring.Id, new DateOnly(2026, 2, 1));
        var mar = RecurringTransactionException.CreateSkipped(recurring.Id, new DateOnly(2026, 3, 1));
        var apr = RecurringTransactionException.CreateSkipped(recurring.Id, new DateOnly(2026, 4, 1));

        await repository.AddExceptionAsync(jan);
        await repository.AddExceptionAsync(feb);
        await repository.AddExceptionAsync(mar);
        await repository.AddExceptionAsync(apr);
        await context.SaveChangesAsync();

        // Act — remove March and later
        await repository.RemoveExceptionsFromDateAsync(recurring.Id, new DateOnly(2026, 3, 1));
        await context.SaveChangesAsync();

        // Assert — Jan and Feb remain, March and April deleted
        await using var verifyContext = this._fixture.CreateSharedContext(context);
        var verifyRepo = new RecurringTransactionRepository(verifyContext, FakeUserContext.CreateDefault());

        Assert.NotNull(await verifyRepo.GetExceptionAsync(recurring.Id, new DateOnly(2026, 1, 1)));
        Assert.NotNull(await verifyRepo.GetExceptionAsync(recurring.Id, new DateOnly(2026, 2, 1)));
        Assert.Null(await verifyRepo.GetExceptionAsync(recurring.Id, new DateOnly(2026, 3, 1)));
        Assert.Null(await verifyRepo.GetExceptionAsync(recurring.Id, new DateOnly(2026, 4, 1)));
    }

    [Fact]
    public async Task ScopeFilter_SharedScope_Returns_Only_Shared_Transactions()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var userId = FakeUserContext.DefaultUserId;

        var account = Account.Create("Checking", AccountType.Checking);
        context.Accounts.Add(account);
        await context.SaveChangesAsync();

        var shared = RecurringTransaction.Create(account.Id, "Shared Rent", MoneyValue.Create("USD", 1500m), RecurrencePatternValue.CreateMonthly(1, 1), new DateOnly(2026, 1, 1));
        var personal = RecurringTransaction.Create(account.Id, "Personal Sub", MoneyValue.Create("USD", 15m), RecurrencePatternValue.CreateMonthly(1, 1), new DateOnly(2026, 1, 1));

        context.RecurringTransactions.AddRange(shared, personal);
        await context.SaveChangesAsync();

        context.Entry(personal).Property(e => e.Scope).CurrentValue = BudgetScope.Personal;
        context.Entry(personal).Property(e => e.OwnerUserId).CurrentValue = userId;
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = this._fixture.CreateSharedContext(context);
        var sharedScopeContext = FakeUserContext.CreateForSharedScope();
        var repository = new RecurringTransactionRepository(verifyContext, sharedScopeContext);
        var results = await repository.GetAllAsync();

        // Assert
        Assert.Single(results);
        Assert.Equal("Shared Rent", results[0].Description);
    }

    [Fact]
    public async Task GetExceptionAsync_Returns_Null_For_NonExistent_Date()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var account = Account.Create("Checking", AccountType.Checking);
        context.Accounts.Add(account);
        await context.SaveChangesAsync();

        var repository = new RecurringTransactionRepository(context, FakeUserContext.CreateDefault());
        var recurring = RecurringTransaction.Create(account.Id, "Test", MoneyValue.Create("USD", 100m), RecurrencePatternValue.CreateMonthly(1, 1), new DateOnly(2026, 1, 1));
        await repository.AddAsync(recurring);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetExceptionAsync(recurring.Id, new DateOnly(2026, 12, 1));

        // Assert
        Assert.Null(result);
    }
}
