// <copyright file="RuleSuggestionService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Diagnostics;

using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.Categorization;

/// <summary>
/// Orchestrates AI-powered rule suggestion analysis workflows.
/// Delegates parsing to <see cref="IRuleSuggestionResponseParser"/>,
/// prompt building to <see cref="RuleSuggestionPromptBuilder"/>,
/// and lifecycle operations to <see cref="ISuggestionAcceptanceHandler"/>.
/// </summary>
public sealed class RuleSuggestionService : IRuleSuggestionService
{
    /// <summary>
    /// Minimum number of reviewed suggestions required before acceptance rate is evaluated.
    /// </summary>
    internal const int MinReviewedForRateEvaluation = 5;

    /// <summary>
    /// Acceptance rate threshold below which a suggestion type is de-prioritized.
    /// </summary>
    internal const decimal AcceptanceRateThreshold = 0.2m;

    private readonly IAiService _aiService;
    private readonly ITransactionRepository _transactionRepository;
    private readonly ICategorizationRuleRepository _ruleRepository;
    private readonly IBudgetCategoryRepository _categoryRepository;
    private readonly IRuleSuggestionRepository _suggestionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRuleSuggestionResponseParser _responseParser;
    private readonly ISuggestionAcceptanceHandler _acceptanceHandler;

