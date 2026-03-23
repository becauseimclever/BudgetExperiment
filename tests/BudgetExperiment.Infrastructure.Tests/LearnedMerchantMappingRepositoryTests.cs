// <copyright file="LearnedMerchantMappingRepositoryTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;
using BudgetExperiment.Infrastructure.Persistence.Repositories;

namespace BudgetExperiment.Infrastructure.Tests;

/// <summary>
/// Integration tests for <see cref="LearnedMerchantMappingRepository"/>.
/// </summary>
[Collection("PostgreSqlDb")]
public class LearnedMerchantMappingRepositoryTests
{
    private readonly PostgreSqlFixture _fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="LearnedMerchantMappingRepositoryTests"/> class.
    /// </summary>
    /// <param name="fixture">The shared PostgreSQL database fixture.</param>
    public LearnedMerchantMappingRepositoryTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetByPatternAsync_Finds_By_Normalized_Pattern()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var category = BudgetCategory.Create("Groceries", CategoryType.Expense);
        context.BudgetCategories.Add(category);
        await context.SaveChangesAsync();

        var repository = new LearnedMerchantMappingRepository(context);

        var mapping = LearnedMerchantMapping.Create("WALMART", category.Id, "owner1");
        await repository.AddAsync(mapping);
        await context.SaveChangesAsync();

        // Act — search with different casing and whitespace
        await using var verifyContext = _fixture.CreateSharedContext(context);
        var verifyRepo = new LearnedMerchantMappingRepository(verifyContext);
        var exact = await verifyRepo.GetByPatternAsync("owner1", "WALMART");
        var lower = await verifyRepo.GetByPatternAsync("owner1", "walmart");
        var padded = await verifyRepo.GetByPatternAsync("owner1", "  walmart  ");
        var wrongOwner = await verifyRepo.GetByPatternAsync("owner2", "WALMART");

        // Assert — normalization matches, wrong owner doesn't
        Assert.NotNull(exact);
        Assert.NotNull(lower);
        Assert.NotNull(padded);
        Assert.Null(wrongOwner);
    }

    [Fact]
    public async Task ExistsAsync_Is_Case_Insensitive_And_Trims()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var category = BudgetCategory.Create("Electronics", CategoryType.Expense);
        context.BudgetCategories.Add(category);
        await context.SaveChangesAsync();

        var repository = new LearnedMerchantMappingRepository(context);

        var mapping = LearnedMerchantMapping.Create("TARGET", category.Id, "owner1");
        await repository.AddAsync(mapping);
        await context.SaveChangesAsync();

        // Act
        var upper = await repository.ExistsAsync("owner1", "TARGET");
        var lower = await repository.ExistsAsync("owner1", "target");
        var mixed = await repository.ExistsAsync("owner1", "  Target  ");
        var wrongOwner = await repository.ExistsAsync("owner2", "TARGET");
        var noMatch = await repository.ExistsAsync("owner1", "COSTCO");

        // Assert
        Assert.True(upper);
        Assert.True(lower);
        Assert.True(mixed);
        Assert.False(wrongOwner);
        Assert.False(noMatch);
    }

    [Fact]
    public async Task GetByOwnerAsync_Returns_Ordered_By_LearnCount_Desc_Then_Pattern()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var category = BudgetCategory.Create("Groceries", CategoryType.Expense);
        context.BudgetCategories.Add(category);
        await context.SaveChangesAsync();

        var repository = new LearnedMerchantMappingRepository(context);

        var mappingA = LearnedMerchantMapping.Create("ALDI", category.Id, "owner1");
        var mappingW = LearnedMerchantMapping.Create("WALMART", category.Id, "owner1");
        mappingW.IncrementLearnCount(); // LearnCount = 2
        mappingW.IncrementLearnCount(); // LearnCount = 3
        var mappingT = LearnedMerchantMapping.Create("TARGET", category.Id, "owner1");
        mappingT.IncrementLearnCount(); // LearnCount = 2

        await repository.AddAsync(mappingA);
        await repository.AddAsync(mappingW);
        await repository.AddAsync(mappingT);
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = _fixture.CreateSharedContext(context);
        var verifyRepo = new LearnedMerchantMappingRepository(verifyContext);
        var results = await verifyRepo.GetByOwnerAsync("owner1");

        // Assert — ordered by LearnCount desc (3, 2, 1), then by pattern
        Assert.Equal(3, results.Count);
        Assert.Equal("WALMART", results[0].MerchantPattern);
        Assert.Equal("TARGET", results[1].MerchantPattern);
        Assert.Equal("ALDI", results[2].MerchantPattern);
    }

    [Fact]
    public async Task GetByCategoryAsync_Returns_Mappings_For_Category_Ordered_By_Pattern()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var groceryCat = BudgetCategory.Create("Groceries", CategoryType.Expense);
        var gasCat = BudgetCategory.Create("Gas", CategoryType.Expense);
        context.BudgetCategories.AddRange(groceryCat, gasCat);
        await context.SaveChangesAsync();

        var repository = new LearnedMerchantMappingRepository(context);

        var walmart = LearnedMerchantMapping.Create("WALMART", groceryCat.Id, "owner1");
        var kroger = LearnedMerchantMapping.Create("KROGER", groceryCat.Id, "owner1");
        var shell = LearnedMerchantMapping.Create("SHELL", gasCat.Id, "owner1");

        await repository.AddAsync(walmart);
        await repository.AddAsync(kroger);
        await repository.AddAsync(shell);
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = _fixture.CreateSharedContext(context);
        var verifyRepo = new LearnedMerchantMappingRepository(verifyContext);
        var results = await verifyRepo.GetByCategoryAsync(groceryCat.Id);

        // Assert — only grocery category, ordered alphabetically
        Assert.Equal(2, results.Count);
        Assert.Equal("KROGER", results[0].MerchantPattern);
        Assert.Equal("WALMART", results[1].MerchantPattern);
    }

    [Fact]
    public async Task GetByOwnerAsync_Does_Not_Return_Other_Owners_Mappings()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var category = BudgetCategory.Create("Groceries", CategoryType.Expense);
        context.BudgetCategories.Add(category);
        await context.SaveChangesAsync();

        var repository = new LearnedMerchantMappingRepository(context);

        var owner1Mapping = LearnedMerchantMapping.Create("TRADER JOES", category.Id, "owner1");
        var owner2Mapping = LearnedMerchantMapping.Create("WHOLE FOODS", category.Id, "owner2");

        await repository.AddAsync(owner1Mapping);
        await repository.AddAsync(owner2Mapping);
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = _fixture.CreateSharedContext(context);
        var verifyRepo = new LearnedMerchantMappingRepository(verifyContext);
        var results = await verifyRepo.GetByOwnerAsync("owner1");

        // Assert
        Assert.Single(results);
        Assert.Equal("TRADER JOES", results[0].MerchantPattern);
    }
}
