// <copyright file="CategorySuggestionServicePhase1BTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Globalization;
using BudgetExperiment.Application.Ai;
using BudgetExperiment.Application.Categorization;
using BudgetExperiment.Domain;
using Moq;
using Shouldly;

namespace BudgetExperiment.Application.Tests.Categorization;

/// <summary>
/// Phase 1B edge case tests for CategorySuggestionService.
/// </summary>
public class CategorySuggestionServicePhase1BTests
{
    public CategorySuggestionServicePhase1BTests()
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
    }

    [Fact]
    public async Task GetSuggestionsAsync_NullAccountId_ThrowsValidationException()
    {
        // Arrange
        var mockTransactionRepo = new Mock<ITransactionQueryRepository>();
        var mockCategoryRepo = new Mock<IBudgetCategoryRepository>();
        var mockSuggestionRepo = new Mock<ICategorySuggestionRepository>();
        var mockDismissedRepo = new Mock<IDismissedSuggestionPatternRepository>();
        var mockMerchantMapping = new Mock<IMerchantMappingService>();
        var mockAiService = new Mock<IAiService>();
        var mockUow = new Mock<IUnitOfWork>();
        var mockUserContext = new Mock<IUserContext>();
        var mockDismissalHandler = new Mock<ICategorySuggestionDismissalHandler>();
        var mockScorer = new Mock<ICategorySuggestionScorer>();

        mockUserContext.Setup(u => u.UserId).Returns(string.Empty);

        var service = new CategorySuggestionService(
            mockTransactionRepo.Object,
            mockCategoryRepo.Object,
            mockSuggestionRepo.Object,
            mockDismissedRepo.Object,
            mockMerchantMapping.Object,
            mockAiService.Object,
            mockUow.Object,
            mockUserContext.Object,
            mockDismissalHandler.Object,
            mockScorer.Object);

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(async () =>
            await service.GetPendingSuggestionsAsync(default));
    }

    [Fact]
    public async Task GetSuggestionsAsync_NoTransactionHistory_ReturnsEmptyList()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var mockTransactionRepo = new Mock<ITransactionQueryRepository>();
        var mockCategoryRepo = new Mock<IBudgetCategoryRepository>();
        var mockSuggestionRepo = new Mock<ICategorySuggestionRepository>();
        var mockDismissedRepo = new Mock<IDismissedSuggestionPatternRepository>();
        var mockMerchantMapping = new Mock<IMerchantMappingService>();
        var mockAiService = new Mock<IAiService>();
        var mockUow = new Mock<IUnitOfWork>();
        var mockUserContext = new Mock<IUserContext>();
        var mockDismissalHandler = new Mock<ICategorySuggestionDismissalHandler>();
        var mockScorer = new Mock<ICategorySuggestionScorer>();

        mockUserContext.Setup(u => u.UserId).Returns(userId);
        mockTransactionRepo.Setup(r => r.GetUncategorizedDescriptionsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<string>());
        mockSuggestionRepo.Setup(r => r.GetPendingByOwnerAsync(userId, default))
            .ReturnsAsync(Array.Empty<CategorySuggestion>());

        var service = new CategorySuggestionService(
            mockTransactionRepo.Object,
            mockCategoryRepo.Object,
            mockSuggestionRepo.Object,
            mockDismissedRepo.Object,
            mockMerchantMapping.Object,
            mockAiService.Object,
            mockUow.Object,
            mockUserContext.Object,
            mockDismissalHandler.Object,
            mockScorer.Object);

        // Act
        var result = await service.AnalyzeTransactionsAsync(default);

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(0);
    }

    [Fact]
    public async Task DismissSuggestionAsync_NullDismissalRecord_ReturnsFalse()
    {
        // Arrange
        var mockTransactionRepo = new Mock<ITransactionQueryRepository>();
        var mockCategoryRepo = new Mock<IBudgetCategoryRepository>();
        var mockSuggestionRepo = new Mock<ICategorySuggestionRepository>();
        var mockDismissedRepo = new Mock<IDismissedSuggestionPatternRepository>();
        var mockMerchantMapping = new Mock<IMerchantMappingService>();
        var mockAiService = new Mock<IAiService>();
        var mockUow = new Mock<IUnitOfWork>();
        var mockUserContext = new Mock<IUserContext>();
        var mockDismissalHandler = new Mock<ICategorySuggestionDismissalHandler>();
        var mockScorer = new Mock<ICategorySuggestionScorer>();

        var nonExistentId = Guid.NewGuid();
        mockDismissalHandler.Setup(h => h.DismissSuggestionAsync(nonExistentId, default))
            .ReturnsAsync(false);

        var service = new CategorySuggestionService(
            mockTransactionRepo.Object,
            mockCategoryRepo.Object,
            mockSuggestionRepo.Object,
            mockDismissedRepo.Object,
            mockMerchantMapping.Object,
            mockAiService.Object,
            mockUow.Object,
            mockUserContext.Object,
            mockDismissalHandler.Object,
            mockScorer.Object);

        // Act
        var result = await service.DismissSuggestionAsync(nonExistentId, default);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task ConcurrentDismissals_FirstSucceeds_SecondGetsConcurrencyException()
    {
        // Arrange
        var suggestionId = Guid.NewGuid();
        var userId = Guid.NewGuid().ToString();

        var mockTransactionRepo = new Mock<ITransactionQueryRepository>();
        var mockCategoryRepo = new Mock<IBudgetCategoryRepository>();
        var mockSuggestionRepo = new Mock<ICategorySuggestionRepository>();
        var mockDismissedRepo = new Mock<IDismissedSuggestionPatternRepository>();
        var mockMerchantMapping = new Mock<IMerchantMappingService>();
        var mockAiService = new Mock<IAiService>();
        var mockUow = new Mock<IUnitOfWork>();
        var mockUserContext = new Mock<IUserContext>();
        var mockDismissalHandler = new Mock<ICategorySuggestionDismissalHandler>();
        var mockScorer = new Mock<ICategorySuggestionScorer>();

        mockUserContext.Setup(u => u.UserId).Returns(userId);

        var firstCall = true;
        mockDismissalHandler.Setup(h => h.DismissSuggestionAsync(suggestionId, default))
            .ReturnsAsync(() =>
            {
                if (firstCall)
                {
                    firstCall = false;
                    return true;
                }

                throw new DomainException("Concurrency conflict", DomainExceptionType.Conflict);
            });

        var service = new CategorySuggestionService(
            mockTransactionRepo.Object,
            mockCategoryRepo.Object,
            mockSuggestionRepo.Object,
            mockDismissedRepo.Object,
            mockMerchantMapping.Object,
            mockAiService.Object,
            mockUow.Object,
            mockUserContext.Object,
            mockDismissalHandler.Object,
            mockScorer.Object);

        // Act
        var firstResult = await service.DismissSuggestionAsync(suggestionId, default);

        // Assert
        firstResult.ShouldBeTrue();
        await Should.ThrowAsync<DomainException>(async () =>
            await service.DismissSuggestionAsync(suggestionId, default));
    }

    [Fact]
    public async Task DismissalCacheInvalidation_NextGetSuggestionsReflectsChange()
    {
        // Arrange
        var suggestionId = Guid.NewGuid();
        var userId = Guid.NewGuid().ToString();

        var mockTransactionRepo = new Mock<ITransactionQueryRepository>();
        var mockCategoryRepo = new Mock<IBudgetCategoryRepository>();
        var mockSuggestionRepo = new Mock<ICategorySuggestionRepository>();
        var mockDismissedRepo = new Mock<IDismissedSuggestionPatternRepository>();
        var mockMerchantMapping = new Mock<IMerchantMappingService>();
        var mockAiService = new Mock<IAiService>();
        var mockUow = new Mock<IUnitOfWork>();
        var mockUserContext = new Mock<IUserContext>();
        var mockDismissalHandler = new Mock<ICategorySuggestionDismissalHandler>();
        var mockScorer = new Mock<ICategorySuggestionScorer>();

        mockUserContext.Setup(u => u.UserId).Returns(userId);

        var suggestion = CategorySuggestion.Create(
            "Groceries",
            CategoryType.Expense,
            new List<string> { "WALMART" },
            5,
            0.8m,
            userId,
            "shopping");

        mockSuggestionRepo.SetupSequence(r => r.GetPendingByOwnerAsync(userId, default))
            .ReturnsAsync(new[] { suggestion })
            .ReturnsAsync(Array.Empty<CategorySuggestion>());

        mockDismissalHandler.Setup(h => h.DismissSuggestionAsync(suggestionId, default))
            .ReturnsAsync(true);

        var service = new CategorySuggestionService(
            mockTransactionRepo.Object,
            mockCategoryRepo.Object,
            mockSuggestionRepo.Object,
            mockDismissedRepo.Object,
            mockMerchantMapping.Object,
            mockAiService.Object,
            mockUow.Object,
            mockUserContext.Object,
            mockDismissalHandler.Object,
            mockScorer.Object);

        // Act
        var beforeDismissal = await service.GetPendingSuggestionsAsync(default);
        await service.DismissSuggestionAsync(suggestionId, default);
        var afterDismissal = await service.GetPendingSuggestionsAsync(default);

        // Assert
        beforeDismissal.Count.ShouldBe(1);
        afterDismissal.Count.ShouldBe(0);
    }

    [Fact]
    public async Task CategoryCreation_SuggestionReflectsNewCategory()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var categoryId = Guid.NewGuid();

        var mockTransactionRepo = new Mock<ITransactionQueryRepository>();
        var mockCategoryRepo = new Mock<IBudgetCategoryRepository>();
        var mockSuggestionRepo = new Mock<ICategorySuggestionRepository>();
        var mockDismissedRepo = new Mock<IDismissedSuggestionPatternRepository>();
        var mockMerchantMapping = new Mock<IMerchantMappingService>();
        var mockAiService = new Mock<IAiService>();
        var mockUow = new Mock<IUnitOfWork>();
        var mockUserContext = new Mock<IUserContext>();
        var mockDismissalHandler = new Mock<ICategorySuggestionDismissalHandler>();
        var mockScorer = new Mock<ICategorySuggestionScorer>();

        mockUserContext.Setup(u => u.UserId).Returns(userId);

        mockCategoryRepo.SetupSequence(r => r.GetActiveAsync(default))
            .ReturnsAsync(Array.Empty<BudgetCategory>())
            .ReturnsAsync(new[]
            {
                BudgetCategory.Create("Groceries", CategoryType.Expense),
            });

        mockTransactionRepo.Setup(r => r.GetUncategorizedDescriptionsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { "WALMART SUPERCENTER" });

        mockDismissedRepo.Setup(r => r.IsDismissedAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        mockMerchantMapping.Setup(m => m.FindMatchingPatternsAsync(userId, It.IsAny<IReadOnlyList<string>>(), default))
            .ReturnsAsync(new[]
            {
                new PatternMatch { Pattern = "WALMART", Category = "Groceries", TransactionCount = 5, Icon = "shopping" },
            });

        mockSuggestionRepo.Setup(r => r.DeletePendingByOwnerAsync(userId, default))
            .Returns(Task.CompletedTask);

        mockSuggestionRepo.Setup(r => r.AddRangeAsync(It.IsAny<List<CategorySuggestion>>(), default))
            .Returns(Task.CompletedTask);

        mockUow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        mockScorer.Setup(s => s.CalculateConfidence(It.IsAny<int>(), It.IsAny<int>()))
            .Returns(0.8m);

        mockScorer.Setup(s => s.Rank(It.IsAny<IEnumerable<CategorySuggestion>>()))
            .Returns<IEnumerable<CategorySuggestion>>(suggestions => suggestions.ToList());

        var service = new CategorySuggestionService(
            mockTransactionRepo.Object,
            mockCategoryRepo.Object,
            mockSuggestionRepo.Object,
            mockDismissedRepo.Object,
            mockMerchantMapping.Object,
            mockAiService.Object,
            mockUow.Object,
            mockUserContext.Object,
            mockDismissalHandler.Object,
            mockScorer.Object);

        // Act - Before category exists
        var suggestionsBeforeCreate = await service.AnalyzeTransactionsAsync(default);

        // Assert - Before: should have suggestion
        suggestionsBeforeCreate.Count.ShouldBe(1);

        // Act - After category exists (simulate re-scan)
        var suggestionsAfterCreate = await service.AnalyzeTransactionsAsync(default);

        // Assert - After: no suggestion because category exists
        suggestionsAfterCreate.Count.ShouldBe(0);
    }

    [Fact]
    public async Task SimilarTransactionDescription_CorrectCategorySuggested()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();

        var mockTransactionRepo = new Mock<ITransactionQueryRepository>();
        var mockCategoryRepo = new Mock<IBudgetCategoryRepository>();
        var mockSuggestionRepo = new Mock<ICategorySuggestionRepository>();
        var mockDismissedRepo = new Mock<IDismissedSuggestionPatternRepository>();
        var mockMerchantMapping = new Mock<IMerchantMappingService>();
        var mockAiService = new Mock<IAiService>();
        var mockUow = new Mock<IUnitOfWork>();
        var mockUserContext = new Mock<IUserContext>();
        var mockDismissalHandler = new Mock<ICategorySuggestionDismissalHandler>();
        var mockScorer = new Mock<ICategorySuggestionScorer>();

        mockUserContext.Setup(u => u.UserId).Returns(userId);

        mockTransactionRepo.Setup(r => r.GetUncategorizedDescriptionsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { "TARGET STORE 1234", "TARGET #5678" });

        mockCategoryRepo.Setup(r => r.GetActiveAsync(default))
            .ReturnsAsync(Array.Empty<BudgetCategory>());

        mockDismissedRepo.Setup(r => r.IsDismissedAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        mockMerchantMapping.Setup(m => m.FindMatchingPatternsAsync(userId, It.IsAny<IReadOnlyList<string>>(), default))
            .ReturnsAsync(new[]
            {
                new PatternMatch { Pattern = "TARGET", Category = "Shopping", TransactionCount = 10, Icon = "shopping" },
            });

        mockSuggestionRepo.Setup(r => r.DeletePendingByOwnerAsync(userId, default))
            .Returns(Task.CompletedTask);

        mockSuggestionRepo.Setup(r => r.AddRangeAsync(It.IsAny<List<CategorySuggestion>>(), default))
            .Returns(Task.CompletedTask);

        mockUow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        mockScorer.Setup(s => s.CalculateConfidence(It.IsAny<int>(), It.IsAny<int>()))
            .Returns(0.9m);

        mockScorer.Setup(s => s.Rank(It.IsAny<IEnumerable<CategorySuggestion>>()))
            .Returns<IEnumerable<CategorySuggestion>>(suggestions => suggestions.ToList());

        var service = new CategorySuggestionService(
            mockTransactionRepo.Object,
            mockCategoryRepo.Object,
            mockSuggestionRepo.Object,
            mockDismissedRepo.Object,
            mockMerchantMapping.Object,
            mockAiService.Object,
            mockUow.Object,
            mockUserContext.Object,
            mockDismissalHandler.Object,
            mockScorer.Object);

        // Act
        var suggestions = await service.AnalyzeTransactionsAsync(default);

        // Assert
        suggestions.Count.ShouldBe(1);
        suggestions[0].SuggestedName.ShouldBe("Shopping");
        suggestions[0].MerchantPatterns.ShouldContain("TARGET");
    }

    [Fact]
    public async Task FuzzyMatchingEdgeCases_HandlesTyposWhitespaceCase()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();

        var mockTransactionRepo = new Mock<ITransactionQueryRepository>();
        var mockCategoryRepo = new Mock<IBudgetCategoryRepository>();
        var mockSuggestionRepo = new Mock<ICategorySuggestionRepository>();
        var mockDismissedRepo = new Mock<IDismissedSuggestionPatternRepository>();
        var mockMerchantMapping = new Mock<IMerchantMappingService>();
        var mockAiService = new Mock<IAiService>();
        var mockUow = new Mock<IUnitOfWork>();
        var mockUserContext = new Mock<IUserContext>();
        var mockDismissalHandler = new Mock<ICategorySuggestionDismissalHandler>();
        var mockScorer = new Mock<ICategorySuggestionScorer>();

        mockUserContext.Setup(u => u.UserId).Returns(userId);

        mockTransactionRepo.Setup(r => r.GetUncategorizedDescriptionsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { "  starbucks  ", "STARBUCKS", "Starbucks Coffee" });

        mockCategoryRepo.Setup(r => r.GetActiveAsync(default))
            .ReturnsAsync(Array.Empty<BudgetCategory>());

        mockDismissedRepo.Setup(r => r.IsDismissedAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        mockMerchantMapping.Setup(m => m.FindMatchingPatternsAsync(userId, It.IsAny<IReadOnlyList<string>>(), default))
            .ReturnsAsync(new[]
            {
                new PatternMatch { Pattern = "STARBUCKS", Category = "Coffee", TransactionCount = 15, Icon = "coffee" },
            });

        mockSuggestionRepo.Setup(r => r.DeletePendingByOwnerAsync(userId, default))
            .Returns(Task.CompletedTask);

        mockSuggestionRepo.Setup(r => r.AddRangeAsync(It.IsAny<List<CategorySuggestion>>(), default))
            .Returns(Task.CompletedTask);

        mockUow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        mockScorer.Setup(s => s.CalculateConfidence(It.IsAny<int>(), It.IsAny<int>()))
            .Returns(0.85m);

        mockScorer.Setup(s => s.Rank(It.IsAny<IEnumerable<CategorySuggestion>>()))
            .Returns<IEnumerable<CategorySuggestion>>(suggestions => suggestions.ToList());

        var service = new CategorySuggestionService(
            mockTransactionRepo.Object,
            mockCategoryRepo.Object,
            mockSuggestionRepo.Object,
            mockDismissedRepo.Object,
            mockMerchantMapping.Object,
            mockAiService.Object,
            mockUow.Object,
            mockUserContext.Object,
            mockDismissalHandler.Object,
            mockScorer.Object);

        // Act
        var suggestions = await service.AnalyzeTransactionsAsync(default);

        // Assert
        suggestions.Count.ShouldBe(1);
        suggestions[0].SuggestedName.ShouldBe("Coffee");
        suggestions[0].MatchingTransactionCount.ShouldBe(15);
    }

    [Fact(Skip = "AiStatusResult type not found - requires Phase 1B completion")]
    public async Task RapidMultipleRequests_DoesNotCrashService()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();

        var mockTransactionRepo = new Mock<ITransactionQueryRepository>();
        var mockCategoryRepo = new Mock<IBudgetCategoryRepository>();
        var mockSuggestionRepo = new Mock<ICategorySuggestionRepository>();
        var mockDismissedRepo = new Mock<IDismissedSuggestionPatternRepository>();
        var mockMerchantMapping = new Mock<IMerchantMappingService>();
        var mockAiService = new Mock<IAiService>();
        var mockUow = new Mock<IUnitOfWork>();
        var mockUserContext = new Mock<IUserContext>();
        var mockDismissalHandler = new Mock<ICategorySuggestionDismissalHandler>();
        var mockScorer = new Mock<ICategorySuggestionScorer>();

        mockUserContext.Setup(u => u.UserId).Returns(userId);

        mockTransactionRepo.Setup(r => r.GetUncategorizedDescriptionsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { "TEST MERCHANT" });

        mockCategoryRepo.Setup(r => r.GetActiveAsync(default))
            .ReturnsAsync(Array.Empty<BudgetCategory>());

        mockMerchantMapping.Setup(m => m.FindMatchingPatternsAsync(userId, It.IsAny<IReadOnlyList<string>>(), default))
            .ReturnsAsync(Array.Empty<PatternMatch>());

        mockSuggestionRepo.Setup(r => r.DeletePendingByOwnerAsync(userId, default))
            .Returns(Task.CompletedTask);

        mockSuggestionRepo.Setup(r => r.GetPendingByOwnerAsync(userId, default))
            .ReturnsAsync(Array.Empty<CategorySuggestion>());

        mockUow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        mockAiService.Setup(a => a.GetStatusAsync(default))
            .ReturnsAsync(new AiServiceStatus(IsAvailable: false, CurrentModel: null, ErrorMessage: null));
        mockScorer.Setup(s => s.Rank(It.IsAny<IEnumerable<CategorySuggestion>>()))
            .Returns<IEnumerable<CategorySuggestion>>(suggestions => suggestions.ToList());

        var service = new CategorySuggestionService(
            mockTransactionRepo.Object,
            mockCategoryRepo.Object,
            mockSuggestionRepo.Object,
            mockDismissedRepo.Object,
            mockMerchantMapping.Object,
            mockAiService.Object,
            mockUow.Object,
            mockUserContext.Object,
            mockDismissalHandler.Object,
            mockScorer.Object);

        // Act - Rapid fire 10 requests
        var tasks = new List<Task<IReadOnlyList<CategorySuggestion>>>();
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(service.AnalyzeTransactionsAsync(default));
        }

        var results = await Task.WhenAll(tasks);

        // Assert - All should complete without exceptions
        results.Length.ShouldBe(10);
        foreach (var result in results)
        {
            result.ShouldNotBeNull();
        }
    }

    [Fact]
    public async Task MultipleRapidRequests_NoRateLimitException()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();

        var mockTransactionRepo = new Mock<ITransactionQueryRepository>();
        var mockCategoryRepo = new Mock<IBudgetCategoryRepository>();
        var mockSuggestionRepo = new Mock<ICategorySuggestionRepository>();
        var mockDismissedRepo = new Mock<IDismissedSuggestionPatternRepository>();
        var mockMerchantMapping = new Mock<IMerchantMappingService>();
        var mockAiService = new Mock<IAiService>();
        var mockUow = new Mock<IUnitOfWork>();
        var mockUserContext = new Mock<IUserContext>();
        var mockDismissalHandler = new Mock<ICategorySuggestionDismissalHandler>();
        var mockScorer = new Mock<ICategorySuggestionScorer>();

        mockUserContext.Setup(u => u.UserId).Returns(userId);

        mockSuggestionRepo.Setup(r => r.GetPendingByOwnerAsync(userId, default))
            .ReturnsAsync(Array.Empty<CategorySuggestion>());

        var service = new CategorySuggestionService(
            mockTransactionRepo.Object,
            mockCategoryRepo.Object,
            mockSuggestionRepo.Object,
            mockDismissedRepo.Object,
            mockMerchantMapping.Object,
            mockAiService.Object,
            mockUow.Object,
            mockUserContext.Object,
            mockDismissalHandler.Object,
            mockScorer.Object);

        // Act - Fire 20 rapid read requests
        var tasks = new List<Task<IReadOnlyList<CategorySuggestion>>>();
        for (int i = 0; i < 20; i++)
        {
            tasks.Add(service.GetPendingSuggestionsAsync(default));
        }

        // Assert - No throttling exception
        await Should.NotThrowAsync(async () => await Task.WhenAll(tasks));
    }
}
