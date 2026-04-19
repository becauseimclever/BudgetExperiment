// <copyright file="BudgetGoalRepositoryTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;
using BudgetExperiment.Infrastructure.Persistence.Repositories;

namespace BudgetExperiment.Infrastructure.Tests;

/// <summary>
/// Integration tests for <see cref="BudgetGoalRepository"/>.
/// </summary>
[Collection("PostgreSqlDb")]
public class BudgetGoalRepositoryTests
{
    private readonly PostgreSqlFixture _fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="BudgetGoalRepositoryTests"/> class.
    /// </summary>
    /// <param name="fixture">The shared PostgreSQL database fixture.</param>
    public BudgetGoalRepositoryTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetByCategoryAndMonthAsync_Returns_Matching_Goal()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var category = BudgetCategory.Create("Groceries", CategoryType.Expense);
        context.BudgetCategories.Add(category);
        await context.SaveChangesAsync();

        var goal = BudgetGoal.Create(category.Id, 2026, 3, MoneyValue.Create("USD", 500m));
        var repository = new BudgetGoalRepository(context, FakeUserContext.CreateDefault());
        await repository.AddAsync(goal);
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = _fixture.CreateSharedContext(context);
        var verifyRepo = new BudgetGoalRepository(verifyContext, FakeUserContext.CreateDefault());
        var result = await verifyRepo.GetByCategoryAndMonthAsync(category.Id, 2026, 3);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(category.Id, result.CategoryId);
        Assert.Equal(2026, result.Year);
        Assert.Equal(3, result.Month);
        Assert.Equal(500m, result.TargetAmount.Amount);
    }

    [Fact]
    public async Task GetByCategoryAndMonthAsync_Returns_Null_For_Wrong_Month()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var category = BudgetCategory.Create("Rent", CategoryType.Expense);
        context.BudgetCategories.Add(category);
        await context.SaveChangesAsync();

        var goal = BudgetGoal.Create(category.Id, 2026, 3, MoneyValue.Create("USD", 1200m));
        var repository = new BudgetGoalRepository(context, FakeUserContext.CreateDefault());
        await repository.AddAsync(goal);
        await context.SaveChangesAsync();

        // Act — different month
        var result = await repository.GetByCategoryAndMonthAsync(category.Id, 2026, 4);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByCategoryAndMonthAsync_Returns_Null_For_Wrong_Category()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var category = BudgetCategory.Create("Utilities", CategoryType.Expense);
        context.BudgetCategories.Add(category);
        await context.SaveChangesAsync();

        var goal = BudgetGoal.Create(category.Id, 2026, 3, MoneyValue.Create("USD", 200m));
        var repository = new BudgetGoalRepository(context, FakeUserContext.CreateDefault());
        await repository.AddAsync(goal);
        await context.SaveChangesAsync();

        // Act — different category
        var result = await repository.GetByCategoryAndMonthAsync(Guid.NewGuid(), 2026, 3);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByMonthAsync_Returns_All_Goals_For_Month_With_Category_Included()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var cat1 = BudgetCategory.Create("Food", CategoryType.Expense);
        var cat2 = BudgetCategory.Create("Transport", CategoryType.Expense);
        context.BudgetCategories.AddRange(cat1, cat2);
        await context.SaveChangesAsync();

        var marchGoal1 = BudgetGoal.Create(cat1.Id, 2026, 3, MoneyValue.Create("USD", 400m));
        var marchGoal2 = BudgetGoal.Create(cat2.Id, 2026, 3, MoneyValue.Create("USD", 150m));
        var aprilGoal = BudgetGoal.Create(cat1.Id, 2026, 4, MoneyValue.Create("USD", 450m));

        var repository = new BudgetGoalRepository(context, FakeUserContext.CreateDefault());
        await repository.AddAsync(marchGoal1);
        await repository.AddAsync(marchGoal2);
        await repository.AddAsync(aprilGoal);
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = _fixture.CreateSharedContext(context);
        var verifyRepo = new BudgetGoalRepository(verifyContext, FakeUserContext.CreateDefault());
        var results = await verifyRepo.GetByMonthAsync(2026, 3);

        // Assert — only March goals, Category navigation loaded
        Assert.Equal(2, results.Count);
        Assert.All(results, g => Assert.Equal(2026, g.Year));
        Assert.All(results, g => Assert.Equal(3, g.Month));
        Assert.All(results, g => Assert.NotNull(g.Category));
    }

    [Fact]
    public async Task GetByCategoryAsync_Returns_Goals_Ordered_By_Year_Month_Descending()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var category = BudgetCategory.Create("Groceries", CategoryType.Expense);
        context.BudgetCategories.Add(category);
        await context.SaveChangesAsync();

        var jan = BudgetGoal.Create(category.Id, 2026, 1, MoneyValue.Create("USD", 300m));
        var mar = BudgetGoal.Create(category.Id, 2026, 3, MoneyValue.Create("USD", 500m));
        var dec = BudgetGoal.Create(category.Id, 2025, 12, MoneyValue.Create("USD", 350m));

        var repository = new BudgetGoalRepository(context, FakeUserContext.CreateDefault());
        await repository.AddAsync(jan);
        await repository.AddAsync(mar);
        await repository.AddAsync(dec);
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = _fixture.CreateSharedContext(context);
        var verifyRepo = new BudgetGoalRepository(verifyContext, FakeUserContext.CreateDefault());
        var results = await verifyRepo.GetByCategoryAsync(category.Id);

        // Assert — ordered Year desc, Month desc: 2026/3, 2026/1, 2025/12
        Assert.Equal(3, results.Count);
        Assert.Equal(3, results[0].Month);
        Assert.Equal(2026, results[0].Year);
        Assert.Equal(1, results[1].Month);
        Assert.Equal(2026, results[1].Year);
        Assert.Equal(12, results[2].Month);
        Assert.Equal(2025, results[2].Year);
    }
}
