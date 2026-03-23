// <copyright file="RecurringChargeSuggestionRepositoryTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Infrastructure.Persistence.Repositories;

namespace BudgetExperiment.Infrastructure.Tests;

/// <summary>
/// Integration tests for <see cref="RecurringChargeSuggestionRepository"/>.
/// </summary>
[Collection("InfraDb")]
public class RecurringChargeSuggestionRepositoryTests : IClassFixture<PostgreSqlFixture>
{
    private readonly PostgreSqlFixture _fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecurringChargeSuggestionRepositoryTests"/> class.
    /// </summary>
    /// <param name="fixture">The shared PostgreSQL database fixture.</param>
    public RecurringChargeSuggestionRepositoryTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task AddAsync_And_GetByIdAsync_Persists_Suggestion()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var repository = new RecurringChargeSuggestionRepository(context);
        var accountId = Guid.NewGuid();
        var userId = FakeUserContext.DefaultUserId;
        var suggestion = RecurringChargeSuggestion.Create(accountId, CreateTestPattern("netflix", 15.99m), BudgetScope.Shared, userId);

        // Act
        await repository.AddAsync(suggestion);
        await context.SaveChangesAsync();

        // Assert
        await using var verifyContext = _fixture.CreateSharedContext(context);
        var verifyRepo = new RecurringChargeSuggestionRepository(verifyContext);
        var retrieved = await verifyRepo.GetByIdAsync(suggestion.Id);

