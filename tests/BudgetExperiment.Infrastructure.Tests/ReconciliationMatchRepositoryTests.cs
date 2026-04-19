// <copyright file="ReconciliationMatchRepositoryTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;
using BudgetExperiment.Infrastructure.Persistence;
using BudgetExperiment.Infrastructure.Persistence.Repositories;

namespace BudgetExperiment.Infrastructure.Tests;

/// <summary>
/// Integration tests for <see cref="ReconciliationMatchRepository"/>.
/// </summary>
[Collection("PostgreSqlDb")]
public class ReconciliationMatchRepositoryTests
{
    private readonly PostgreSqlFixture _fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReconciliationMatchRepositoryTests"/> class.
    /// </summary>
    /// <param name="fixture">The shared PostgreSQL database fixture.</param>
    public ReconciliationMatchRepositoryTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task AddAsync_And_GetByIdAsync_Returns_Match()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var (_, transactionId, recurringId) = await this.CreateTestDataAsync(context);

        var match = ReconciliationMatch.Create(
            transactionId,
            recurringId,
            new DateOnly(2026, 1, 15),
            confidenceScore: 0.85m,
            amountVariance: 0m,
            dateOffsetDays: 0,
            ownerUserId: null);

        var repo = new ReconciliationMatchRepository(context, FakeUserContext.CreateDefault());
        await repo.AddAsync(match);
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = _fixture.CreateSharedContext(context);
        var verifyRepo = new ReconciliationMatchRepository(verifyContext, FakeUserContext.CreateDefault());
        var retrieved = await verifyRepo.GetByIdAsync(match.Id);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(match.Id, retrieved.Id);
        Assert.Equal(transactionId, retrieved.ImportedTransactionId);
        Assert.Equal(recurringId, retrieved.RecurringTransactionId);
        Assert.Equal(0.85m, retrieved.ConfidenceScore);
        Assert.Equal(ReconciliationMatchStatus.Suggested, retrieved.Status);
    }

    [Fact]
    public async Task GetPendingMatchesAsync_Returns_Only_Suggested_Matches()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var (_, transactionId, recurringId) = await this.CreateTestDataAsync(context);

        var suggestedMatch = ReconciliationMatch.Create(
            transactionId,
            recurringId,
            new DateOnly(2026, 1, 15),
            confidenceScore: 0.75m,
            amountVariance: 0m,
            dateOffsetDays: 0,
            ownerUserId: null);

        var acceptedMatch = ReconciliationMatch.Create(
            transactionId,
            recurringId,
            new DateOnly(2026, 1, 22),
            confidenceScore: 0.90m,
            amountVariance: -5.00m,
            dateOffsetDays: 1,
            ownerUserId: null);
        acceptedMatch.Accept();

        var repo = new ReconciliationMatchRepository(context, FakeUserContext.CreateDefault());
        await repo.AddAsync(suggestedMatch);
        await repo.AddAsync(acceptedMatch);
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = _fixture.CreateSharedContext(context);
        var verifyRepo = new ReconciliationMatchRepository(verifyContext, FakeUserContext.CreateDefault());
        var pendingMatches = await verifyRepo.GetPendingMatchesAsync();

        // Assert
        Assert.Single(pendingMatches);
        Assert.Equal(suggestedMatch.Id, pendingMatches[0].Id);
    }

    [Fact]
    public async Task GetByRecurringTransactionAsync_Filters_By_RecurringId_And_DateRange()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var (_, transactionId, recurringId) = await this.CreateTestDataAsync(context);

        var janMatch = ReconciliationMatch.Create(
            transactionId,
            recurringId,
            new DateOnly(2026, 1, 15),
            confidenceScore: 0.85m,
            amountVariance: 0m,
            dateOffsetDays: 0,
            ownerUserId: null);

        var febMatch = ReconciliationMatch.Create(
            transactionId,
            recurringId,
            new DateOnly(2026, 2, 15),
            confidenceScore: 0.80m,
            amountVariance: 0m,
            dateOffsetDays: 0,
            ownerUserId: null);

        var repo = new ReconciliationMatchRepository(context, FakeUserContext.CreateDefault());
        await repo.AddAsync(janMatch);
        await repo.AddAsync(febMatch);
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = _fixture.CreateSharedContext(context);
        var verifyRepo = new ReconciliationMatchRepository(verifyContext, FakeUserContext.CreateDefault());
        var janMatches = await verifyRepo.GetByRecurringTransactionAsync(
            recurringId,
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 31));

        // Assert
        Assert.Single(janMatches);
        Assert.Equal(janMatch.Id, janMatches[0].Id);
    }

    [Fact]
    public async Task GetByTransactionIdAsync_Returns_Matches_For_Transaction()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var (_, transactionId, recurringId) = await this.CreateTestDataAsync(context);

        var match = ReconciliationMatch.Create(
            transactionId,
            recurringId,
            new DateOnly(2026, 1, 15),
            confidenceScore: 0.85m,
            amountVariance: 0m,
            dateOffsetDays: 0,
            ownerUserId: null);

        var repo = new ReconciliationMatchRepository(context, FakeUserContext.CreateDefault());
        await repo.AddAsync(match);
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = _fixture.CreateSharedContext(context);
        var verifyRepo = new ReconciliationMatchRepository(verifyContext, FakeUserContext.CreateDefault());
        var matches = await verifyRepo.GetByTransactionIdAsync(transactionId);

        // Assert
        Assert.Single(matches);
        Assert.Equal(match.Id, matches[0].Id);
    }

    [Fact]
    public async Task GetByPeriodAsync_Returns_Matches_For_Month()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var (_, transactionId, recurringId) = await this.CreateTestDataAsync(context);

        var janMatch = ReconciliationMatch.Create(
            transactionId,
            recurringId,
            new DateOnly(2026, 1, 15),
            confidenceScore: 0.85m,
            amountVariance: 0m,
            dateOffsetDays: 0,
            ownerUserId: null);

        var febMatch = ReconciliationMatch.Create(
            transactionId,
            recurringId,
            new DateOnly(2026, 2, 15),
            confidenceScore: 0.80m,
            amountVariance: 0m,
            dateOffsetDays: 0,
            ownerUserId: null);

        var repo = new ReconciliationMatchRepository(context, FakeUserContext.CreateDefault());
        await repo.AddAsync(janMatch);
        await repo.AddAsync(febMatch);
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = _fixture.CreateSharedContext(context);
        var verifyRepo = new ReconciliationMatchRepository(verifyContext, FakeUserContext.CreateDefault());
        var janMatches = await verifyRepo.GetByPeriodAsync(2026, 1);

        // Assert
        Assert.Single(janMatches);
        Assert.Equal(janMatch.Id, janMatches[0].Id);
    }

    [Fact]
    public async Task ExistsAsync_Returns_True_When_Match_Exists()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var (_, transactionId, recurringId) = await this.CreateTestDataAsync(context);
        var instanceDate = new DateOnly(2026, 1, 15);

        var match = ReconciliationMatch.Create(
            transactionId,
            recurringId,
            instanceDate,
            confidenceScore: 0.85m,
            amountVariance: 0m,
            dateOffsetDays: 0,
            ownerUserId: null);

        var repo = new ReconciliationMatchRepository(context, FakeUserContext.CreateDefault());
        await repo.AddAsync(match);
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = _fixture.CreateSharedContext(context);
        var verifyRepo = new ReconciliationMatchRepository(verifyContext, FakeUserContext.CreateDefault());
        var exists = await verifyRepo.ExistsAsync(transactionId, recurringId, instanceDate);

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public async Task ExistsAsync_Returns_False_When_Match_Does_Not_Exist()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act
        var repo = new ReconciliationMatchRepository(context, FakeUserContext.CreateDefault());
        var exists = await repo.ExistsAsync(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 1, 15));

        // Assert
        Assert.False(exists);
    }

    [Fact]
    public async Task RemoveAsync_Deletes_Match()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var (_, transactionId, recurringId) = await this.CreateTestDataAsync(context);

        var match = ReconciliationMatch.Create(
            transactionId,
            recurringId,
            new DateOnly(2026, 1, 15),
            confidenceScore: 0.85m,
            amountVariance: 0m,
            dateOffsetDays: 0,
            ownerUserId: null);

        var repo = new ReconciliationMatchRepository(context, FakeUserContext.CreateDefault());
        await repo.AddAsync(match);
        await context.SaveChangesAsync();

        // Act
        await repo.RemoveAsync(match);
        await context.SaveChangesAsync();

        // Assert
        await using var verifyContext = _fixture.CreateSharedContext(context);
        var verifyRepo = new ReconciliationMatchRepository(verifyContext, FakeUserContext.CreateDefault());
        var retrieved = await verifyRepo.GetByIdAsync(match.Id);
        Assert.Null(retrieved);
    }

    private async Task<(Guid AccountId, Guid TransactionId, Guid RecurringId)> CreateTestDataAsync(BudgetDbContext context)
    {
        // Create an account with a transaction and a recurring transaction
        var account = Account.Create("Test Account", AccountType.Checking);
        var transaction = account.AddTransaction(
            MoneyValue.Create("USD", 15.99m),
            new DateOnly(2026, 1, 15),
            "Netflix");

        var recurring = RecurringTransaction.Create(
            account.Id,
            "Netflix Subscription",
            MoneyValue.Create("USD", 15.99m),
            RecurrencePatternValue.CreateMonthly(1, 15),
            new DateOnly(2026, 1, 1));

        context.Accounts.Add(account);
        context.RecurringTransactions.Add(recurring);
        await context.SaveChangesAsync();

        return (account.Id, transaction.Id, recurring.Id);
    }
}
