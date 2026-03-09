// <copyright file="AiSuggestionsViewModel.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Services;
using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Client.ViewModels;

/// <summary>
/// ViewModel for the unified AI Suggestions page that handles both rule and category suggestions.
/// </summary>
public sealed class AiSuggestionsViewModel : IDisposable
{
    private readonly IAiApiService _aiService;
    private readonly ICategorySuggestionApiService _categoryService;
    private readonly IAiAvailabilityService _availabilityService;

    private System.Timers.Timer? _elapsedTimer;
    private DateTime _analysisStartTime;
    private CancellationTokenSource? _cts;

    /// <summary>
    /// Initializes a new instance of the <see cref="AiSuggestionsViewModel"/> class.
    /// </summary>
    /// <param name="aiService">The AI API service for rule suggestions.</param>
    /// <param name="categoryService">The category suggestion API service.</param>
    /// <param name="availabilityService">The AI availability service.</param>
    public AiSuggestionsViewModel(
        IAiApiService aiService,
        ICategorySuggestionApiService categoryService,
        IAiAvailabilityService availabilityService)
    {
        this._aiService = aiService;
        this._categoryService = categoryService;
        this._availabilityService = availabilityService;
    }

    /// <summary>
    /// Gets or sets the callback to notify the Razor page that state has changed and it should re-render.
    /// </summary>
    public Action? OnStateChanged { get; set; }

    /// <summary>
    /// Gets the list of pending rule suggestions.
    /// </summary>
    public IReadOnlyList<RuleSuggestionDto> RuleSuggestions { get; private set; } = Array.Empty<RuleSuggestionDto>();

    /// <summary>
    /// Gets the list of pending category suggestions.
    /// </summary>
    public IReadOnlyList<CategorySuggestionDto> CategorySuggestions { get; private set; } = Array.Empty<CategorySuggestionDto>();

    /// <summary>
    /// Gets the AI status.
    /// </summary>
    public AiStatusDto? Status { get; private set; }

    /// <summary>
    /// Gets a value indicating whether data is loading.
    /// </summary>
    public bool IsLoading { get; private set; }

    /// <summary>
    /// Gets a value indicating whether AI analysis is running.
    /// </summary>
    public bool IsAnalyzing { get; private set; }

    /// <summary>
    /// Gets a value indicating whether a suggestion action is processing.
    /// </summary>
    public bool IsProcessing { get; private set; }

    /// <summary>
    /// Gets the current error message.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Gets the current success message.
    /// </summary>
    public string? SuccessMessage { get; private set; }

    /// <summary>
    /// Gets the analysis elapsed time.
    /// </summary>
    public TimeSpan AnalysisElapsed { get; private set; }

    /// <summary>
    /// Gets the analysis error message.
    /// </summary>
    public string? AnalysisError { get; private set; }

    /// <summary>
    /// Gets the analysis result for rule suggestions.
    /// </summary>
    public AnalysisResponseDto? RuleAnalysisResult { get; private set; }

    /// <summary>
    /// Gets the count of newly generated category suggestions.
    /// </summary>
    public int? CategoryAnalysisCount { get; private set; }

    /// <summary>
    /// Gets the suggestion quality metrics.
    /// </summary>
    public SuggestionMetricsDto? Metrics { get; private set; }

    /// <summary>
    /// Gets a value indicating whether AI is available and enabled.
    /// </summary>
    public bool IsAiAvailable => this.Status?.IsAvailable == true && this.Status?.IsEnabled == true;

    /// <summary>
    /// Gets a value indicating whether all suggestions have been handled.
    /// </summary>
    public bool IsAllCaughtUp => !this.IsLoading && this.RuleSuggestions.Count == 0 && this.CategorySuggestions.Count == 0 && this.IsAiAvailable;

    /// <summary>
    /// Gets the total count of all suggestions.
    /// </summary>
    public int TotalSuggestionCount => this.RuleSuggestions.Count + this.CategorySuggestions.Count;

    /// <summary>
    /// Gets the grouped and sorted suggestions. Groups by type with priority ordering.
    /// </summary>
    public IReadOnlyList<SuggestionGroupModel> GroupedSuggestions => this.BuildGroups();

    /// <summary>
    /// Gets a value indicating whether review mode is active.
    /// </summary>
    public bool IsReviewMode { get; private set; }

    /// <summary>
    /// Gets the index of the current suggestion in review mode.
    /// </summary>
    public int ReviewIndex { get; private set; }

