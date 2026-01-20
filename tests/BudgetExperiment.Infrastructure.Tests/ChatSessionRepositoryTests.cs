// <copyright file="ChatSessionRepositoryTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;
using BudgetExperiment.Infrastructure.Repositories;

namespace BudgetExperiment.Infrastructure.Tests;

/// <summary>
/// Integration tests for <see cref="ChatSessionRepository"/>.
/// </summary>
[Collection("InMemoryDb")]
public class ChatSessionRepositoryTests
{
    private readonly InMemoryDbFixture _fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatSessionRepositoryTests"/> class.
    /// </summary>
    /// <param name="fixture">The shared in-memory database fixture.</param>
    public ChatSessionRepositoryTests(InMemoryDbFixture fixture)
    {
        this._fixture = fixture;
    }

    [Fact]
    public async Task AddAsync_And_GetByIdAsync_Persists_Session()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var repository = new ChatSessionRepository(context);
        var session = ChatSession.Create();

        // Act
        await repository.AddAsync(session);
        await context.SaveChangesAsync();

        // Assert - use shared context to verify persistence
        await using var verifyContext = this._fixture.CreateSharedContext(context);
        var verifyRepo = new ChatSessionRepository(verifyContext);
        var retrieved = await verifyRepo.GetByIdAsync(session.Id);

        Assert.NotNull(retrieved);
        Assert.Equal(session.Id, retrieved.Id);
        Assert.True(retrieved.IsActive);
    }

    [Fact]
    public async Task GetByIdAsync_Returns_Null_When_Not_Found()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var repository = new ChatSessionRepository(context);

        // Act
        var result = await repository.GetByIdAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetActiveSessionAsync_Returns_Active_Session()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var repository = new ChatSessionRepository(context);

        var activeSession = ChatSession.Create();
        var closedSession = ChatSession.Create();
        closedSession.Close();

        await repository.AddAsync(activeSession);
        await repository.AddAsync(closedSession);
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = this._fixture.CreateSharedContext(context);
        var verifyRepo = new ChatSessionRepository(verifyContext);
        var result = await verifyRepo.GetActiveSessionAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(activeSession.Id, result.Id);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task GetActiveSessionAsync_Returns_Null_When_No_Active_Session()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var repository = new ChatSessionRepository(context);

        var closedSession = ChatSession.Create();
        closedSession.Close();

        await repository.AddAsync(closedSession);
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = this._fixture.CreateSharedContext(context);
        var verifyRepo = new ChatSessionRepository(verifyContext);
        var result = await verifyRepo.GetActiveSessionAsync();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ListAsync_Returns_Sessions_Ordered_By_LastMessageAtUtc()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var repository = new ChatSessionRepository(context);

        var session1 = ChatSession.Create();
        await Task.Delay(10); // Ensure different timestamps
        var session2 = ChatSession.Create();

        await repository.AddAsync(session1);
        await repository.AddAsync(session2);
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = this._fixture.CreateSharedContext(context);
        var verifyRepo = new ChatSessionRepository(verifyContext);
        var results = await verifyRepo.ListAsync(0, 10);

        // Assert
        Assert.Equal(2, results.Count);
        Assert.Equal(session2.Id, results[0].Id); // Most recent first
        Assert.Equal(session1.Id, results[1].Id);
    }

    [Fact]
    public async Task CountAsync_Returns_Total_Count()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var repository = new ChatSessionRepository(context);

        await repository.AddAsync(ChatSession.Create());
        await repository.AddAsync(ChatSession.Create());
        await context.SaveChangesAsync();

        // Act
        var count = await repository.CountAsync();

        // Assert
        Assert.Equal(2, count);
    }

    [Fact]
    public async Task RemoveAsync_Deletes_Session()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var repository = new ChatSessionRepository(context);
        var session = ChatSession.Create();

        await repository.AddAsync(session);
        await context.SaveChangesAsync();

        // Act
        await repository.RemoveAsync(session);
        await context.SaveChangesAsync();

        // Assert
        await using var verifyContext = this._fixture.CreateSharedContext(context);
        var verifyRepo = new ChatSessionRepository(verifyContext);
        var result = await verifyRepo.GetByIdAsync(session.Id);
        Assert.Null(result);
    }
}
