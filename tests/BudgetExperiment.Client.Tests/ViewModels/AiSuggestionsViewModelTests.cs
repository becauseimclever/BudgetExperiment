// <copyright file="AiSuggestionsViewModelTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Tests.TestHelpers;
using BudgetExperiment.Client.ViewModels;
using BudgetExperiment.Contracts.Dtos;

using Shouldly;

namespace BudgetExperiment.Client.Tests.ViewModels;

/// <summary>
/// Unit tests for <see cref="AiSuggestionsViewModel"/>.
/// </summary>
public sealed class AiSuggestionsViewModelTests : IDisposable
{
    private readonly StubAiApiService _aiService = new();
    private readonly StubCategorySuggestionApiService _categoryService = new();
    private readonly StubAiAvailabilityService _availabilityService = new();
    private readonly StubApiErrorContext _apiErrorContext = new();
    private readonly AiSuggestionsViewModel _sut;
    private int _stateChangedCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="AiSuggestionsViewModelTests"/> class.
    /// </summary>
    public AiSuggestionsViewModelTests()
    {
        _sut = new AiSuggestionsViewModel(
            _aiService,
            _categoryService,
            _availabilityService,
            _apiErrorContext);
        _sut.OnStateChanged = () => _stateChangedCount++;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _sut.Dispose();
    }

    // --- Initialization ---

