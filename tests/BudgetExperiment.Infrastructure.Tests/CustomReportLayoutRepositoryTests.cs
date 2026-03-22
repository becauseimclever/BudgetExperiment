// <copyright file="CustomReportLayoutRepositoryTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain.Reports;
using BudgetExperiment.Infrastructure.Persistence.Repositories;

namespace BudgetExperiment.Infrastructure.Tests;

/// <summary>
/// Integration tests for <see cref="CustomReportLayoutRepository"/>.
/// </summary>
[Collection("InfraDb")]
public class CustomReportLayoutRepositoryTests : IClassFixture<PostgreSqlFixture>
{
    private readonly PostgreSqlFixture _fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomReportLayoutRepositoryTests"/> class.
    /// </summary>
    /// <param name="fixture">The shared PostgreSQL database fixture.</param>
    public CustomReportLayoutRepositoryTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task AddAsync_And_GetByIdAsync_Persists_Shared_Layout()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var userId = FakeUserContext.DefaultUserId;
        var repository = new CustomReportLayoutRepository(context, FakeUserContext.CreateDefault());
        var layout = CustomReportLayout.CreateShared("Monthly Spending", "{\"type\": \"bar\"}", userId);

        // Act
        await repository.AddAsync(layout);
        await context.SaveChangesAsync();

        // Assert
        await using var verifyContext = _fixture.CreateSharedContext(context);
        var verifyRepo = new CustomReportLayoutRepository(verifyContext, FakeUserContext.CreateDefault());
        var retrieved = await verifyRepo.GetByIdAsync(layout.Id);

