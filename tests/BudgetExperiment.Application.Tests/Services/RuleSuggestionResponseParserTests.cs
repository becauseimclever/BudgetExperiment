// <copyright file="RuleSuggestionResponseParserTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Text.Json;
using BudgetExperiment.Application.Categorization;
using BudgetExperiment.Domain;
using Moq;

namespace BudgetExperiment.Application.Tests.Services;

/// <summary>
/// Unit tests for <see cref="RuleSuggestionResponseParser"/>.
/// </summary>
public class RuleSuggestionResponseParserTests
{
    private static readonly Guid GroceryCategoryId = Guid.NewGuid();
    private static readonly Guid UtilitiesCategoryId = Guid.NewGuid();

    private readonly Mock<IRuleSuggestionRepository> _suggestionRepo;
    private readonly RuleSuggestionResponseParser _parser;

    public RuleSuggestionResponseParserTests()
    {
        _suggestionRepo = new Mock<IRuleSuggestionRepository>();
        _parser = new RuleSuggestionResponseParser(_suggestionRepo.Object);
    }

    [Fact]
    public void ExtractJson_Returns_Pure_Json_Unchanged()
    {
        var json = """{"suggestions": []}""";

        var result = RuleSuggestionResponseParser.ExtractJson(json);

        Assert.Equal(json, result);
    }

    [Fact]
    public void ExtractJson_Strips_Markdown_Code_Block()
    {
        var content = """
            ```json
            {"suggestions": [{"pattern": "WALMART"}]}
            ```
            """;

        var result = RuleSuggestionResponseParser.ExtractJson(content);

        Assert.StartsWith("{", result);
        Assert.EndsWith("}", result);
        Assert.Contains("WALMART", result);
    }

    [Fact]
    public void ExtractJson_Strips_Preamble_And_Trailing_Text()
    {
        var content = """
            Here is the response:
            {"data": "value"}
            Hope this helps!
            """;

        var result = RuleSuggestionResponseParser.ExtractJson(content);

        Assert.StartsWith("{", result);
        Assert.EndsWith("}", result);
    }

    [Fact]
    public void ExtractJson_Throws_JsonException_When_No_Json_Found()
    {
        Assert.Throws<JsonException>(() =>
            RuleSuggestionResponseParser.ExtractJson("No JSON here"));
    }

    [Fact]
    public void ParseNewRuleSuggestions_Returns_Suggestions_From_Valid_Json()
    {
        // Arrange
        var categories = CreateCategories(("Groceries", GroceryCategoryId));
        var json = """
            {
              "suggestions": [
                {
                  "pattern": "WALMART",
                  "matchType": "Contains",
                  "categoryName": "Groceries",
                  "confidence": 0.95,
                  "reasoning": "Walmart is a grocery store",
                  "sampleMatches": ["WALMART STORE #123"]
                }
              ]
            }
            """;

        // Act
        var result = _parser.ParseNewRuleSuggestions(json, categories, 10);

        // Assert
        Assert.Single(result);
        Assert.Equal("WALMART", result[0].SuggestedPattern);
        Assert.Equal(RuleMatchType.Contains, result[0].SuggestedMatchType);
        Assert.Equal(GroceryCategoryId, result[0].SuggestedCategoryId);
    }

