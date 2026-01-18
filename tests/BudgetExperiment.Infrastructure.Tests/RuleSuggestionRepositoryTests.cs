// <copyright file="RuleSuggestionRepositoryTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;
using BudgetExperiment.Infrastructure.Repositories;

namespace BudgetExperiment.Infrastructure.Tests;

/// <summary>
/// Integration tests for <see cref="RuleSuggestionRepository"/>.
/// </summary>
[Collection("InMemoryDb")]
public class RuleSuggestionRepositoryTests
{
    private readonly InMemoryDbFixture _fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="RuleSuggestionRepositoryTests"/> class.
    /// </summary>
    /// <param name="fixture">The shared in-memory database fixture.</param>
    public RuleSuggestionRepositoryTests(InMemoryDbFixture fixture)
    {
        this._fixture = fixture;
    }

    [Fact]
    public async Task AddAsync_And_GetByIdAsync_Persists_Suggestion()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var repository = new RuleSuggestionRepository(context);
        var categoryId = Guid.NewGuid();
        var suggestion = RuleSuggestion.CreateNewRuleSuggestion(
            title: "Walmart Rule",
            description: "Create a rule for Walmart transactions",
            reasoning: "Found 50 uncategorized transactions matching WALMART",
            confidence: 0.95m,
            suggestedPattern: "WALMART.*",
            suggestedMatchType: RuleMatchType.Regex,
            suggestedCategoryId: categoryId,
            affectedTransactionCount: 50,
            sampleDescriptions: new List<string> { "WALMART STORE #123", "WALMART SUPERCENTER" });

        // Act
        await repository.AddAsync(suggestion);
        await context.SaveChangesAsync();

        // Assert
        await using var verifyContext = this._fixture.CreateSharedContext(context);
        var verifyRepo = new RuleSuggestionRepository(verifyContext);
        var retrieved = await verifyRepo.GetByIdAsync(suggestion.Id);