    /// <summary>
    /// Verifies that InitializeAsync loads both rule and category suggestions.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task InitializeAsync_LoadsBothSuggestionTypes()
    {
        _aiService.AiStatus = new AiStatusDto { IsAvailable = true, IsEnabled = true };
        _aiService.PendingSuggestions.Add(CreateRuleSuggestion("Test Rule"));
        _categoryService.PendingSuggestions.Add(CreateCategorySuggestion("Test Category"));

        await _sut.InitializeAsync();

        _sut.RuleSuggestions.Count.ShouldBe(1);
        _sut.CategorySuggestions.Count.ShouldBe(1);
        _sut.IsLoading.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that InitializeAsync loads AI status.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task InitializeAsync_LoadsAiStatus()
    {
        _aiService.AiStatus = new AiStatusDto { IsAvailable = true, IsEnabled = true, CurrentModel = "llama3" };

        await _sut.InitializeAsync();

        _sut.Status.ShouldNotBeNull();
        _sut.Status!.CurrentModel.ShouldBe("llama3");
        _sut.IsAiAvailable.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies that InitializeAsync handles errors gracefully.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task InitializeAsync_SetsErrorMessage_WhenLoadFails()
    {
        _aiService.AiStatus = new AiStatusDto { IsAvailable = true, IsEnabled = true };
        _aiService.GetPendingSuggestionsException = new HttpRequestException("API down");

        await _sut.InitializeAsync();

        _sut.ErrorMessage.ShouldNotBeNull();
        _sut.ErrorMessage.ShouldContain("Failed to load suggestions");
        _sut.IsLoading.ShouldBeFalse();
    }

    // --- AI Availability ---

    /// <summary>
    /// Verifies IsAiAvailable is false when AI is not configured.
    /// </summary>
    [Fact]
    public void IsAiAvailable_ReturnsFalse_WhenStatusIsNull()
    {
        _sut.IsAiAvailable.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies IsAiAvailable is false when AI is disabled.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task IsAiAvailable_ReturnsFalse_WhenDisabled()
    {
        _aiService.AiStatus = new AiStatusDto { IsAvailable = true, IsEnabled = false };

        await _sut.LoadStatusAsync();

        _sut.IsAiAvailable.ShouldBeFalse();
    }

    // --- Composite Score ---

    /// <summary>
    /// Verifies composite score computation: confidence × impact.
    /// </summary>
    [Fact]
    public void ComputeCompositeScore_MultipliesConfidenceByImpact()
    {
        AiSuggestionsViewModel.ComputeCompositeScore(0.9m, 100).ShouldBe(90m);
    }

    /// <summary>
    /// Verifies composite score uses minimum 1 for transaction count.
    /// </summary>
    [Fact]
    public void ComputeCompositeScore_UsesMinimumOneForTransactionCount()
    {
        AiSuggestionsViewModel.ComputeCompositeScore(0.8m, 0).ShouldBe(0.8m);
    }

    // --- Grouping ---

    /// <summary>
    /// Verifies suggestions are grouped in correct priority order.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GroupedSuggestions_ReturnsGroupsInPriorityOrder()
    {
        _aiService.AiStatus = new AiStatusDto { IsAvailable = true, IsEnabled = true };
        _aiService.PendingSuggestions.Add(CreateRuleSuggestion("Rule1", type: "NewRule"));
        _aiService.PendingSuggestions.Add(CreateRuleSuggestion("Rule2", type: "PatternOptimization"));
        _aiService.PendingSuggestions.Add(CreateRuleSuggestion("Rule3", type: "RuleConflict"));
        _categoryService.PendingSuggestions.Add(CreateCategorySuggestion("Cat1"));

        await _sut.InitializeAsync();

        var groups = _sut.GroupedSuggestions;
        groups.Count.ShouldBe(4);
        groups[0].Key.ShouldBe("NewCategories");
        groups[1].Key.ShouldBe("NewRules");
        groups[2].Key.ShouldBe("Optimizations");
        groups[3].Key.ShouldBe("ConflictsCleanup");
    }

    /// <summary>
    /// Verifies empty groups are excluded.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GroupedSuggestions_ExcludesEmptyGroups()
    {
        _aiService.AiStatus = new AiStatusDto { IsAvailable = true, IsEnabled = true };
        _categoryService.PendingSuggestions.Add(CreateCategorySuggestion("Cat1"));

        await _sut.InitializeAsync();

        var groups = _sut.GroupedSuggestions;
        groups.Count.ShouldBe(1);
        groups[0].Key.ShouldBe("NewCategories");
    }

    /// <summary>
    /// Verifies items within groups are sorted by composite score descending.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GroupedSuggestions_SortsItemsByCompositeScoreDescending()
    {
        _aiService.AiStatus = new AiStatusDto { IsAvailable = true, IsEnabled = true };
        _aiService.PendingSuggestions.Add(CreateRuleSuggestion("Low", type: "NewRule", confidence: 0.3m, affectedCount: 5));
        _aiService.PendingSuggestions.Add(CreateRuleSuggestion("High", type: "NewRule", confidence: 0.9m, affectedCount: 50));

        await _sut.InitializeAsync();

        var group = _sut.GroupedSuggestions.First(g => g.Key == "NewRules");
        var firstItem = (RuleSuggestionDto)group.Items[0];
        firstItem.Title.ShouldBe("High");
    }

    /// <summary>
    /// Verifies HighConfidenceCount is set correctly.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GroupedSuggestions_SetsHighConfidenceCount()
    {
        _aiService.AiStatus = new AiStatusDto { IsAvailable = true, IsEnabled = true };
        _aiService.PendingSuggestions.Add(CreateRuleSuggestion("High1", type: "NewRule", confidence: 0.9m));
        _aiService.PendingSuggestions.Add(CreateRuleSuggestion("High2", type: "NewRule", confidence: 0.85m));
        _aiService.PendingSuggestions.Add(CreateRuleSuggestion("Low", type: "NewRule", confidence: 0.5m));

        await _sut.InitializeAsync();

        var group = _sut.GroupedSuggestions.First(g => g.Key == "NewRules");
        group.HighConfidenceCount.ShouldBe(2);
    }

    // --- Accept Rule Suggestion ---

    /// <summary>
    /// Verifies accepting a rule suggestion removes it from the list.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task AcceptRuleSuggestionAsync_RemovesSuggestionFromList()
    {
        _aiService.AiStatus = new AiStatusDto { IsAvailable = true, IsEnabled = true };
        var suggestion = CreateRuleSuggestion("Test");
        _aiService.PendingSuggestions.Add(suggestion);
        _aiService.AcceptSuggestionResult = new CategorizationRuleDto { Id = Guid.NewGuid(), Name = "TestRule" };
        await _sut.InitializeAsync();

        await _sut.AcceptRuleSuggestionAsync(suggestion.Id);

        _sut.RuleSuggestions.Count.ShouldBe(0);
        _sut.SuccessMessage.ShouldNotBeNull();
    }

    /// <summary>
    /// Verifies accepting a rule suggestion sets error when it fails.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task AcceptRuleSuggestionAsync_SetsError_WhenApiFails()
    {
        _aiService.AiStatus = new AiStatusDto { IsAvailable = true, IsEnabled = true };
        _aiService.PendingSuggestions.Add(CreateRuleSuggestion("Test"));
        _aiService.AcceptSuggestionResult = null; // failure
        await _sut.InitializeAsync();

        await _sut.AcceptRuleSuggestionAsync(Guid.NewGuid());

        _sut.ErrorMessage.ShouldBe("Failed to accept suggestion.");
    }

    // --- Dismiss Rule Suggestion ---

    /// <summary>
    /// Verifies dismissing a rule suggestion removes it from the list.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task DismissRuleSuggestionAsync_RemovesSuggestionFromList()
    {
        _aiService.AiStatus = new AiStatusDto { IsAvailable = true, IsEnabled = true };
        var suggestion = CreateRuleSuggestion("Test");
        _aiService.PendingSuggestions.Add(suggestion);
        _aiService.DismissSuggestionResult = true;
        await _sut.InitializeAsync();

        await _sut.DismissRuleSuggestionAsync(suggestion.Id);

        _sut.RuleSuggestions.Count.ShouldBe(0);
    }

    // --- Accept Category Suggestion ---

    /// <summary>
    /// Verifies accepting a category suggestion removes it from the list.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task AcceptCategorySuggestionAsync_RemovesSuggestionFromList()
    {
        _aiService.AiStatus = new AiStatusDto { IsAvailable = true, IsEnabled = true };
        var suggestion = CreateCategorySuggestion("Cat");
        _categoryService.PendingSuggestions.Add(suggestion);
        _categoryService.AcceptResult = new AcceptCategorySuggestionResultDto
        {
            SuggestionId = suggestion.Id,
            Success = true,
            CategoryId = Guid.NewGuid(),
            CategoryName = "Cat",
        };
        await _sut.InitializeAsync();

        await _sut.AcceptCategorySuggestionAsync(suggestion.Id);

        _sut.CategorySuggestions.Count.ShouldBe(0);
        _sut.SuccessMessage!.ShouldContain("Cat");
    }

    // --- Dismiss Category Suggestion ---

    /// <summary>
    /// Verifies dismissing a category suggestion removes it from the list.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task DismissCategorySuggestionAsync_RemovesSuggestionFromList()
    {
        _aiService.AiStatus = new AiStatusDto { IsAvailable = true, IsEnabled = true };
        var suggestion = CreateCategorySuggestion("Cat");
        _categoryService.PendingSuggestions.Add(suggestion);
        _categoryService.DismissResult = true;
        await _sut.InitializeAsync();

        await _sut.DismissCategorySuggestionAsync(suggestion.Id);

        _sut.CategorySuggestions.Count.ShouldBe(0);
    }

    // --- Feedback ---

    /// <summary>
    /// Verifies providing feedback updates the suggestion in the list.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ProvideFeedbackAsync_UpdatesSuggestionFeedback()
    {
        _aiService.AiStatus = new AiStatusDto { IsAvailable = true, IsEnabled = true };
        var suggestion = CreateRuleSuggestion("Test");
        _aiService.PendingSuggestions.Add(suggestion);
        _aiService.ProvideFeedbackResult = true;
        await _sut.InitializeAsync();

        await _sut.ProvideFeedbackAsync(suggestion.Id, true);

        _sut.RuleSuggestions.First(s => s.Id == suggestion.Id).UserFeedbackPositive.ShouldBe(true);
    }

    // --- All Caught Up ---

    /// <summary>
    /// Verifies IsAllCaughtUp when AI is available and there are no suggestions.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task IsAllCaughtUp_ReturnsTrue_WhenNoSuggestionsAndAiAvailable()
    {
        _aiService.AiStatus = new AiStatusDto { IsAvailable = true, IsEnabled = true };

        await _sut.InitializeAsync();

        _sut.IsAllCaughtUp.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies IsAllCaughtUp is false when there are suggestions present.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task IsAllCaughtUp_ReturnsFalse_WhenSuggestionsExist()
    {
        _aiService.AiStatus = new AiStatusDto { IsAvailable = true, IsEnabled = true };
        _aiService.PendingSuggestions.Add(CreateRuleSuggestion("Test"));

        await _sut.InitializeAsync();

        _sut.IsAllCaughtUp.ShouldBeFalse();
    }

    // --- Review Mode ---

    /// <summary>
    /// Verifies toggling review mode on and off.
    /// </summary>
    [Fact]
    public void ToggleReviewMode_TogglesState()
    {
        _sut.IsReviewMode.ShouldBeFalse();

        _sut.ToggleReviewMode();
        _sut.IsReviewMode.ShouldBeTrue();
        _sut.ReviewIndex.ShouldBe(0);

        _sut.ToggleReviewMode();
        _sut.IsReviewMode.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies NextReviewItem advances the index.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task NextReviewItem_AdvancesIndex()
    {
        _aiService.AiStatus = new AiStatusDto { IsAvailable = true, IsEnabled = true };
        _aiService.PendingSuggestions.Add(CreateRuleSuggestion("Rule1"));
        _aiService.PendingSuggestions.Add(CreateRuleSuggestion("Rule2"));
        await _sut.InitializeAsync();

        _sut.ToggleReviewMode();
        _sut.NextReviewItem();

        _sut.ReviewIndex.ShouldBe(1);
    }

    /// <summary>
    /// Verifies NextReviewItem does not advance past the last item.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task NextReviewItem_DoesNotExceedTotal()
    {
        _aiService.AiStatus = new AiStatusDto { IsAvailable = true, IsEnabled = true };
        _aiService.PendingSuggestions.Add(CreateRuleSuggestion("Rule1"));
        await _sut.InitializeAsync();

        _sut.ToggleReviewMode();
        _sut.NextReviewItem();
        _sut.NextReviewItem(); // should not advance

        _sut.ReviewIndex.ShouldBe(0);
    }

    // --- Dismiss Messages ---

    /// <summary>
    /// Verifies DismissError clears the error message.
    /// </summary>
    [Fact]
    public void DismissError_ClearsErrorMessage()
    {
        _aiService.GetPendingSuggestionsException = new HttpRequestException("fail");

        _sut.DismissError();

        _sut.ErrorMessage.ShouldBeNull();
    }

    /// <summary>
    /// Verifies DismissSuccess clears the success message.
    /// </summary>
    [Fact]
    public void DismissSuccess_ClearsSuccessMessage()
    {
        _sut.DismissSuccess();

        _sut.SuccessMessage.ShouldBeNull();
    }

    /// <summary>
    /// Verifies DismissAnalysisResult clears analysis state.
    /// </summary>
    [Fact]
    public void DismissAnalysisResult_ClearsAnalysisState()
    {
        _sut.DismissAnalysisResult();

        _sut.RuleAnalysisResult.ShouldBeNull();
        _sut.CategoryAnalysisCount.ShouldBeNull();
        _sut.AnalysisError.ShouldBeNull();
    }

    // --- Analysis ---

    /// <summary>
    /// Verifies StartAnalysisAsync does nothing when AI is unavailable.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task StartAnalysisAsync_DoesNothing_WhenAiUnavailable()
    {
        // Status is null => not available
        await _sut.StartAnalysisAsync();

        _sut.RuleAnalysisResult.ShouldBeNull();
        _sut.CategoryAnalysisCount.ShouldBeNull();
    }

    /// <summary>
    /// Verifies StartAnalysisAsync runs both analyses.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task StartAnalysisAsync_RunsBothAnalyses()
    {
        _aiService.AiStatus = new AiStatusDto { IsAvailable = true, IsEnabled = true };
        _aiService.AnalyzeResult = new AnalysisResponseDto { NewRuleSuggestions = 3 };
        _categoryService.AnalyzeResult.Add(CreateCategorySuggestion("New"));
        await _sut.LoadStatusAsync();

        await _sut.StartAnalysisAsync();

        _sut.RuleAnalysisResult.ShouldNotBeNull();
        _sut.RuleAnalysisResult!.NewRuleSuggestions.ShouldBe(3);
        _sut.CategoryAnalysisCount.ShouldBe(1);
        _sut.IsAnalyzing.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies StartAnalysisAsync sets AnalysisError on failure.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task StartAnalysisAsync_SetsError_WhenBothReturnNothing()
    {
        _aiService.AiStatus = new AiStatusDto { IsAvailable = true, IsEnabled = true };
        _aiService.AnalyzeResult = null;
        await _sut.LoadStatusAsync();

        await _sut.StartAnalysisAsync();

        _sut.AnalysisError.ShouldNotBeNull();
    }

    // --- TotalSuggestionCount ---

    /// <summary>
    /// Verifies TotalSuggestionCount is correct.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task TotalSuggestionCount_ReturnsCorrectTotal()
    {
        _aiService.AiStatus = new AiStatusDto { IsAvailable = true, IsEnabled = true };
        _aiService.PendingSuggestions.Add(CreateRuleSuggestion("R1"));
        _aiService.PendingSuggestions.Add(CreateRuleSuggestion("R2"));
        _categoryService.PendingSuggestions.Add(CreateCategorySuggestion("C1"));

        await _sut.InitializeAsync();

        _sut.TotalSuggestionCount.ShouldBe(3);
    }

    // --- Dismiss All ---

    /// <summary>
    /// Verifies DismissAllAsync clears all suggestions.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task DismissAllAsync_ClearsAllSuggestions()
    {
        _aiService.AiStatus = new AiStatusDto { IsAvailable = true, IsEnabled = true };
        _aiService.PendingSuggestions.Add(CreateRuleSuggestion("R1"));
        _categoryService.PendingSuggestions.Add(CreateCategorySuggestion("C1"));
        _aiService.DismissSuggestionResult = true;
        _categoryService.DismissResult = true;
        await _sut.InitializeAsync();

        await _sut.DismissAllAsync();

        _sut.RuleSuggestions.Count.ShouldBe(0);
        _sut.CategorySuggestions.Count.ShouldBe(0);
        _sut.SuccessMessage.ShouldBe("All suggestions dismissed.");
    }

    // --- Helpers ---
    private static RuleSuggestionDto CreateRuleSuggestion(
        string title,
        string type = "NewRule",
        decimal confidence = 0.8m,
        int affectedCount = 10)
    {
        return new RuleSuggestionDto
        {
            Id = Guid.NewGuid(),
            Type = type,
            Status = "Pending",
            Title = title,
            Description = $"Description for {title}",
            Reasoning = "AI reasoning",
            Confidence = confidence,
            SuggestedPattern = "TEST*",
            SuggestedMatchType = "Contains",
            AffectedTransactionCount = affectedCount,
            SampleDescriptions = new[] { "Sample 1", "Sample 2" },
            CreatedAtUtc = DateTime.UtcNow,
        };
    }

    private static CategorySuggestionDto CreateCategorySuggestion(string name, decimal confidence = 0.85m)
    {
        return new CategorySuggestionDto
        {
            Id = Guid.NewGuid(),
            SuggestedName = name,
            SuggestedType = "Expense",
            Confidence = confidence,
            MerchantPatterns = new[] { "PATTERN1", "PATTERN2" },
            MatchingTransactionCount = 15,
            Status = "Pending",
            Source = "PatternMatch",
            CreatedAtUtc = DateTime.UtcNow,
        };
    }
}
