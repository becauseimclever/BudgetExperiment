// <copyright file="SuggestionAcceptanceHandlerTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Application.Categorization;
using BudgetExperiment.Domain;

using Moq;

namespace BudgetExperiment.Application.Tests.Services;

/// <summary>
/// Unit tests for <see cref="SuggestionAcceptanceHandler"/>.
/// </summary>
public class SuggestionAcceptanceHandlerTests
{
    private static readonly Guid GroceryCategoryId = Guid.NewGuid();

    private readonly Mock<IRuleSuggestionRepository> _suggestionRepo;
    private readonly Mock<ICategorizationRuleRepository> _ruleRepo;
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly Mock<IRuleConsolidationService> _consolidationService;
    private readonly SuggestionAcceptanceHandler _handler;

    public SuggestionAcceptanceHandlerTests()
    {
        _suggestionRepo = new Mock<IRuleSuggestionRepository>();
        _ruleRepo = new Mock<ICategorizationRuleRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _consolidationService = new Mock<IRuleConsolidationService>();
        _handler = new SuggestionAcceptanceHandler(
            _suggestionRepo.Object,
            _ruleRepo.Object,
            _unitOfWork.Object,
            _consolidationService.Object);
    }

    [Fact]
    public async Task AcceptSuggestionAsync_NewRule_CreatesRule()
    {
        // Arrange
        var suggestion = RuleSuggestion.CreateNewRuleSuggestion(
            title: "Create rule for WALMART",
            description: "Walmart is a grocery store",
            reasoning: "Pattern found",
            confidence: 0.9m,
            suggestedPattern: "WALMART",
            suggestedMatchType: RuleMatchType.Contains,
            suggestedCategoryId: GroceryCategoryId,
            affectedTransactionCount: 5,
            sampleDescriptions: new List<string> { "WALMART STORE #123" });

        _suggestionRepo
            .Setup(r => r.GetByIdAsync(suggestion.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(suggestion);
        _ruleRepo
            .Setup(r => r.GetNextPriorityAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(100);

        // Act
        var rule = await _handler.AcceptSuggestionAsync(suggestion.Id);

        // Assert
        Assert.Equal("WALMART", rule.Pattern);
        Assert.Equal(RuleMatchType.Contains, rule.MatchType);
        Assert.Equal(GroceryCategoryId, rule.CategoryId);
        _ruleRepo.Verify(r => r.AddAsync(It.IsAny<CategorizationRule>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AcceptSuggestionAsync_PatternOptimization_UpdatesRule()
    {
        // Arrange
        var ruleId = Guid.NewGuid();
        var rule = CategorizationRule.Create("Old Rule", RuleMatchType.Contains, "OLD.*PATTERN", GroceryCategoryId);
        typeof(CategorizationRule).GetProperty("Id")!.SetValue(rule, ruleId);

        var suggestion = RuleSuggestion.CreateOptimizationSuggestion(
            title: "Optimize pattern",
            description: "Simplify pattern",
            reasoning: "Pattern can be simplified",
            confidence: 0.8m,
            targetRuleId: ruleId,
            optimizedPattern: "OLD");

        _suggestionRepo
            .Setup(r => r.GetByIdAsync(suggestion.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(suggestion);
        _ruleRepo
            .Setup(r => r.GetByIdAsync(ruleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rule);

        // Act
        var result = await _handler.AcceptSuggestionAsync(suggestion.Id);

        // Assert
        Assert.Equal("OLD", result.Pattern);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AcceptSuggestionAsync_UnusedRule_DeactivatesRule()
    {
        // Arrange
        var ruleId = Guid.NewGuid();
        var rule = CategorizationRule.Create("Unused Rule", RuleMatchType.Contains, "UNUSED", GroceryCategoryId);
        typeof(CategorizationRule).GetProperty("Id")!.SetValue(rule, ruleId);

        var suggestion = RuleSuggestion.CreateUnusedRuleSuggestion(
            title: "Remove unused rule",
            description: "Rule never matched",
            reasoning: "0 matches",
            targetRuleId: ruleId);

        _suggestionRepo
            .Setup(r => r.GetByIdAsync(suggestion.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(suggestion);
        _ruleRepo
            .Setup(r => r.GetByIdAsync(ruleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rule);

        // Act
        var result = await _handler.AcceptSuggestionAsync(suggestion.Id);

        // Assert
        Assert.False(result.IsActive);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AcceptSuggestionAsync_NotFound_ThrowsDomainException()
    {
        // Arrange
        var id = Guid.NewGuid();
        _suggestionRepo
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RuleSuggestion?)null);

        // Act & Assert
        await Assert.ThrowsAsync<DomainException>(() => _handler.AcceptSuggestionAsync(id));
    }

    [Fact]
    public async Task DismissSuggestionAsync_DismissesSuggestion()
    {
        // Arrange
        var suggestion = RuleSuggestion.CreateNewRuleSuggestion(
            title: "Test",
            description: "Test",
            reasoning: "Test",
            confidence: 0.9m,
            suggestedPattern: "TEST",
            suggestedMatchType: RuleMatchType.Contains,
            suggestedCategoryId: GroceryCategoryId,
            affectedTransactionCount: 1,
            sampleDescriptions: new List<string>());

        _suggestionRepo
            .Setup(r => r.GetByIdAsync(suggestion.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(suggestion);

        // Act
        await _handler.DismissSuggestionAsync(suggestion.Id, "Not useful");

        // Assert
        Assert.Equal(SuggestionStatus.Dismissed, suggestion.Status);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DismissSuggestionAsync_NotFound_ThrowsDomainException()
    {
        // Arrange
        var id = Guid.NewGuid();
        _suggestionRepo
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RuleSuggestion?)null);

        // Act & Assert
        await Assert.ThrowsAsync<DomainException>(() => _handler.DismissSuggestionAsync(id));
    }

    [Fact]
    public async Task ProvideFeedbackAsync_RecordsFeedback()
    {
        // Arrange
        var suggestion = RuleSuggestion.CreateNewRuleSuggestion(
            title: "Test",
            description: "Test",
            reasoning: "Test",
            confidence: 0.9m,
            suggestedPattern: "TEST",
            suggestedMatchType: RuleMatchType.Contains,
            suggestedCategoryId: GroceryCategoryId,
            affectedTransactionCount: 1,
            sampleDescriptions: new List<string>());

        // Must be reviewed (not Pending) before feedback is allowed
        suggestion.Dismiss("Not needed");

        _suggestionRepo
            .Setup(r => r.GetByIdAsync(suggestion.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(suggestion);

        // Act
        await _handler.ProvideFeedbackAsync(suggestion.Id, isPositive: true);

        // Assert
        Assert.True(suggestion.UserFeedbackPositive);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProvideFeedbackAsync_NotFound_ThrowsDomainException()
    {
        var id = Guid.NewGuid();
        _suggestionRepo
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RuleSuggestion?)null);

        await Assert.ThrowsAsync<DomainException>(() => _handler.ProvideFeedbackAsync(id, true));
    }
}