        Assert.NotNull(retrieved);
        Assert.Equal(layout.Id, retrieved.Id);
        Assert.Equal("Monthly Spending", retrieved.Name);
        Assert.Equal("{\"type\": \"bar\"}", retrieved.LayoutJson);
        Assert.Equal(BudgetScope.Shared, retrieved.Scope);
        Assert.Null(retrieved.OwnerUserId);
    }

    [Fact]
    public async Task AddAsync_And_GetByIdAsync_Persists_Personal_Layout()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var userId = FakeUserContext.DefaultUserId;
        var userContext = FakeUserContext.CreateForPersonalScope(userId);
        var repository = new CustomReportLayoutRepository(context, userContext);
        var layout = CustomReportLayout.CreatePersonal("My Private Report", "{\"type\":\"pie\"}", userId);

        // Act
        await repository.AddAsync(layout);
        await context.SaveChangesAsync();

        // Assert
        await using var verifyContext = _fixture.CreateSharedContext(context);
        var verifyRepo = new CustomReportLayoutRepository(verifyContext, userContext);
        var retrieved = await verifyRepo.GetByIdAsync(layout.Id);

        Assert.NotNull(retrieved);
        Assert.Equal(BudgetScope.Personal, retrieved.Scope);
        Assert.Equal(userId, retrieved.OwnerUserId);
    }

    [Fact]
    public async Task GetByIdAsync_Returns_Null_When_Not_Found()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var repository = new CustomReportLayoutRepository(context, FakeUserContext.CreateDefault());

        // Act
        var result = await repository.GetByIdAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_Returns_Layouts_OrderedBy_UpdatedAtUtc_Descending()
    {
        // Arrange - add two shared layouts; rely on natural creation-time ordering
        await using var context = _fixture.CreateContext();
        var userId = FakeUserContext.DefaultUserId;
        var repository = new CustomReportLayoutRepository(context, FakeUserContext.CreateDefault());

        var first = CustomReportLayout.CreateShared("Alpha Layout", "{}", userId);
        await repository.AddAsync(first);
        await context.SaveChangesAsync();

        // Small delay to ensure distinct UpdatedAtUtc values
        await Task.Delay(10);

        var second = CustomReportLayout.CreateShared("Beta Layout", "{}", userId);
        await repository.AddAsync(second);
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = _fixture.CreateSharedContext(context);
        var verifyRepo = new CustomReportLayoutRepository(verifyContext, FakeUserContext.CreateDefault());
        var layouts = await verifyRepo.GetAllAsync();

        // Assert - most recently updated first
        Assert.Equal(2, layouts.Count);
        Assert.Equal("Beta Layout", layouts[0].Name);
        Assert.Equal("Alpha Layout", layouts[1].Name);
    }

    [Fact]
    public async Task ListAsync_Supports_Pagination()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var userId = FakeUserContext.DefaultUserId;
        var repository = new CustomReportLayoutRepository(context, FakeUserContext.CreateDefault());

        for (int i = 0; i < 5; i++)
        {
            await Task.Delay(5);
            await repository.AddAsync(CustomReportLayout.CreateShared($"Layout {i:D2}", "{}", userId));
        }

        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = _fixture.CreateSharedContext(context);
        var verifyRepo = new CustomReportLayoutRepository(verifyContext, FakeUserContext.CreateDefault());
        var page1 = await verifyRepo.ListAsync(0, 2);
        var page2 = await verifyRepo.ListAsync(2, 2);

        // Assert
        Assert.Equal(2, page1.Count);
        Assert.Equal(2, page2.Count);
        Assert.NotEqual(page1[0].Id, page2[0].Id);
    }

    [Fact]
    public async Task CountAsync_Returns_Correct_Count()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var userId = FakeUserContext.DefaultUserId;
        var repository = new CustomReportLayoutRepository(context, FakeUserContext.CreateDefault());

        var initialCount = await repository.CountAsync();
        Assert.Equal(0, initialCount);

        await repository.AddAsync(CustomReportLayout.CreateShared("Counted Layout", "{}", userId));
        await context.SaveChangesAsync();

        // Act
        var newCount = await repository.CountAsync();

        // Assert
        Assert.Equal(1, newCount);
    }

    [Fact]
    public async Task RemoveAsync_Deletes_Layout()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var userId = FakeUserContext.DefaultUserId;
        var repository = new CustomReportLayoutRepository(context, FakeUserContext.CreateDefault());
        var layout = CustomReportLayout.CreateShared("To Delete", "{}", userId);
        await repository.AddAsync(layout);
        await context.SaveChangesAsync();

        // Act
        await repository.RemoveAsync(layout);
        await context.SaveChangesAsync();

        // Assert
        await using var verifyContext = _fixture.CreateSharedContext(context);
        var verifyRepo = new CustomReportLayoutRepository(verifyContext, FakeUserContext.CreateDefault());
        var retrieved = await verifyRepo.GetByIdAsync(layout.Id);
        Assert.Null(retrieved);
    }

    [Fact]
    public async Task UpdateName_Persists_Via_Change_Tracking()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var userId = FakeUserContext.DefaultUserId;
        var repository = new CustomReportLayoutRepository(context, FakeUserContext.CreateDefault());
        var layout = CustomReportLayout.CreateShared("Original Name", "{}", userId);
        await repository.AddAsync(layout);
        await context.SaveChangesAsync();

        // Act
        layout.UpdateName("Updated Name");
        await context.SaveChangesAsync();

        // Assert
        await using var verifyContext = _fixture.CreateSharedContext(context);
        var verifyRepo = new CustomReportLayoutRepository(verifyContext, FakeUserContext.CreateDefault());
        var retrieved = await verifyRepo.GetByIdAsync(layout.Id);

        Assert.NotNull(retrieved);
        Assert.Equal("Updated Name", retrieved.Name);
    }

    [Fact]
    public async Task GetAllAsync_WithPersonalScope_Returns_Only_Current_User_Personal_Layouts()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var userId = FakeUserContext.DefaultUserId;
        var otherUserId = Guid.NewGuid();

        var sharedLayout = CustomReportLayout.CreateShared("Shared", "{}", userId);
        var myPersonal = CustomReportLayout.CreatePersonal("My Personal", "{}", userId);
        var otherPersonal = CustomReportLayout.CreatePersonal("Other Personal", "{}", otherUserId);

        context.CustomReportLayouts.AddRange(sharedLayout, myPersonal, otherPersonal);
        await context.SaveChangesAsync();

        // Act - personal scope should return only current user's personal layouts
        await using var verifyContext = _fixture.CreateSharedContext(context);
        var personalContext = FakeUserContext.CreateForPersonalScope(userId);
        var repository = new CustomReportLayoutRepository(verifyContext, personalContext);
        var layouts = await repository.GetAllAsync();

        // Assert
        Assert.Single(layouts);
        Assert.Equal("My Personal", layouts[0].Name);
        Assert.Equal(BudgetScope.Personal, layouts[0].Scope);
    }

    [Fact]
    public async Task GetAllAsync_WithSharedScope_Returns_Only_Shared_Layouts()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var userId = FakeUserContext.DefaultUserId;

        var sharedLayout = CustomReportLayout.CreateShared("Shared Report", "{}", userId);
        var personalLayout = CustomReportLayout.CreatePersonal("Personal Report", "{}", userId);

        context.CustomReportLayouts.AddRange(sharedLayout, personalLayout);
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = _fixture.CreateSharedContext(context);
        var repository = new CustomReportLayoutRepository(verifyContext, FakeUserContext.CreateForSharedScope());
        var layouts = await repository.GetAllAsync();

        // Assert - only shared layouts returned
        Assert.Single(layouts);
        Assert.Equal("Shared Report", layouts[0].Name);
    }

    [Fact]
    public async Task GetAllAsync_WithNullScope_Returns_Shared_And_Current_User_Personal_Layouts()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var userId = FakeUserContext.DefaultUserId;
        var otherUserId = Guid.NewGuid();

        var sharedLayout = CustomReportLayout.CreateShared("Shared", "{}", userId);
        var myPersonal = CustomReportLayout.CreatePersonal("Mine", "{}", userId);
        var otherPersonal = CustomReportLayout.CreatePersonal("Other's", "{}", otherUserId);

        context.CustomReportLayouts.AddRange(sharedLayout, myPersonal, otherPersonal);
        await context.SaveChangesAsync();

        // Act - null scope = all visible (shared + own personal)
        await using var verifyContext = _fixture.CreateSharedContext(context);
        var allScopeContext = new FakeUserContext(userId: userId, currentScope: null);
        var repository = new CustomReportLayoutRepository(verifyContext, allScopeContext);
        var layouts = await repository.GetAllAsync();

        // Assert
        Assert.Equal(2, layouts.Count);
        Assert.Contains(layouts, l => l.Name == "Shared");
        Assert.Contains(layouts, l => l.Name == "Mine");
        Assert.DoesNotContain(layouts, l => l.Name == "Other's");
    }
}
