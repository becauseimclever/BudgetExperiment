// <copyright file="RuleConsolidationServiceTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using Moq;
using Shouldly;

namespace BudgetExperiment.Application.Tests.Categorization;

/// <summary>
/// Unit tests for <see cref="RuleConsolidationService"/>.
/// Feature 116 Slice 4: Analysis orchestration and suggestion persistence.
/// These tests are RED — <see cref="RuleConsolidationService"/> does not yet exist.
/// Lucius must implement the class to make them green.
/// </summary>
public class RuleConsolidationServiceTests
{
    private static readonly Guid GroceryCategoryId = Guid.NewGuid();
    private static readonly Guid TransportCategoryId = Guid.NewGuid();

    private readonly Mock<ICategorizationRuleRepository> _mockRuleRepo;
    private readonly Mock<IRuleSuggestionRepository> _mockSuggestionRepo;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly RuleConsolidationAnalyzer _analyzer;

    /// <summary>
    /// Initializes a new instance of the <see cref="RuleConsolidationServiceTests"/> class.
    /// </summary>
    public RuleConsolidationServiceTests()
    {
        _mockRuleRepo = new Mock<ICategorizationRuleRepository>();
        _mockSuggestionRepo = new Mock<IRuleSuggestionRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mockSuggestionRepo
            .Setup(r => r.AddRangeAsync(
                It.IsAny<IEnumerable<RuleSuggestion>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _analyzer = new RuleConsolidationAnalyzer();
    }

    // -------------------------------------------------------------------------
    // 1. Duplicate rules → suggestion created and persisted
    // -------------------------------------------------------------------------

    /// <summary>
    /// When two active rules share the same pattern, category, and match type,
    /// the service creates one consolidation suggestion and persists it.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task AnalyzeAndStoreAsync_WithDuplicateRules_CreatesSuggestionsAndPersists()
    {
        // Arrange
        var rules = new List<CategorizationRule>
        {
            CategorizationRule.Create("Walmart A", RuleMatchType.Contains, "WALMART", GroceryCategoryId, priority: 1),
            CategorizationRule.Create("Walmart B", RuleMatchType.Contains, "WALMART", GroceryCategoryId, priority: 2),
        };

        _mockRuleRepo
            .Setup(r => r.GetActiveByPriorityAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(rules.AsReadOnly());

        var service = new RuleConsolidationService(
            _mockRuleRepo.Object,
            _mockSuggestionRepo.Object,
            _analyzer,
            _mockUnitOfWork.Object);

        // Act
        var result = await service.AnalyzeAndStoreAsync();

        // Assert
        result.ShouldNotBeEmpty();
        result.Count.ShouldBeGreaterThanOrEqualTo(1);
        result[0].Type.ShouldBe(SuggestionType.RuleConsolidation);

        _mockSuggestionRepo.Verify(
            r => r.AddRangeAsync(
                It.Is<IEnumerable<RuleSuggestion>>(items => items.Any()),
                It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    // -------------------------------------------------------------------------
    // 2. No rules → empty result, repository not called
    // -------------------------------------------------------------------------

    /// <summary>
    /// When no active rules exist the service returns an empty list and does not
    /// attempt to persist anything.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task AnalyzeAndStoreAsync_NoRules_ReturnsEmpty()
    {
        // Arrange
        _mockRuleRepo
            .Setup(r => r.GetActiveByPriorityAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<CategorizationRule>)Array.Empty<CategorizationRule>());

        var service = new RuleConsolidationService(
            _mockRuleRepo.Object,
            _mockSuggestionRepo.Object,
            _analyzer,
            _mockUnitOfWork.Object);

        // Act
        var result = await service.AnalyzeAndStoreAsync();

        // Assert
        result.ShouldBeEmpty();

        _mockSuggestionRepo.Verify(
            r => r.AddRangeAsync(
                It.IsAny<IEnumerable<RuleSuggestion>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // -------------------------------------------------------------------------
    // 3. Single rule → no consolidation opportunities
    // -------------------------------------------------------------------------

    /// <summary>
    /// A single rule cannot be consolidated with anything; the service returns empty
    /// and does not call the suggestion repository.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task AnalyzeAndStoreAsync_NoConsolidationOpportunities_ReturnsEmpty()
    {
        // Arrange
        var rules = new List<CategorizationRule>
        {
            CategorizationRule.Create("Walmart Only", RuleMatchType.Contains, "WALMART", GroceryCategoryId, priority: 1),
        };

        _mockRuleRepo
            .Setup(r => r.GetActiveByPriorityAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(rules.AsReadOnly());

        var service = new RuleConsolidationService(
            _mockRuleRepo.Object,
            _mockSuggestionRepo.Object,
            _analyzer,
            _mockUnitOfWork.Object);

        // Act
        var result = await service.AnalyzeAndStoreAsync();

        // Assert
        result.ShouldBeEmpty();

        _mockSuggestionRepo.Verify(
            r => r.AddRangeAsync(
                It.IsAny<IEnumerable<RuleSuggestion>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // -------------------------------------------------------------------------
    // 4. Two distinct duplicate pairs → two suggestions persisted
    // -------------------------------------------------------------------------

    /// <summary>
    /// When there are two independent groups of duplicate rules the service creates
    /// one suggestion per group and persists all of them together.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task AnalyzeAndStoreAsync_MultipleConsolidations_PersistsAll()
    {
        // Arrange — two duplicate pairs in different categories
        var rules = new List<CategorizationRule>
        {
            CategorizationRule.Create("Walmart A", RuleMatchType.Contains, "WALMART", GroceryCategoryId, priority: 1),
            CategorizationRule.Create("Walmart B", RuleMatchType.Contains, "WALMART", GroceryCategoryId, priority: 2),
            CategorizationRule.Create("Uber A", RuleMatchType.Contains, "UBER", TransportCategoryId, priority: 3),
            CategorizationRule.Create("Uber B", RuleMatchType.Contains, "UBER", TransportCategoryId, priority: 4),
        };

        _mockRuleRepo
            .Setup(r => r.GetActiveByPriorityAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(rules.AsReadOnly());

        var service = new RuleConsolidationService(
            _mockRuleRepo.Object,
            _mockSuggestionRepo.Object,
            _analyzer,
            _mockUnitOfWork.Object);

        // Act
        var result = await service.AnalyzeAndStoreAsync();

        // Assert
        result.Count.ShouldBe(2);
        result.ShouldAllBe(s => s.Type == SuggestionType.RuleConsolidation);

        _mockSuggestionRepo.Verify(
            r => r.AddRangeAsync(
                It.Is<IEnumerable<RuleSuggestion>>(items => items.Count() == 2),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // -------------------------------------------------------------------------
    // Slice 5 — AcceptConsolidationAsync: happy path
    // -------------------------------------------------------------------------

    /// <summary>
    /// Accepting a valid consolidation suggestion must create a new merged rule via
    /// <see cref="ICategorizationRuleRepository.AddAsync"/> and deactivate every source rule.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task AcceptConsolidationAsync_ValidSuggestion_CreatesNewRuleAndDeactivatesSources()
    {
        // Arrange
        var ruleId1 = Guid.NewGuid();
        var ruleId2 = Guid.NewGuid();
        var suggestion = RuleSuggestion.CreateConsolidationSuggestion(
            title: "Consolidate WALMART rules",
            description: "Merge 2 duplicate rules",
            reasoning: "Same pattern and category",
            confidence: 0.95m,
            ruleIds: new List<Guid> { ruleId1, ruleId2 },
            consolidatedPattern: "WALMART|WALMART GROCERY");

        var sourceRule1 = CategorizationRule.Create("Walmart A", RuleMatchType.Contains, "WALMART", GroceryCategoryId, priority: 1);
        var sourceRule2 = CategorizationRule.Create("Walmart B", RuleMatchType.Contains, "WALMART GROCERY", GroceryCategoryId, priority: 2);

        _mockSuggestionRepo
            .Setup(r => r.GetByIdAsync(suggestion.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(suggestion);

        _mockRuleRepo
            .Setup(r => r.GetByIdsAsync(It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CategorizationRule> { sourceRule1, sourceRule2 }.AsReadOnly());

        _mockRuleRepo
            .Setup(r => r.GetNextPriorityAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(10);

        _mockRuleRepo
            .Setup(r => r.AddAsync(It.IsAny<CategorizationRule>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new RuleConsolidationService(
            _mockRuleRepo.Object,
            _mockSuggestionRepo.Object,
            _analyzer,
            _mockUnitOfWork.Object);

        // Act
        var result = await service.AcceptConsolidationAsync(suggestion.Id);

        // Assert
        result.ShouldNotBeNull();

        _mockRuleRepo.Verify(
            r => r.AddAsync(It.IsAny<CategorizationRule>(), It.IsAny<CancellationToken>()),
            Times.Once);

        sourceRule1.IsActive.ShouldBeFalse();
        sourceRule2.IsActive.ShouldBeFalse();
    }

    // -------------------------------------------------------------------------
    // Slice 5 — AcceptConsolidationAsync: suggestion not found
    // -------------------------------------------------------------------------

    /// <summary>
    /// When the suggestion ID does not exist the method must throw a
    /// <see cref="DomainException"/> with <see cref="DomainExceptionType.NotFound"/>.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task AcceptConsolidationAsync_SuggestionNotFound_ThrowsDomainException()
    {
        // Arrange
        var missingId = Guid.NewGuid();

        _mockSuggestionRepo
            .Setup(r => r.GetByIdAsync(missingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RuleSuggestion?)null);

        var service = new RuleConsolidationService(
            _mockRuleRepo.Object,
            _mockSuggestionRepo.Object,
            _analyzer,
            _mockUnitOfWork.Object);

        // Act & Assert
        var ex = await Should.ThrowAsync<DomainException>(() => service.AcceptConsolidationAsync(missingId));
        ex.ExceptionType.ShouldBe(DomainExceptionType.NotFound);
    }

    // -------------------------------------------------------------------------
    // Slice 5 — AcceptConsolidationAsync: wrong suggestion type
    // -------------------------------------------------------------------------

    /// <summary>
    /// Passing a suggestion whose type is not <see cref="SuggestionType.RuleConsolidation"/>
    /// must throw a <see cref="DomainException"/>.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task AcceptConsolidationAsync_WrongSuggestionType_ThrowsDomainException()
    {
        // Arrange — PatternOptimization suggestion (not a consolidation)
        var ruleId = Guid.NewGuid();
        var suggestion = RuleSuggestion.CreateOptimizationSuggestion(
            title: "Optimize WALMART pattern",
            description: "Simplify pattern",
            reasoning: "Redundant prefix",
            confidence: 0.8m,
            targetRuleId: ruleId,
            optimizedPattern: "WALMART");

        _mockSuggestionRepo
            .Setup(r => r.GetByIdAsync(suggestion.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(suggestion);

        var service = new RuleConsolidationService(
            _mockRuleRepo.Object,
            _mockSuggestionRepo.Object,
            _analyzer,
            _mockUnitOfWork.Object);

        // Act & Assert
        await Should.ThrowAsync<DomainException>(() => service.AcceptConsolidationAsync(suggestion.Id));
    }

    // -------------------------------------------------------------------------
    // Slice 5 — DismissConsolidationAsync: happy path
    // -------------------------------------------------------------------------

    /// <summary>
    /// Dismissing a pending consolidation suggestion must mark its status as
    /// <see cref="SuggestionStatus.Dismissed"/> and persist the change.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task DismissConsolidationAsync_ValidSuggestion_MarksAsDismissed()
    {
        // Arrange
        var suggestion = RuleSuggestion.CreateConsolidationSuggestion(
            title: "Consolidate rules",
            description: "Merge 2 rules",
            reasoning: "Duplicates",
            confidence: 0.7m,
            ruleIds: new List<Guid> { Guid.NewGuid(), Guid.NewGuid() },
            consolidatedPattern: "KROGER|KROGER GROCERY");

        _mockSuggestionRepo
            .Setup(r => r.GetByIdAsync(suggestion.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(suggestion);

        var service = new RuleConsolidationService(
            _mockRuleRepo.Object,
            _mockSuggestionRepo.Object,
            _analyzer,
            _mockUnitOfWork.Object);

        // Act
        await service.DismissConsolidationAsync(suggestion.Id);

        // Assert
        suggestion.Status.ShouldBe(SuggestionStatus.Dismissed);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    // -------------------------------------------------------------------------
    // Slice 5 — DismissConsolidationAsync: suggestion not found
    // -------------------------------------------------------------------------

    /// <summary>
    /// When the suggestion ID does not exist the dismiss method must throw a
    /// <see cref="DomainException"/> with <see cref="DomainExceptionType.NotFound"/>.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task DismissConsolidationAsync_SuggestionNotFound_ThrowsDomainException()
    {
        // Arrange
        var missingId = Guid.NewGuid();

        _mockSuggestionRepo
            .Setup(r => r.GetByIdAsync(missingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RuleSuggestion?)null);

        var service = new RuleConsolidationService(
            _mockRuleRepo.Object,
            _mockSuggestionRepo.Object,
            _analyzer,
            _mockUnitOfWork.Object);

        // Act & Assert
        var ex = await Should.ThrowAsync<DomainException>(() => service.DismissConsolidationAsync(missingId));
        ex.ExceptionType.ShouldBe(DomainExceptionType.NotFound);
    }

    // -------------------------------------------------------------------------
    // Slice 6 — UndoConsolidationAsync: source rules reactivated and merged rule deactivated
    // -------------------------------------------------------------------------

    /// <summary>
    /// Undoing an accepted consolidation must call Activate() on every source rule
    /// and Deactivate() on the merged rule so that the rule set is restored.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UndoConsolidationAsync_AcceptedSuggestion_ReactivatesSourceRules()
    {
        // Arrange
        var mergedRuleId = Guid.NewGuid();
        var suggestion = RuleSuggestion.CreateConsolidationSuggestion(
            title: "Consolidate WALMART rules",
            description: "Merge 2 duplicate rules",
            reasoning: "Same pattern and category",
            confidence: 0.9m,
            ruleIds: new List<Guid> { Guid.NewGuid(), Guid.NewGuid() },
            consolidatedPattern: "WALMART|WALMART GROCERY");
        suggestion.RecordMergedRuleId(mergedRuleId);
        suggestion.Accept();

        var sourceRule1 = CategorizationRule.Create("Walmart A", RuleMatchType.Contains, "WALMART", GroceryCategoryId, priority: 1);
        sourceRule1.Deactivate();
        var sourceRule2 = CategorizationRule.Create("Walmart B", RuleMatchType.Contains, "WALMART GROCERY", GroceryCategoryId, priority: 2);
        sourceRule2.Deactivate();
        var mergedRule = CategorizationRule.Create("Consolidated: WALMART|WALMART GROCERY", RuleMatchType.Regex, "WALMART|WALMART GROCERY", GroceryCategoryId, priority: 10);

        _mockSuggestionRepo
            .Setup(r => r.GetByIdAsync(suggestion.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(suggestion);

        _mockRuleRepo
            .Setup(r => r.GetByIdsAsync(It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CategorizationRule> { sourceRule1, sourceRule2 }.AsReadOnly());

        _mockRuleRepo
            .Setup(r => r.GetByIdAsync(mergedRuleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mergedRule);

        var service = new RuleConsolidationService(
            _mockRuleRepo.Object,
            _mockSuggestionRepo.Object,
            _analyzer,
            _mockUnitOfWork.Object);

        // Act
        await service.UndoConsolidationAsync(suggestion.Id);

        // Assert
        sourceRule1.IsActive.ShouldBeTrue();
        sourceRule2.IsActive.ShouldBeTrue();
        mergedRule.IsActive.ShouldBeFalse();
    }

    // -------------------------------------------------------------------------
    // Slice 6 — UndoConsolidationAsync: suggestion state reset to pending
    // -------------------------------------------------------------------------

    /// <summary>
    /// After undo the suggestion must be reset to <see cref="SuggestionStatus.Pending"/>
    /// and all changes must be persisted via <see cref="IUnitOfWork.SaveChangesAsync"/>.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UndoConsolidationAsync_AcceptedSuggestion_ResetsSuggestionState()
    {
        // Arrange
        var mergedRuleId = Guid.NewGuid();
        var suggestion = RuleSuggestion.CreateConsolidationSuggestion(
            title: "Consolidate KROGER rules",
            description: "Merge 2 Kroger rules",
            reasoning: "Same pattern",
            confidence: 0.8m,
            ruleIds: new List<Guid> { Guid.NewGuid(), Guid.NewGuid() },
            consolidatedPattern: "KROGER|KROGER GROCERY");
        suggestion.RecordMergedRuleId(mergedRuleId);
        suggestion.Accept();

        var sourceRule1 = CategorizationRule.Create("Kroger A", RuleMatchType.Contains, "KROGER", GroceryCategoryId, priority: 1);
        sourceRule1.Deactivate();
        var sourceRule2 = CategorizationRule.Create("Kroger B", RuleMatchType.Contains, "KROGER GROCERY", GroceryCategoryId, priority: 2);
        sourceRule2.Deactivate();
        var mergedRule = CategorizationRule.Create("Consolidated: KROGER|KROGER GROCERY", RuleMatchType.Regex, "KROGER|KROGER GROCERY", GroceryCategoryId, priority: 10);

        _mockSuggestionRepo
            .Setup(r => r.GetByIdAsync(suggestion.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(suggestion);

        _mockRuleRepo
            .Setup(r => r.GetByIdsAsync(It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CategorizationRule> { sourceRule1, sourceRule2 }.AsReadOnly());

        _mockRuleRepo
            .Setup(r => r.GetByIdAsync(mergedRuleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mergedRule);

        var service = new RuleConsolidationService(
            _mockRuleRepo.Object,
            _mockSuggestionRepo.Object,
            _analyzer,
            _mockUnitOfWork.Object);

        // Act
        await service.UndoConsolidationAsync(suggestion.Id);

        // Assert
        suggestion.Status.ShouldBe(SuggestionStatus.Pending);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    // -------------------------------------------------------------------------
    // Slice 6 — UndoConsolidationAsync: suggestion not found
    // -------------------------------------------------------------------------

    /// <summary>
    /// When the suggestion ID does not exist the undo method must throw a
    /// <see cref="DomainException"/> with <see cref="DomainExceptionType.NotFound"/>.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UndoConsolidationAsync_SuggestionNotFound_ThrowsDomainException()
    {
        // Arrange
        var missingId = Guid.NewGuid();

        _mockSuggestionRepo
            .Setup(r => r.GetByIdAsync(missingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RuleSuggestion?)null);

        var service = new RuleConsolidationService(
            _mockRuleRepo.Object,
            _mockSuggestionRepo.Object,
            _analyzer,
            _mockUnitOfWork.Object);

        // Act & Assert
        var ex = await Should.ThrowAsync<DomainException>(() => service.UndoConsolidationAsync(missingId));
        ex.ExceptionType.ShouldBe(DomainExceptionType.NotFound);
    }

    // -------------------------------------------------------------------------
    // Slice 6 — UndoConsolidationAsync: suggestion not in accepted state
    // -------------------------------------------------------------------------

    /// <summary>
    /// Attempting to undo a suggestion that is not in the accepted state must throw a
    /// <see cref="DomainException"/> indicating the invalid state.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UndoConsolidationAsync_SuggestionNotAccepted_ThrowsDomainException()
    {
        // Arrange — suggestion in Pending state (never accepted)
        var suggestion = RuleSuggestion.CreateConsolidationSuggestion(
            title: "Consolidate TARGET rules",
            description: "Merge 2 Target rules",
            reasoning: "Same pattern",
            confidence: 0.85m,
            ruleIds: new List<Guid> { Guid.NewGuid(), Guid.NewGuid() },
            consolidatedPattern: "TARGET|TARGET STORE");

        _mockSuggestionRepo
            .Setup(r => r.GetByIdAsync(suggestion.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(suggestion);

        var service = new RuleConsolidationService(
            _mockRuleRepo.Object,
            _mockSuggestionRepo.Object,
            _analyzer,
            _mockUnitOfWork.Object);

        // Act & Assert
        await Should.ThrowAsync<DomainException>(() => service.UndoConsolidationAsync(suggestion.Id));
    }

    // -------------------------------------------------------------------------
    // Slice 6 — UndoConsolidationAsync: combined end-to-end state verification
    // -------------------------------------------------------------------------

    /// <summary>
    /// Combined assertion: two source rules must go from inactive to active, the merged rule
    /// must go from active to inactive, and all changes must be saved in a single call.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UndoConsolidationAsync_SourceRulesReactivated_AndMergedRuleDeactivated()
    {
        // Arrange
        var mergedRuleId = Guid.NewGuid();
        var suggestion = RuleSuggestion.CreateConsolidationSuggestion(
            title: "Consolidate UBER rules",
            description: "Merge 2 Uber rules",
            reasoning: "Same category and pattern",
            confidence: 0.92m,
            ruleIds: new List<Guid> { Guid.NewGuid(), Guid.NewGuid() },
            consolidatedPattern: "UBER|UBER EATS");
        suggestion.RecordMergedRuleId(mergedRuleId);
        suggestion.Accept();

        var sourceRule1 = CategorizationRule.Create("Uber A", RuleMatchType.Contains, "UBER", TransportCategoryId, priority: 1);
        sourceRule1.Deactivate();
        var sourceRule2 = CategorizationRule.Create("Uber B", RuleMatchType.Contains, "UBER EATS", TransportCategoryId, priority: 2);
        sourceRule2.Deactivate();
        var mergedRule = CategorizationRule.Create("Consolidated: UBER|UBER EATS", RuleMatchType.Regex, "UBER|UBER EATS", TransportCategoryId, priority: 10);

        _mockSuggestionRepo
            .Setup(r => r.GetByIdAsync(suggestion.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(suggestion);

        _mockRuleRepo
            .Setup(r => r.GetByIdsAsync(It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CategorizationRule> { sourceRule1, sourceRule2 }.AsReadOnly());

        _mockRuleRepo
            .Setup(r => r.GetByIdAsync(mergedRuleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mergedRule);

        var service = new RuleConsolidationService(
            _mockRuleRepo.Object,
            _mockSuggestionRepo.Object,
            _analyzer,
            _mockUnitOfWork.Object);

        // Act
        await service.UndoConsolidationAsync(suggestion.Id);

        // Assert
        sourceRule1.IsActive.ShouldBeTrue();
        sourceRule2.IsActive.ShouldBeTrue();
        mergedRule.IsActive.ShouldBeFalse();
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