    [Fact]
    public void ParseNewRuleSuggestions_Skips_Unknown_Category()
    {
        // Arrange
        var categories = CreateCategories(("Groceries", GroceryCategoryId));
        var json = """
            {
              "suggestions": [
                {
                  "pattern": "STARBUCKS",
                  "matchType": "Contains",
                  "categoryName": "Coffee",
                  "confidence": 0.90,
                  "reasoning": "Starbucks is coffee"
                }
              ]
            }
            """;

        // Act
        var result = _parser.ParseNewRuleSuggestions(json, categories, 5);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void ParseNewRuleSuggestions_Returns_Empty_On_Invalid_Json()
    {
        // Arrange
        var categories = CreateCategories(("Groceries", GroceryCategoryId));

        // Act
        var result = _parser.ParseNewRuleSuggestions("not json at all", categories, 5);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void ParseNewRuleSuggestions_Defaults_To_Contains_For_Unknown_MatchType()
    {
        // Arrange
        var categories = CreateCategories(("Groceries", GroceryCategoryId));
        var json = """
            {
              "suggestions": [
                {
                  "pattern": "WALMART",
                  "matchType": "InvalidType",
                  "categoryName": "Groceries",
                  "confidence": 0.85
                }
              ]
            }
            """;

        // Act
        var result = _parser.ParseNewRuleSuggestions(json, categories, 5);

        // Assert
        Assert.Single(result);
        Assert.Equal(RuleMatchType.Contains, result[0].SuggestedMatchType);
    }

    [Fact]
    public void ParseNewRuleSuggestions_Clamps_Confidence()
    {
        // Arrange
        var categories = CreateCategories(("Groceries", GroceryCategoryId));
        var json = """
            {
              "suggestions": [
                {
                  "pattern": "WALMART",
                  "matchType": "Contains",
                  "categoryName": "Groceries",
                  "confidence": 1.5
                }
              ]
            }
            """;

        // Act
        var result = _parser.ParseNewRuleSuggestions(json, categories, 5);

        // Assert
        Assert.Single(result);
        Assert.Equal(1m, result[0].Confidence);
    }

    [Fact]
    public async Task ParseOptimizationSuggestionsAsync_Parses_Remove_Suggestion()
    {
        // Arrange
        var ruleId = Guid.NewGuid();
        var rule = CategorizationRule.Create("Old Rule", RuleMatchType.Contains, "OLD", GroceryCategoryId);
        typeof(CategorizationRule).GetProperty("Id")!.SetValue(rule, ruleId);
        var rules = new List<CategorizationRule> { rule };

        _suggestionRepo
            .Setup(r => r.ExistsPendingForRuleAsync(ruleId, SuggestionType.UnusedRule, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var json = $$"""
            {
              "suggestions": [
                {
                  "type": "remove",
                  "targetRuleId": "{{ruleId}}",
                  "reasoning": "Rule has 0 matches"
                }
              ]
            }
            """;

        // Act
        var result = await _parser.ParseOptimizationSuggestionsAsync(json, rules);

        // Assert
        Assert.Single(result);
        Assert.Equal(SuggestionType.UnusedRule, result[0].Type);
        Assert.Equal(ruleId, result[0].TargetRuleId);
    }

    [Fact]
    public async Task ParseOptimizationSuggestionsAsync_Skips_Duplicate_Suggestions()
    {
        // Arrange
        var ruleId = Guid.NewGuid();
        var rule = CategorizationRule.Create("Old Rule", RuleMatchType.Contains, "OLD", GroceryCategoryId);
        typeof(CategorizationRule).GetProperty("Id")!.SetValue(rule, ruleId);
        var rules = new List<CategorizationRule> { rule };

        _suggestionRepo
            .Setup(r => r.ExistsPendingForRuleAsync(ruleId, SuggestionType.UnusedRule, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true); // Already exists

        var json = $$"""
            {
              "suggestions": [
                {
                  "type": "remove",
                  "targetRuleId": "{{ruleId}}",
                  "reasoning": "Rule has 0 matches"
                }
              ]
            }
            """;

        // Act
        var result = await _parser.ParseOptimizationSuggestionsAsync(json, rules);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ParseOptimizationSuggestionsAsync_Parses_Simplify_Suggestion()
    {
        // Arrange
        var ruleId = Guid.NewGuid();
        var rule = CategorizationRule.Create("Complex Rule", RuleMatchType.Contains, "COMPLEX.*PATTERN", GroceryCategoryId);
        typeof(CategorizationRule).GetProperty("Id")!.SetValue(rule, ruleId);
        var rules = new List<CategorizationRule> { rule };

        _suggestionRepo
            .Setup(r => r.ExistsPendingForRuleAsync(ruleId, SuggestionType.PatternOptimization, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var json = $$"""
            {
              "suggestions": [
                {
                  "type": "simplify",
                  "targetRuleId": "{{ruleId}}",
                  "suggestedPattern": "COMPLEX",
                  "reasoning": "Pattern can be simplified"
                }
              ]
            }
            """;

        // Act
        var result = await _parser.ParseOptimizationSuggestionsAsync(json, rules);

        // Assert
        Assert.Single(result);
        Assert.Equal(SuggestionType.PatternOptimization, result[0].Type);
        Assert.Equal("COMPLEX", result[0].OptimizedPattern);
    }

    [Fact]
    public async Task ParseOptimizationSuggestionsAsync_Returns_Empty_On_Invalid_Json()
    {
        var rules = new List<CategorizationRule>();

        var result = await _parser.ParseOptimizationSuggestionsAsync("broken json", rules);

        Assert.Empty(result);
    }

    [Fact]
    public async Task ParseConflictSuggestionsAsync_Parses_Conflict()
    {
        // Arrange
        var ruleId1 = Guid.NewGuid();
        var ruleId2 = Guid.NewGuid();
        var rule1 = CategorizationRule.Create("Rule A", RuleMatchType.Contains, "AMAZON", GroceryCategoryId);
        var rule2 = CategorizationRule.Create("Rule B", RuleMatchType.Contains, "AMAZON", UtilitiesCategoryId);
        typeof(CategorizationRule).GetProperty("Id")!.SetValue(rule1, ruleId1);
        typeof(CategorizationRule).GetProperty("Id")!.SetValue(rule2, ruleId2);
        var rules = new List<CategorizationRule> { rule1, rule2 };

        _suggestionRepo
            .Setup(r => r.ExistsPendingForRulesAsync(It.IsAny<IReadOnlyList<Guid>>(), SuggestionType.RuleConflict, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var json = $$"""
            {
              "conflicts": [
                {
                  "ruleIds": ["{{ruleId1}}", "{{ruleId2}}"],
                  "conflictType": "contradiction",
                  "description": "Both rules match AMAZON but assign different categories",
                  "resolution": "Adjust patterns to be more specific"
                }
              ]
            }
            """;

        // Act
        var result = await _parser.ParseConflictSuggestionsAsync(json, rules);

        // Assert
        Assert.Single(result);
        Assert.Equal(SuggestionType.RuleConflict, result[0].Type);
        Assert.Contains("Contradictory", result[0].Title);
    }

    [Fact]
    public async Task ParseConflictSuggestionsAsync_Returns_Empty_On_Invalid_Json()
    {
        var rules = new List<CategorizationRule>();

        var result = await _parser.ParseConflictSuggestionsAsync("not json", rules);

        Assert.Empty(result);
    }

    [Fact]
    public async Task ParseConflictSuggestionsAsync_Skips_Conflicts_With_Less_Than_Two_Rules()
    {
        // Arrange
        var ruleId = Guid.NewGuid();
        var rule = CategorizationRule.Create("Rule A", RuleMatchType.Contains, "AMAZON", GroceryCategoryId);
        typeof(CategorizationRule).GetProperty("Id")!.SetValue(rule, ruleId);
        var rules = new List<CategorizationRule> { rule };

        var json = $$"""
            {
              "conflicts": [
                {
                  "ruleIds": ["{{ruleId}}"],
                  "conflictType": "overlap"
                }
              ]
            }
            """;

        // Act
        var result = await _parser.ParseConflictSuggestionsAsync(json, rules);

        // Assert
        Assert.Empty(result);
    }

    private static IReadOnlyList<BudgetCategory> CreateCategories(params (string Name, Guid Id)[] categories)
    {
        return categories.Select(c =>
        {
            var category = BudgetCategory.Create(c.Name, CategoryType.Expense);
            typeof(BudgetCategory).GetProperty("Id")!.SetValue(category, c.Id);
            return category;
        }).ToList();
    }
}
