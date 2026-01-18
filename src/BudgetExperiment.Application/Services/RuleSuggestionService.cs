// <copyright file="RuleSuggestionService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Diagnostics;
using System.Text.Json;
using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.Services;

/// <summary>
/// Service for generating and managing AI-powered rule suggestions.
/// </summary>
public sealed class RuleSuggestionService : IRuleSuggestionService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly IAiService _aiService;
    private readonly ITransactionRepository _transactionRepository;
    private readonly ICategorizationRuleRepository _ruleRepository;
    private readonly IBudgetCategoryRepository _categoryRepository;
    private readonly IRuleSuggestionRepository _suggestionRepository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of the <see cref="RuleSuggestionService"/> class.
    /// </summary>
    /// <param name="aiService">The AI service.</param>
    /// <param name="transactionRepository">The transaction repository.</param>
    /// <param name="ruleRepository">The categorization rule repository.</param>
    /// <param name="categoryRepository">The budget category repository.</param>
    /// <param name="suggestionRepository">The rule suggestion repository.</param>
    /// <param name="unitOfWork">The unit of work.</param>
    public RuleSuggestionService(
        IAiService aiService,
        ITransactionRepository transactionRepository,
        ICategorizationRuleRepository ruleRepository,
        IBudgetCategoryRepository categoryRepository,
        IRuleSuggestionRepository suggestionRepository,
        IUnitOfWork unitOfWork)
    {
        _aiService = aiService;
        _transactionRepository = transactionRepository;
        _ruleRepository = ruleRepository;
        _categoryRepository = categoryRepository;
        _suggestionRepository = suggestionRepository;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<RuleSuggestion>> SuggestNewRulesAsync(
        int maxSuggestions = 10,
        CancellationToken ct = default)
    {
        // Get uncategorized transactions
        var uncategorized = await _transactionRepository.GetUncategorizedAsync(ct);
        if (uncategorized.Count == 0)
        {
            return Array.Empty<RuleSuggestion>();
        }

        // Check if AI is available
        var status = await _aiService.GetStatusAsync(ct);
        if (!status.IsAvailable)
        {
            return Array.Empty<RuleSuggestion>();
        }

        // Get existing categories and rules for context
        var categories = await _categoryRepository.ListAsync(0, int.MaxValue, ct);
        var existingRules = await _ruleRepository.ListAsync(0, int.MaxValue, ct);

        // Build the prompt
        var prompt = BuildNewRulePrompt(uncategorized, categories, existingRules);

        // Get AI response
        var response = await _aiService.CompleteAsync(prompt, ct);
        if (!response.Success)
        {
            return Array.Empty<RuleSuggestion>();
        }

        // Parse response
        var suggestions = ParseNewRuleSuggestions(response.Content, categories, uncategorized.Count);
        if (suggestions.Count == 0)
        {
            return Array.Empty<RuleSuggestion>();
        }

        // Filter out duplicates
        var filteredSuggestions = new List<RuleSuggestion>();
        foreach (var suggestion in suggestions)
        {
            if (suggestion.SuggestedPattern is not null)
            {
                var exists = await _suggestionRepository.ExistsPendingWithPatternAsync(
                    suggestion.SuggestedPattern, ct);
                if (!exists)
                {
                    filteredSuggestions.Add(suggestion);
                }
            }
        }

        // Limit to max suggestions
        var limitedSuggestions = filteredSuggestions.Take(maxSuggestions).ToList();

        // Persist suggestions
        if (limitedSuggestions.Count > 0)
        {
            await _suggestionRepository.AddRangeAsync(limitedSuggestions, ct);
            await _unitOfWork.SaveChangesAsync(ct);
        }

        return limitedSuggestions;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<RuleSuggestion>> SuggestOptimizationsAsync(
        CancellationToken ct = default)
    {
        // Phase 5 implementation
        await Task.CompletedTask;
        return Array.Empty<RuleSuggestion>();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<RuleSuggestion>> DetectConflictsAsync(
        CancellationToken ct = default)
    {
        // Phase 5 implementation
        await Task.CompletedTask;
        return Array.Empty<RuleSuggestion>();
    }

    /// <inheritdoc/>
    public async Task<RuleSuggestionAnalysis> AnalyzeAllAsync(
        IProgress<AnalysisProgress>? progress = null,
        CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();

        progress?.Report(new AnalysisProgress { CurrentStep = "Analyzing uncategorized transactions...", PercentComplete = 10 });
        var newRules = await SuggestNewRulesAsync(ct: ct);

        progress?.Report(new AnalysisProgress { CurrentStep = "Analyzing rule optimizations...", PercentComplete = 40 });
        var optimizations = await SuggestOptimizationsAsync(ct);

        progress?.Report(new AnalysisProgress { CurrentStep = "Detecting conflicts...", PercentComplete = 70 });
        var conflicts = await DetectConflictsAsync(ct);

        progress?.Report(new AnalysisProgress { CurrentStep = "Complete", PercentComplete = 100 });

        var uncategorized = await _transactionRepository.GetUncategorizedAsync(ct);
        var rules = await _ruleRepository.ListAsync(0, int.MaxValue, ct);

        stopwatch.Stop();

        return new RuleSuggestionAnalysis
        {
            NewRuleSuggestions = newRules,
            OptimizationSuggestions = optimizations,
            ConflictSuggestions = conflicts,
            UncategorizedTransactionsAnalyzed = uncategorized.Count,
            RulesAnalyzed = rules.Count,
            AnalysisDuration = stopwatch.Elapsed,
        };
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<RuleSuggestion>> GetPendingSuggestionsAsync(
        SuggestionType? typeFilter = null,
        CancellationToken ct = default)
    {
        if (typeFilter.HasValue)
        {
            return await _suggestionRepository.GetPendingByTypeAsync(typeFilter.Value, ct);
        }

        return await _suggestionRepository.GetPendingAsync(ct);
    }

    /// <inheritdoc/>
    public async Task<CategorizationRule> AcceptSuggestionAsync(
        Guid suggestionId,
        CancellationToken ct = default)
    {
        var suggestion = await _suggestionRepository.GetByIdAsync(suggestionId, ct)
            ?? throw new DomainException($"Suggestion {suggestionId} not found");

        if (suggestion.Type != SuggestionType.NewRule)
        {
            throw new DomainException($"Only NewRule suggestions can be accepted. Type: {suggestion.Type}");
        }

        if (suggestion.SuggestedPattern is null ||
            suggestion.SuggestedMatchType is null ||
            suggestion.SuggestedCategoryId is null)
        {
            throw new DomainException("Suggestion is missing required fields for rule creation");
        }

        // Get next priority
        var priority = await _ruleRepository.GetNextPriorityAsync(ct);

        // Create the rule
        var rule = CategorizationRule.Create(
            name: suggestion.Title,
            matchType: suggestion.SuggestedMatchType.Value,
            pattern: suggestion.SuggestedPattern,
            categoryId: suggestion.SuggestedCategoryId.Value,
            priority: priority);

        await _ruleRepository.AddAsync(rule, ct);

        // Mark suggestion as accepted
        suggestion.Accept();

        await _unitOfWork.SaveChangesAsync(ct);

        return rule;
    }

    /// <inheritdoc/>
    public async Task DismissSuggestionAsync(
        Guid suggestionId,
        string? reason = null,
        CancellationToken ct = default)
    {
        var suggestion = await _suggestionRepository.GetByIdAsync(suggestionId, ct)
            ?? throw new DomainException($"Suggestion {suggestionId} not found");

        suggestion.Dismiss(reason);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    /// <inheritdoc/>
    public async Task ProvideFeedbackAsync(
        Guid suggestionId,
        bool isPositive,
        CancellationToken ct = default)
    {
        var suggestion = await _suggestionRepository.GetByIdAsync(suggestionId, ct)
            ?? throw new DomainException($"Suggestion {suggestionId} not found");

        suggestion.ProvideFeedback(isPositive);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    private static AiPrompt BuildNewRulePrompt(
        IReadOnlyList<Transaction> uncategorized,
        IReadOnlyList<BudgetCategory> categories,
        IReadOnlyList<CategorizationRule> existingRules)
    {
        var categoryNames = categories.Select(c => c.Name);
        var ruleInfo = existingRules.Select(r => (r.Name, r.Pattern, r.MatchType.ToString()));
        var descriptions = uncategorized.Select(t => t.Description);

        var userPrompt = AiPrompts.NewRuleSuggestionPrompt
            .Replace("{categories}", AiPrompts.FormatCategories(categoryNames))
            .Replace("{existingRules}", AiPrompts.FormatExistingRules(ruleInfo))
            .Replace("{descriptions}", AiPrompts.FormatDescriptions(descriptions));

        return new AiPrompt(
            SystemPrompt: AiPrompts.SystemPrompt,
            UserPrompt: userPrompt,
            Temperature: 0.3m,
            MaxTokens: 2000);
    }

    private static IReadOnlyList<RuleSuggestion> ParseNewRuleSuggestions(
        string jsonContent,
        IReadOnlyList<BudgetCategory> categories,
        int transactionCount)
    {
        try
        {
            var parsed = JsonSerializer.Deserialize<NewRuleSuggestionResponse>(jsonContent, JsonOptions);
            if (parsed?.Suggestions is null || parsed.Suggestions.Count == 0)
            {
                return Array.Empty<RuleSuggestion>();
            }

            var categoryLookup = categories.ToDictionary(c => c.Name, c => c.Id, StringComparer.OrdinalIgnoreCase);
            var suggestions = new List<RuleSuggestion>();

            foreach (var item in parsed.Suggestions)
            {
                // Skip if category not found
                if (!categoryLookup.TryGetValue(item.CategoryName ?? string.Empty, out var categoryId))
                {
                    continue;
                }

                // Parse match type
                if (!Enum.TryParse<RuleMatchType>(item.MatchType, ignoreCase: true, out var matchType))
                {
                    matchType = RuleMatchType.Contains; // Default
                }

                var suggestion = RuleSuggestion.CreateNewRuleSuggestion(
                    title: $"Create rule for {item.Pattern}",
                    description: item.Reasoning ?? string.Empty,
                    reasoning: item.Reasoning ?? string.Empty,
                    confidence: Math.Clamp(item.Confidence, 0m, 1m),
                    suggestedPattern: item.Pattern ?? string.Empty,
                    suggestedMatchType: matchType,
                    suggestedCategoryId: categoryId,
                    affectedTransactionCount: item.SampleMatches?.Count ?? 0,
                    sampleDescriptions: item.SampleMatches ?? Array.Empty<string>());

                suggestions.Add(suggestion);
            }

            return suggestions;
        }
        catch (JsonException)
        {
            return Array.Empty<RuleSuggestion>();
        }
    }

    /// <summary>
    /// DTO for parsing AI responses for new rule suggestions.
    /// </summary>
    private sealed record NewRuleSuggestionResponse
    {
        public IReadOnlyList<NewRuleSuggestionItem>? Suggestions { get; init; }
    }

    /// <summary>
    /// DTO for a single suggestion item in the AI response.
    /// </summary>
    private sealed record NewRuleSuggestionItem
    {
        public string? Pattern { get; init; }

        public string? MatchType { get; init; }

        public string? CategoryName { get; init; }

        public decimal Confidence { get; init; }

        public string? Reasoning { get; init; }

        public IReadOnlyList<string>? SampleMatches { get; init; }
    }
}