    /// <summary>
    /// Initializes a new instance of the <see cref="RuleSuggestionService"/> class.
    /// </summary>
    /// <param name="aiService">The AI service.</param>
    /// <param name="transactionRepository">The transaction repository.</param>
    /// <param name="ruleRepository">The categorization rule repository.</param>
    /// <param name="categoryRepository">The budget category repository.</param>
    /// <param name="suggestionRepository">The rule suggestion repository.</param>
    /// <param name="unitOfWork">The unit of work.</param>
    /// <param name="responseParser">The AI response parser.</param>
    /// <param name="acceptanceHandler">The suggestion acceptance handler.</param>
    public RuleSuggestionService(
        IAiService aiService,
        ITransactionRepository transactionRepository,
        ICategorizationRuleRepository ruleRepository,
        IBudgetCategoryRepository categoryRepository,
        IRuleSuggestionRepository suggestionRepository,
        IUnitOfWork unitOfWork,
        IRuleSuggestionResponseParser responseParser,
        ISuggestionAcceptanceHandler acceptanceHandler)
    {
        _aiService = aiService;
        _transactionRepository = transactionRepository;
        _ruleRepository = ruleRepository;
        _categoryRepository = categoryRepository;
        _suggestionRepository = suggestionRepository;
        _unitOfWork = unitOfWork;
        _responseParser = responseParser;
        _acceptanceHandler = acceptanceHandler;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<RuleSuggestion>> SuggestNewRulesAsync(
        int maxSuggestions = 10,
        CancellationToken ct = default)
    {
        var uncategorized = await _transactionRepository.GetUncategorizedAsync(ct);
        if (uncategorized.Count == 0)
        {
            return Array.Empty<RuleSuggestion>();
        }

        var status = await _aiService.GetStatusAsync(ct);
        if (!status.IsAvailable)
        {
            return Array.Empty<RuleSuggestion>();
        }

        var categories = await _categoryRepository.ListAsync(0, int.MaxValue, ct);
        var existingRules = await _ruleRepository.ListAsync(0, int.MaxValue, ct);

        var feedback = await BuildFeedbackContextAsync(categories, ct);
        var prompt = RuleSuggestionPromptBuilder.BuildNewRulePrompt(uncategorized, categories, existingRules, feedback);

        var response = await _aiService.CompleteAsync(prompt, ct);
        if (!response.Success)
        {
            return Array.Empty<RuleSuggestion>();
        }

        var parseResult = _responseParser.ParseNewRuleSuggestions(response.Content, categories, uncategorized.Count);
        if (parseResult.Result.Count == 0)
        {
            return Array.Empty<RuleSuggestion>();
        }

        var filteredSuggestions = await FilterDuplicateSuggestionsAsync(parseResult.Result, ct);
        var limitedSuggestions = filteredSuggestions.Take(maxSuggestions).ToList();

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
        var rules = await _ruleRepository.ListAsync(0, int.MaxValue, ct);
        if (rules.Count == 0)
        {
            return Array.Empty<RuleSuggestion>();
        }

        var status = await _aiService.GetStatusAsync(ct);
        if (!status.IsAvailable)
        {
            return Array.Empty<RuleSuggestion>();
        }

        var descriptions = await _transactionRepository.GetAllDescriptionsAsync(ct);
        var matchStats = RuleSuggestionPromptBuilder.CalculateMatchStats(rules, descriptions);
        var categories = await _categoryRepository.ListAsync(0, int.MaxValue, ct);

        var prompt = RuleSuggestionPromptBuilder.BuildOptimizationPrompt(rules, categories, matchStats);

        var response = await _aiService.CompleteAsync(prompt, ct);
        if (!response.Success)
        {
            return Array.Empty<RuleSuggestion>();
        }

        var parseResult = await _responseParser.ParseOptimizationSuggestionsAsync(response.Content, rules, ct);
        if (parseResult.Result.Count == 0)
        {
            return Array.Empty<RuleSuggestion>();
        }

        if (parseResult.Result.Count > 0)
        {
            await _suggestionRepository.AddRangeAsync(parseResult.Result, ct);
            await _unitOfWork.SaveChangesAsync(ct);
        }

        return parseResult.Result;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<RuleSuggestion>> DetectConflictsAsync(
        CancellationToken ct = default)
    {
        var rules = await _ruleRepository.ListAsync(0, int.MaxValue, ct);
        if (rules.Count < 2)
        {
            return Array.Empty<RuleSuggestion>();
        }

        var status = await _aiService.GetStatusAsync(ct);
        if (!status.IsAvailable)
        {
            return Array.Empty<RuleSuggestion>();
        }

        var categories = await _categoryRepository.ListAsync(0, int.MaxValue, ct);
        var prompt = RuleSuggestionPromptBuilder.BuildConflictDetectionPrompt(rules, categories);

        var response = await _aiService.CompleteAsync(prompt, ct);
        if (!response.Success)
        {
            return Array.Empty<RuleSuggestion>();
        }

        var parseResult = await _responseParser.ParseConflictSuggestionsAsync(response.Content, rules, ct);
        if (parseResult.Result.Count == 0)
        {
            return Array.Empty<RuleSuggestion>();
        }

        if (parseResult.Result.Count > 0)
        {
            await _suggestionRepository.AddRangeAsync(parseResult.Result, ct);
            await _unitOfWork.SaveChangesAsync(ct);
        }

        return parseResult.Result;
    }

    /// <inheritdoc/>
    public async Task<RuleSuggestionAnalysis> AnalyzeAllAsync(
        IProgress<AnalysisProgress>? progress = null,
        CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var skippedTypes = await GetDeprioritizedTypesAsync(ct);

        IReadOnlyList<RuleSuggestion> newRules = Array.Empty<RuleSuggestion>();
        if (!skippedTypes.Contains(SuggestionType.NewRule))
        {
            progress?.Report(new AnalysisProgress { CurrentStep = "Analyzing uncategorized transactions...", PercentComplete = 10 });
            newRules = await SuggestNewRulesAsync(ct: ct);
        }

        IReadOnlyList<RuleSuggestion> optimizations = Array.Empty<RuleSuggestion>();
        if (!skippedTypes.Contains(SuggestionType.PatternOptimization))
        {
            progress?.Report(new AnalysisProgress { CurrentStep = "Analyzing rule optimizations...", PercentComplete = 40 });
            optimizations = await SuggestOptimizationsAsync(ct);
        }

        IReadOnlyList<RuleSuggestion> conflicts = Array.Empty<RuleSuggestion>();
        if (!skippedTypes.Contains(SuggestionType.RuleConflict))
        {
            progress?.Report(new AnalysisProgress { CurrentStep = "Detecting conflicts...", PercentComplete = 70 });
            conflicts = await DetectConflictsAsync(ct);
        }

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
    public Task<CategorizationRule> AcceptSuggestionAsync(
        Guid suggestionId,
        CancellationToken ct = default)
    {
        return _acceptanceHandler.AcceptSuggestionAsync(suggestionId, ct);
    }

    /// <inheritdoc/>
    public Task DismissSuggestionAsync(
        Guid suggestionId,
        string? reason = null,
        CancellationToken ct = default)
    {
        return _acceptanceHandler.DismissSuggestionAsync(suggestionId, reason, ct);
    }

    /// <inheritdoc/>
    public Task ProvideFeedbackAsync(
        Guid suggestionId,
        bool isPositive,
        CancellationToken ct = default)
    {
        return _acceptanceHandler.ProvideFeedbackAsync(suggestionId, isPositive, ct);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<RuleSuggestionDto>> MapSuggestionsToDtosAsync(
        IReadOnlyList<RuleSuggestion> suggestions,
        CancellationToken ct = default)
    {
        var categories = await _categoryRepository.ListAsync(0, int.MaxValue, ct);
        var categoryLookup = categories.ToDictionary(c => c.Id, c => c.Name);

        var rules = await _ruleRepository.ListAsync(0, int.MaxValue, ct);
        var ruleLookup = rules.ToDictionary(r => r.Id, r => r.Name);

        return suggestions.Select(s => CategorizationMapper.ToDto(
            s,
            s.SuggestedCategoryId.HasValue ? categoryLookup.GetValueOrDefault(s.SuggestedCategoryId.Value) : null,
            s.TargetRuleId.HasValue ? ruleLookup.GetValueOrDefault(s.TargetRuleId.Value) : null))
            .ToList();
    }

    /// <inheritdoc/>
    public async Task<RuleSuggestionDto> MapSuggestionToDtoAsync(
        RuleSuggestion suggestion,
        CancellationToken ct = default)
    {
        string? categoryName = null;
        string? ruleName = null;

        if (suggestion.SuggestedCategoryId.HasValue)
        {
            var category = await _categoryRepository.GetByIdAsync(suggestion.SuggestedCategoryId.Value, ct);
            categoryName = category?.Name;
        }

        if (suggestion.TargetRuleId.HasValue)
        {
            var rule = await _ruleRepository.GetByIdAsync(suggestion.TargetRuleId.Value, ct);
            ruleName = rule?.Name;
        }

        return CategorizationMapper.ToDto(suggestion, categoryName, ruleName);
    }

    private async Task<List<RuleSuggestion>> FilterDuplicateSuggestionsAsync(
        IReadOnlyList<RuleSuggestion> suggestions,
        CancellationToken ct)
    {
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

        return filteredSuggestions;
    }

    private async Task<SuggestionFeedbackContext> BuildFeedbackContextAsync(
        IReadOnlyList<BudgetCategory> categories,
        CancellationToken ct)
    {
        var dismissedPatterns = await _suggestionRepository.GetDismissedNewRulePatternsAsync(ct);

        var acceptedRules = await _suggestionRepository.GetAcceptedNewRulesAsync(ct);
        var categoryLookup = categories.ToDictionary(c => c.Id, c => c.Name);
        var acceptedExamples = acceptedRules
            .Where(r => categoryLookup.ContainsKey(r.CategoryId))
            .Select(r => new AcceptedSuggestionExample(r.Pattern, categoryLookup[r.CategoryId]))
            .ToList();

        return new SuggestionFeedbackContext(dismissedPatterns, acceptedExamples);
    }

    private async Task<HashSet<SuggestionType>> GetDeprioritizedTypesAsync(CancellationToken ct)
    {
        var counts = await _suggestionRepository.GetReviewedCountsByTypeAsync(ct);
        var skipped = new HashSet<SuggestionType>();

        foreach (var type in new[] { SuggestionType.NewRule, SuggestionType.PatternOptimization, SuggestionType.RuleConflict })
        {
            counts.TryGetValue((type, SuggestionStatus.Accepted), out var accepted);
            counts.TryGetValue((type, SuggestionStatus.Dismissed), out var dismissed);
            var total = accepted + dismissed;

            if (total >= MinReviewedForRateEvaluation)
            {
                var rate = (decimal)accepted / total;
                if (rate < AcceptanceRateThreshold)
                {
                    skipped.Add(type);
                }
            }
        }

        return skipped;
    }
}
