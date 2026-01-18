// <copyright file="RuleSuggestionServiceTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Application.Services;
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

    #region SuggestNewRulesAsync Tests

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

    #endregion

    #region GetPendingSuggestionsAsync Tests

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

    #endregion

    #region AcceptSuggestionAsync Tests

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

    #endregion

    #region DismissSuggestionAsync Tests

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

    #endregion

    #region ProvideFeedbackAsync Tests

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

    #endregion

    #region Helper Methods

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

        var service = new RuleSuggestionService(
            aiService.Object,
            transactionRepo.Object,
            ruleRepo.Object,
            categoryRepo.Object,
            suggestionRepo.Object,
            unitOfWork.Object);

        return (service, aiService, transactionRepo, ruleRepo, categoryRepo, suggestionRepo);
    }

    private static void SetupUncategorizedTransactions(
        Mock<ITransactionRepository> repo,
        params string[] descriptions)
    {
        var accountId = Guid.NewGuid();
        var transactions = descriptions.Select(d =>
            Transaction.Create(accountId, MoneyValue.Create("USD", -50.00m), DateOnly.FromDateTime(DateTime.UtcNow), d))
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

    #endregion
}