    /// <summary>
    /// Computes a composite score for ranking suggestions (confidence × impact).
    /// </summary>
    /// <param name="confidence">The confidence score (0-1).</param>
    /// <param name="transactionCount">The number of affected transactions.</param>
    /// <returns>The composite score.</returns>
    public static decimal ComputeCompositeScore(decimal confidence, int transactionCount)
    {
        return confidence * Math.Max(1, transactionCount);
    }

    /// <summary>
    /// Initializes the ViewModel by loading status and suggestions.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    public async Task InitializeAsync()
    {
        await this.LoadStatusAsync();
        await this.LoadAllSuggestionsAsync();
        await this.LoadMetricsAsync();
    }

    /// <summary>
    /// Loads the AI status from the API.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    public async Task LoadStatusAsync()
    {
        try
        {
            this.Status = await this._aiService.GetStatusAsync();
        }
        catch (Exception ex)
        {
            this.ErrorMessage = $"Failed to load AI status: {ex.Message}";
        }
    }

    /// <summary>
    /// Loads all suggestions (both rule and category) from the API.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    public async Task LoadAllSuggestionsAsync()
    {
        this.IsLoading = true;
        this.ErrorMessage = null;

        try
        {
            this._cts = new CancellationTokenSource();
            var ruleTask = this._aiService.GetPendingSuggestionsAsync();
            var categoryTask = this._categoryService.GetPendingAsync(this._cts.Token);

            await Task.WhenAll(ruleTask, categoryTask);

            this.RuleSuggestions = await ruleTask;
            this.CategorySuggestions = await categoryTask;
        }
        catch (Exception ex)
        {
            this.ErrorMessage = $"Failed to load suggestions: {ex.Message}";
            this.RuleSuggestions = Array.Empty<RuleSuggestionDto>();
            this.CategorySuggestions = Array.Empty<CategorySuggestionDto>();
        }
        finally
        {
            this.IsLoading = false;
        }
    }

    /// <summary>
    /// Loads suggestion quality metrics from the API.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    public async Task LoadMetricsAsync()
    {
        try
        {
            this.Metrics = await this._aiService.GetMetricsAsync();
        }
        catch (Exception)
        {
            // Metrics are non-critical; silently ignore failures
        }
    }

    /// <summary>
    /// Starts the AI analysis for both rule and category suggestions.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    public async Task StartAnalysisAsync()
    {
        if (this.IsAnalyzing || !this.IsAiAvailable)
        {
            return;
        }

        this.IsAnalyzing = true;
        this.RuleAnalysisResult = null;
        this.CategoryAnalysisCount = null;
        this.AnalysisError = null;
        this.AnalysisElapsed = TimeSpan.Zero;
        this._analysisStartTime = DateTime.UtcNow;
        this.SuccessMessage = null;

        this._elapsedTimer = new System.Timers.Timer(1000);
        this._elapsedTimer.Elapsed += this.OnElapsedTick;
        this._elapsedTimer.Start();

        this.NotifyStateChanged();

        try
        {
            this._cts = new CancellationTokenSource();
            var ruleTask = this._aiService.AnalyzeAsync();
            var categoryTask = this._categoryService.AnalyzeAsync(this._cts.Token);

            await Task.WhenAll(ruleTask, categoryTask);

            this.RuleAnalysisResult = await ruleTask;
            var categoryResults = await categoryTask;
            this.CategoryAnalysisCount = categoryResults.Count;

            if (this.RuleAnalysisResult == null && categoryResults.Count == 0)
            {
                this.AnalysisError = "Analysis completed but produced no results. AI service may be unavailable.";
            }
            else
            {
                await this.LoadAllSuggestionsAsync();
                await this.LoadMetricsAsync();
            }
        }
        catch (Exception ex)
        {
            this.AnalysisError = $"Analysis failed: {ex.Message}";
        }
        finally
        {
            this.IsAnalyzing = false;
            this.StopElapsedTimer();
            this.NotifyStateChanged();
        }
    }

    /// <summary>
    /// Accepts a rule suggestion.
    /// </summary>
    /// <param name="id">The suggestion ID.</param>
    /// <returns>A task representing the async operation.</returns>
    public async Task AcceptRuleSuggestionAsync(Guid id)
    {
        this.IsProcessing = true;

        try
        {
            var rule = await this._aiService.AcceptSuggestionAsync(id);
            if (rule != null)
            {
                this.RuleSuggestions = this.RuleSuggestions.Where(s => s.Id != id).ToList();
                this.SuccessMessage = $"Rule suggestion accepted and rule '{rule.Name}' created.";
            }
            else
            {
                this.ErrorMessage = "Failed to accept suggestion.";
            }
        }
        catch (Exception ex)
        {
            this.ErrorMessage = $"Failed to accept suggestion: {ex.Message}";
        }
        finally
        {
            this.IsProcessing = false;
            this.NotifyStateChanged();
        }
    }

