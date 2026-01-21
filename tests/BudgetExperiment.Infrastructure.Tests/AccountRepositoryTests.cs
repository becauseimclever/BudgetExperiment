// <copyright file="AccountRepositoryTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;
using BudgetExperiment.Infrastructure.Persistence.Repositories;

namespace BudgetExperiment.Infrastructure.Tests;

/// <summary>
/// Integration tests for <see cref="AccountRepository"/>.
/// </summary>
[Collection("InMemoryDb")]
public class AccountRepositoryTests
{
    private readonly InMemoryDbFixture _fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="AccountRepositoryTests"/> class.
    /// </summary>
    /// <param name="fixture">The shared in-memory database fixture.</param>
    public AccountRepositoryTests(InMemoryDbFixture fixture)
    {
        this._fixture = fixture;
    }

    [Fact]
    public async Task AddAsync_And_GetByIdAsync_Persists_Account()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var repository = new AccountRepository(context, FakeUserContext.CreateDefault());
        var account = Account.Create("Test Checking", AccountType.Checking);

        // Act
        await repository.AddAsync(account);
        await context.SaveChangesAsync();

        // Assert - use shared context to verify persistence
        await using var verifyContext = this._fixture.CreateSharedContext(context);
        var verifyRepo = new AccountRepository(verifyContext, FakeUserContext.CreateDefault());
        var retrieved = await verifyRepo.GetByIdAsync(account.Id);