        Assert.NotNull(retrieved);
        Assert.Equal(suggestion.Id, retrieved.Id);
        Assert.Equal(SuggestionType.NewRule, retrieved.Type);
        Assert.Equal(SuggestionStatus.Pending, retrieved.Status);
        Assert.Equal("WALMART.*", retrieved.SuggestedPattern);
        Assert.Equal(categoryId, retrieved.SuggestedCategoryId);
        Assert.Equal(RuleMatchType.Regex, retrieved.SuggestedMatchType);
        Assert.Equal(0.95m, retrieved.Confidence);
    }

    [Fact]
    public async Task GetByIdAsync_Returns_Null_When_Not_Found()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var repository = new RuleSuggestionRepository(context);

        // Act
        var result = await repository.GetByIdAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetPendingAsync_Returns_Only_Pending_Suggestions()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var repository = new RuleSuggestionRepository(context);

        var pending1 = CreateTestSuggestion("AMAZON.*", "Amazon Rule");
        var pending2 = CreateTestSuggestion("COSTCO.*", "Costco Rule");
        var accepted = CreateTestSuggestion("TARGET.*", "Target Rule");
        accepted.Accept();

        await repository.AddAsync(pending1);
        await repository.AddAsync(pending2);
        await repository.AddAsync(accepted);
        await context.SaveChangesAsync();

        // Act
        var pendingSuggestions = await repository.GetPendingAsync();

        // Assert
        Assert.Equal(2, pendingSuggestions.Count);
        Assert.All(pendingSuggestions, s => Assert.Equal(SuggestionStatus.Pending, s.Status));
    }

    [Fact]
    public async Task GetPendingByTypeAsync_Filters_By_Type()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var repository = new RuleSuggestionRepository(context);

        var newRule = CreateTestSuggestion("UBER.*", "Uber Rule");
        var optimization = RuleSuggestion.CreateOptimizationSuggestion(
            title: "Optimize Rideshare Pattern",
            description: "Consolidate rideshare patterns",
            reasoning: "Multiple similar patterns detected",
            confidence: 0.88m,
            targetRuleId: Guid.NewGuid(),
            optimizedPattern: "UBER.*|LYFT.*");
        var conflictRule = RuleSuggestion.CreateConflictSuggestion(
            title: "Conflicting Gas Rules",
            description: "Two rules match the same transactions",
            reasoning: "Rules overlap on gas station transactions",
            conflictingRuleIds: new List<Guid> { Guid.NewGuid(), Guid.NewGuid() });

        await repository.AddAsync(newRule);
        await repository.AddAsync(optimization);
        await repository.AddAsync(conflictRule);
        await context.SaveChangesAsync();

        // Act
        var newRuleSuggestions = await repository.GetPendingByTypeAsync(SuggestionType.NewRule);
        var optimizationSuggestions = await repository.GetPendingByTypeAsync(SuggestionType.PatternOptimization);

        // Assert
        Assert.Single(newRuleSuggestions);
        Assert.Equal(SuggestionType.NewRule, newRuleSuggestions[0].Type);
        Assert.Single(optimizationSuggestions);
        Assert.Equal(SuggestionType.PatternOptimization, optimizationSuggestions[0].Type);
    }

    [Fact]
    public async Task GetByStatusAsync_Paginates_Results()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var repository = new RuleSuggestionRepository(context);

        for (int i = 0; i < 10; i++)
        {
            var suggestion = CreateTestSuggestion($"PATTERN{i}.*", $"Rule {i}");
            await repository.AddAsync(suggestion);
        }

        await context.SaveChangesAsync();

        // Act
        var page1 = await repository.GetByStatusAsync(SuggestionStatus.Pending, skip: 0, take: 3);
        var page2 = await repository.GetByStatusAsync(SuggestionStatus.Pending, skip: 3, take: 3);

        // Assert
        Assert.Equal(3, page1.Count);
        Assert.Equal(3, page2.Count);
        Assert.DoesNotContain(page1, s => page2.Contains(s));
    }

    [Fact]
    public async Task ExistsPendingWithPatternAsync_Returns_True_When_Exists()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var repository = new RuleSuggestionRepository(context);

        var suggestion = CreateTestSuggestion("STARBUCKS.*", "Starbucks Rule");

        await repository.AddAsync(suggestion);
        await context.SaveChangesAsync();

        // Act
        var exists = await repository.ExistsPendingWithPatternAsync("STARBUCKS.*");
        var notExists = await repository.ExistsPendingWithPatternAsync("DUNKIN.*");

        // Assert
        Assert.True(exists);
        Assert.False(notExists);
    }

    [Fact]
    public async Task ExistsPendingWithPatternAsync_Returns_False_For_Accepted_Suggestions()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var repository = new RuleSuggestionRepository(context);

        var suggestion = CreateTestSuggestion("CHIPOTLE.*", "Chipotle Rule");
        suggestion.Accept();

        await repository.AddAsync(suggestion);
        await context.SaveChangesAsync();

        // Act
        var exists = await repository.ExistsPendingWithPatternAsync("CHIPOTLE.*");

        // Assert
        Assert.False(exists);
    }

    [Fact]
    public async Task AddRangeAsync_Persists_Multiple_Suggestions()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var repository = new RuleSuggestionRepository(context);

        var suggestions = new List<RuleSuggestion>
        {
            CreateTestSuggestion("NETFLIX.*", "Netflix Rule"),
            CreateTestSuggestion("SPOTIFY.*", "Spotify Rule"),
            CreateTestSuggestion("HULU.*", "Hulu Rule"),
        };

        // Act
        await repository.AddRangeAsync(suggestions);
        await context.SaveChangesAsync();

        // Assert
        await using var verifyContext = this._fixture.CreateSharedContext(context);
        var verifyRepo = new RuleSuggestionRepository(verifyContext);
        var count = await verifyRepo.CountAsync();

        Assert.Equal(3, count);
    }

    [Fact]
    public async Task ListAsync_Orders_By_CreatedAtUtc_Descending()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var repository = new RuleSuggestionRepository(context);

        var first = CreateTestSuggestion("FIRST.*", "First Rule");
        await repository.AddAsync(first);
        await context.SaveChangesAsync();

        await Task.Delay(10); // Ensure different timestamps

        var second = CreateTestSuggestion("SECOND.*", "Second Rule");
        await repository.AddAsync(second);
        await context.SaveChangesAsync();

        // Act
        var results = await repository.ListAsync(skip: 0, take: 10);

        // Assert
        Assert.Equal(2, results.Count);
        Assert.Equal("SECOND.*", results[0].SuggestedPattern);
        Assert.Equal("FIRST.*", results[1].SuggestedPattern);
    }

    [Fact]
    public async Task RemoveAsync_Deletes_Suggestion()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var repository = new RuleSuggestionRepository(context);

        var suggestion = CreateTestSuggestion("DELETEME.*", "To Delete");

        await repository.AddAsync(suggestion);
        await context.SaveChangesAsync();

        // Act
        await repository.RemoveAsync(suggestion);
        await context.SaveChangesAsync();

        // Assert
        var retrieved = await repository.GetByIdAsync(suggestion.Id);
        Assert.Null(retrieved);
    }

    [Fact]
    public async Task Persists_ConflictingRuleIds_And_SampleDescriptions()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var repository = new RuleSuggestionRepository(context);

        var conflictingIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };

        var suggestion = RuleSuggestion.CreateConflictSuggestion(
            title: "Conflicting Rules Detected",
            description: "Multiple rules match same transactions",
            reasoning: "Rule A and Rule B both match gas station transactions",
            conflictingRuleIds: conflictingIds);

        await repository.AddAsync(suggestion);
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = this._fixture.CreateSharedContext(context);
        var verifyRepo = new RuleSuggestionRepository(verifyContext);
        var retrieved = await verifyRepo.GetByIdAsync(suggestion.Id);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(2, retrieved.ConflictingRuleIds.Count);
        Assert.Contains(conflictingIds[0], retrieved.ConflictingRuleIds);
        Assert.Contains(conflictingIds[1], retrieved.ConflictingRuleIds);
    }

    [Fact]
    public async Task Persists_SampleDescriptions()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var repository = new RuleSuggestionRepository(context);

        var samples = new List<string> { "Sample 1", "Sample 2", "Sample 3" };
        var suggestion = RuleSuggestion.CreateNewRuleSuggestion(
            title: "Test Rule",
            description: "Test description",
            reasoning: "Test reasoning",
            confidence: 0.9m,
            suggestedPattern: "TEST.*",
            suggestedMatchType: RuleMatchType.Regex,
            suggestedCategoryId: Guid.NewGuid(),
            affectedTransactionCount: 5,
            sampleDescriptions: samples);

        await repository.AddAsync(suggestion);
        await context.SaveChangesAsync();

        // Act
        await using var verifyContext = this._fixture.CreateSharedContext(context);
        var verifyRepo = new RuleSuggestionRepository(verifyContext);
        var retrieved = await verifyRepo.GetByIdAsync(suggestion.Id);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(3, retrieved.SampleDescriptions.Count);
        Assert.Equal("Sample 1", retrieved.SampleDescriptions[0]);
    }

    [Fact]
    public async Task CountAsync_Returns_Total_Count()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var repository = new RuleSuggestionRepository(context);

        await repository.AddAsync(CreateTestSuggestion("A.*", "Rule A"));
        await repository.AddAsync(CreateTestSuggestion("B.*", "Rule B"));
        await repository.AddAsync(CreateTestSuggestion("C.*", "Rule C"));
        await context.SaveChangesAsync();

        // Act
        var count = await repository.CountAsync();

        // Assert
        Assert.Equal(3, count);
    }

    [Fact]
    public async Task ExistsPendingForRuleAsync_Returns_True_When_Exists()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var repository = new RuleSuggestionRepository(context);

        var targetRuleId = Guid.NewGuid();
        var suggestion = RuleSuggestion.CreateUnusedRuleSuggestion(
            title: "Unused rule",
            description: "Rule never matches",
            reasoning: "0 matches",
            targetRuleId: targetRuleId);

        await repository.AddAsync(suggestion);
        await context.SaveChangesAsync();

        // Act
        var exists = await repository.ExistsPendingForRuleAsync(targetRuleId, SuggestionType.UnusedRule);

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public async Task ExistsPendingForRuleAsync_Returns_False_When_Not_Exists()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var repository = new RuleSuggestionRepository(context);

        // Act
        var exists = await repository.ExistsPendingForRuleAsync(Guid.NewGuid(), SuggestionType.UnusedRule);

        // Assert
        Assert.False(exists);
    }

    [Fact]
    public async Task ExistsPendingForRuleAsync_Returns_False_For_Different_Type()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var repository = new RuleSuggestionRepository(context);

        var targetRuleId = Guid.NewGuid();
        var suggestion = RuleSuggestion.CreateUnusedRuleSuggestion(
            title: "Unused rule",
            description: "Rule never matches",
            reasoning: "0 matches",
            targetRuleId: targetRuleId);

        await repository.AddAsync(suggestion);
        await context.SaveChangesAsync();

        // Act - query for different type
        var exists = await repository.ExistsPendingForRuleAsync(targetRuleId, SuggestionType.PatternOptimization);

        // Assert
        Assert.False(exists);
    }

    [Fact]
    public async Task ExistsPendingForRulesAsync_Returns_True_When_Same_Rules_Exist()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var repository = new RuleSuggestionRepository(context);

        var ruleIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        var suggestion = RuleSuggestion.CreateConflictSuggestion(
            title: "Conflicting rules",
            description: "Rules conflict",
            reasoning: "Same pattern different categories",
            conflictingRuleIds: ruleIds);

        await repository.AddAsync(suggestion);
        await context.SaveChangesAsync();

        // Act
        var exists = await repository.ExistsPendingForRulesAsync(ruleIds, SuggestionType.RuleConflict);

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public async Task ExistsPendingForRulesAsync_Returns_False_When_Different_Rules()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var repository = new RuleSuggestionRepository(context);

        var ruleIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        var suggestion = RuleSuggestion.CreateConflictSuggestion(
            title: "Conflicting rules",
            description: "Rules conflict",
            reasoning: "Same pattern different categories",
            conflictingRuleIds: ruleIds);

        await repository.AddAsync(suggestion);
        await context.SaveChangesAsync();

        // Act - different set of rule IDs
        var differentRuleIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        var exists = await repository.ExistsPendingForRulesAsync(differentRuleIds, SuggestionType.RuleConflict);

        // Assert
        Assert.False(exists);
    }

    [Fact]
    public async Task ExistsPendingForRulesAsync_Returns_False_For_Empty_List()
    {
        // Arrange
        await using var context = this._fixture.CreateContext();
        var repository = new RuleSuggestionRepository(context);

        // Act
        var exists = await repository.ExistsPendingForRulesAsync(new List<Guid>(), SuggestionType.RuleConflict);

        // Assert
        Assert.False(exists);
    }

    private static RuleSuggestion CreateTestSuggestion(string pattern, string title)
    {
        return RuleSuggestion.CreateNewRuleSuggestion(
            title: title,
            description: $"Create a rule for {title}",
            reasoning: $"Found transactions matching {pattern}",
            confidence: 0.9m,
            suggestedPattern: pattern,
            suggestedMatchType: RuleMatchType.Regex,
            suggestedCategoryId: Guid.NewGuid(),
            affectedTransactionCount: 10,
            sampleDescriptions: new List<string>());
    }
}
