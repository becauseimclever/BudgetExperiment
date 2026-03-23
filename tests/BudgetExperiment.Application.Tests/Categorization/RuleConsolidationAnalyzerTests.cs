// <copyright file="RuleConsolidationAnalyzerTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using Shouldly;

namespace BudgetExperiment.Application.Tests.Categorization;

/// <summary>
/// Unit tests for <see cref="RuleConsolidationAnalyzer"/>.
/// Feature 116 Slice 1: Exact duplicates and substring containment detection.
/// </summary>
public class RuleConsolidationAnalyzerTests
{
    private static readonly Guid GroceryCategoryId = Guid.NewGuid();
    private static readonly Guid TransportCategoryId = Guid.NewGuid();
    private static readonly Guid ShoppingCategoryId = Guid.NewGuid();

    [Fact]
    public async Task AnalyzeAsync_ExactDuplicates_ReturnsSingleSuggestion()
    {
        // Arrange
        var rules = new List<CategorizationRule>
        {
            CategorizationRule.Create("Walmart 1", RuleMatchType.Contains, "WALMART", GroceryCategoryId, priority: 1),
            CategorizationRule.Create("Walmart 2", RuleMatchType.Contains, "WALMART", GroceryCategoryId, priority: 2),
        };

        var analyzer = new RuleConsolidationAnalyzer();

        // Act
        var result = await analyzer.AnalyzeAsync(rules);

        // Assert
        result.ShouldNotBeEmpty();
        result.Count.ShouldBe(1);

        var suggestion = result[0];
        suggestion.SourceRuleIds.Count.ShouldBe(2);
        suggestion.SourceRuleIds.ShouldContain(rules[0].Id);
        suggestion.SourceRuleIds.ShouldContain(rules[1].Id);
        suggestion.MergedPattern.ShouldBe("WALMART");
        suggestion.MergedMatchType.ShouldBe(RuleMatchType.Contains);
        suggestion.Confidence.ShouldBe(1.0);
    }

    [Fact]
    public async Task AnalyzeAsync_ExactDuplicates_ThreePlusRules_IncludesAllIds()
    {
        // Arrange
        var rules = new List<CategorizationRule>
        {
            CategorizationRule.Create("Amazon 1", RuleMatchType.Contains, "AMAZON", ShoppingCategoryId, priority: 1),
            CategorizationRule.Create("Amazon 2", RuleMatchType.Contains, "AMAZON", ShoppingCategoryId, priority: 2),
            CategorizationRule.Create("Amazon 3", RuleMatchType.Contains, "AMAZON", ShoppingCategoryId, priority: 3),
        };

        var analyzer = new RuleConsolidationAnalyzer();

        // Act
        var result = await analyzer.AnalyzeAsync(rules);

        // Assert
        result.ShouldNotBeEmpty();
        result.Count.ShouldBe(1);

        var suggestion = result[0];
        suggestion.SourceRuleIds.Count.ShouldBe(3);
        suggestion.SourceRuleIds.ShouldContain(rules[0].Id);
        suggestion.SourceRuleIds.ShouldContain(rules[1].Id);
        suggestion.SourceRuleIds.ShouldContain(rules[2].Id);
        suggestion.MergedPattern.ShouldBe("AMAZON");
        suggestion.MergedMatchType.ShouldBe(RuleMatchType.Contains);
        suggestion.Confidence.ShouldBe(1.0);
    }