    /// <summary>
    /// Dismisses a rule suggestion.
    /// </summary>
    /// <param name="id">The suggestion ID.</param>
    /// <returns>A task representing the async operation.</returns>
    public async Task DismissRuleSuggestionAsync(Guid id)
    {
        this.IsProcessing = true;

        try
        {
            var success = await this._aiService.DismissSuggestionAsync(id);
            if (success)
            {
                this.RuleSuggestions = this.RuleSuggestions.Where(s => s.Id != id).ToList();
            }
            else
            {
                this.ErrorMessage = "Failed to dismiss suggestion.";
            }
        }
        catch (Exception ex)
        {
            this.ErrorMessage = $"Failed to dismiss suggestion: {ex.Message}";
        }
        finally
        {
            this.IsProcessing = false;
            this.NotifyStateChanged();
        }
    }

    /// <summary>
    /// Accepts a category suggestion.
    /// </summary>
    /// <param name="id">The suggestion ID.</param>
    /// <param name="customName">Optional custom name for the category.</param>
    /// <param name="createRules">Whether to also create categorization rules.</param>
    /// <returns>A task representing the async operation.</returns>
    public async Task AcceptCategorySuggestionAsync(Guid id, string? customName = null, bool createRules = true)
    {
        this.IsProcessing = true;

        try
        {
            var suggestion = this.CategorySuggestions.FirstOrDefault(s => s.Id == id);
            var request = new AcceptCategorySuggestionRequest
            {
                CustomName = string.IsNullOrWhiteSpace(customName) ? null : customName,
            };

            var result = await this._categoryService.AcceptAsync(id, request);

            if (result.Success)
            {
                if (createRules && suggestion != null)
                {
                    var rulesRequest = new CreateRulesFromSuggestionRequest
                    {
                        CategoryId = result.CategoryId!.Value,
                        Patterns = suggestion.MerchantPatterns.ToList(),
                    };
                    await this._categoryService.CreateRulesAsync(id, rulesRequest);
                }

                this.CategorySuggestions = this.CategorySuggestions.Where(s => s.Id != id).ToList();
                this.SuccessMessage = $"Category '{result.CategoryName}' created successfully!";
            }
            else
            {
                this.ErrorMessage = "Failed to accept category suggestion.";
            }
        }
        catch (Exception ex)
        {
            this.ErrorMessage = $"Failed to accept category suggestion: {ex.Message}";
        }
        finally
        {
            this.IsProcessing = false;
            this.NotifyStateChanged();
        }
    }

    /// <summary>
    /// Dismisses a category suggestion.
    /// </summary>
    /// <param name="id">The suggestion ID.</param>
    /// <returns>A task representing the async operation.</returns>
    public async Task DismissCategorySuggestionAsync(Guid id)
    {
        this.IsProcessing = true;

        try
        {
            var success = await this._categoryService.DismissAsync(id);
            if (success)
            {
                this.CategorySuggestions = this.CategorySuggestions.Where(s => s.Id != id).ToList();
            }
            else
            {
                this.ErrorMessage = "Failed to dismiss suggestion.";
            }
        }
        catch (Exception ex)
        {
            this.ErrorMessage = $"Failed to dismiss suggestion: {ex.Message}";
        }
        finally
        {
            this.IsProcessing = false;
            this.NotifyStateChanged();
        }
    }

