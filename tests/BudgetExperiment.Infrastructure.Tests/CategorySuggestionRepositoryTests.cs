// <copyright file="CategorySuggestionRepositoryTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;
using BudgetExperiment.Infrastructure.Persistence.Repositories;

namespace BudgetExperiment.Infrastructure.Tests;

/// <summary>
/// Integration tests for <see cref="CategorySuggestionRepository"/>.
/// </summary>
[Collection("InMemoryDb")]
public class CategorySuggestionRepositoryTests
{
    private readonly InMemoryDbFixture _fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="CategorySuggestionRepositoryTests"/> class.
    /// </summary>
    /// <param name="fixture">The shared in-memory database fixture.</param>
    public CategorySuggestionRepositoryTests(InMemoryDbFixture fixture)
    {
        this._fixture = fixture;
    }

    [Fact]
    public async Task GetPendingByOwnerAsync_Returns_Only_Pending_For_Owner()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var repository = new CategorySuggestionRepository(context);

        var pending = CategorySuggestion.Create("Dining", CategoryType.Expense, ["RESTAURANT"], 5, 0.85m, "owner1");
        var accepted = CategorySuggestion.Create("Transport", CategoryType.Expense, ["UBER"], 3, 0.9m, "owner1");
        accepted.Accept();
        var otherOwner = CategorySuggestion.Create("Gas", CategoryType.Expense, ["SHELL"], 2, 0.75m, "owner2");

        await repository.AddAsync(pending);
        await repository.AddAsync(accepted);
        await repository.AddAsync(otherOwner);
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = this._fixture.CreateSharedContext(context);
        var verifyRepo = new CategorySuggestionRepository(verifyContext);
        var results = await verifyRepo.GetPendingByOwnerAsync("owner1");

        // Assert — only pending for owner1
        Assert.Single(results);
        Assert.Equal("Dining", results[0].SuggestedName);
    }

    [Fact]
    public async Task GetByStatusAsync_Filters_By_Owner_And_Status_With_Pagination()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var repository = new CategorySuggestionRepository(context);

        for (int i = 0; i < 5; i++)
        {
            var suggestion = CategorySuggestion.Create($"Cat{i}", CategoryType.Expense, [$"PATTERN{i}"], i + 1, 0.8m, "owner1");
            await repository.AddAsync(suggestion);
        }

        await context.SaveChangesAsync();

        // Act — page 1 of 2
        await using var verifyContext = this._fixture.CreateSharedContext(context);
        var verifyRepo = new CategorySuggestionRepository(verifyContext);
        var page1 = await verifyRepo.GetByStatusAsync("owner1", SuggestionStatus.Pending, 0, 2);
        var page2 = await verifyRepo.GetByStatusAsync("owner1", SuggestionStatus.Pending, 2, 2);

        // Assert
        Assert.Equal(2, page1.Count);
        Assert.Equal(2, page2.Count);
    }

    [Fact]
    public async Task ExistsPendingWithNameAsync_Is_Case_Insensitive()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var repository = new CategorySuggestionRepository(context);

        var suggestion = CategorySuggestion.Create("Groceries", CategoryType.Expense, ["WALMART"], 10, 0.95m, "owner1");
        await repository.AddAsync(suggestion);
        await context.SaveChangesAsync();

        // Act — search with different casing and whitespace
        var exact = await repository.ExistsPendingWithNameAsync("owner1", "Groceries");
        var upper = await repository.ExistsPendingWithNameAsync("owner1", "GROCERIES");
        var lower = await repository.ExistsPendingWithNameAsync("owner1", "groceries");
        var padded = await repository.ExistsPendingWithNameAsync("owner1", "  Groceries  ");
        var wrongOwner = await repository.ExistsPendingWithNameAsync("owner2", "Groceries");
        var noMatch = await repository.ExistsPendingWithNameAsync("owner1", "NonExistent");

        // Assert — all case/whitespace variants match, wrong owner and no-match don't
        Assert.True(exact);
        Assert.True(upper);
        Assert.True(lower);
        Assert.True(padded);
        Assert.False(wrongOwner);
        Assert.False(noMatch);
    }

    [Fact]
    public async Task ExistsPendingWithNameAsync_Returns_False_When_Accepted()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var repository = new CategorySuggestionRepository(context);

        var suggestion = CategorySuggestion.Create("AcceptedCat", CategoryType.Expense, ["PAT"], 5, 0.9m, "owner1");
        suggestion.Accept();
        await repository.AddAsync(suggestion);
        await context.SaveChangesAsync();

        // Act — pending check should miss accepted suggestions
        var result = await repository.ExistsPendingWithNameAsync("owner1", "AcceptedCat");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task AddRangeAsync_Persists_Multiple_Suggestions()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var repository = new CategorySuggestionRepository(context);

        var suggestions = new[]
        {
            CategorySuggestion.Create("Batch1", CategoryType.Expense, ["A"], 1, 0.5m, "owner1"),
            CategorySuggestion.Create("Batch2", CategoryType.Income, ["B"], 2, 0.6m, "owner1"),
            CategorySuggestion.Create("Batch3", CategoryType.Expense, ["C"], 3, 0.7m, "owner1"),
        };

        // Act
        await repository.AddRangeAsync(suggestions);
        await context.SaveChangesAsync();

        // Assert
        await using var verifyContext = this._fixture.CreateSharedContext(context);
        var verifyRepo = new CategorySuggestionRepository(verifyContext);
        var count = await verifyRepo.CountAsync();
        Assert.Equal(3, count);
    }

    [Fact]
    public async Task DeletePendingByOwnerAsync_Removes_Only_Pending_For_Owner()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var repository = new CategorySuggestionRepository(context);

        var pending1 = CategorySuggestion.Create("Pending1", CategoryType.Expense, ["X"], 1, 0.5m, "owner1");
        var pending2 = CategorySuggestion.Create("Pending2", CategoryType.Expense, ["Y"], 2, 0.6m, "owner1");
        var accepted = CategorySuggestion.Create("Accepted", CategoryType.Expense, ["Z"], 3, 0.7m, "owner1");
        accepted.Accept();
        var otherOwner = CategorySuggestion.Create("Other", CategoryType.Expense, ["W"], 4, 0.8m, "owner2");

        await repository.AddAsync(pending1);
        await repository.AddAsync(pending2);
        await repository.AddAsync(accepted);
        await repository.AddAsync(otherOwner);
        await context.SaveChangesAsync();

        // Act
        await repository.DeletePendingByOwnerAsync("owner1");
        await context.SaveChangesAsync();

        // Assert — owner1's pending removed, accepted and other owner untouched
        var remaining = await repository.ListAsync(0, 100);
        Assert.Equal(2, remaining.Count);
        Assert.Contains(remaining, s => s.SuggestedName == "Accepted");
        Assert.Contains(remaining, s => s.SuggestedName == "Other");
    }
}
