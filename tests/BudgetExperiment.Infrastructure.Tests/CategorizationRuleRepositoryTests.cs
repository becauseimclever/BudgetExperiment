// <copyright file="CategorizationRuleRepositoryTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;
using BudgetExperiment.Infrastructure.Persistence;
using BudgetExperiment.Infrastructure.Persistence.Repositories;

namespace BudgetExperiment.Infrastructure.Tests;

/// <summary>
/// Integration tests for <see cref="CategorizationRuleRepository"/>.
/// </summary>
[Collection("InMemoryDb")]
public class CategorizationRuleRepositoryTests
{
    private readonly InMemoryDbFixture _fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="CategorizationRuleRepositoryTests"/> class.
    /// </summary>
    /// <param name="fixture">The shared in-memory database fixture.</param>
    public CategorizationRuleRepositoryTests(InMemoryDbFixture fixture)
    {
        this._fixture = fixture;
    }

    [Fact]
    public async Task AddAsync_And_GetByIdAsync_Persists_Rule()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var category = await this.CreateCategoryAsync(context, "Groceries");
        var repository = new CategorizationRuleRepository(context);
        var rule = CategorizationRule.Create("Walmart", RuleMatchType.Contains, "WALMART", category.Id, priority: 10);

        // Act
        await repository.AddAsync(rule);
        await context.SaveChangesAsync();

        // Assert
        await using var verifyContext = this._fixture.CreateSharedContext(context);
        var verifyRepo = new CategorizationRuleRepository(verifyContext);
        var retrieved = await verifyRepo.GetByIdAsync(rule.Id);

