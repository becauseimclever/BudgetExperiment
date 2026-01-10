// <copyright file="AccountRepositoryTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;
using BudgetExperiment.Infrastructure.Repositories;

namespace BudgetExperiment.Infrastructure.Tests;

/// <summary>
/// Integration tests for <see cref="AccountRepository"/>.
/// </summary>
[Collection("Postgres")]
public class AccountRepositoryTests
{
    private readonly PostgresFixture _fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="AccountRepositoryTests"/> class.
    /// </summary>
    /// <param name="fixture">The shared PostgreSQL fixture.</param>
    public AccountRepositoryTests(PostgresFixture fixture)
    {
        this._fixture = fixture;
    }

    [Fact]
    public async Task AddAsync_And_GetByIdAsync_Persists_Account()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var repository = new AccountRepository(context);
        var account = Account.Create("Test Checking", AccountType.Checking);

        // Act
        await repository.AddAsync(account);
        await context.SaveChangesAsync();

        // Assert - use fresh context to verify persistence
        await using var verifyContext = this._fixture.CreateContext();
        var verifyRepo = new AccountRepository(verifyContext);
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
        var repository = new AccountRepository(context);

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
        var repository = new AccountRepository(context);

        var account1 = Account.Create("Zebra Account", AccountType.Savings);
        var account2 = Account.Create("Alpha Account", AccountType.Checking);

        await repository.AddAsync(account1);
        await repository.AddAsync(account2);
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = this._fixture.CreateContext();
        var verifyRepo = new AccountRepository(verifyContext);
        var accounts = await verifyRepo.GetAllAsync();

        // Assert - should be ordered by name
        Assert.True(accounts.Count >= 2);
        var alphaIndex = accounts.ToList().FindIndex(a => a.Name == "Alpha Account");
        var zebraIndex = accounts.ToList().FindIndex(a => a.Name == "Zebra Account");
        Assert.True(alphaIndex < zebraIndex);
    }

    [Fact]
    public async Task GetByIdWithTransactionsAsync_Includes_Transactions()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var repository = new AccountRepository(context);

        var account = Account.Create("Account With Transactions", AccountType.Checking);
        account.AddTransaction(MoneyValue.Create("USD", 100m), new DateOnly(2026, 1, 9), "Test Transaction");

        await repository.AddAsync(account);
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = this._fixture.CreateContext();
        var verifyRepo = new AccountRepository(verifyContext);
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
        var repository = new AccountRepository(context);

        var account = Account.Create("To Be Deleted", AccountType.Cash);
        await repository.AddAsync(account);
        await context.SaveChangesAsync();

        // Act
        await repository.RemoveAsync(account);
        await context.SaveChangesAsync();

        // Assert
        await using var verifyContext = this._fixture.CreateContext();
        var verifyRepo = new AccountRepository(verifyContext);
        var retrieved = await verifyRepo.GetByIdAsync(account.Id);
        Assert.Null(retrieved);
    }

    [Fact]
    public async Task CountAsync_Returns_Correct_Count()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var repository = new AccountRepository(context);

        var initialCount = await repository.CountAsync();

        var account = Account.Create("Count Test Account", AccountType.Other);
        await repository.AddAsync(account);
        await context.SaveChangesAsync();

        // Act
        var newCount = await repository.CountAsync();

        // Assert
        Assert.Equal(initialCount + 1, newCount);
    }

    [Fact]
    public async Task ListAsync_Supports_Pagination()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var repository = new AccountRepository(context);

        // Add accounts with unique prefix
        var prefix = Guid.NewGuid().ToString()[..8];
        for (int i = 0; i < 5; i++)
        {
            var account = Account.Create($"{prefix}_Account_{i:D2}", AccountType.Checking);
            await repository.AddAsync(account);
        }

        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = this._fixture.CreateContext();
        var verifyRepo = new AccountRepository(verifyContext);
        var page1 = await verifyRepo.ListAsync(0, 2);
        var page2 = await verifyRepo.ListAsync(2, 2);

        // Assert
        Assert.Equal(2, page1.Count);
        Assert.Equal(2, page2.Count);
        Assert.NotEqual(page1[0].Id, page2[0].Id);
    }
}