    /// <summary>
    /// Provides feedback on a rule suggestion.
    /// </summary>
    /// <param name="id">The suggestion ID.</param>
    /// <param name="isPositive">Whether the feedback is positive.</param>
    /// <returns>A task representing the async operation.</returns>
    public async Task ProvideFeedbackAsync(Guid id, bool isPositive)
    {
        try
        {
            var success = await this._aiService.ProvideFeedbackAsync(id, isPositive);
            if (success)
            {
                this.RuleSuggestions = this.RuleSuggestions.Select(s =>
                {
                    if (s.Id == id)
                    {
                        return new RuleSuggestionDto
                        {
                            Id = s.Id,
                            Type = s.Type,
                            Status = s.Status,
                            Title = s.Title,
                            Description = s.Description,
                            Reasoning = s.Reasoning,
                            Confidence = s.Confidence,
                            SuggestedPattern = s.SuggestedPattern,
                            SuggestedMatchType = s.SuggestedMatchType,
                            SuggestedCategoryId = s.SuggestedCategoryId,
                            SuggestedCategoryName = s.SuggestedCategoryName,
                            TargetRuleId = s.TargetRuleId,
                            TargetRuleName = s.TargetRuleName,
                            OptimizedPattern = s.OptimizedPattern,
                            ConflictingRuleIds = s.ConflictingRuleIds,
                            AffectedTransactionCount = s.AffectedTransactionCount,
                            SampleDescriptions = s.SampleDescriptions,
                            CreatedAtUtc = s.CreatedAtUtc,
                            ReviewedAtUtc = s.ReviewedAtUtc,
                            UserFeedbackPositive = isPositive,
                        };
                    }

                    return s;
                }).ToList();
            }
        }
        catch (Exception ex)
        {
            this.ErrorMessage = $"Failed to submit feedback: {ex.Message}";
        }

        this.NotifyStateChanged();
    }

    /// <summary>
    /// Accepts all high-confidence suggestions in a group.
    /// </summary>
    /// <param name="groupKey">The group key to accept.</param>
    /// <returns>A task representing the async operation.</returns>
    public async Task AcceptAllHighConfidenceAsync(string groupKey)
    {
        this.IsProcessing = true;

        try
        {
            if (groupKey == "NewCategories")
            {
                var highConfidence = this.CategorySuggestions
                    .Where(s => s.Confidence >= 0.8m)
                    .Select(s => s.Id);
                var results = await this._categoryService.BulkAcceptAsync(highConfidence);
                var successCount = results.Count(r => r.Success);
                if (successCount > 0)
                {
                    this.SuccessMessage = $"Accepted {successCount} high-confidence category suggestion(s).";
                    await this.LoadAllSuggestionsAsync();
                }
            }
            else
            {
                var highConfidenceRules = this.RuleSuggestions
                    .Where(s => s.Confidence >= 0.8m && this.GetGroupKeyForRule(s) == groupKey)
                    .ToList();

                int accepted = 0;
                foreach (var rule in highConfidenceRules)
                {
                    var result = await this._aiService.AcceptSuggestionAsync(rule.Id);
                    if (result != null)
                    {
                        accepted++;
                    }
                }

                if (accepted > 0)
                {
                    this.SuccessMessage = $"Accepted {accepted} high-confidence rule suggestion(s).";
                    await this.LoadAllSuggestionsAsync();
                }
            }
        }
        catch (Exception ex)
        {
            this.ErrorMessage = $"Failed to accept suggestions: {ex.Message}";
        }
        finally
        {
            this.IsProcessing = false;
            this.NotifyStateChanged();
        }
    }

    /// <summary>
    /// Dismisses all suggestions across all groups.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    public async Task DismissAllAsync()
    {
        this.IsProcessing = true;

        try
        {
            foreach (var rule in this.RuleSuggestions.ToList())
            {
                await this._aiService.DismissSuggestionAsync(rule.Id);
            }

            foreach (var category in this.CategorySuggestions.ToList())
            {
                await this._categoryService.DismissAsync(category.Id);
            }

            this.RuleSuggestions = Array.Empty<RuleSuggestionDto>();
            this.CategorySuggestions = Array.Empty<CategorySuggestionDto>();
            this.SuccessMessage = "All suggestions dismissed.";
        }
        catch (Exception ex)
        {
            this.ErrorMessage = $"Failed to dismiss all suggestions: {ex.Message}";
            await this.LoadAllSuggestionsAsync();
        }
        finally
        {
            this.IsProcessing = false;
            this.NotifyStateChanged();
        }
    }

    /// <summary>
    /// Toggles review mode on or off.
    /// </summary>
    public void ToggleReviewMode()
    {
        this.IsReviewMode = !this.IsReviewMode;
        this.ReviewIndex = 0;
        this.NotifyStateChanged();
    }

    /// <summary>
    /// Advances to the next suggestion in review mode.
    /// </summary>
    public void NextReviewItem()
    {
        if (this.ReviewIndex < this.TotalSuggestionCount - 1)
        {
            this.ReviewIndex++;
            this.NotifyStateChanged();
        }
    }

    /// <summary>
    /// Skips the current suggestion in review mode (moves to next without action).
    /// </summary>
    public void SkipReviewItem()
    {
        this.NextReviewItem();
    }