        Assert.NotNull(retrieved);
        Assert.Equal(rule.Id, retrieved.Id);
        Assert.Equal("Walmart", retrieved.Name);
        Assert.Equal(RuleMatchType.Contains, retrieved.MatchType);
        Assert.Equal("WALMART", retrieved.Pattern);
        Assert.Equal(category.Id, retrieved.CategoryId);
        Assert.Equal(10, retrieved.Priority);
        Assert.True(retrieved.IsActive);
        Assert.False(retrieved.CaseSensitive);
    }

    [Fact]
    public async Task GetByIdAsync_Returns_Null_When_Not_Found()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var repository = new CategorizationRuleRepository(context);

        // Act
        var result = await repository.GetByIdAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_Includes_Category()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var category = await this.CreateCategoryAsync(context, "Transportation");
        var repository = new CategorizationRuleRepository(context);
        var rule = CategorizationRule.Create("Uber", RuleMatchType.StartsWith, "UBER", category.Id);

        await repository.AddAsync(rule);
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = this._fixture.CreateSharedContext(context);
        var verifyRepo = new CategorizationRuleRepository(verifyContext);
        var retrieved = await verifyRepo.GetByIdAsync(rule.Id);

        // Assert
        Assert.NotNull(retrieved);
        Assert.NotNull(retrieved.Category);
        Assert.Equal("Transportation", retrieved.Category.Name);
    }

    [Fact]
    public async Task GetActiveByPriorityAsync_Returns_Active_Rules_Ordered_By_Priority()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var category = await this.CreateCategoryAsync(context, "Shopping");
        var repository = new CategorizationRuleRepository(context);

        var rule1 = CategorizationRule.Create("Low Priority", RuleMatchType.Contains, "LOW", category.Id, priority: 100);
        var rule2 = CategorizationRule.Create("High Priority", RuleMatchType.Contains, "HIGH", category.Id, priority: 10);
        var rule3 = CategorizationRule.Create("Medium Priority", RuleMatchType.Contains, "MED", category.Id, priority: 50);
        var inactiveRule = CategorizationRule.Create("Inactive", RuleMatchType.Contains, "INACTIVE", category.Id, priority: 5);
        inactiveRule.Deactivate();

        await repository.AddAsync(rule1);
        await repository.AddAsync(rule2);
        await repository.AddAsync(rule3);
        await repository.AddAsync(inactiveRule);
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = this._fixture.CreateSharedContext(context);
        var verifyRepo = new CategorizationRuleRepository(verifyContext);
        var rules = await verifyRepo.GetActiveByPriorityAsync();

        // Assert
        Assert.Equal(3, rules.Count);
        Assert.Equal("High Priority", rules[0].Name);
        Assert.Equal("Medium Priority", rules[1].Name);
        Assert.Equal("Low Priority", rules[2].Name);
    }

    [Fact]
    public async Task GetByCategoryAsync_Returns_Rules_For_Category()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var category1 = await this.CreateCategoryAsync(context, "Food");
        var category2 = await this.CreateCategoryAsync(context, "Entertainment");
        var repository = new CategorizationRuleRepository(context);

        var rule1 = CategorizationRule.Create("Restaurant 1", RuleMatchType.Contains, "MCDONALD", category1.Id);
        var rule2 = CategorizationRule.Create("Restaurant 2", RuleMatchType.Contains, "SUBWAY", category1.Id);
        var rule3 = CategorizationRule.Create("Netflix", RuleMatchType.Contains, "NETFLIX", category2.Id);

        await repository.AddAsync(rule1);
        await repository.AddAsync(rule2);
        await repository.AddAsync(rule3);
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = this._fixture.CreateSharedContext(context);
        var verifyRepo = new CategorizationRuleRepository(verifyContext);
        var rules = await verifyRepo.GetByCategoryAsync(category1.Id);

        // Assert
        Assert.Equal(2, rules.Count);
        Assert.All(rules, r => Assert.Equal(category1.Id, r.CategoryId));
    }

    [Fact]
    public async Task GetNextPriorityAsync_Returns_MaxPriority_Plus_One()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var category = await this.CreateCategoryAsync(context, "Utilities");
        var repository = new CategorizationRuleRepository(context);

        var rule1 = CategorizationRule.Create("Rule 1", RuleMatchType.Contains, "RULE1", category.Id, priority: 50);
        var rule2 = CategorizationRule.Create("Rule 2", RuleMatchType.Contains, "RULE2", category.Id, priority: 75);

        await repository.AddAsync(rule1);
        await repository.AddAsync(rule2);
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = this._fixture.CreateSharedContext(context);
        var verifyRepo = new CategorizationRuleRepository(verifyContext);
        var nextPriority = await verifyRepo.GetNextPriorityAsync();

        // Assert
        Assert.Equal(76, nextPriority);
    }

    [Fact]
    public async Task GetNextPriorityAsync_Returns_One_When_No_Rules_Exist()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var repository = new CategorizationRuleRepository(context);

        // Act
        var nextPriority = await repository.GetNextPriorityAsync();

        // Assert
        Assert.Equal(1, nextPriority);
    }

    [Fact]
    public async Task ReorderPrioritiesAsync_Updates_Multiple_Rule_Priorities()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var category = await this.CreateCategoryAsync(context, "Bills");
        var repository = new CategorizationRuleRepository(context);

        var rule1 = CategorizationRule.Create("Rule A", RuleMatchType.Contains, "A", category.Id, priority: 1);
        var rule2 = CategorizationRule.Create("Rule B", RuleMatchType.Contains, "B", category.Id, priority: 2);
        var rule3 = CategorizationRule.Create("Rule C", RuleMatchType.Contains, "C", category.Id, priority: 3);

        await repository.AddAsync(rule1);
        await repository.AddAsync(rule2);
        await repository.AddAsync(rule3);
        await context.SaveChangesAsync();

        // Act - reverse the order
        var newPriorities = new[]
        {
            (rule1.Id, 3),
            (rule2.Id, 2),
            (rule3.Id, 1),
        };
        await repository.ReorderPrioritiesAsync(newPriorities);
        await context.SaveChangesAsync();

        // Assert
        await using var verifyContext = this._fixture.CreateSharedContext(context);
        var verifyRepo = new CategorizationRuleRepository(verifyContext);
        var rules = await verifyRepo.GetActiveByPriorityAsync();

        Assert.Equal("Rule C", rules[0].Name);
        Assert.Equal(1, rules[0].Priority);
        Assert.Equal("Rule B", rules[1].Name);
        Assert.Equal(2, rules[1].Priority);
        Assert.Equal("Rule A", rules[2].Name);
        Assert.Equal(3, rules[2].Priority);
    }

    [Fact]
    public async Task ListAsync_Returns_Paginated_Rules()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var category = await this.CreateCategoryAsync(context, "General");
        var repository = new CategorizationRuleRepository(context);

        for (int i = 1; i <= 5; i++)
        {
            var rule = CategorizationRule.Create($"Rule {i}", RuleMatchType.Contains, $"PATTERN{i}", category.Id, priority: i);
            await repository.AddAsync(rule);
        }

        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = this._fixture.CreateSharedContext(context);
        var verifyRepo = new CategorizationRuleRepository(verifyContext);
        var page = await verifyRepo.ListAsync(skip: 1, take: 2);

        // Assert
        Assert.Equal(2, page.Count);
        Assert.Equal("Rule 2", page[0].Name);
        Assert.Equal("Rule 3", page[1].Name);
    }

    [Fact]
    public async Task CountAsync_Returns_Total_Count()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var category = await this.CreateCategoryAsync(context, "Misc");
        var repository = new CategorizationRuleRepository(context);

        for (int i = 1; i <= 3; i++)
        {
            var rule = CategorizationRule.Create($"Count Rule {i}", RuleMatchType.Contains, $"COUNT{i}", category.Id);
            await repository.AddAsync(rule);
        }

        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = this._fixture.CreateSharedContext(context);
        var verifyRepo = new CategorizationRuleRepository(verifyContext);
        var count = await verifyRepo.CountAsync();

        // Assert
        Assert.Equal(3, count);
    }

    [Fact]
    public async Task RemoveAsync_Deletes_Rule()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var category = await this.CreateCategoryAsync(context, "ToDelete");
        var repository = new CategorizationRuleRepository(context);

        var rule = CategorizationRule.Create("Delete Me", RuleMatchType.Contains, "DELETE", category.Id);
        await repository.AddAsync(rule);
        await context.SaveChangesAsync();

        // Act
        await repository.RemoveAsync(rule);
        await context.SaveChangesAsync();

        // Assert
        await using var verifyContext = this._fixture.CreateSharedContext(context);
        var verifyRepo = new CategorizationRuleRepository(verifyContext);
        var retrieved = await verifyRepo.GetByIdAsync(rule.Id);
        Assert.Null(retrieved);
    }

    private async Task<BudgetCategory> CreateCategoryAsync(BudgetDbContext context, string name)
    {
        var category = BudgetCategory.Create(name, CategoryType.Expense);
        context.BudgetCategories.Add(category);
        await context.SaveChangesAsync();
        return category;
    }
}