    [Fact]
    public async Task AnalyzeAsync_SingleRulePerCategory_NoSuggestions()
    {
        // Arrange
        var rules = new List<CategorizationRule>
        {
            CategorizationRule.Create("Grocery Rule", RuleMatchType.Contains, "GROCERY", GroceryCategoryId, priority: 1),
            CategorizationRule.Create("Transport Rule", RuleMatchType.Contains, "UBER", TransportCategoryId, priority: 2),
        };

        var analyzer = new RuleConsolidationAnalyzer();

        // Act
        var result = await analyzer.AnalyzeAsync(rules);

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task AnalyzeAsync_InactiveRulesExcluded_NoSuggestion()
    {
        // Arrange
        var activeRule = CategorizationRule.Create("Walmart Active", RuleMatchType.Contains, "WALMART", GroceryCategoryId, priority: 1);
        var inactiveRule = CategorizationRule.Create("Walmart Inactive", RuleMatchType.Contains, "WALMART", GroceryCategoryId, priority: 2);
        inactiveRule.Deactivate();

        var rules = new List<CategorizationRule> { activeRule, inactiveRule };

        var analyzer = new RuleConsolidationAnalyzer();

        // Act
        var result = await analyzer.AnalyzeAsync(rules);

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task AnalyzeAsync_CrossCategory_NotMerged()
    {
        // Arrange
        var rules = new List<CategorizationRule>
        {
            CategorizationRule.Create("Amazon Grocery", RuleMatchType.Contains, "AMAZON", GroceryCategoryId, priority: 1),
            CategorizationRule.Create("Amazon Shopping", RuleMatchType.Contains, "AMAZON", ShoppingCategoryId, priority: 2),
        };

        var analyzer = new RuleConsolidationAnalyzer();

        // Act
        var result = await analyzer.AnalyzeAsync(rules);

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task AnalyzeAsync_MixedMatchTypes_NotMerged()
    {
        // Arrange
        var rules = new List<CategorizationRule>
        {
            CategorizationRule.Create("Walmart Contains", RuleMatchType.Contains, "WALMART", GroceryCategoryId, priority: 1),
            CategorizationRule.Create("Walmart Regex", RuleMatchType.Regex, "WALMART", GroceryCategoryId, priority: 2),
        };

        var analyzer = new RuleConsolidationAnalyzer();

        // Act
        var result = await analyzer.AnalyzeAsync(rules);

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task AnalyzeAsync_SubstringContainment_SuggestsBroaderPattern()
    {
        // Arrange
        var rules = new List<CategorizationRule>
        {
            CategorizationRule.Create("Amazon Broad", RuleMatchType.Contains, "AMAZON", ShoppingCategoryId, priority: 1),
            CategorizationRule.Create("Amazon Narrow", RuleMatchType.Contains, "AMAZON MKTPL", ShoppingCategoryId, priority: 2),
        };

        var analyzer = new RuleConsolidationAnalyzer();

        // Act
        var result = await analyzer.AnalyzeAsync(rules);

        // Assert
        result.ShouldNotBeEmpty();
        result.Count.ShouldBe(1);

        var suggestion = result[0];
        suggestion.SourceRuleIds.Count.ShouldBe(2);
        suggestion.MergedPattern.ShouldBe("AMAZON");
        suggestion.MergedMatchType.ShouldBe(RuleMatchType.Contains);
        suggestion.Confidence.ShouldBe(1.0);
    }

    [Fact]
    public async Task AnalyzeAsync_SubstringContainment_KeepsShorterBroaderPattern()
    {
        // Arrange - "AMAZON" is shorter and broader than "AMAZON MKTPL"
        var rules = new List<CategorizationRule>
        {
            CategorizationRule.Create("Amazon Marketplace", RuleMatchType.Contains, "AMAZON MKTPL", ShoppingCategoryId, priority: 1),
            CategorizationRule.Create("Amazon Generic", RuleMatchType.Contains, "AMAZON", ShoppingCategoryId, priority: 2),
        };

        var analyzer = new RuleConsolidationAnalyzer();

        // Act
        var result = await analyzer.AnalyzeAsync(rules);

        // Assert
        result.ShouldNotBeEmpty();
        var suggestion = result[0];
        suggestion.MergedPattern.ShouldBe("AMAZON");
    }

    [Fact]
    public async Task AnalyzeAsync_SubstringContainment_CrossCategory_Skipped()
    {
        // Arrange
        var rules = new List<CategorizationRule>
        {
            CategorizationRule.Create("Amazon Grocery", RuleMatchType.Contains, "AMAZON", GroceryCategoryId, priority: 1),
            CategorizationRule.Create("Amazon Mktpl Shopping", RuleMatchType.Contains, "AMAZON MKTPL", ShoppingCategoryId, priority: 2),
        };

        var analyzer = new RuleConsolidationAnalyzer();

        // Act
        var result = await analyzer.AnalyzeAsync(rules);

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task AnalyzeAsync_SubstringContainment_NonContainsType_Skipped()
    {
        // Arrange - substring relationship exists but not both Contains type
        var rules = new List<CategorizationRule>
        {
            CategorizationRule.Create("Amazon Regex", RuleMatchType.Regex, "AMAZON", ShoppingCategoryId, priority: 1),
            CategorizationRule.Create("Amazon Mktpl Contains", RuleMatchType.Contains, "AMAZON MKTPL", ShoppingCategoryId, priority: 2),
        };

        var analyzer = new RuleConsolidationAnalyzer();

        // Act
        var result = await analyzer.AnalyzeAsync(rules);

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task AnalyzeAsync_CaseInsensitiveDuplicates_DetectsAsDuplicate()
    {
        // Arrange
        var rules = new List<CategorizationRule>
        {
            CategorizationRule.Create("Walmart Lower", RuleMatchType.Contains, "walmart", GroceryCategoryId, priority: 1),
            CategorizationRule.Create("Walmart Upper", RuleMatchType.Contains, "WALMART", GroceryCategoryId, priority: 2),
        };

        var analyzer = new RuleConsolidationAnalyzer();

        // Act
        var result = await analyzer.AnalyzeAsync(rules);

        // Assert
        result.ShouldNotBeEmpty();
        result.Count.ShouldBe(1);

        var suggestion = result[0];
        suggestion.SourceRuleIds.Count.ShouldBe(2);
        suggestion.Confidence.ShouldBe(1.0);
    }

    [Fact]
    public async Task AnalyzeAsync_MixedStrategies_ReturnsBothSuggestions()
    {
        // Arrange - both exact duplicates AND substring containment
        var rules = new List<CategorizationRule>
        {
            // Exact duplicates for Grocery
            CategorizationRule.Create("Kroger 1", RuleMatchType.Contains, "KROGER", GroceryCategoryId, priority: 1),
            CategorizationRule.Create("Kroger 2", RuleMatchType.Contains, "KROGER", GroceryCategoryId, priority: 2),

            // Substring containment for Shopping
            CategorizationRule.Create("Amazon", RuleMatchType.Contains, "AMAZON", ShoppingCategoryId, priority: 3),
            CategorizationRule.Create("Amazon MKTPL", RuleMatchType.Contains, "AMAZON MKTPL", ShoppingCategoryId, priority: 4),
        };

        var analyzer = new RuleConsolidationAnalyzer();

        // Act
        var result = await analyzer.AnalyzeAsync(rules);

        // Assert
        result.Count.ShouldBe(2);

        var krogerSuggestion = result.FirstOrDefault(s => s.MergedPattern == "KROGER");
        krogerSuggestion.ShouldNotBeNull();
        krogerSuggestion.SourceRuleIds.Count.ShouldBe(2);

        var amazonSuggestion = result.FirstOrDefault(s => s.MergedPattern == "AMAZON");
        amazonSuggestion.ShouldNotBeNull();
        amazonSuggestion.SourceRuleIds.Count.ShouldBe(2);
    }

    [Fact]
    public async Task AnalyzeAsync_EmptyRuleList_ReturnsEmpty()
    {
        // Arrange
        var rules = new List<CategorizationRule>();
        var analyzer = new RuleConsolidationAnalyzer();

        // Act
        var result = await analyzer.AnalyzeAsync(rules);

        // Assert
        result.ShouldBeEmpty();
    }

    // Slice 2 — Strategy 3: Regex Alternation
    [Fact]
    public async Task AnalyzeAsync_RegexAlternation_TwoContainsRules_ReturnsSingleRegexSuggestion()
    {
        // Arrange
        var rules = new List<CategorizationRule>
        {
            CategorizationRule.Create("Walmart", RuleMatchType.Contains, "WALMART", GroceryCategoryId, priority: 1),
            CategorizationRule.Create("Kroger", RuleMatchType.Contains, "KROGER", GroceryCategoryId, priority: 2),
        };

        var analyzer = new RuleConsolidationAnalyzer();

        // Act
        var result = await analyzer.AnalyzeAsync(rules);

        // Assert
        result.Count.ShouldBe(1);

        var suggestion = result[0];
        suggestion.MergedMatchType.ShouldBe(RuleMatchType.Regex);
        suggestion.MergedPattern.ShouldContain("WALMART");
        suggestion.MergedPattern.ShouldContain("KROGER");
        suggestion.MergedPattern.ShouldContain("|");
        suggestion.Confidence.ShouldBe(1.0);
        suggestion.SourceRuleIds.ShouldContain(rules[0].Id);
        suggestion.SourceRuleIds.ShouldContain(rules[1].Id);
    }

    [Fact]
    public async Task AnalyzeAsync_RegexAlternation_ThreePlusContainsRules_MergesAll()
    {
        // Arrange
        var rules = new List<CategorizationRule>
        {
            CategorizationRule.Create("Walmart", RuleMatchType.Contains, "WALMART", GroceryCategoryId, priority: 1),
            CategorizationRule.Create("Kroger", RuleMatchType.Contains, "KROGER", GroceryCategoryId, priority: 2),
            CategorizationRule.Create("Safeway", RuleMatchType.Contains, "SAFEWAY", GroceryCategoryId, priority: 3),
        };

        var analyzer = new RuleConsolidationAnalyzer();

        // Act
        var result = await analyzer.AnalyzeAsync(rules);

        // Assert
        result.Count.ShouldBe(1);

        var suggestion = result[0];
        suggestion.SourceRuleIds.Count.ShouldBe(3);
        suggestion.SourceRuleIds.ShouldContain(rules[0].Id);
        suggestion.SourceRuleIds.ShouldContain(rules[1].Id);
        suggestion.SourceRuleIds.ShouldContain(rules[2].Id);
        suggestion.MergedMatchType.ShouldBe(RuleMatchType.Regex);
        suggestion.MergedPattern.ShouldContain("WALMART");
        suggestion.MergedPattern.ShouldContain("KROGER");
        suggestion.MergedPattern.ShouldContain("SAFEWAY");

        var pipeCount = suggestion.MergedPattern.Count(c => c == '|');
        pipeCount.ShouldBe(2);
    }

    [Fact]
    public async Task AnalyzeAsync_RegexAlternation_SpecialCharactersEscaped()
    {
        // Arrange — '.' and '+' are regex metacharacters; they must be escaped in the merged pattern
        var rules = new List<CategorizationRule>
        {
            CategorizationRule.Create("PayCom", RuleMatchType.Contains, "PAY.COM", GroceryCategoryId, priority: 1),
            CategorizationRule.Create("PayLater", RuleMatchType.Contains, "PAY+LATER", GroceryCategoryId, priority: 2),
        };

        var analyzer = new RuleConsolidationAnalyzer();

        // Act
        var result = await analyzer.AnalyzeAsync(rules);

        // Assert
        result.Count.ShouldBe(1);

        var suggestion = result[0];
        suggestion.MergedMatchType.ShouldBe(RuleMatchType.Regex);
        suggestion.MergedPattern.ShouldContain(@"\.");
        suggestion.MergedPattern.ShouldContain(@"\+");
    }

    [Fact]
    public async Task AnalyzeAsync_RegexAlternation_CrossCategory_NotMerged()
    {
        // Arrange
        var rules = new List<CategorizationRule>
        {
            CategorizationRule.Create("Walmart", RuleMatchType.Contains, "WALMART", GroceryCategoryId, priority: 1),
            CategorizationRule.Create("Kroger", RuleMatchType.Contains, "KROGER", TransportCategoryId, priority: 2),
        };

        var analyzer = new RuleConsolidationAnalyzer();

        // Act
        var result = await analyzer.AnalyzeAsync(rules);

        // Assert — different categories, no alternation should be produced
        var alternationSuggestions = result.Where(s => s.MergedMatchType == RuleMatchType.Regex).ToList();
        alternationSuggestions.ShouldBeEmpty();
    }

    [Fact]
    public async Task AnalyzeAsync_RegexAlternation_NotAppliedToNonContainsTypes()
    {
        // Arrange — both rules are already Regex type; Strategy 3 only promotes Contains → Regex
        var rules = new List<CategorizationRule>
        {
            CategorizationRule.Create("Walmart Regex", RuleMatchType.Regex, "WALMART", GroceryCategoryId, priority: 1),
            CategorizationRule.Create("Kroger Regex", RuleMatchType.Regex, "KROGER", GroceryCategoryId, priority: 2),
        };

        var analyzer = new RuleConsolidationAnalyzer();

        // Act
        var result = await analyzer.AnalyzeAsync(rules);

        // Assert — no alternation suggestion produced for already-Regex rules
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task AnalyzeAsync_RegexAlternation_SingleRule_NotSuggested()
    {
        // Arrange — only one Contains rule in the category; nothing to alternate
        var rules = new List<CategorizationRule>
        {
            CategorizationRule.Create("Walmart Only", RuleMatchType.Contains, "WALMART", GroceryCategoryId, priority: 1),
        };

        var analyzer = new RuleConsolidationAnalyzer();

        // Act
        var result = await analyzer.AnalyzeAsync(rules);

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task AnalyzeAsync_RegexAlternation_PatternTooLong_SplitIntoMultipleSuggestions()
    {
        // Arrange — 15 rules with ~50-char patterns; joined alternation ≈ 764 chars, exceeds 500-char limit
        var rules = Enumerable.Range(1, 15)
            .Select(i => CategorizationRule.Create(
                $"Merchant {i}",
                RuleMatchType.Contains,
                $"MERCHANTPATTERN{i:D2}XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX",
                GroceryCategoryId,
                priority: i))
            .ToList();

        var analyzer = new RuleConsolidationAnalyzer();

        // Act
        var result = await analyzer.AnalyzeAsync(rules);

        // Assert — split into multiple suggestions each within the length limit
        result.Count.ShouldBeGreaterThan(1);

        foreach (var suggestion in result)
        {
            suggestion.MergedPattern.Length.ShouldBeLessThanOrEqualTo(500);
            suggestion.MergedMatchType.ShouldBe(RuleMatchType.Regex);
        }

        var allSourceIds = result.SelectMany(s => s.SourceRuleIds).ToHashSet();
        foreach (var rule in rules)
        {
            allSourceIds.ShouldContain(rule.Id);
        }
    }

    [Fact]
    public async Task AnalyzeAsync_AlreadyExactDuplicate_NotAlsoAlternation()
    {
        // Arrange — two Contains rules with the same pattern; Strategy 1 handles them as an exact duplicate
        var rules = new List<CategorizationRule>
        {
            CategorizationRule.Create("Walmart 1", RuleMatchType.Contains, "WALMART", GroceryCategoryId, priority: 1),
            CategorizationRule.Create("Walmart 2", RuleMatchType.Contains, "WALMART", GroceryCategoryId, priority: 2),
        };

        var analyzer = new RuleConsolidationAnalyzer();

        // Act
        var result = await analyzer.AnalyzeAsync(rules);

        // Assert — exactly one suggestion (Strategy 1 duplicate), not an alternation
        result.Count.ShouldBe(1);
        result[0].MergedMatchType.ShouldBe(RuleMatchType.Contains);
    }
}
