// <copyright file="SuggestionAcceptanceHandlerConsolidationTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using Moq;
using Shouldly;

namespace BudgetExperiment.Application.Tests.Categorization;

/// <summary>
/// Slice 5 tests for the consolidation path in <see cref="SuggestionAcceptanceHandler"/>.
/// Verifies that after the Slice 5 fix the handler delegates to
/// <see cref="IRuleConsolidationService"/> instead of throwing the old "manual review" exception.
///
/// RED PHASE: These tests require <see cref="SuggestionAcceptanceHandler"/> to accept
/// <see cref="IRuleConsolidationService"/> as a constructor parameter (4th arg).
/// They will compile and turn green once Lucius injects the service and replaces the throw.
/// </summary>
public class SuggestionAcceptanceHandlerConsolidationTests
{
    private static readonly Guid GroceryCategoryId = Guid.NewGuid();

    private readonly Mock<IRuleSuggestionRepository> _suggestionRepo;
    private readonly Mock<ICategorizationRuleRepository> _ruleRepo;
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly Mock<IRuleConsolidationService> _consolidationService;

    /// <summary>
    /// Initializes a new instance of the <see cref="SuggestionAcceptanceHandlerConsolidationTests"/> class.
    /// </summary>
    public SuggestionAcceptanceHandlerConsolidationTests()
    {
        _suggestionRepo = new Mock<IRuleSuggestionRepository>();
        _ruleRepo = new Mock<ICategorizationRuleRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _consolidationService = new Mock<IRuleConsolidationService>();

        _unitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
    }

    // -------------------------------------------------------------------------
    // 1. Consolidation suggestion → delegates to IRuleConsolidationService
    // -------------------------------------------------------------------------

    /// <summary>
    /// When AcceptSuggestionAsync is called with a RuleConsolidation suggestion it must
    /// delegate to <see cref="IRuleConsolidationService.AcceptConsolidationAsync"/> and
    /// return the rule that the service provides.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task HandleAsync_ConsolidationSuggestion_DelegatesToConsolidationService()
    {
        // Arrange
        var suggestion = RuleSuggestion.CreateConsolidationSuggestion(
            title: "Consolidate WALMART rules",
            description: "Merge 2 rules",
            reasoning: "Same pattern",
            confidence: 0.9m,
            ruleIds: new List<Guid> { Guid.NewGuid(), Guid.NewGuid() },
            consolidatedPattern: "WALMART|WALMART GROCERY");

        var mergedRule = CategorizationRule.Create(
            "Merged WALMART",
            RuleMatchType.Regex,
            "WALMART|WALMART GROCERY",
            GroceryCategoryId,
            priority: 10);

        _suggestionRepo
            .Setup(r => r.GetByIdAsync(suggestion.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(suggestion);

        _consolidationService
            .Setup(s => s.AcceptConsolidationAsync(suggestion.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mergedRule);

        // SuggestionAcceptanceHandler must accept IRuleConsolidationService as a 4th parameter.
        // This will compile once Lucius updates the constructor.
        var handler = new SuggestionAcceptanceHandler(
            _suggestionRepo.Object,
            _ruleRepo.Object,
            _unitOfWork.Object,
            _consolidationService.Object);

        // Act
        var result = await handler.AcceptSuggestionAsync(suggestion.Id);

        // Assert
        result.ShouldNotBeNull();

        _consolidationService.Verify(
            s => s.AcceptConsolidationAsync(suggestion.Id, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // -------------------------------------------------------------------------
    // 2. Regression guard: no longer throws "manual review" exception
    // -------------------------------------------------------------------------

    /// <summary>
    /// Regression guard: after the Slice 5 fix, accepting a RuleConsolidation suggestion
    /// must NOT throw a <see cref="DomainException"/> with the old "manual review" message.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task HandleAsync_ConsolidationSuggestion_DoesNotThrowManualReviewException()
    {
        // Arrange
        var suggestion = RuleSuggestion.CreateConsolidationSuggestion(
            title: "Consolidate rules",
            description: "Two duplicate rules",
            reasoning: "Exact duplicate",
            confidence: 1.0m,
            ruleIds: new List<Guid> { Guid.NewGuid(), Guid.NewGuid() },
            consolidatedPattern: "UBER");

        var mergedRule = CategorizationRule.Create(
            "Merged UBER",
            RuleMatchType.Contains,
            "UBER",
            GroceryCategoryId,
            priority: 5);

        _suggestionRepo
            .Setup(r => r.GetByIdAsync(suggestion.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(suggestion);

        _consolidationService
            .Setup(s => s.AcceptConsolidationAsync(suggestion.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mergedRule);

        var handler = new SuggestionAcceptanceHandler(
            _suggestionRepo.Object,
            _ruleRepo.Object,
            _unitOfWork.Object,
            _consolidationService.Object);

        // Act & Assert — must not throw the old "requires manual review" DomainException
        await Should.NotThrowAsync(() => handler.AcceptSuggestionAsync(suggestion.Id));
    }
}