        Assert.NotNull(retrieved);
        Assert.Equal(suggestion.Id, retrieved.Id);
        Assert.Equal(accountId, retrieved.AccountId);
        Assert.Equal("netflix", retrieved.NormalizedDescription);
        Assert.Equal("NETFLIX.COM", retrieved.SampleDescription);
        Assert.Equal(15.99m, retrieved.AverageAmount.Amount);
        Assert.Equal(SuggestionStatus.Pending, retrieved.Status);
    }

    [Fact]
    public async Task GetByIdAsync_Returns_Null_When_Not_Found()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var repository = new RecurringChargeSuggestionRepository(context);

        // Act
        var result = await repository.GetByIdAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ListAsync_Orders_By_Confidence_Descending()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var repository = new RecurringChargeSuggestionRepository(context);
        var accountId = Guid.NewGuid();
        var userId = FakeUserContext.DefaultUserId;

        var lowConfidence = RecurringChargeSuggestion.Create(accountId, CreateTestPattern("spotify", 9.99m, confidence: 0.60m), BudgetScope.Shared, userId);
        var highConfidence = RecurringChargeSuggestion.Create(accountId, CreateTestPattern("netflix", 15.99m, confidence: 0.95m), BudgetScope.Shared, userId);
        var midConfidence = RecurringChargeSuggestion.Create(accountId, CreateTestPattern("hulu", 12.99m, confidence: 0.75m), BudgetScope.Shared, userId);

        await repository.AddAsync(lowConfidence);
        await repository.AddAsync(highConfidence);
        await repository.AddAsync(midConfidence);
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = _fixture.CreateSharedContext(context);
        var verifyRepo = new RecurringChargeSuggestionRepository(verifyContext);
        var results = await verifyRepo.ListAsync(0, 10);

        // Assert - ordered by confidence descending
        Assert.Equal(3, results.Count);
        Assert.Equal(0.95m, results[0].Confidence);
        Assert.Equal(0.75m, results[1].Confidence);
        Assert.Equal(0.60m, results[2].Confidence);
    }

    [Fact]
    public async Task CountAsync_Returns_Correct_Count()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var repository = new RecurringChargeSuggestionRepository(context);

        var initial = await repository.CountAsync();
        Assert.Equal(0, initial);

        var userId = FakeUserContext.DefaultUserId;
        await repository.AddAsync(RecurringChargeSuggestion.Create(Guid.NewGuid(), CreateTestPattern("netflix", 15.99m), BudgetScope.Shared, userId));
        await context.SaveChangesAsync();

        // Act
        var count = await repository.CountAsync();

        // Assert
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task RemoveAsync_Deletes_Suggestion()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var repository = new RecurringChargeSuggestionRepository(context);
        var userId = FakeUserContext.DefaultUserId;
        var suggestion = RecurringChargeSuggestion.Create(Guid.NewGuid(), CreateTestPattern("netflix", 15.99m), BudgetScope.Shared, userId);
        await repository.AddAsync(suggestion);
        await context.SaveChangesAsync();

        // Act
        await repository.RemoveAsync(suggestion);
        await context.SaveChangesAsync();

        // Assert
        await using var verifyContext = _fixture.CreateSharedContext(context);
        var verifyRepo = new RecurringChargeSuggestionRepository(verifyContext);
        var retrieved = await verifyRepo.GetByIdAsync(suggestion.Id);
        Assert.Null(retrieved);
    }

    [Fact]
    public async Task AddRangeAsync_Persists_Multiple_Suggestions()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var repository = new RecurringChargeSuggestionRepository(context);
        var accountId = Guid.NewGuid();
        var userId = FakeUserContext.DefaultUserId;

        var suggestions = new[]
        {
            RecurringChargeSuggestion.Create(accountId, CreateTestPattern("netflix", 15.99m), BudgetScope.Shared, userId),
            RecurringChargeSuggestion.Create(accountId, CreateTestPattern("spotify", 9.99m), BudgetScope.Shared, userId),
            RecurringChargeSuggestion.Create(accountId, CreateTestPattern("hulu", 12.99m), BudgetScope.Shared, userId),
        };

        // Act
        await repository.AddRangeAsync(suggestions);
        await context.SaveChangesAsync();

        // Assert
        await using var verifyContext = _fixture.CreateSharedContext(context);
        var verifyRepo = new RecurringChargeSuggestionRepository(verifyContext);
        var count = await verifyRepo.CountAsync();
        Assert.Equal(3, count);
    }

    [Fact]
    public async Task GetByStatusAsync_Filters_By_Status()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var repository = new RecurringChargeSuggestionRepository(context);
        var accountId = Guid.NewGuid();
        var userId = FakeUserContext.DefaultUserId;

        var pending = RecurringChargeSuggestion.Create(accountId, CreateTestPattern("netflix", 15.99m), BudgetScope.Shared, userId);
        var toAccept = RecurringChargeSuggestion.Create(accountId, CreateTestPattern("spotify", 9.99m), BudgetScope.Shared, userId);
        toAccept.Accept(Guid.NewGuid());

        await repository.AddAsync(pending);
        await repository.AddAsync(toAccept);
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = _fixture.CreateSharedContext(context);
        var verifyRepo = new RecurringChargeSuggestionRepository(verifyContext);
        var pendingResults = await verifyRepo.GetByStatusAsync(accountId: null, status: SuggestionStatus.Pending, skip: 0, take: 10);
        var acceptedResults = await verifyRepo.GetByStatusAsync(accountId: null, status: SuggestionStatus.Accepted, skip: 0, take: 10);

        // Assert
        Assert.Single(pendingResults);
        Assert.Equal(SuggestionStatus.Pending, pendingResults[0].Status);
        Assert.Single(acceptedResults);
        Assert.Equal(SuggestionStatus.Accepted, acceptedResults[0].Status);
    }

    [Fact]
    public async Task GetByStatusAsync_Filters_By_AccountId()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var repository = new RecurringChargeSuggestionRepository(context);
        var accountA = Guid.NewGuid();
        var accountB = Guid.NewGuid();
        var userId = FakeUserContext.DefaultUserId;

        await repository.AddAsync(RecurringChargeSuggestion.Create(accountA, CreateTestPattern("netflix", 15.99m), BudgetScope.Shared, userId));
        await repository.AddAsync(RecurringChargeSuggestion.Create(accountB, CreateTestPattern("spotify", 9.99m), BudgetScope.Shared, userId));
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = _fixture.CreateSharedContext(context);
        var verifyRepo = new RecurringChargeSuggestionRepository(verifyContext);
        var results = await verifyRepo.GetByStatusAsync(accountId: accountA, status: null, skip: 0, take: 10);

        // Assert
        Assert.Single(results);
        Assert.Equal(accountA, results[0].AccountId);
    }

    [Fact]
    public async Task CountByStatusAsync_Returns_Correct_Filtered_Count()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var repository = new RecurringChargeSuggestionRepository(context);
        var accountId = Guid.NewGuid();
        var userId = FakeUserContext.DefaultUserId;

        var pending1 = RecurringChargeSuggestion.Create(accountId, CreateTestPattern("netflix", 15.99m), BudgetScope.Shared, userId);
        var pending2 = RecurringChargeSuggestion.Create(accountId, CreateTestPattern("spotify", 9.99m), BudgetScope.Shared, userId);
        var dismissed = RecurringChargeSuggestion.Create(accountId, CreateTestPattern("hulu", 12.99m), BudgetScope.Shared, userId);
        dismissed.Dismiss();

        await repository.AddRangeAsync(new[] { pending1, pending2, dismissed });
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = _fixture.CreateSharedContext(context);
        var verifyRepo = new RecurringChargeSuggestionRepository(verifyContext);
        var pendingCount = await verifyRepo.CountByStatusAsync(accountId: accountId, status: SuggestionStatus.Pending);
        var dismissedCount = await verifyRepo.CountByStatusAsync(accountId: accountId, status: SuggestionStatus.Dismissed);

        // Assert
        Assert.Equal(2, pendingCount);
        Assert.Equal(1, dismissedCount);
    }

    [Fact]
    public async Task GetByNormalizedDescriptionAndAccountAsync_Returns_Matching_Pending_Suggestion()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var repository = new RecurringChargeSuggestionRepository(context);
        var accountId = Guid.NewGuid();
        var userId = FakeUserContext.DefaultUserId;
        var suggestion = RecurringChargeSuggestion.Create(accountId, CreateTestPattern("netflix", 15.99m), BudgetScope.Shared, userId);

        await repository.AddAsync(suggestion);
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = _fixture.CreateSharedContext(context);
        var verifyRepo = new RecurringChargeSuggestionRepository(verifyContext);
        var result = await verifyRepo.GetByNormalizedDescriptionAndAccountAsync("netflix", accountId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(suggestion.Id, result.Id);
    }

    [Fact]
    public async Task GetByNormalizedDescriptionAndAccountAsync_Returns_Null_For_Accepted_Suggestion()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var repository = new RecurringChargeSuggestionRepository(context);
        var accountId = Guid.NewGuid();
        var userId = FakeUserContext.DefaultUserId;
        var suggestion = RecurringChargeSuggestion.Create(accountId, CreateTestPattern("netflix", 15.99m), BudgetScope.Shared, userId);
        suggestion.Accept(Guid.NewGuid());

        await repository.AddAsync(suggestion);
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = _fixture.CreateSharedContext(context);
        var verifyRepo = new RecurringChargeSuggestionRepository(verifyContext);
        var result = await verifyRepo.GetByNormalizedDescriptionAndAccountAsync("netflix", accountId);

        // Assert - accepted suggestions should not be returned
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByNormalizedDescriptionAndAccountAsync_Returns_Null_When_No_Match()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var repository = new RecurringChargeSuggestionRepository(context);

        // Act
        var result = await repository.GetByNormalizedDescriptionAndAccountAsync("no-match", Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    private static DetectedPattern CreateTestPattern(
        string normalizedDescription,
        decimal amount,
        decimal confidence = 0.95m) =>
        new DetectedPattern(
            NormalizedDescription: normalizedDescription,
            SampleDescription: normalizedDescription.ToUpperInvariant() + ".COM",
            AverageAmount: MoneyValue.Create("USD", amount),
            Frequency: RecurrenceFrequency.Monthly,
            Interval: 1,
            Confidence: confidence,
            MatchingTransactionIds: new List<Guid> { Guid.NewGuid(), Guid.NewGuid() },
            FirstOccurrence: new DateOnly(2025, 1, 1),
            LastOccurrence: new DateOnly(2025, 6, 1),
            MostUsedCategoryId: null);
}
