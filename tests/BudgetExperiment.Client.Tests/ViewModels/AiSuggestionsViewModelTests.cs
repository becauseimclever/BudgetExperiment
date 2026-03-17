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
        this._sut = new AiSuggestionsViewModel(
            this._aiService,
            this._categoryService,
            this._availabilityService,
            this._apiErrorContext);
        this._sut.OnStateChanged = () => this._stateChangedCount++;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        this._sut.Dispose();
    }

    // --- Initialization ---

    /// <summary>
    /// Verifies that InitializeAsync loads both rule and category suggestions.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task InitializeAsync_LoadsBothSuggestionTypes()
    {
        this._aiService.AiStatus = new AiStatusDto { IsAvailable = true, IsEnabled = true };
        this._aiService.PendingSuggestions.Add(CreateRuleSuggestion("Test Rule"));
        this._categoryService.PendingSuggestions.Add(CreateCategorySuggestion("Test Category"));

        await this._sut.InitializeAsync();

        this._sut.RuleSuggestions.Count.ShouldBe(1);
        this._sut.CategorySuggestions.Count.ShouldBe(1);
        this._sut.IsLoading.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that InitializeAsync loads AI status.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task InitializeAsync_LoadsAiStatus()
    {
        this._aiService.AiStatus = new AiStatusDto { IsAvailable = true, IsEnabled = true, CurrentModel = "llama3" };

        await this._sut.InitializeAsync();

        this._sut.Status.ShouldNotBeNull();
        this._sut.Status!.CurrentModel.ShouldBe("llama3");
        this._sut.IsAiAvailable.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies that InitializeAsync handles errors gracefully.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task InitializeAsync_SetsErrorMessage_WhenLoadFails()
    {
        this._aiService.AiStatus = new AiStatusDto { IsAvailable = true, IsEnabled = true };
        this._aiService.GetPendingSuggestionsException = new HttpRequestException("API down");

        await this._sut.InitializeAsync();

        this._sut.ErrorMessage.ShouldNotBeNull();
        this._sut.ErrorMessage.ShouldContain("Failed to load suggestions");
        this._sut.IsLoading.ShouldBeFalse();
    }

    // --- AI Availability ---

    /// <summary>
    /// Verifies IsAiAvailable is false when AI is not configured.
    /// </summary>
    [Fact]
    public void IsAiAvailable_ReturnsFalse_WhenStatusIsNull()
    {
        this._sut.IsAiAvailable.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies IsAiAvailable is false when AI is disabled.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task IsAiAvailable_ReturnsFalse_WhenDisabled()
    {
        this._aiService.AiStatus = new AiStatusDto { IsAvailable = true, IsEnabled = false };

        await this._sut.LoadStatusAsync();

        this._sut.IsAiAvailable.ShouldBeFalse();
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
        this._aiService.AiStatus = new AiStatusDto { IsAvailable = true, IsEnabled = true };
        this._aiService.PendingSuggestions.Add(CreateRuleSuggestion("Rule1", type: "NewRule"));
        this._aiService.PendingSuggestions.Add(CreateRuleSuggestion("Rule2", type: "PatternOptimization"));
        this._aiService.PendingSuggestions.Add(CreateRuleSuggestion("Rule3", type: "RuleConflict"));
        this._categoryService.PendingSuggestions.Add(CreateCategorySuggestion("Cat1"));

        await this._sut.InitializeAsync();

        var groups = this._sut.GroupedSuggestions;
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
        this._aiService.AiStatus = new AiStatusDto { IsAvailable = true, IsEnabled = true };
        this._categoryService.PendingSuggestions.Add(CreateCategorySuggestion("Cat1"));

        await this._sut.InitializeAsync();

        var groups = this._sut.GroupedSuggestions;
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
        this._aiService.AiStatus = new AiStatusDto { IsAvailable = true, IsEnabled = true };
        this._aiService.PendingSuggestions.Add(CreateRuleSuggestion("Low", type: "NewRule", confidence: 0.3m, affectedCount: 5));
        this._aiService.PendingSuggestions.Add(CreateRuleSuggestion("High", type: "NewRule", confidence: 0.9m, affectedCount: 50));

        await this._sut.InitializeAsync();

        var group = this._sut.GroupedSuggestions.First(g => g.Key == "NewRules");
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
        this._aiService.AiStatus = new AiStatusDto { IsAvailable = true, IsEnabled = true };
        this._aiService.PendingSuggestions.Add(CreateRuleSuggestion("High1", type: "NewRule", confidence: 0.9m));
        this._aiService.PendingSuggestions.Add(CreateRuleSuggestion("High2", type: "NewRule", confidence: 0.85m));
        this._aiService.PendingSuggestions.Add(CreateRuleSuggestion("Low", type: "NewRule", confidence: 0.5m));

        await this._sut.InitializeAsync();

        var group = this._sut.GroupedSuggestions.First(g => g.Key == "NewRules");
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
        this._aiService.AiStatus = new AiStatusDto { IsAvailable = true, IsEnabled = true };
        var suggestion = CreateRuleSuggestion("Test");
        this._aiService.PendingSuggestions.Add(suggestion);
        this._aiService.AcceptSuggestionResult = new CategorizationRuleDto { Id = Guid.NewGuid(), Name = "TestRule" };
        await this._sut.InitializeAsync();

        await this._sut.AcceptRuleSuggestionAsync(suggestion.Id);

        this._sut.RuleSuggestions.Count.ShouldBe(0);
        this._sut.SuccessMessage.ShouldNotBeNull();
    }

    /// <summary>
    /// Verifies accepting a rule suggestion sets error when it fails.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task AcceptRuleSuggestionAsync_SetsError_WhenApiFails()
    {
        this._aiService.AiStatus = new AiStatusDto { IsAvailable = true, IsEnabled = true };
        this._aiService.PendingSuggestions.Add(CreateRuleSuggestion("Test"));
        this._aiService.AcceptSuggestionResult = null; // failure
        await this._sut.InitializeAsync();

        await this._sut.AcceptRuleSuggestionAsync(Guid.NewGuid());

        this._sut.ErrorMessage.ShouldBe("Failed to accept suggestion.");
    }

    // --- Dismiss Rule Suggestion ---

    /// <summary>
    /// Verifies dismissing a rule suggestion removes it from the list.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task DismissRuleSuggestionAsync_RemovesSuggestionFromList()
    {
        this._aiService.AiStatus = new AiStatusDto { IsAvailable = true, IsEnabled = true };
        var suggestion = CreateRuleSuggestion("Test");
        this._aiService.PendingSuggestions.Add(suggestion);
        this._aiService.DismissSuggestionResult = true;
        await this._sut.InitializeAsync();

        await this._sut.DismissRuleSuggestionAsync(suggestion.Id);

        this._sut.RuleSuggestions.Count.ShouldBe(0);
    }

    // --- Accept Category Suggestion ---

    /// <summary>
    /// Verifies accepting a category suggestion removes it from the list.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task AcceptCategorySuggestionAsync_RemovesSuggestionFromList()
    {
        this._aiService.AiStatus = new AiStatusDto { IsAvailable = true, IsEnabled = true };
        var suggestion = CreateCategorySuggestion("Cat");
        this._categoryService.PendingSuggestions.Add(suggestion);
        this._categoryService.AcceptResult = new AcceptCategorySuggestionResultDto
        {
            SuggestionId = suggestion.Id,
            Success = true,
            CategoryId = Guid.NewGuid(),
            CategoryName = "Cat",
        };
        await this._sut.InitializeAsync();

        await this._sut.AcceptCategorySuggestionAsync(suggestion.Id);

        this._sut.CategorySuggestions.Count.ShouldBe(0);
        this._sut.SuccessMessage!.ShouldContain("Cat");
    }

    // --- Dismiss Category Suggestion ---

    /// <summary>
    /// Verifies dismissing a category suggestion removes it from the list.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task DismissCategorySuggestionAsync_RemovesSuggestionFromList()
    {
        this._aiService.AiStatus = new AiStatusDto { IsAvailable = true, IsEnabled = true };
        var suggestion = CreateCategorySuggestion("Cat");
        this._categoryService.PendingSuggestions.Add(suggestion);
        this._categoryService.DismissResult = true;
        await this._sut.InitializeAsync();

        await this._sut.DismissCategorySuggestionAsync(suggestion.Id);

        this._sut.CategorySuggestions.Count.ShouldBe(0);
    }

    // --- Feedback ---

    /// <summary>
    /// Verifies providing feedback updates the suggestion in the list.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ProvideFeedbackAsync_UpdatesSuggestionFeedback()
    {
        this._aiService.AiStatus = new AiStatusDto { IsAvailable = true, IsEnabled = true };
        var suggestion = CreateRuleSuggestion("Test");
        this._aiService.PendingSuggestions.Add(suggestion);
        this._aiService.ProvideFeedbackResult = true;
        await this._sut.InitializeAsync();

        await this._sut.ProvideFeedbackAsync(suggestion.Id, true);

        this._sut.RuleSuggestions.First(s => s.Id == suggestion.Id).UserFeedbackPositive.ShouldBe(true);
    }

    // --- All Caught Up ---

    /// <summary>
    /// Verifies IsAllCaughtUp when AI is available and there are no suggestions.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task IsAllCaughtUp_ReturnsTrue_WhenNoSuggestionsAndAiAvailable()
    {
        this._aiService.AiStatus = new AiStatusDto { IsAvailable = true, IsEnabled = true };

        await this._sut.InitializeAsync();

        this._sut.IsAllCaughtUp.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies IsAllCaughtUp is false when there are suggestions present.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task IsAllCaughtUp_ReturnsFalse_WhenSuggestionsExist()
    {
        this._aiService.AiStatus = new AiStatusDto { IsAvailable = true, IsEnabled = true };
        this._aiService.PendingSuggestions.Add(CreateRuleSuggestion("Test"));

        await this._sut.InitializeAsync();

        this._sut.IsAllCaughtUp.ShouldBeFalse();
    }

    // --- Review Mode ---

    /// <summary>
    /// Verifies toggling review mode on and off.
    /// </summary>
    [Fact]
    public void ToggleReviewMode_TogglesState()
    {
        this._sut.IsReviewMode.ShouldBeFalse();

        this._sut.ToggleReviewMode();
        this._sut.IsReviewMode.ShouldBeTrue();
        this._sut.ReviewIndex.ShouldBe(0);

        this._sut.ToggleReviewMode();
        this._sut.IsReviewMode.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies NextReviewItem advances the index.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task NextReviewItem_AdvancesIndex()
    {
        this._aiService.AiStatus = new AiStatusDto { IsAvailable = true, IsEnabled = true };
        this._aiService.PendingSuggestions.Add(CreateRuleSuggestion("Rule1"));
        this._aiService.PendingSuggestions.Add(CreateRuleSuggestion("Rule2"));
        await this._sut.InitializeAsync();

        this._sut.ToggleReviewMode();
        this._sut.NextReviewItem();

        this._sut.ReviewIndex.ShouldBe(1);
    }

    /// <summary>
    /// Verifies NextReviewItem does not advance past the last item.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task NextReviewItem_DoesNotExceedTotal()
    {
        this._aiService.AiStatus = new AiStatusDto { IsAvailable = true, IsEnabled = true };
        this._aiService.PendingSuggestions.Add(CreateRuleSuggestion("Rule1"));
        await this._sut.InitializeAsync();

        this._sut.ToggleReviewMode();
        this._sut.NextReviewItem();
        this._sut.NextReviewItem(); // should not advance

        this._sut.ReviewIndex.ShouldBe(0);
    }

    // --- Dismiss Messages ---

    /// <summary>
    /// Verifies DismissError clears the error message.
    /// </summary>
    [Fact]
    public void DismissError_ClearsErrorMessage()
    {
        this._aiService.GetPendingSuggestionsException = new HttpRequestException("fail");

        this._sut.DismissError();

        this._sut.ErrorMessage.ShouldBeNull();
    }

    /// <summary>
    /// Verifies DismissSuccess clears the success message.
    /// </summary>
    [Fact]
    public void DismissSuccess_ClearsSuccessMessage()
    {
        this._sut.DismissSuccess();

        this._sut.SuccessMessage.ShouldBeNull();
    }

    /// <summary>
    /// Verifies DismissAnalysisResult clears analysis state.
    /// </summary>
    [Fact]
    public void DismissAnalysisResult_ClearsAnalysisState()
    {
        this._sut.DismissAnalysisResult();

        this._sut.RuleAnalysisResult.ShouldBeNull();
        this._sut.CategoryAnalysisCount.ShouldBeNull();
        this._sut.AnalysisError.ShouldBeNull();
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
        await this._sut.StartAnalysisAsync();

        this._sut.RuleAnalysisResult.ShouldBeNull();
        this._sut.CategoryAnalysisCount.ShouldBeNull();
    }

    /// <summary>
    /// Verifies StartAnalysisAsync runs both analyses.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task StartAnalysisAsync_RunsBothAnalyses()
    {
        this._aiService.AiStatus = new AiStatusDto { IsAvailable = true, IsEnabled = true };
        this._aiService.AnalyzeResult = new AnalysisResponseDto { NewRuleSuggestions = 3 };
        this._categoryService.AnalyzeResult.Add(CreateCategorySuggestion("New"));
        await this._sut.LoadStatusAsync();

        await this._sut.StartAnalysisAsync();

        this._sut.RuleAnalysisResult.ShouldNotBeNull();
        this._sut.RuleAnalysisResult!.NewRuleSuggestions.ShouldBe(3);
        this._sut.CategoryAnalysisCount.ShouldBe(1);
        this._sut.IsAnalyzing.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies StartAnalysisAsync sets AnalysisError on failure.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task StartAnalysisAsync_SetsError_WhenBothReturnNothing()
    {
        this._aiService.AiStatus = new AiStatusDto { IsAvailable = true, IsEnabled = true };
        this._aiService.AnalyzeResult = null;
        await this._sut.LoadStatusAsync();

        await this._sut.StartAnalysisAsync();

        this._sut.AnalysisError.ShouldNotBeNull();
    }

    // --- TotalSuggestionCount ---

    /// <summary>
    /// Verifies TotalSuggestionCount is correct.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task TotalSuggestionCount_ReturnsCorrectTotal()
    {
        this._aiService.AiStatus = new AiStatusDto { IsAvailable = true, IsEnabled = true };
        this._aiService.PendingSuggestions.Add(CreateRuleSuggestion("R1"));
        this._aiService.PendingSuggestions.Add(CreateRuleSuggestion("R2"));
        this._categoryService.PendingSuggestions.Add(CreateCategorySuggestion("C1"));

        await this._sut.InitializeAsync();

        this._sut.TotalSuggestionCount.ShouldBe(3);
    }

    // --- Dismiss All ---

    /// <summary>
    /// Verifies DismissAllAsync clears all suggestions.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task DismissAllAsync_ClearsAllSuggestions()
    {
        this._aiService.AiStatus = new AiStatusDto { IsAvailable = true, IsEnabled = true };
        this._aiService.PendingSuggestions.Add(CreateRuleSuggestion("R1"));
        this._categoryService.PendingSuggestions.Add(CreateCategorySuggestion("C1"));
        this._aiService.DismissSuggestionResult = true;
        this._categoryService.DismissResult = true;
        await this._sut.InitializeAsync();

        await this._sut.DismissAllAsync();

        this._sut.RuleSuggestions.Count.ShouldBe(0);
        this._sut.CategorySuggestions.Count.ShouldBe(0);
        this._sut.SuccessMessage.ShouldBe("All suggestions dismissed.");
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
            CreatedAtUtc = DateTime.UtcNow,
        };
    }
}
