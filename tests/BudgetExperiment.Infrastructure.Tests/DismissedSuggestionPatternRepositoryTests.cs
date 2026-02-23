// <copyright file="DismissedSuggestionPatternRepositoryTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;
using BudgetExperiment.Infrastructure.Persistence.Repositories;

namespace BudgetExperiment.Infrastructure.Tests;

/// <summary>
/// Integration tests for <see cref="DismissedSuggestionPatternRepository"/>.
/// </summary>
[Collection("InMemoryDb")]
public class DismissedSuggestionPatternRepositoryTests
{
    private readonly InMemoryDbFixture _fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="DismissedSuggestionPatternRepositoryTests"/> class.
    /// </summary>
    /// <param name="fixture">The shared in-memory database fixture.</param>
    public DismissedSuggestionPatternRepositoryTests(InMemoryDbFixture fixture)
    {
        this._fixture = fixture;
    }

    [Fact]
    public async Task ClearByOwnerAsync_DeletesAllPatternsForOwner()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var repository = new DismissedSuggestionPatternRepository(context);

        var pattern1 = DismissedSuggestionPattern.Create("NETFLIX", "user-1");
        var pattern2 = DismissedSuggestionPattern.Create("SPOTIFY", "user-1");
        await repository.AddAsync(pattern1);
        await repository.AddAsync(pattern2);
        await context.SaveChangesAsync();

        // Act
        var count = await repository.ClearByOwnerAsync("user-1");
        await context.SaveChangesAsync();

        // Assert
        Assert.Equal(2, count);
        await using var verifyContext = this._fixture.CreateSharedContext(context);
        var verifyRepo = new DismissedSuggestionPatternRepository(verifyContext);
        var remaining = await verifyRepo.GetByOwnerAsync("user-1");
        Assert.Empty(remaining);
    }

    [Fact]
    public async Task ClearByOwnerAsync_DoesNotDeleteOtherOwnerPatterns()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var repository = new DismissedSuggestionPatternRepository(context);

        var userPattern = DismissedSuggestionPattern.Create("NETFLIX", "user-1");
        var otherPattern = DismissedSuggestionPattern.Create("HULU", "user-2");
        await repository.AddAsync(userPattern);
        await repository.AddAsync(otherPattern);
        await context.SaveChangesAsync();

        // Act
        var count = await repository.ClearByOwnerAsync("user-1");
        await context.SaveChangesAsync();

        // Assert
        Assert.Equal(1, count);
        await using var verifyContext = this._fixture.CreateSharedContext(context);
        var verifyRepo = new DismissedSuggestionPatternRepository(verifyContext);
        var otherRemaining = await verifyRepo.GetByOwnerAsync("user-2");
        Assert.Single(otherRemaining);
    }

    [Fact]
    public async Task ClearByOwnerAsync_ReturnsZero_WhenNoneExist()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var repository = new DismissedSuggestionPatternRepository(context);

        // Act
        var count = await repository.ClearByOwnerAsync("nonexistent-user");

        // Assert
        Assert.Equal(0, count);
    }
}
