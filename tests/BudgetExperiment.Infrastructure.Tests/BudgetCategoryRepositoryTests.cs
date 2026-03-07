// <copyright file="BudgetCategoryRepositoryTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;
using BudgetExperiment.Infrastructure.Persistence.Repositories;

namespace BudgetExperiment.Infrastructure.Tests;

/// <summary>
/// Integration tests for <see cref="BudgetCategoryRepository"/>.
/// </summary>
[Collection("InMemoryDb")]
public class BudgetCategoryRepositoryTests
{
    private readonly InMemoryDbFixture _fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="BudgetCategoryRepositoryTests"/> class.
    /// </summary>
    /// <param name="fixture">The shared in-memory database fixture.</param>
    public BudgetCategoryRepositoryTests(InMemoryDbFixture fixture)
    {
        this._fixture = fixture;
    }

    [Fact]
    public async Task GetByNameAsync_Returns_Category_With_Matching_Name()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var repository = new BudgetCategoryRepository(context, FakeUserContext.CreateDefault());

        var category = BudgetCategory.Create("Groceries", CategoryType.Expense);
        await repository.AddAsync(category);
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = this._fixture.CreateSharedContext(context);
        var verifyRepo = new BudgetCategoryRepository(verifyContext, FakeUserContext.CreateDefault());
        var result = await verifyRepo.GetByNameAsync("Groceries");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Groceries", result.Name);
    }

    [Fact]
    public async Task GetByNameAsync_Returns_Null_When_Name_Not_Found()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var repository = new BudgetCategoryRepository(context, FakeUserContext.CreateDefault());

        // Act
        var result = await repository.GetByNameAsync("NonExistent");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetActiveAsync_Returns_Only_Active_Categories_Ordered_By_SortOrder_Then_Name()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var repository = new BudgetCategoryRepository(context, FakeUserContext.CreateDefault());

        var activeB = BudgetCategory.Create("B-Category", CategoryType.Expense);
        var activeA = BudgetCategory.Create("A-Category", CategoryType.Income);
        var inactive = BudgetCategory.Create("Inactive", CategoryType.Expense);
        inactive.Deactivate();

        await repository.AddAsync(activeB);
        await repository.AddAsync(activeA);
        await repository.AddAsync(inactive);
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = this._fixture.CreateSharedContext(context);
        var verifyRepo = new BudgetCategoryRepository(verifyContext, FakeUserContext.CreateDefault());
        var results = await verifyRepo.GetActiveAsync();

        // Assert - inactive excluded, remaining ordered by SortOrder (all 0) then Name
        Assert.Equal(2, results.Count);
        Assert.Equal("A-Category", results[0].Name);
        Assert.Equal("B-Category", results[1].Name);
        Assert.DoesNotContain(results, c => c.Name == "Inactive");
    }

    [Fact]
    public async Task GetByTypeAsync_Returns_Only_Categories_Of_Given_Type()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var repository = new BudgetCategoryRepository(context, FakeUserContext.CreateDefault());

        var expense1 = BudgetCategory.Create("Rent", CategoryType.Expense);
        var expense2 = BudgetCategory.Create("Food", CategoryType.Expense);
        var income = BudgetCategory.Create("Salary", CategoryType.Income);

        await repository.AddAsync(expense1);
        await repository.AddAsync(expense2);
        await repository.AddAsync(income);
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = this._fixture.CreateSharedContext(context);
        var verifyRepo = new BudgetCategoryRepository(verifyContext, FakeUserContext.CreateDefault());
        var expenses = await verifyRepo.GetByTypeAsync(CategoryType.Expense);
        var incomes = await verifyRepo.GetByTypeAsync(CategoryType.Income);

        // Assert
        Assert.Equal(2, expenses.Count);
        Assert.All(expenses, c => Assert.Equal(CategoryType.Expense, c.Type));
        Assert.Single(incomes);
        Assert.Equal("Salary", incomes[0].Name);
    }

    [Fact]
    public async Task GetByIdsAsync_Returns_Only_Categories_With_Matching_Ids()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var repository = new BudgetCategoryRepository(context, FakeUserContext.CreateDefault());

        var cat1 = BudgetCategory.Create("Cat1", CategoryType.Expense);
        var cat2 = BudgetCategory.Create("Cat2", CategoryType.Expense);
        var cat3 = BudgetCategory.Create("Cat3", CategoryType.Income);

        await repository.AddAsync(cat1);
        await repository.AddAsync(cat2);
        await repository.AddAsync(cat3);
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = this._fixture.CreateSharedContext(context);
        var verifyRepo = new BudgetCategoryRepository(verifyContext, FakeUserContext.CreateDefault());
        var results = await verifyRepo.GetByIdsAsync([cat1.Id, cat3.Id]);

        // Assert
        Assert.Equal(2, results.Count);
        Assert.Contains(results, c => c.Id == cat1.Id);
        Assert.Contains(results, c => c.Id == cat3.Id);
        Assert.DoesNotContain(results, c => c.Id == cat2.Id);
    }

    [Fact]
    public async Task GetByIdsAsync_Returns_Empty_For_NonExistent_Ids()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var repository = new BudgetCategoryRepository(context, FakeUserContext.CreateDefault());

        // Act
        var results = await repository.GetByIdsAsync([Guid.NewGuid(), Guid.NewGuid()]);

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public async Task GetAllAsync_Returns_Categories_Ordered_By_SortOrder_Then_Name()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var repository = new BudgetCategoryRepository(context, FakeUserContext.CreateDefault());

        var catC = BudgetCategory.Create("C-Category", CategoryType.Expense);
        var catA = BudgetCategory.Create("A-Category", CategoryType.Income);
        var catB = BudgetCategory.Create("B-Category", CategoryType.Transfer);

        await repository.AddAsync(catC);
        await repository.AddAsync(catA);
        await repository.AddAsync(catB);
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = this._fixture.CreateSharedContext(context);
        var verifyRepo = new BudgetCategoryRepository(verifyContext, FakeUserContext.CreateDefault());
        var results = await verifyRepo.GetAllAsync();

        // Assert - all have SortOrder 0, so ordered by Name
        Assert.Equal(3, results.Count);
        Assert.Equal("A-Category", results[0].Name);
        Assert.Equal("B-Category", results[1].Name);
        Assert.Equal("C-Category", results[2].Name);
    }

    [Fact]
    public async Task ScopeFilter_SharedScope_Returns_Only_Shared_Categories()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var userId = FakeUserContext.DefaultUserId;

        var sharedCategory = BudgetCategory.Create("Shared Category", CategoryType.Expense);
        var personalCategory = BudgetCategory.Create("Personal Category", CategoryType.Income);

        // Set personal scope via EF since domain Create() defaults to Shared
        context.BudgetCategories.Add(sharedCategory);
        context.BudgetCategories.Add(personalCategory);
        await context.SaveChangesAsync();

        context.Entry(personalCategory).Property(e => e.Scope).CurrentValue = BudgetScope.Personal;
        context.Entry(personalCategory).Property(e => e.OwnerUserId).CurrentValue = userId;
        await context.SaveChangesAsync();

        // Act - query with Shared scope only
        await using var verifyContext = this._fixture.CreateSharedContext(context);
        var sharedScopeContext = FakeUserContext.CreateForSharedScope();
        var repository = new BudgetCategoryRepository(verifyContext, sharedScopeContext);
        var results = await repository.GetAllAsync();

        // Assert
        Assert.Single(results);
        Assert.Equal("Shared Category", results[0].Name);
    }

    [Fact]
    public async Task ScopeFilter_PersonalScope_Returns_Only_Users_Personal_Categories()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var userId = FakeUserContext.DefaultUserId;
        var otherUserId = Guid.NewGuid();

        var sharedCategory = BudgetCategory.Create("Shared", CategoryType.Expense);
        var myPersonal = BudgetCategory.Create("My Personal", CategoryType.Income);
        var otherPersonal = BudgetCategory.Create("Other Personal", CategoryType.Expense);

        context.BudgetCategories.AddRange(sharedCategory, myPersonal, otherPersonal);
        await context.SaveChangesAsync();

        context.Entry(myPersonal).Property(e => e.Scope).CurrentValue = BudgetScope.Personal;
        context.Entry(myPersonal).Property(e => e.OwnerUserId).CurrentValue = userId;
        context.Entry(otherPersonal).Property(e => e.Scope).CurrentValue = BudgetScope.Personal;
        context.Entry(otherPersonal).Property(e => e.OwnerUserId).CurrentValue = otherUserId;
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = this._fixture.CreateSharedContext(context);
        var personalContext = FakeUserContext.CreateForPersonalScope(userId);
        var repository = new BudgetCategoryRepository(verifyContext, personalContext);
        var results = await repository.GetAllAsync();

        // Assert
        Assert.Single(results);
        Assert.Equal("My Personal", results[0].Name);
    }
}