        Assert.NotNull(retrieved);
        Assert.Equal(account.Id, retrieved.Id);
        Assert.Equal("Test Checking", retrieved.Name);
        Assert.Equal(AccountType.Checking, retrieved.Type);
    }

    [Fact]
    public async Task GetByIdAsync_Returns_Null_When_Not_Found()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var repository = new AccountRepository(context, FakeUserContext.CreateDefault());

        // Act
        var result = await repository.GetByIdAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_Returns_All_Accounts_Ordered_By_Name()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var repository = new AccountRepository(context, FakeUserContext.CreateDefault());

        var account1 = Account.Create("Zebra Account", AccountType.Savings);
        var account2 = Account.Create("Alpha Account", AccountType.Checking);

        await repository.AddAsync(account1);
        await repository.AddAsync(account2);
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = this._fixture.CreateSharedContext(context);
        var verifyRepo = new AccountRepository(verifyContext, FakeUserContext.CreateDefault());
        var accounts = await verifyRepo.GetAllAsync();

        // Assert - should be ordered by name
        Assert.Equal(2, accounts.Count);
        Assert.Equal("Alpha Account", accounts[0].Name);
        Assert.Equal("Zebra Account", accounts[1].Name);
    }

    [Fact]
    public async Task GetByIdWithTransactionsAsync_Includes_Transactions()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var repository = new AccountRepository(context, FakeUserContext.CreateDefault());

        var account = Account.Create("Account With Transactions", AccountType.Checking);
        account.AddTransaction(MoneyValue.Create("USD", 100m), new DateOnly(2026, 1, 9), "Test Transaction");

        await repository.AddAsync(account);
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = this._fixture.CreateSharedContext(context);
        var verifyRepo = new AccountRepository(verifyContext, FakeUserContext.CreateDefault());
        var retrieved = await verifyRepo.GetByIdWithTransactionsAsync(account.Id);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Single(retrieved.Transactions);
        Assert.Equal("Test Transaction", retrieved.Transactions.First().Description);
    }

    [Fact]
    public async Task RemoveAsync_Deletes_Account()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var repository = new AccountRepository(context, FakeUserContext.CreateDefault());

        var account = Account.Create("To Be Deleted", AccountType.Cash);
        await repository.AddAsync(account);
        await context.SaveChangesAsync();

        // Act
        await repository.RemoveAsync(account);
        await context.SaveChangesAsync();

        // Assert
        await using var verifyContext = this._fixture.CreateSharedContext(context);
        var verifyRepo = new AccountRepository(verifyContext, FakeUserContext.CreateDefault());
        var retrieved = await verifyRepo.GetByIdAsync(account.Id);
        Assert.Null(retrieved);
    }

    [Fact]
    public async Task CountAsync_Returns_Correct_Count()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var repository = new AccountRepository(context, FakeUserContext.CreateDefault());

        var initialCount = await repository.CountAsync();
        Assert.Equal(0, initialCount);

        var account = Account.Create("Count Test Account", AccountType.Other);
        await repository.AddAsync(account);
        await context.SaveChangesAsync();

        // Act
        var newCount = await repository.CountAsync();

        // Assert
        Assert.Equal(1, newCount);
    }

    [Fact]
    public async Task ListAsync_Supports_Pagination()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var repository = new AccountRepository(context, FakeUserContext.CreateDefault());

        for (int i = 0; i < 5; i++)
        {
            var account = Account.Create($"Account_{i:D2}", AccountType.Checking);
            await repository.AddAsync(account);
        }

        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = this._fixture.CreateSharedContext(context);
        var verifyRepo = new AccountRepository(verifyContext, FakeUserContext.CreateDefault());
        var page1 = await verifyRepo.ListAsync(0, 2);
        var page2 = await verifyRepo.ListAsync(2, 2);

        // Assert
        Assert.Equal(2, page1.Count);
        Assert.Equal(2, page2.Count);
        Assert.NotEqual(page1[0].Id, page2[0].Id);
    }

    [Fact]
    public async Task AddAsync_Persists_Account_With_InitialBalance()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var repository = new AccountRepository(context, FakeUserContext.CreateDefault());
        var initialBalance = MoneyValue.Create("USD", 1500.00m);
        var initialBalanceDate = new DateOnly(2026, 1, 1);
        var account = Account.Create("Checking With Balance", AccountType.Checking, initialBalance, initialBalanceDate);

        // Act
        await repository.AddAsync(account);
        await context.SaveChangesAsync();

        // Assert
        await using var verifyContext = this._fixture.CreateSharedContext(context);
        var verifyRepo = new AccountRepository(verifyContext, FakeUserContext.CreateDefault());
        var retrieved = await verifyRepo.GetByIdAsync(account.Id);

        Assert.NotNull(retrieved);
        Assert.Equal(1500.00m, retrieved.InitialBalance.Amount);
        Assert.Equal("USD", retrieved.InitialBalance.Currency);
        Assert.Equal(new DateOnly(2026, 1, 1), retrieved.InitialBalanceDate);
    }

    [Fact]
    public async Task Account_InitialBalance_Update_Persists()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var repository = new AccountRepository(context, FakeUserContext.CreateDefault());
        var account = Account.Create("Update Balance Test", AccountType.Savings);
        await repository.AddAsync(account);
        await context.SaveChangesAsync();

        // Act - update initial balance
        account.UpdateInitialBalance(MoneyValue.Create("USD", 2500.00m), new DateOnly(2026, 1, 15));
        await context.SaveChangesAsync();

        // Assert
        await using var verifyContext = this._fixture.CreateSharedContext(context);
        var verifyRepo = new AccountRepository(verifyContext, FakeUserContext.CreateDefault());
        var retrieved = await verifyRepo.GetByIdAsync(account.Id);

        Assert.NotNull(retrieved);
        Assert.Equal(2500.00m, retrieved.InitialBalance.Amount);
        Assert.Equal(new DateOnly(2026, 1, 15), retrieved.InitialBalanceDate);
    }

    [Fact]
    public async Task Account_With_Negative_InitialBalance_Persists()
    {
        // Arrange - credit card with existing debt
        await using var context = this._fixture.CreateContext();
        var repository = new AccountRepository(context, FakeUserContext.CreateDefault());
        var initialBalance = MoneyValue.Create("USD", -2500.00m);
        var account = Account.Create("Credit Card Debt", AccountType.CreditCard, initialBalance, new DateOnly(2026, 1, 1));

        // Act
        await repository.AddAsync(account);
        await context.SaveChangesAsync();

        // Assert
        await using var verifyContext = this._fixture.CreateSharedContext(context);
        var verifyRepo = new AccountRepository(verifyContext, FakeUserContext.CreateDefault());
        var retrieved = await verifyRepo.GetByIdAsync(account.Id);

        Assert.NotNull(retrieved);
        Assert.Equal(-2500.00m, retrieved.InitialBalance.Amount);
    }

    [Fact]
    public async Task GetAllAsync_WithPersonalScope_Returns_Only_Users_Personal_Accounts()
    {
        // Arrange - create a personal account for the test user
        await using var context = this._fixture.CreateContext();
        var userId = FakeUserContext.DefaultUserId;
        var otherUserId = Guid.NewGuid();

        // Create personal account for test user
        var userPersonalAccount = Account.CreatePersonal("My Personal Account", AccountType.Checking, userId);

        // Create personal account for another user
        var otherUserAccount = Account.CreatePersonal("Other User Account", AccountType.Savings, otherUserId);

        // Create shared account (should not be returned in Personal scope)
        var sharedAccount = Account.CreateShared("Shared Account", AccountType.Checking, userId);

        context.Accounts.AddRange(userPersonalAccount, otherUserAccount, sharedAccount);
        await context.SaveChangesAsync();

        // Act - query with Personal scope for the test user
        await using var verifyContext = this._fixture.CreateSharedContext(context);
        var personalScopeContext = FakeUserContext.CreateForPersonalScope(userId);
        var repository = new AccountRepository(verifyContext, personalScopeContext);
        var accounts = await repository.GetAllAsync();

        // Assert - should only return the current user's personal account
        Assert.Single(accounts);
        Assert.Equal("My Personal Account", accounts[0].Name);
        Assert.Equal(BudgetScope.Personal, accounts[0].Scope);
        Assert.Equal(userId, accounts[0].OwnerUserId);
    }

    [Fact]
    public async Task GetAllAsync_WithAllScope_Returns_Shared_And_Users_Personal_Accounts()
    {
        // Arrange - create accounts with different scopes
        await using var context = this._fixture.CreateContext();
        var userId = FakeUserContext.DefaultUserId;
        var otherUserId = Guid.NewGuid();

        // Create personal account for test user
        var userPersonalAccount = Account.CreatePersonal("My Personal Account", AccountType.Checking, userId);

        // Create personal account for another user (should NOT be returned)
        var otherUserAccount = Account.CreatePersonal("Other User Account", AccountType.Savings, otherUserId);

        // Create shared account (should be returned)
        var sharedAccount = Account.CreateShared("Shared Account", AccountType.Checking, userId);

        context.Accounts.AddRange(userPersonalAccount, otherUserAccount, sharedAccount);
        await context.SaveChangesAsync();

        // Act - query with "All" scope (null CurrentScope) for the test user
        await using var verifyContext = this._fixture.CreateSharedContext(context);
        var allScopeContext = new FakeUserContext(userId: userId, currentScope: null); // null = All
        var repository = new AccountRepository(verifyContext, allScopeContext);
        var accounts = await repository.GetAllAsync();

        // Assert - should return shared AND the current user's personal account, but NOT other user's personal
        Assert.Equal(2, accounts.Count);
        Assert.Contains(accounts, a => a.Name == "My Personal Account");
        Assert.Contains(accounts, a => a.Name == "Shared Account");
        Assert.DoesNotContain(accounts, a => a.Name == "Other User Account");
    }
}
