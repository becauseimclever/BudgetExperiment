// <copyright file="RuleSuggestionServiceTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain;

using Moq;

namespace BudgetExperiment.Application.Tests;

/// <summary>
/// Unit tests for <see cref="RuleSuggestionService"/>.
/// </summary>
public class RuleSuggestionServiceTests
{
    private static readonly Guid GroceryCategoryId = Guid.NewGuid();
    private static readonly Guid RestaurantCategoryId = Guid.NewGuid();

    [Fact]
    public async Task SuggestNewRulesAsync_Returns_Empty_When_No_Uncategorized_Transactions()
    {
        // Arrange
        var (service, _, transactionRepo, _, _, _) = CreateService();
        transactionRepo
            .Setup(r => r.GetUncategorizedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Transaction>());

        // Act
        var result = await service.SuggestNewRulesAsync();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task SuggestNewRulesAsync_Returns_Empty_When_AI_Not_Available()
    {
        // Arrange
        var (service, aiService, transactionRepo, _, categoryRepo, _) = CreateService();
        SetupUncategorizedTransactions(transactionRepo, "WALMART STORE #123", "TARGET STORE #456");
        SetupCategories(categoryRepo, ("Groceries", GroceryCategoryId));
        aiService
            .Setup(s => s.GetStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiServiceStatus(false, null, "Connection refused"));

        // Act
        var result = await service.SuggestNewRulesAsync();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task SuggestNewRulesAsync_Sends_Correct_Prompt_To_AI()
    {
        // Arrange
        var (service, aiService, transactionRepo, ruleRepo, categoryRepo, _) = CreateService();
        SetupUncategorizedTransactions(transactionRepo, "WALMART STORE #123", "AMAZON MARKETPLACE");
        SetupCategories(categoryRepo, ("Groceries", GroceryCategoryId));
        SetupExistingRules(ruleRepo);
        SetupAiAvailable(aiService);

        AiPrompt? capturedPrompt = null;
        aiService
            .Setup(s => s.CompleteAsync(It.IsAny<AiPrompt>(), It.IsAny<CancellationToken>()))
            .Callback<AiPrompt, CancellationToken>((p, _) => capturedPrompt = p)
            .ReturnsAsync(new AiResponse(true, "{\"suggestions\": []}", null, 100, TimeSpan.FromSeconds(1)));

        // Act
        await service.SuggestNewRulesAsync();

        // Assert
        Assert.NotNull(capturedPrompt);
        Assert.Contains("WALMART STORE #123", capturedPrompt.UserPrompt);
        Assert.Contains("AMAZON MARKETPLACE", capturedPrompt.UserPrompt);
        Assert.Contains("Groceries", capturedPrompt.UserPrompt);
    }

    [Fact]
    public async Task SuggestNewRulesAsync_Parses_AI_Response_Into_Suggestions()
    {
        // Arrange
        var (service, aiService, transactionRepo, ruleRepo, categoryRepo, _) = CreateService();
        SetupUncategorizedTransactions(transactionRepo, "WALMART STORE #123", "WALMART SUPERCENTER");
        SetupCategories(categoryRepo, ("Groceries", GroceryCategoryId));
        SetupExistingRules(ruleRepo);
        SetupAiAvailable(aiService);

        var aiResponse = """
            {
              "suggestions": [
                {
                  "pattern": "WALMART",
                  "matchType": "Contains",
                  "categoryName": "Groceries",
                  "confidence": 0.95,
                  "reasoning": "Both transactions contain WALMART and are grocery purchases",
                  "sampleMatches": ["WALMART STORE #123", "WALMART SUPERCENTER"]
                }
              ]
            }
            """;

        aiService
            .Setup(s => s.CompleteAsync(It.IsAny<AiPrompt>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiResponse(true, aiResponse, null, 200, TimeSpan.FromSeconds(2)));

        // Act
        var result = await service.SuggestNewRulesAsync();

        // Assert
        Assert.Single(result);
        var suggestion = result[0];
        Assert.Equal(SuggestionType.NewRule, suggestion.Type);
        Assert.Equal(SuggestionStatus.Pending, suggestion.Status);
        Assert.Equal("WALMART", suggestion.SuggestedPattern);
        Assert.Equal(RuleMatchType.Contains, suggestion.SuggestedMatchType);
        Assert.Equal(GroceryCategoryId, suggestion.SuggestedCategoryId);
        Assert.Equal(0.95m, suggestion.Confidence);
        Assert.Equal(2, suggestion.SampleDescriptions.Count);
    }

    [Fact]
    public async Task SuggestNewRulesAsync_Skips_Patterns_That_Already_Exist()
    {
        // Arrange
        var (service, aiService, transactionRepo, ruleRepo, categoryRepo, suggestionRepo) = CreateService();
        SetupUncategorizedTransactions(transactionRepo, "WALMART STORE #123");
        SetupCategories(categoryRepo, ("Groceries", GroceryCategoryId));
        SetupExistingRules(ruleRepo);
        SetupAiAvailable(aiService);

        // Mark WALMART pattern as already having a pending suggestion
        suggestionRepo
            .Setup(r => r.ExistsPendingWithPatternAsync("WALMART", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var aiResponse = """
            {
              "suggestions": [
                {
                  "pattern": "WALMART",
                  "matchType": "Contains",
                  "categoryName": "Groceries",
                  "confidence": 0.9,
                  "reasoning": "Walmart pattern",
                  "sampleMatches": ["WALMART STORE #123"]
                }
              ]
            }
            """;

        aiService
            .Setup(s => s.CompleteAsync(It.IsAny<AiPrompt>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiResponse(true, aiResponse, null, 100, TimeSpan.FromSeconds(1)));

        // Act
        var result = await service.SuggestNewRulesAsync();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task SuggestNewRulesAsync_Returns_Empty_When_AI_Returns_Error()
    {
        // Arrange
        var (service, aiService, transactionRepo, ruleRepo, categoryRepo, _) = CreateService();
        SetupUncategorizedTransactions(transactionRepo, "WALMART STORE #123");
        SetupCategories(categoryRepo, ("Groceries", GroceryCategoryId));
        SetupExistingRules(ruleRepo);
        SetupAiAvailable(aiService);

        aiService
            .Setup(s => s.CompleteAsync(It.IsAny<AiPrompt>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiResponse(false, string.Empty, "Model error", 0, TimeSpan.Zero));

        // Act
        var result = await service.SuggestNewRulesAsync();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task SuggestNewRulesAsync_Returns_Empty_When_AI_Returns_Invalid_Json()
    {
        // Arrange
        var (service, aiService, transactionRepo, ruleRepo, categoryRepo, _) = CreateService();
        SetupUncategorizedTransactions(transactionRepo, "WALMART STORE #123");
        SetupCategories(categoryRepo, ("Groceries", GroceryCategoryId));
        SetupExistingRules(ruleRepo);
        SetupAiAvailable(aiService);

        aiService
            .Setup(s => s.CompleteAsync(It.IsAny<AiPrompt>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiResponse(true, "This is not valid JSON", null, 100, TimeSpan.FromSeconds(1)));

        // Act
        var result = await service.SuggestNewRulesAsync();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task SuggestNewRulesAsync_Persists_Suggestions_To_Repository()
    {
        // Arrange
        var (service, aiService, transactionRepo, ruleRepo, categoryRepo, suggestionRepo) = CreateService();
        SetupUncategorizedTransactions(transactionRepo, "WALMART STORE #123");
        SetupCategories(categoryRepo, ("Groceries", GroceryCategoryId));
        SetupExistingRules(ruleRepo);
        SetupAiAvailable(aiService);

        suggestionRepo
            .Setup(r => r.ExistsPendingWithPatternAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var aiResponse = """
            {
              "suggestions": [
                {
                  "pattern": "WALMART",
                  "matchType": "Contains",
                  "categoryName": "Groceries",
                  "confidence": 0.9,
                  "reasoning": "Walmart grocery",
                  "sampleMatches": ["WALMART STORE #123"]
                }
              ]
            }
            """;

        aiService
            .Setup(s => s.CompleteAsync(It.IsAny<AiPrompt>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiResponse(true, aiResponse, null, 100, TimeSpan.FromSeconds(1)));

        // Act
        await service.SuggestNewRulesAsync();

        // Assert
        suggestionRepo.Verify(
            r => r.AddRangeAsync(
                It.Is<IEnumerable<RuleSuggestion>>(s => s.Count() == 1),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SuggestNewRulesAsync_Limits_Results_To_MaxSuggestions()
    {
        // Arrange
        var (service, aiService, transactionRepo, ruleRepo, categoryRepo, suggestionRepo) = CreateService();
        SetupUncategorizedTransactions(transactionRepo, "TX1", "TX2", "TX3");
        SetupCategories(categoryRepo, ("Groceries", GroceryCategoryId));
        SetupExistingRules(ruleRepo);
        SetupAiAvailable(aiService);

        suggestionRepo
            .Setup(r => r.ExistsPendingWithPatternAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var aiResponse = """
            {
              "suggestions": [
                { "pattern": "A", "matchType": "Contains", "categoryName": "Groceries", "confidence": 0.9, "reasoning": "R1", "sampleMatches": [] },
                { "pattern": "B", "matchType": "Contains", "categoryName": "Groceries", "confidence": 0.8, "reasoning": "R2", "sampleMatches": [] },
                { "pattern": "C", "matchType": "Contains", "categoryName": "Groceries", "confidence": 0.7, "reasoning": "R3", "sampleMatches": [] }
              ]
            }
            """;

        aiService
            .Setup(s => s.CompleteAsync(It.IsAny<AiPrompt>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiResponse(true, aiResponse, null, 100, TimeSpan.FromSeconds(1)));

        // Act - only request 2 suggestions
        var result = await service.SuggestNewRulesAsync(maxSuggestions: 2);

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task SuggestNewRulesAsync_Maps_MatchType_Correctly()
    {
        // Arrange
        var (service, aiService, transactionRepo, ruleRepo, categoryRepo, suggestionRepo) = CreateService();
        SetupUncategorizedTransactions(transactionRepo, "TX1");
        SetupCategories(categoryRepo, ("Groceries", GroceryCategoryId));
        SetupExistingRules(ruleRepo);
        SetupAiAvailable(aiService);

        suggestionRepo
            .Setup(r => r.ExistsPendingWithPatternAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var aiResponse = """
            {
              "suggestions": [
                { "pattern": "^AMAZON.*", "matchType": "Regex", "categoryName": "Groceries", "confidence": 0.85, "reasoning": "R", "sampleMatches": [] }
              ]
            }
            """;

        aiService
            .Setup(s => s.CompleteAsync(It.IsAny<AiPrompt>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiResponse(true, aiResponse, null, 100, TimeSpan.FromSeconds(1)));

        // Act
        var result = await service.SuggestNewRulesAsync();

        // Assert
        Assert.Single(result);
        Assert.Equal(RuleMatchType.Regex, result[0].SuggestedMatchType);
    }

    [Fact]
    public async Task GetPendingSuggestionsAsync_Returns_All_Pending_When_No_Filter()
    {
        // Arrange
        var (service, _, _, _, _, suggestionRepo) = CreateService();
        var suggestions = new List<RuleSuggestion>
        {
            CreateNewRuleSuggestion("WALMART"),
            CreateNewRuleSuggestion("AMAZON"),
        };

        suggestionRepo
            .Setup(r => r.GetPendingAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(suggestions);

        // Act
        var result = await service.GetPendingSuggestionsAsync();

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetPendingSuggestionsAsync_Filters_By_Type()
    {
        // Arrange
        var (service, _, _, _, _, suggestionRepo) = CreateService();
        var suggestions = new List<RuleSuggestion>
        {
            CreateNewRuleSuggestion("WALMART"),
        };

        suggestionRepo
            .Setup(r => r.GetPendingByTypeAsync(SuggestionType.NewRule, It.IsAny<CancellationToken>()))
            .ReturnsAsync(suggestions);

        // Act
        var result = await service.GetPendingSuggestionsAsync(typeFilter: SuggestionType.NewRule);

        // Assert
        Assert.Single(result);
        suggestionRepo.Verify(r => r.GetPendingByTypeAsync(SuggestionType.NewRule, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AcceptSuggestionAsync_Creates_Rule_From_Suggestion()
    {
        // Arrange
        var (service, _, _, ruleRepo, _, suggestionRepo) = CreateService();
        var suggestion = CreateNewRuleSuggestion("WALMART");

        suggestionRepo
            .Setup(r => r.GetByIdAsync(suggestion.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(suggestion);

        CategorizationRule? capturedRule = null;
        ruleRepo
            .Setup(r => r.AddAsync(It.IsAny<CategorizationRule>(), It.IsAny<CancellationToken>()))
            .Callback<CategorizationRule, CancellationToken>((r, _) => capturedRule = r)
            .Returns(Task.CompletedTask);
        ruleRepo
            .Setup(r => r.GetNextPriorityAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(100);

        // Act
        var result = await service.AcceptSuggestionAsync(suggestion.Id);

        // Assert
        Assert.NotNull(capturedRule);
        Assert.Equal("WALMART", capturedRule!.Pattern);
        Assert.Equal(RuleMatchType.Contains, capturedRule.MatchType);
        Assert.Equal(GroceryCategoryId, capturedRule.CategoryId);
    }

    [Fact]
    public async Task AcceptSuggestionAsync_Marks_Suggestion_As_Accepted()
    {
        // Arrange
        var (service, _, _, ruleRepo, _, suggestionRepo) = CreateService();
        var suggestion = CreateNewRuleSuggestion("WALMART");

        suggestionRepo
            .Setup(r => r.GetByIdAsync(suggestion.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(suggestion);
        ruleRepo
            .Setup(r => r.GetNextPriorityAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(100);

        // Act
        await service.AcceptSuggestionAsync(suggestion.Id);

        // Assert
        Assert.Equal(SuggestionStatus.Accepted, suggestion.Status);
    }

    [Fact]
    public async Task AcceptSuggestionAsync_Throws_When_Suggestion_Not_Found()
    {
        // Arrange
        var (service, _, _, _, _, suggestionRepo) = CreateService();
        suggestionRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RuleSuggestion?)null);

        // Act & Assert
        await Assert.ThrowsAsync<DomainException>(() => service.AcceptSuggestionAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task DismissSuggestionAsync_Marks_Suggestion_As_Dismissed()
    {
        // Arrange
        var (service, _, _, _, _, suggestionRepo) = CreateService();
        var suggestion = CreateNewRuleSuggestion("WALMART");

        suggestionRepo
            .Setup(r => r.GetByIdAsync(suggestion.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(suggestion);

        // Act
        await service.DismissSuggestionAsync(suggestion.Id, "Not needed");

        // Assert
        Assert.Equal(SuggestionStatus.Dismissed, suggestion.Status);
        Assert.Equal("Not needed", suggestion.DismissalReason);
    }

    [Fact]
    public async Task ProvideFeedbackAsync_Records_Positive_Feedback()
    {
        // Arrange
        var (service, _, _, _, _, suggestionRepo) = CreateService();
        var suggestion = CreateNewRuleSuggestion("WALMART");
        suggestion.Accept(); // Feedback typically on reviewed suggestions

        suggestionRepo
            .Setup(r => r.GetByIdAsync(suggestion.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(suggestion);

        // Act
        await service.ProvideFeedbackAsync(suggestion.Id, isPositive: true);

        // Assert
        Assert.True(suggestion.UserFeedbackPositive);
    }

    [Fact]
    public async Task SuggestOptimizationsAsync_Returns_Empty_When_No_Rules()
    {
        // Arrange
        var (service, _, _, ruleRepo, _, _) = CreateService();
        ruleRepo
            .Setup(r => r.ListAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CategorizationRule>());

        // Act
        var result = await service.SuggestOptimizationsAsync();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task SuggestOptimizationsAsync_Returns_Empty_When_AI_Not_Available()
    {
        // Arrange
        var (service, aiService, transactionRepo, ruleRepo, categoryRepo, _) = CreateService();
        SetupRulesWithCategories(ruleRepo, categoryRepo, ("Amazon Rule", "AMAZON", GroceryCategoryId));
        SetupAllTransactions(transactionRepo, "AMAZON PURCHASE", "WALMART STORE");
        aiService
            .Setup(s => s.GetStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiServiceStatus(false, null, "Connection refused"));

        // Act
        var result = await service.SuggestOptimizationsAsync();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task SuggestOptimizationsAsync_Detects_Unused_Rules()
    {
        // Arrange
        var (service, aiService, transactionRepo, ruleRepo, categoryRepo, suggestionRepo) = CreateService();
        var unusedRuleId = SetupRulesWithCategories(ruleRepo, categoryRepo, ("Unused Rule", "NONEXISTENT", GroceryCategoryId));
        SetupAllTransactions(transactionRepo, "WALMART STORE", "TARGET STORE");
        SetupAiAvailable(aiService);

        suggestionRepo
            .Setup(r => r.ExistsPendingForRuleAsync(It.IsAny<Guid>(), SuggestionType.UnusedRule, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var aiResponse = """
            {
              "suggestions": [
                {
                  "type": "remove",
                  "targetRuleId": "{RULE_ID}",
                  "reasoning": "Rule 'Unused Rule' has never matched any transactions"
                }
              ]
            }
            """.Replace("{RULE_ID}", unusedRuleId.ToString());

        aiService
            .Setup(s => s.CompleteAsync(It.IsAny<AiPrompt>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiResponse(true, aiResponse, null, 200, TimeSpan.FromSeconds(2)));

        // Act
        var result = await service.SuggestOptimizationsAsync();

        // Assert
        Assert.Single(result);
        Assert.Equal(SuggestionType.UnusedRule, result[0].Type);
    }

    [Fact]
    public async Task SuggestOptimizationsAsync_Suggests_Pattern_Simplification()
    {
        // Arrange
        var (service, aiService, transactionRepo, ruleRepo, categoryRepo, suggestionRepo) = CreateService();
        var ruleId = SetupRulesWithCategories(ruleRepo, categoryRepo, ("Complex Amazon Rule", "AMAZON\\.COM|AMZN|AMAZON PRIME", GroceryCategoryId));
        SetupAllTransactions(transactionRepo, "AMAZON.COM PURCHASE", "AMZN MARKETPLACE");
        SetupAiAvailable(aiService);

        suggestionRepo
            .Setup(r => r.ExistsPendingForRuleAsync(It.IsAny<Guid>(), SuggestionType.PatternOptimization, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var aiResponse = """
            {
              "suggestions": [
                {
                  "type": "simplify",
                  "targetRuleId": "{RULE_ID}",
                  "suggestedPattern": "AMAZON",
                  "reasoning": "Pattern can be simplified to 'AMAZON' which would match all relevant transactions"
                }
              ]
            }
            """.Replace("{RULE_ID}", ruleId.ToString());

        aiService
            .Setup(s => s.CompleteAsync(It.IsAny<AiPrompt>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiResponse(true, aiResponse, null, 200, TimeSpan.FromSeconds(2)));

        // Act
        var result = await service.SuggestOptimizationsAsync();

        // Assert
        Assert.Single(result);
        Assert.Equal(SuggestionType.PatternOptimization, result[0].Type);
        Assert.Equal("AMAZON", result[0].OptimizedPattern);
    }

    [Fact]
    public async Task SuggestOptimizationsAsync_Suggests_Rule_Consolidation()
    {
        // Arrange
        var (service, aiService, transactionRepo, ruleRepo, categoryRepo, suggestionRepo) = CreateService();
        var ruleIds = SetupMultipleRulesWithCategories(
            ruleRepo,
            categoryRepo,
            ("Amazon Rule 1", "AMAZON.COM", GroceryCategoryId),
            ("Amazon Rule 2", "AMZN", GroceryCategoryId),
            ("Amazon Rule 3", "AMAZON PRIME", GroceryCategoryId));
        SetupAllTransactions(transactionRepo, "AMAZON.COM PURCHASE", "AMZN MARKETPLACE", "AMAZON PRIME VIDEO");
        SetupAiAvailable(aiService);

        suggestionRepo
            .Setup(r => r.ExistsPendingForRulesAsync(It.IsAny<IReadOnlyList<Guid>>(), SuggestionType.RuleConsolidation, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var aiResponse = """
            {
              "suggestions": [
                {
                  "type": "consolidate",
                  "targetRuleIds": ["{RULE1}", "{RULE2}", "{RULE3}"],
                  "suggestedPattern": "AMAZON|AMZN",
                  "reasoning": "These three rules all target Amazon transactions and can be merged"
                }
              ]
            }
            """
            .Replace("{RULE1}", ruleIds[0].ToString())
            .Replace("{RULE2}", ruleIds[1].ToString())
            .Replace("{RULE3}", ruleIds[2].ToString());

        aiService
            .Setup(s => s.CompleteAsync(It.IsAny<AiPrompt>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiResponse(true, aiResponse, null, 200, TimeSpan.FromSeconds(2)));

        // Act
        var result = await service.SuggestOptimizationsAsync();

        // Assert
        Assert.Single(result);
        Assert.Equal(SuggestionType.RuleConsolidation, result[0].Type);
        Assert.Equal(3, result[0].ConflictingRuleIds.Count);
    }

    [Fact]
    public async Task SuggestOptimizationsAsync_Persists_Suggestions_To_Repository()
    {
        // Arrange
        var (service, aiService, transactionRepo, ruleRepo, categoryRepo, suggestionRepo) = CreateService();
        var ruleId = SetupRulesWithCategories(ruleRepo, categoryRepo, ("Test Rule", "PATTERN", GroceryCategoryId));
        SetupAllTransactions(transactionRepo);
        SetupAiAvailable(aiService);

        suggestionRepo
            .Setup(r => r.ExistsPendingForRuleAsync(It.IsAny<Guid>(), It.IsAny<SuggestionType>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var aiResponse = """
            {
              "suggestions": [
                {
                  "type": "remove",
                  "targetRuleId": "{RULE_ID}",
                  "reasoning": "Unused rule"
                }
              ]
            }
            """.Replace("{RULE_ID}", ruleId.ToString());

        aiService
            .Setup(s => s.CompleteAsync(It.IsAny<AiPrompt>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiResponse(true, aiResponse, null, 100, TimeSpan.FromSeconds(1)));

        // Act
        await service.SuggestOptimizationsAsync();

        // Assert
        suggestionRepo.Verify(
            r => r.AddRangeAsync(
                It.Is<IEnumerable<RuleSuggestion>>(s => s.Any()),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DetectConflictsAsync_Returns_Empty_When_No_Rules()
    {
        // Arrange
        var (service, _, _, ruleRepo, _, _) = CreateService();
        ruleRepo
            .Setup(r => r.ListAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CategorizationRule>());

        // Act
        var result = await service.DetectConflictsAsync();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task DetectConflictsAsync_Returns_Empty_When_Single_Rule()
    {
        // Arrange
        var (service, _, _, ruleRepo, categoryRepo, _) = CreateService();
        SetupRulesWithCategories(ruleRepo, categoryRepo, ("Single Rule", "PATTERN", GroceryCategoryId));

        // Act
        var result = await service.DetectConflictsAsync();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task DetectConflictsAsync_Detects_Overlapping_Rules()
    {
        // Arrange
        var (service, aiService, _, ruleRepo, categoryRepo, suggestionRepo) = CreateService();
        var ruleIds = SetupMultipleRulesWithCategories(
            ruleRepo,
            categoryRepo,
            ("Amazon Rule", "AMAZON", GroceryCategoryId),
            ("Amazon Prime Rule", "AMAZON PRIME", RestaurantCategoryId));
        SetupAiAvailable(aiService);

        suggestionRepo
            .Setup(r => r.ExistsPendingForRulesAsync(It.IsAny<IReadOnlyList<Guid>>(), SuggestionType.RuleConflict, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var aiResponse = """
            {
              "conflicts": [
                {
                  "ruleIds": ["{RULE1}", "{RULE2}"],
                  "conflictType": "overlap",
                  "description": "Both rules match 'AMAZON PRIME' transactions",
                  "resolution": "Adjust priorities or make patterns more specific"
                }
              ]
            }
            """
            .Replace("{RULE1}", ruleIds[0].ToString())
            .Replace("{RULE2}", ruleIds[1].ToString());

        aiService
            .Setup(s => s.CompleteAsync(It.IsAny<AiPrompt>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiResponse(true, aiResponse, null, 200, TimeSpan.FromSeconds(2)));

        // Act
        var result = await service.DetectConflictsAsync();

        // Assert
        Assert.Single(result);
        Assert.Equal(SuggestionType.RuleConflict, result[0].Type);
        Assert.Equal(2, result[0].ConflictingRuleIds.Count);
    }

    [Fact]
    public async Task DetectConflictsAsync_Detects_Contradictory_Rules()
    {
        // Arrange
        var (service, aiService, _, ruleRepo, categoryRepo, suggestionRepo) = CreateService();
        var ruleIds = SetupMultipleRulesWithCategories(
            ruleRepo,
            categoryRepo,
            ("Amazon Groceries", "AMAZON", GroceryCategoryId),
            ("Amazon Shopping", "AMAZON", RestaurantCategoryId));
        SetupAiAvailable(aiService);

        suggestionRepo
            .Setup(r => r.ExistsPendingForRulesAsync(It.IsAny<IReadOnlyList<Guid>>(), SuggestionType.RuleConflict, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var aiResponse = """
            {
              "conflicts": [
                {
                  "ruleIds": ["{RULE1}", "{RULE2}"],
                  "conflictType": "contradiction",
                  "description": "Same pattern 'AMAZON' maps to different categories",
                  "resolution": "Remove duplicate or use more specific patterns"
                }
              ]
            }
            """
            .Replace("{RULE1}", ruleIds[0].ToString())
            .Replace("{RULE2}", ruleIds[1].ToString());

        aiService
            .Setup(s => s.CompleteAsync(It.IsAny<AiPrompt>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiResponse(true, aiResponse, null, 200, TimeSpan.FromSeconds(2)));

        // Act
        var result = await service.DetectConflictsAsync();

        // Assert
        Assert.Single(result);
        Assert.Equal(SuggestionType.RuleConflict, result[0].Type);
    }

    [Fact]
    public async Task DetectConflictsAsync_Persists_Conflict_Suggestions()
    {
        // Arrange
        var (service, aiService, _, ruleRepo, categoryRepo, suggestionRepo) = CreateService();
        var ruleIds = SetupMultipleRulesWithCategories(
            ruleRepo,
            categoryRepo,
            ("Rule A", "PATTERN", GroceryCategoryId),
            ("Rule B", "PATTERN", RestaurantCategoryId));
        SetupAiAvailable(aiService);

        suggestionRepo
            .Setup(r => r.ExistsPendingForRulesAsync(It.IsAny<IReadOnlyList<Guid>>(), SuggestionType.RuleConflict, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var aiResponse = """
            {
              "conflicts": [
                {
                  "ruleIds": ["{RULE1}", "{RULE2}"],
                  "conflictType": "contradiction",
                  "description": "Conflict detected",
                  "resolution": "Fix it"
                }
              ]
            }
            """
            .Replace("{RULE1}", ruleIds[0].ToString())
            .Replace("{RULE2}", ruleIds[1].ToString());

        aiService
            .Setup(s => s.CompleteAsync(It.IsAny<AiPrompt>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiResponse(true, aiResponse, null, 100, TimeSpan.FromSeconds(1)));

        // Act
        await service.DetectConflictsAsync();

        // Assert
        suggestionRepo.Verify(
            r => r.AddRangeAsync(
                It.Is<IEnumerable<RuleSuggestion>>(s => s.Any()),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AnalyzeAllAsync_Runs_All_Analysis_Types()
    {
        // Arrange
        var (service, aiService, transactionRepo, ruleRepo, categoryRepo, suggestionRepo) = CreateService();
        SetupUncategorizedTransactions(transactionRepo, "TX1", "TX2");
        SetupRulesWithCategories(ruleRepo, categoryRepo, ("Rule1", "PATTERN", GroceryCategoryId));
        SetupAllTransactions(transactionRepo, "TX1", "TX2");
        SetupAiAvailable(aiService);

        suggestionRepo
            .Setup(r => r.ExistsPendingWithPatternAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        suggestionRepo
            .Setup(r => r.ExistsPendingForRuleAsync(It.IsAny<Guid>(), It.IsAny<SuggestionType>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        suggestionRepo
            .Setup(r => r.ExistsPendingForRulesAsync(It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<SuggestionType>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        aiService
            .Setup(s => s.CompleteAsync(It.IsAny<AiPrompt>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiResponse(true, "{\"suggestions\": []}", null, 100, TimeSpan.FromSeconds(1)));

        // Act
        var result = await service.AnalyzeAllAsync();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.AnalysisDuration > TimeSpan.Zero);
    }

    [Fact]
    public async Task AnalyzeAllAsync_Reports_Progress()
    {
        // Arrange
        var (service, aiService, transactionRepo, ruleRepo, categoryRepo, suggestionRepo) = CreateService();
        SetupUncategorizedTransactions(transactionRepo, "TX1");
        SetupRulesWithCategories(ruleRepo, categoryRepo, ("Rule1", "PATTERN", GroceryCategoryId));
        SetupAllTransactions(transactionRepo, "TX1");
        SetupAiAvailable(aiService);

        suggestionRepo
            .Setup(r => r.ExistsPendingWithPatternAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        suggestionRepo
            .Setup(r => r.ExistsPendingForRuleAsync(It.IsAny<Guid>(), It.IsAny<SuggestionType>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        suggestionRepo
            .Setup(r => r.ExistsPendingForRulesAsync(It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<SuggestionType>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        aiService
            .Setup(s => s.CompleteAsync(It.IsAny<AiPrompt>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiResponse(true, "{\"suggestions\": []}", null, 100, TimeSpan.FromSeconds(1)));

        var progressReports = new List<AnalysisProgress>();
        var progress = new Progress<AnalysisProgress>(p => progressReports.Add(p));

        // Act
        await service.AnalyzeAllAsync(progress);

        // Assert - allow time for progress to be reported
        await Task.Delay(50);
        Assert.True(progressReports.Count >= 3); // At least 3 steps
        Assert.Contains(progressReports, p => p.PercentComplete == 100);
    }

    [Fact]
    public async Task AnalyzeAllAsync_Returns_Counts_From_All_Methods()
    {
        // Arrange
        var (service, aiService, transactionRepo, ruleRepo, categoryRepo, suggestionRepo) = CreateService();
        SetupUncategorizedTransactions(transactionRepo, "WALMART TX1", "WALMART TX2");
        SetupRulesWithCategories(ruleRepo, categoryRepo, ("Test Rule", "TEST", GroceryCategoryId));
        SetupAllTransactions(transactionRepo, "WALMART TX1", "WALMART TX2");
        SetupAiAvailable(aiService);

        suggestionRepo
            .Setup(r => r.ExistsPendingWithPatternAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        suggestionRepo
            .Setup(r => r.ExistsPendingForRuleAsync(It.IsAny<Guid>(), It.IsAny<SuggestionType>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        suggestionRepo
            .Setup(r => r.ExistsPendingForRulesAsync(It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<SuggestionType>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        aiService
            .Setup(s => s.CompleteAsync(It.IsAny<AiPrompt>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiResponse(true, "{\"suggestions\": []}", null, 100, TimeSpan.FromSeconds(1)));

        // Act
        var result = await service.AnalyzeAllAsync();

        // Assert
        Assert.Equal(2, result.UncategorizedTransactionsAnalyzed);
        Assert.Equal(1, result.RulesAnalyzed);
    }

    [Fact]
    public async Task AnalyzeAllAsync_Skips_NewRule_When_Acceptance_Rate_Below_Threshold()
    {
        // Arrange
        var (service, aiService, transactionRepo, ruleRepo, categoryRepo, suggestionRepo) = CreateService();
        SetupUncategorizedTransactions(transactionRepo, "TX1");
        SetupRulesWithCategories(ruleRepo, categoryRepo, ("Rule1", "PATTERN", GroceryCategoryId));
        SetupAllTransactions(transactionRepo, "TX1");
        SetupAiAvailable(aiService);

        // 10 reviewed, only 1 accepted (10% < 20% threshold)
        suggestionRepo
            .Setup(r => r.GetReviewedCountsByTypeAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<(SuggestionType, SuggestionStatus), int>
            {
                { (SuggestionType.NewRule, SuggestionStatus.Accepted), 1 },
                { (SuggestionType.NewRule, SuggestionStatus.Dismissed), 9 },
            });

        aiService
            .Setup(s => s.CompleteAsync(It.IsAny<AiPrompt>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiResponse(true, "{\"suggestions\": []}", null, 100, TimeSpan.FromSeconds(1)));

        // Act
        var result = await service.AnalyzeAllAsync();

        // Assert — AI should NOT be called for new rules (skipped), but may be called for optimizations/conflicts
        Assert.Empty(result.NewRuleSuggestions);
    }

    [Fact]
    public async Task AnalyzeAllAsync_Does_Not_Skip_When_Insufficient_Review_Data()
    {
        // Arrange
        var (service, aiService, transactionRepo, ruleRepo, categoryRepo, suggestionRepo) = CreateService();
        SetupUncategorizedTransactions(transactionRepo, "TX1");
        SetupRulesWithCategories(ruleRepo, categoryRepo, ("Rule1", "PATTERN", GroceryCategoryId));
        SetupAllTransactions(transactionRepo, "TX1");
        SetupAiAvailable(aiService);

        // Only 3 reviewed (< 5 threshold), so rate evaluation is skipped
        suggestionRepo
            .Setup(r => r.GetReviewedCountsByTypeAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<(SuggestionType, SuggestionStatus), int>
            {
                { (SuggestionType.NewRule, SuggestionStatus.Accepted), 0 },
                { (SuggestionType.NewRule, SuggestionStatus.Dismissed), 3 },
            });

        suggestionRepo
            .Setup(r => r.ExistsPendingWithPatternAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        aiService
            .Setup(s => s.CompleteAsync(It.IsAny<AiPrompt>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiResponse(true, "{\"suggestions\": []}", null, 100, TimeSpan.FromSeconds(1)));

        // Act
        var result = await service.AnalyzeAllAsync();

        // Assert — AI should still be called since not enough data to de-prioritize
        aiService.Verify(s => s.CompleteAsync(It.IsAny<AiPrompt>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task AcceptSuggestionAsync_For_OptimizationSuggestion_Updates_Rule_Pattern()
    {
        // Arrange
        var (service, _, _, ruleRepo, _, suggestionRepo) = CreateService();
        var targetRuleId = Guid.NewGuid();
        var rule = CreateCategorizationRule("Amazon Rule", "AMAZON\\.COM|AMZN", targetRuleId);
        var suggestion = RuleSuggestion.CreateOptimizationSuggestion(
            title: "Simplify Amazon Rule",
            description: "Simplify the pattern",
            reasoning: "Pattern can be simplified",
            confidence: 0.9m,
            targetRuleId: targetRuleId,
            optimizedPattern: "AMAZON");

        suggestionRepo
            .Setup(r => r.GetByIdAsync(suggestion.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(suggestion);
        ruleRepo
            .Setup(r => r.GetByIdAsync(targetRuleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rule);

        // Act
        var result = await service.AcceptSuggestionAsync(suggestion.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("AMAZON", result.Pattern);
        Assert.Equal(SuggestionStatus.Accepted, suggestion.Status);
    }

    [Fact]
    public async Task AcceptSuggestionAsync_For_UnusedRuleSuggestion_Deactivates_Rule()
    {
        // Arrange
        var (service, _, _, ruleRepo, _, suggestionRepo) = CreateService();
        var targetRuleId = Guid.NewGuid();
        var rule = CreateCategorizationRule("Unused Rule", "NONEXISTENT", targetRuleId);
        var suggestion = RuleSuggestion.CreateUnusedRuleSuggestion(
            title: "Remove unused rule",
            description: "This rule never matches",
            reasoning: "Rule has 0 matches",
            targetRuleId: targetRuleId);

        suggestionRepo
            .Setup(r => r.GetByIdAsync(suggestion.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(suggestion);
        ruleRepo
            .Setup(r => r.GetByIdAsync(targetRuleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rule);

        // Act
        var result = await service.AcceptSuggestionAsync(suggestion.Id);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsActive);
        Assert.Equal(SuggestionStatus.Accepted, suggestion.Status);
    }

    [Fact]
    public async Task AcceptSuggestionAsync_Throws_For_ConflictSuggestion()
    {
        // Arrange
        var (service, _, _, _, _, suggestionRepo) = CreateService();
        var suggestion = RuleSuggestion.CreateConflictSuggestion(
            title: "Conflicting rules",
            description: "Rules conflict",
            reasoning: "Same pattern different categories",
            conflictingRuleIds: new List<Guid> { Guid.NewGuid(), Guid.NewGuid() });

        suggestionRepo
            .Setup(r => r.GetByIdAsync(suggestion.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(suggestion);

        // Act & Assert
        await Assert.ThrowsAsync<DomainException>(() => service.AcceptSuggestionAsync(suggestion.Id));
    }

    [Fact]
    public void ExtractJson_Returns_Pure_Json_Unchanged()
    {
        // Arrange
        var json = """{"suggestions": []}""";

        // Act
        var result = RuleSuggestionResponseParser.ExtractJson(json);

        // Assert
        Assert.Equal(json, result);
    }

    [Fact]
    public void ExtractJson_Strips_Markdown_Code_Block_Wrapping()
    {
        // Arrange
        var content = """
            ```json
            {"suggestions": [{"pattern": "WALMART"}]}
            ```
            """;

        // Act
        var result = RuleSuggestionResponseParser.ExtractJson(content);

        // Assert
        Assert.StartsWith("{", result);
        Assert.EndsWith("}", result);
        Assert.Contains("WALMART", result);
    }

    [Fact]
    public void ExtractJson_Strips_Preamble_Text()
    {
        // Arrange
        var content = """
            Here is the JSON response:
            {"suggestions": [{"pattern": "TARGET"}]}
            """;

        // Act
        var result = RuleSuggestionResponseParser.ExtractJson(content);

        // Assert
        Assert.StartsWith("{", result);
        Assert.Contains("TARGET", result);
    }

    [Fact]
    public void ExtractJson_Strips_Trailing_Text()
    {
        // Arrange
        var content = """
            {"suggestions": []}
            Hope this helps!
            """;

        // Act
        var result = RuleSuggestionResponseParser.ExtractJson(content);

        // Assert
        Assert.Equal("""{"suggestions": []}""", result);
    }

    [Fact]
    public void ExtractJson_Throws_JsonException_When_No_Json_Found()
    {
        // Arrange
        var content = "Sorry, I cannot help with that.";

        // Act & Assert
        Assert.Throws<System.Text.Json.JsonException>(() => RuleSuggestionResponseParser.ExtractJson(content));
    }

    [Fact]
    public void ExtractJson_Throws_JsonException_For_Empty_Content()
    {
        // Act & Assert
        Assert.Throws<System.Text.Json.JsonException>(() => RuleSuggestionResponseParser.ExtractJson(string.Empty));
    }

    [Fact]
    public async Task SuggestNewRulesAsync_Parses_Markdown_Wrapped_Json_Response()
    {
        // Arrange
        var (service, aiService, transactionRepo, ruleRepo, categoryRepo, suggestionRepo) = CreateService();
        SetupUncategorizedTransactions(transactionRepo, "WALMART STORE #123", "WALMART SUPERCENTER");
        SetupCategories(categoryRepo, ("Groceries", GroceryCategoryId));
        SetupExistingRules(ruleRepo);
        SetupAiAvailable(aiService);

        suggestionRepo
            .Setup(r => r.ExistsPendingWithPatternAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // AI response wrapped in markdown code block — the exact scenario that caused the bug
        var aiResponse = """
            ```json
            {
              "suggestions": [
                {
                  "pattern": "WALMART",
                  "matchType": "Contains",
                  "categoryName": "Groceries",
                  "confidence": 0.95,
                  "reasoning": "Both contain WALMART",
                  "sampleMatches": ["WALMART STORE #123", "WALMART SUPERCENTER"]
                }
              ]
            }
            ```
            """;

        aiService
            .Setup(s => s.CompleteAsync(It.IsAny<AiPrompt>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiResponse(true, aiResponse, null, 200, TimeSpan.FromSeconds(2)));

        // Act
        var result = await service.SuggestNewRulesAsync();

        // Assert
        Assert.Single(result);
        Assert.Equal("WALMART", result[0].SuggestedPattern);
        Assert.Equal(GroceryCategoryId, result[0].SuggestedCategoryId);
    }

    [Fact]
    public async Task SuggestNewRulesAsync_Parses_Response_With_Preamble_Text()
    {
        // Arrange
        var (service, aiService, transactionRepo, ruleRepo, categoryRepo, suggestionRepo) = CreateService();
        SetupUncategorizedTransactions(transactionRepo, "TARGET STORE #789");
        SetupCategories(categoryRepo, ("Groceries", GroceryCategoryId));
        SetupExistingRules(ruleRepo);
        SetupAiAvailable(aiService);

        suggestionRepo
            .Setup(r => r.ExistsPendingWithPatternAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // AI response with preamble text before JSON
        var aiResponse = """
            Here are my suggestions based on the transaction data:
            {
              "suggestions": [
                {
                  "pattern": "TARGET",
                  "matchType": "Contains",
                  "categoryName": "Groceries",
                  "confidence": 0.90,
                  "reasoning": "Target is a grocery store",
                  "sampleMatches": ["TARGET STORE #789"]
                }
              ]
            }
            """;

        aiService
            .Setup(s => s.CompleteAsync(It.IsAny<AiPrompt>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiResponse(true, aiResponse, null, 200, TimeSpan.FromSeconds(2)));

        // Act
        var result = await service.SuggestNewRulesAsync();

        // Assert
        Assert.Single(result);
        Assert.Equal("TARGET", result[0].SuggestedPattern);
    }

    [Fact]
    public async Task MapSuggestionsToDtosAsync_Returns_Dtos_With_CategoryAndRuleNames()
    {
        // Arrange
        var (service, _, _, ruleRepo, categoryRepo, _) = CreateService();
        var categoryId = GroceryCategoryId;
        var ruleId = Guid.NewGuid();

        SetupCategories(categoryRepo, ("Groceries", categoryId));

        var rule = CategorizationRule.Create("Amazon Rule", RuleMatchType.Contains, "AMAZON", categoryId);
        typeof(CategorizationRule).GetProperty("Id")!.SetValue(rule, ruleId);
        ruleRepo.Setup(r => r.ListAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CategorizationRule> { rule });

        var suggestion = RuleSuggestion.CreateNewRuleSuggestion(
            title: "New rule",
            description: "Desc",
            reasoning: "Reason",
            confidence: 0.9m,
            suggestedPattern: "WALMART",
            suggestedMatchType: RuleMatchType.Contains,
            suggestedCategoryId: categoryId,
            affectedTransactionCount: 5,
            sampleDescriptions: new List<string> { "WALMART #123" });
        typeof(RuleSuggestion).GetProperty("TargetRuleId")!.SetValue(suggestion, ruleId);

        // Act
        var result = await service.MapSuggestionsToDtosAsync(new List<RuleSuggestion> { suggestion });

        // Assert
        Assert.Single(result);
        Assert.Equal("Groceries", result[0].SuggestedCategoryName);
        Assert.Equal("Amazon Rule", result[0].TargetRuleName);
    }

    [Fact]
    public async Task MapSuggestionsToDtosAsync_Returns_Null_Names_When_IdsNotFound()
    {
        // Arrange
        var (service, _, _, ruleRepo, categoryRepo, _) = CreateService();

        categoryRepo.Setup(r => r.ListAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BudgetCategory>());
        ruleRepo.Setup(r => r.ListAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CategorizationRule>());

        var suggestion = RuleSuggestion.CreateNewRuleSuggestion(
            title: "New rule",
            description: "Desc",
            reasoning: "Reason",
            confidence: 0.9m,
            suggestedPattern: "WALMART",
            suggestedMatchType: RuleMatchType.Contains,
            suggestedCategoryId: Guid.NewGuid(),
            affectedTransactionCount: 5,
            sampleDescriptions: new List<string> { "WALMART #123" });
        typeof(RuleSuggestion).GetProperty("TargetRuleId")!.SetValue(suggestion, Guid.NewGuid());

        // Act
        var result = await service.MapSuggestionsToDtosAsync(new List<RuleSuggestion> { suggestion });

        // Assert
        Assert.Single(result);
        Assert.Null(result[0].SuggestedCategoryName);
        Assert.Null(result[0].TargetRuleName);
    }

    [Fact]
    public async Task MapSuggestionsToDtosAsync_Returns_Empty_For_EmptyInput()
    {
        // Arrange
        var (service, _, _, _, categoryRepo, _) = CreateService();
        categoryRepo.Setup(r => r.ListAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BudgetCategory>());

        // Act
        var result = await service.MapSuggestionsToDtosAsync(new List<RuleSuggestion>());

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task MapSuggestionToDtoAsync_Returns_Dto_With_CategoryName()
    {
        // Arrange
        var (service, _, _, _, categoryRepo, _) = CreateService();
        var categoryId = GroceryCategoryId;

        var category = BudgetCategory.Create("Groceries", CategoryType.Expense);
        typeof(BudgetCategory).GetProperty("Id")!.SetValue(category, categoryId);
        categoryRepo.Setup(r => r.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        var suggestion = RuleSuggestion.CreateNewRuleSuggestion(
            title: "New rule",
            description: "Desc",
            reasoning: "Reason",
            confidence: 0.9m,
            suggestedPattern: "WALMART",
            suggestedMatchType: RuleMatchType.Contains,
            suggestedCategoryId: categoryId,
            affectedTransactionCount: 5,
            sampleDescriptions: new List<string> { "WALMART #123" });

        // Act
        var result = await service.MapSuggestionToDtoAsync(suggestion);

        // Assert
        Assert.Equal("Groceries", result.SuggestedCategoryName);
    }

    [Fact]
    public async Task MapSuggestionToDtoAsync_Returns_Dto_With_RuleName()
    {
        // Arrange
        var (service, _, _, ruleRepo, _, _) = CreateService();
        var ruleId = Guid.NewGuid();

        var rule = CategorizationRule.Create("Amazon Rule", RuleMatchType.Contains, "AMAZON", GroceryCategoryId);
        typeof(CategorizationRule).GetProperty("Id")!.SetValue(rule, ruleId);
        ruleRepo.Setup(r => r.GetByIdAsync(ruleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rule);

        var suggestion = RuleSuggestion.CreateNewRuleSuggestion(
            title: "Optimization",
            description: "Desc",
            reasoning: "Reason",
            confidence: 0.85m,
            suggestedPattern: "AMAZON",
            suggestedMatchType: RuleMatchType.Contains,
            suggestedCategoryId: GroceryCategoryId,
            affectedTransactionCount: 3,
            sampleDescriptions: new List<string> { "AMAZON #456" });
        typeof(RuleSuggestion).GetProperty("TargetRuleId")!.SetValue(suggestion, ruleId);

        // Act
        var result = await service.MapSuggestionToDtoAsync(suggestion);

        // Assert
        Assert.Equal("Amazon Rule", result.TargetRuleName);
    }

    [Fact]
    public async Task MapSuggestionToDtoAsync_Returns_Null_Names_When_NoIds()
    {
        // Arrange
        var (service, _, _, _, _, _) = CreateService();

        var suggestion = RuleSuggestion.CreateNewRuleSuggestion(
            title: "New rule",
            description: "Desc",
            reasoning: "Reason",
            confidence: 0.9m,
            suggestedPattern: "TEST",
            suggestedMatchType: RuleMatchType.Contains,
            suggestedCategoryId: GroceryCategoryId,
            affectedTransactionCount: 1,
            sampleDescriptions: new List<string> { "TEST" });

        // Clear the category ID via reflection to simulate no IDs
        typeof(RuleSuggestion).GetProperty("SuggestedCategoryId")!.SetValue(suggestion, (Guid?)null);

        // Act
        var result = await service.MapSuggestionToDtoAsync(suggestion);

        // Assert
        Assert.Null(result.SuggestedCategoryName);
        Assert.Null(result.TargetRuleName);
    }

    private static (
        RuleSuggestionService Service,
        Mock<IAiService> AiService,
        Mock<ITransactionRepository> TransactionRepo,
        Mock<ICategorizationRuleRepository> RuleRepo,
        Mock<IBudgetCategoryRepository> CategoryRepo,
        Mock<IRuleSuggestionRepository> SuggestionRepo) CreateService()
    {
        var aiService = new Mock<IAiService>();
        var transactionRepo = new Mock<ITransactionRepository>();
        var ruleRepo = new Mock<ICategorizationRuleRepository>();
        var categoryRepo = new Mock<IBudgetCategoryRepository>();
        var suggestionRepo = new Mock<IRuleSuggestionRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        // Default: empty existing rules
        ruleRepo
            .Setup(r => r.ListAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CategorizationRule>());

        // Default: no feedback history
        suggestionRepo
            .Setup(r => r.GetDismissedNewRulePatternsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());
        suggestionRepo
            .Setup(r => r.GetAcceptedNewRulesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<(string, Guid)>());
        suggestionRepo
            .Setup(r => r.GetReviewedCountsByTypeAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<(SuggestionType, SuggestionStatus), int>());

        var responseParser = new RuleSuggestionResponseParser(suggestionRepo.Object);
        var acceptanceHandler = new SuggestionAcceptanceHandler(
            suggestionRepo.Object,
            ruleRepo.Object,
            unitOfWork.Object);

        var service = new RuleSuggestionService(
            aiService.Object,
            transactionRepo.Object,
            ruleRepo.Object,
            categoryRepo.Object,
            suggestionRepo.Object,
            unitOfWork.Object,
            responseParser,
            acceptanceHandler);

        return (service, aiService, transactionRepo, ruleRepo, categoryRepo, suggestionRepo);
    }

    private static void SetupUncategorizedTransactions(
        Mock<ITransactionRepository> repo,
        params string[] descriptions)
    {
        var accountId = Guid.NewGuid();
        var transactions = descriptions.Select(d =>
            TransactionFactory.Create(accountId, MoneyValue.Create("USD", -50.00m), DateOnly.FromDateTime(DateTime.UtcNow), d))
            .ToList();

        repo.Setup(r => r.GetUncategorizedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactions);
    }

    private static void SetupCategories(
        Mock<IBudgetCategoryRepository> repo,
        params (string Name, Guid Id)[] categories)
    {
        var categoryList = categories.Select(c =>
        {
            var category = BudgetCategory.Create(c.Name, CategoryType.Expense);

            // Use reflection to set ID since it's normally set by the factory
            typeof(BudgetCategory)
                .GetProperty("Id")!
                .SetValue(category, c.Id);
            return category;
        }).ToList();

        repo.Setup(r => r.ListAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(categoryList);
    }

    private static void SetupExistingRules(Mock<ICategorizationRuleRepository> repo)
    {
        repo.Setup(r => r.ListAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CategorizationRule>());
    }

    private static void SetupAiAvailable(Mock<IAiService> aiService)
    {
        aiService
            .Setup(s => s.GetStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiServiceStatus(true, "llama3.2", null));
    }

    private static RuleSuggestion CreateNewRuleSuggestion(string pattern)
    {
        return RuleSuggestion.CreateNewRuleSuggestion(
            title: $"Create rule for {pattern}",
            description: $"Create a rule to match {pattern} transactions",
            reasoning: $"Found transactions matching {pattern}",
            confidence: 0.9m,
            suggestedPattern: pattern,
            suggestedMatchType: RuleMatchType.Contains,
            suggestedCategoryId: GroceryCategoryId,
            affectedTransactionCount: 5,
            sampleDescriptions: new List<string> { $"{pattern} STORE #123" });
    }

    private static Guid SetupRulesWithCategories(
        Mock<ICategorizationRuleRepository> ruleRepo,
        Mock<IBudgetCategoryRepository> categoryRepo,
        params (string Name, string Pattern, Guid CategoryId)[] rules)
    {
        var ruleList = new List<CategorizationRule>();
        Guid firstRuleId = Guid.Empty;

        foreach (var (name, pattern, categoryId) in rules)
        {
            var rule = CategorizationRule.Create(name, RuleMatchType.Contains, pattern, categoryId);
            ruleList.Add(rule);
            if (firstRuleId == Guid.Empty)
            {
                firstRuleId = rule.Id;
            }
        }

        ruleRepo.Setup(r => r.ListAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ruleList);

        var categoryIds = rules.Select(r => r.CategoryId).Distinct();
        var categoryList = categoryIds.Select(id =>
        {
            var category = BudgetCategory.Create($"Category_{id}", CategoryType.Expense);
            typeof(BudgetCategory).GetProperty("Id")!.SetValue(category, id);
            return category;
        }).ToList();

        categoryRepo.Setup(r => r.ListAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(categoryList);

        return firstRuleId;
    }

    private static IReadOnlyList<Guid> SetupMultipleRulesWithCategories(
        Mock<ICategorizationRuleRepository> ruleRepo,
        Mock<IBudgetCategoryRepository> categoryRepo,
        params (string Name, string Pattern, Guid CategoryId)[] rules)
    {
        var ruleList = new List<CategorizationRule>();
        var ruleIds = new List<Guid>();

        foreach (var (name, pattern, categoryId) in rules)
        {
            var rule = CategorizationRule.Create(name, RuleMatchType.Contains, pattern, categoryId);
            ruleList.Add(rule);
            ruleIds.Add(rule.Id);
        }

        ruleRepo.Setup(r => r.ListAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ruleList);

        var categoryIds = rules.Select(r => r.CategoryId).Distinct();
        var categoryList = categoryIds.Select(id =>
        {
            var category = BudgetCategory.Create($"Category_{id}", CategoryType.Expense);
            typeof(BudgetCategory).GetProperty("Id")!.SetValue(category, id);
            return category;
        }).ToList();

        categoryRepo.Setup(r => r.ListAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(categoryList);

        return ruleIds;
    }

    private static void SetupAllTransactions(
        Mock<ITransactionRepository> repo,
        params string[] descriptions)
    {
        var accountId = Guid.NewGuid();
        var transactions = descriptions.Select(d =>
            TransactionFactory.Create(accountId, MoneyValue.Create("USD", -50.00m), DateOnly.FromDateTime(DateTime.UtcNow), d))
            .ToList();

        repo.Setup(r => r.GetAllDescriptionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(descriptions.ToList());
    }

    private static CategorizationRule CreateCategorizationRule(string name, string pattern, Guid ruleId)
    {
        var rule = CategorizationRule.Create(name, RuleMatchType.Contains, pattern, GroceryCategoryId);
        typeof(CategorizationRule).GetProperty("Id")!.SetValue(rule, ruleId);
        return rule;
    }
}