    /// <summary>
    /// Dismisses the error message.
    /// </summary>
    public void DismissError()
    {
        this.ErrorMessage = null;
        this.NotifyStateChanged();
    }

    /// <summary>
    /// Dismisses the success message.
    /// </summary>
    public void DismissSuccess()
    {
        this.SuccessMessage = null;
        this.NotifyStateChanged();
    }

    /// <summary>
    /// Dismisses the analysis result.
    /// </summary>
    public void DismissAnalysisResult()
    {
        this.RuleAnalysisResult = null;
        this.CategoryAnalysisCount = null;
        this.AnalysisError = null;
        this.NotifyStateChanged();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        this.StopElapsedTimer();

        try
        {
            this._cts?.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // CTS was already disposed
        }

        this._cts?.Dispose();
    }

    private string GetGroupKeyForRule(RuleSuggestionDto rule)
    {
        return rule.Type switch
        {
            "NewRule" => "NewRules",
            "PatternOptimization" => "Optimizations",
            _ => "ConflictsCleanup",
        };
    }

    private IReadOnlyList<SuggestionGroupModel> BuildGroups()
    {
        var groups = new List<SuggestionGroupModel>();

        // Group 1: New Categories (highest priority for new users)
        if (this.CategorySuggestions.Count > 0)
        {
            groups.Add(new SuggestionGroupModel
            {
                Key = "NewCategories",
                Title = "New Categories",
                IconName = "tag",
                Items = this.CategorySuggestions
                    .OrderByDescending(s => ComputeCompositeScore(s.Confidence, s.MatchingTransactionCount))
                    .Cast<object>()
                    .ToList(),
                HighConfidenceCount = this.CategorySuggestions.Count(s => s.Confidence >= 0.8m),
            });
        }

        // Group 2: New Rules
        var newRules = this.RuleSuggestions.Where(s => s.Type == "NewRule").ToList();
        if (newRules.Count > 0)
        {
            groups.Add(new SuggestionGroupModel
            {
                Key = "NewRules",
                Title = "New Rules",
                IconName = "plus-circle",
                Items = newRules
                    .OrderByDescending(s => ComputeCompositeScore(s.Confidence, s.AffectedTransactionCount))
                    .Cast<object>()
                    .ToList(),
                HighConfidenceCount = newRules.Count(s => s.Confidence >= 0.8m),
            });
        }

        // Group 3: Optimizations
        var optimizations = this.RuleSuggestions.Where(s => s.Type == "PatternOptimization").ToList();
        if (optimizations.Count > 0)
        {
            groups.Add(new SuggestionGroupModel
            {
                Key = "Optimizations",
                Title = "Optimizations",
                IconName = "zap",
                Items = optimizations
                    .OrderByDescending(s => ComputeCompositeScore(s.Confidence, s.AffectedTransactionCount))
                    .Cast<object>()
                    .ToList(),
                HighConfidenceCount = optimizations.Count(s => s.Confidence >= 0.8m),
            });
        }

        // Group 4: Conflicts & Cleanup
        var conflictsCleanup = this.RuleSuggestions
            .Where(s => s.Type is "RuleConflict" or "RuleConsolidation" or "UnusedRule")
            .ToList();
        if (conflictsCleanup.Count > 0)
        {
            groups.Add(new SuggestionGroupModel
            {
                Key = "ConflictsCleanup",
                Title = "Conflicts & Cleanup",
                IconName = "alert-triangle",
                Items = conflictsCleanup
                    .OrderByDescending(s => ComputeCompositeScore(s.Confidence, s.AffectedTransactionCount))
                    .Cast<object>()
                    .ToList(),
                HighConfidenceCount = conflictsCleanup.Count(s => s.Confidence >= 0.8m),
            });
        }

        return groups;
    }

    private void OnElapsedTick(object? sender, System.Timers.ElapsedEventArgs e)
    {
        this.AnalysisElapsed = DateTime.UtcNow - this._analysisStartTime;
        this.NotifyStateChanged();
    }

    private void StopElapsedTimer()
    {
        if (this._elapsedTimer != null)
        {
            this._elapsedTimer.Stop();
            this._elapsedTimer.Elapsed -= this.OnElapsedTick;
            this._elapsedTimer.Dispose();
            this._elapsedTimer = null;
        }
    }

    private void NotifyStateChanged()
    {
        this.OnStateChanged?.Invoke();
    }
}
