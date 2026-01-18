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
        // Get all rules
        var rules = await _ruleRepository.ListAsync(0, int.MaxValue, ct);
        if (rules.Count == 0)
        {
            return Array.Empty<RuleSuggestion>();
        }

        // Check if AI is available
        var status = await _aiService.GetStatusAsync(ct);
        if (!status.IsAvailable)
        {
            return Array.Empty<RuleSuggestion>();
        }

        // Get all transaction descriptions for match analysis
        var descriptions = await _transactionRepository.GetAllDescriptionsAsync(ct);

        // Calculate match statistics
        var matchStats = CalculateMatchStats(rules, descriptions);

        // Get categories for context
        var categories = await _categoryRepository.ListAsync(0, int.MaxValue, ct);

        // Build the prompt
        var prompt = BuildOptimizationPrompt(rules, categories, matchStats);

        // Get AI response
        var response = await _aiService.CompleteAsync(prompt, ct);
        if (!response.Success)
        {
            return Array.Empty<RuleSuggestion>();
        }

        // Parse response
        var suggestions = await ParseOptimizationSuggestions(response.Content, rules, ct);
        if (suggestions.Count == 0)
        {
            return Array.Empty<RuleSuggestion>();
        }

        // Persist suggestions
        if (suggestions.Count > 0)
        {
            await _suggestionRepository.AddRangeAsync(suggestions, ct);
            await _unitOfWork.SaveChangesAsync(ct);
        }

        return suggestions;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<RuleSuggestion>> DetectConflictsAsync(
        CancellationToken ct = default)
    {
        // Get all rules
        var rules = await _ruleRepository.ListAsync(0, int.MaxValue, ct);
        if (rules.Count < 2)
        {
            // Need at least 2 rules to have conflicts
            return Array.Empty<RuleSuggestion>();
        }

        // Check if AI is available
        var status = await _aiService.GetStatusAsync(ct);
        if (!status.IsAvailable)
        {
            return Array.Empty<RuleSuggestion>();
        }

        // Get categories for context
        var categories = await _categoryRepository.ListAsync(0, int.MaxValue, ct);

        // Build the prompt
        var prompt = BuildConflictDetectionPrompt(rules, categories);

        // Get AI response
        var response = await _aiService.CompleteAsync(prompt, ct);
        if (!response.Success)
        {
            return Array.Empty<RuleSuggestion>();
        }

        // Parse response
        var suggestions = await ParseConflictSuggestions(response.Content, rules, ct);
        if (suggestions.Count == 0)
        {
            return Array.Empty<RuleSuggestion>();
        }

        // Persist suggestions
        if (suggestions.Count > 0)
        {
            await _suggestionRepository.AddRangeAsync(suggestions, ct);
            await _unitOfWork.SaveChangesAsync(ct);
        }

        return suggestions;
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

        var rule = suggestion.Type switch
        {
            SuggestionType.NewRule => await AcceptNewRuleSuggestion(suggestion, ct),
            SuggestionType.PatternOptimization => await AcceptPatternOptimizationSuggestion(suggestion, ct),
            SuggestionType.UnusedRule => await AcceptUnusedRuleSuggestion(suggestion, ct),
            SuggestionType.RuleConsolidation => throw new DomainException(
                "Rule consolidation requires manual review. Accept individual changes or create a new rule manually."),
            SuggestionType.RuleConflict => throw new DomainException(
                "Conflict suggestions require manual resolution. Review the conflicting rules and adjust manually."),
            _ => throw new DomainException($"Unsupported suggestion type: {suggestion.Type}"),
        };

        // Mark suggestion as accepted
        suggestion.Accept();

        await _unitOfWork.SaveChangesAsync(ct);

        return rule;
    }

    private async Task<CategorizationRule> AcceptNewRuleSuggestion(RuleSuggestion suggestion, CancellationToken ct)
    {
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

        return rule;
    }

    private async Task<CategorizationRule> AcceptPatternOptimizationSuggestion(RuleSuggestion suggestion, CancellationToken ct)
    {
        if (suggestion.TargetRuleId is null ||
            suggestion.OptimizedPattern is null)
        {
            throw new DomainException("Suggestion is missing required fields for pattern optimization");
        }

        var rule = await _ruleRepository.GetByIdAsync(suggestion.TargetRuleId.Value, ct)
            ?? throw new DomainException($"Target rule {suggestion.TargetRuleId} not found");

        // Update the rule with the optimized pattern
        rule.Update(
            name: rule.Name,
            matchType: rule.MatchType,
            pattern: suggestion.OptimizedPattern,
            categoryId: rule.CategoryId,
            caseSensitive: rule.CaseSensitive);

        return rule;
    }

    private async Task<CategorizationRule> AcceptUnusedRuleSuggestion(RuleSuggestion suggestion, CancellationToken ct)
    {
        if (suggestion.TargetRuleId is null)
        {
            throw new DomainException("Suggestion is missing required target rule ID");
        }

        var rule = await _ruleRepository.GetByIdAsync(suggestion.TargetRuleId.Value, ct)
            ?? throw new DomainException($"Target rule {suggestion.TargetRuleId} not found");

        // Deactivate the unused rule
        rule.Deactivate();

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

    /// <summary>
    /// DTO for parsing AI responses for optimization suggestions.
    /// </summary>
    private sealed record OptimizationSuggestionResponse
    {
        public IReadOnlyList<OptimizationSuggestionItem>? Suggestions { get; init; }
    }

    /// <summary>
    /// DTO for a single optimization suggestion item.
    /// </summary>
    private sealed record OptimizationSuggestionItem
    {
        public string? Type { get; init; }

        public string? TargetRuleId { get; init; }

        public IReadOnlyList<string>? TargetRuleIds { get; init; }

        public string? SuggestedPattern { get; init; }

        public string? Reasoning { get; init; }
    }

    /// <summary>
    /// DTO for parsing AI responses for conflict detection.
    /// </summary>
    private sealed record ConflictDetectionResponse
    {
        public IReadOnlyList<ConflictItem>? Conflicts { get; init; }
    }

    /// <summary>
    /// DTO for a single conflict item.
    /// </summary>
    private sealed record ConflictItem
    {
        public IReadOnlyList<string>? RuleIds { get; init; }

        public string? ConflictType { get; init; }

        public string? Description { get; init; }

        public string? Resolution { get; init; }
    }

    private static IReadOnlyList<(string RuleName, int MatchCount)> CalculateMatchStats(
        IReadOnlyList<CategorizationRule> rules,
        IReadOnlyList<string> descriptions)
    {
        var stats = new List<(string RuleName, int MatchCount)>();

        foreach (var rule in rules)
        {
            var matchCount = descriptions.Count(d => rule.Matches(d));
            stats.Add((rule.Name, matchCount));
        }

        return stats;
    }

    private static AiPrompt BuildOptimizationPrompt(
        IReadOnlyList<CategorizationRule> rules,
        IReadOnlyList<BudgetCategory> categories,
        IReadOnlyList<(string RuleName, int MatchCount)> matchStats)
    {
        var categoryLookup = categories.ToDictionary(c => c.Id, c => c.Name);
        var rulesForPrompt = rules.Select(r => (
            r.Id,
            r.Name,
            r.Pattern,
            r.MatchType.ToString(),
            categoryLookup.GetValueOrDefault(r.CategoryId, "Unknown")));

        var userPrompt = AiPrompts.OptimizationPrompt
            .Replace("{rules}", AiPrompts.FormatRulesForOptimization(rulesForPrompt))
            .Replace("{matchStats}", AiPrompts.FormatMatchStats(matchStats));

        return new AiPrompt(
            SystemPrompt: AiPrompts.SystemPrompt,
            UserPrompt: userPrompt,
            Temperature: 0.3m,
            MaxTokens: 2000);
    }

    private static AiPrompt BuildConflictDetectionPrompt(
        IReadOnlyList<CategorizationRule> rules,
        IReadOnlyList<BudgetCategory> categories)
    {
        var categoryLookup = categories.ToDictionary(c => c.Id, c => c.Name);
        var rulesForPrompt = rules.Select(r => (
            r.Id,
            r.Name,
            r.Pattern,
            r.MatchType.ToString(),
            categoryLookup.GetValueOrDefault(r.CategoryId, "Unknown")));

        var userPrompt = AiPrompts.ConflictDetectionPrompt
            .Replace("{rules}", AiPrompts.FormatRulesForOptimization(rulesForPrompt));

        return new AiPrompt(
            SystemPrompt: AiPrompts.SystemPrompt,
            UserPrompt: userPrompt,
            Temperature: 0.3m,
            MaxTokens: 2000);
    }

    private async Task<IReadOnlyList<RuleSuggestion>> ParseOptimizationSuggestions(
        string jsonContent,
        IReadOnlyList<CategorizationRule> rules,
        CancellationToken ct)
    {
        try
        {
            var parsed = JsonSerializer.Deserialize<OptimizationSuggestionResponse>(jsonContent, JsonOptions);
            if (parsed?.Suggestions is null || parsed.Suggestions.Count == 0)
            {
                return Array.Empty<RuleSuggestion>();
            }

            var ruleLookup = rules.ToDictionary(r => r.Id, r => r);
            var suggestions = new List<RuleSuggestion>();

            foreach (var item in parsed.Suggestions)
            {
                var suggestion = await CreateOptimizationSuggestion(item, ruleLookup, ct);
                if (suggestion is not null)
                {
                    suggestions.Add(suggestion);
                }
            }

            return suggestions;
        }
        catch (JsonException)
        {
            return Array.Empty<RuleSuggestion>();
        }
    }

    private async Task<RuleSuggestion?> CreateOptimizationSuggestion(
        OptimizationSuggestionItem item,
        Dictionary<Guid, CategorizationRule> ruleLookup,
        CancellationToken ct)
    {
        var type = item.Type?.ToLowerInvariant();

        switch (type)
        {
            case "remove":
                return await CreateUnusedRuleSuggestion(item, ruleLookup, ct);

            case "simplify":
            case "broaden":
            case "narrow":
                return await CreatePatternOptimizationSuggestion(item, ruleLookup, ct);

            case "consolidate":
                return await CreateConsolidationSuggestion(item, ruleLookup, ct);

            default:
                return null;
        }
    }

    private async Task<RuleSuggestion?> CreateUnusedRuleSuggestion(
        OptimizationSuggestionItem item,
        Dictionary<Guid, CategorizationRule> ruleLookup,
        CancellationToken ct)
    {
        if (!Guid.TryParse(item.TargetRuleId, out var ruleId) || !ruleLookup.TryGetValue(ruleId, out var rule))
        {
            return null;
        }

        // Check for existing suggestion
        if (await _suggestionRepository.ExistsPendingForRuleAsync(ruleId, SuggestionType.UnusedRule, ct))
        {
            return null;
        }

        return RuleSuggestion.CreateUnusedRuleSuggestion(
            title: $"Remove unused rule: {rule.Name}",
            description: $"Rule '{rule.Name}' with pattern '{rule.Pattern}' has never matched any transactions.",
            reasoning: item.Reasoning ?? "Rule has 0 matches and may be outdated or incorrect.",
            targetRuleId: ruleId);
    }

    private async Task<RuleSuggestion?> CreatePatternOptimizationSuggestion(
        OptimizationSuggestionItem item,
        Dictionary<Guid, CategorizationRule> ruleLookup,
        CancellationToken ct)
    {
        if (!Guid.TryParse(item.TargetRuleId, out var ruleId) || !ruleLookup.TryGetValue(ruleId, out var rule))
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(item.SuggestedPattern))
        {
            return null;
        }

        // Check for existing suggestion
        if (await _suggestionRepository.ExistsPendingForRuleAsync(ruleId, SuggestionType.PatternOptimization, ct))
        {
            return null;
        }

        return RuleSuggestion.CreateOptimizationSuggestion(
            title: $"Optimize pattern: {rule.Name}",
            description: $"Simplify pattern from '{rule.Pattern}' to '{item.SuggestedPattern}'.",
            reasoning: item.Reasoning ?? "Pattern can be simplified for better maintainability.",
            confidence: 0.8m,
            targetRuleId: ruleId,
            optimizedPattern: item.SuggestedPattern);
    }

    private async Task<RuleSuggestion?> CreateConsolidationSuggestion(
        OptimizationSuggestionItem item,
        Dictionary<Guid, CategorizationRule> ruleLookup,
        CancellationToken ct)
    {
        if (item.TargetRuleIds is null || item.TargetRuleIds.Count < 2)
        {
            return null;
        }

        var ruleIds = new List<Guid>();
        var ruleNames = new List<string>();

        foreach (var idString in item.TargetRuleIds)
        {
            if (Guid.TryParse(idString, out var id) && ruleLookup.TryGetValue(id, out var rule))
            {
                ruleIds.Add(id);
                ruleNames.Add(rule.Name);
            }
        }

        if (ruleIds.Count < 2)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(item.SuggestedPattern))
        {
            return null;
        }

        // Check for existing suggestion
        if (await _suggestionRepository.ExistsPendingForRulesAsync(ruleIds, SuggestionType.RuleConsolidation, ct))
        {
            return null;
        }

        return RuleSuggestion.CreateConsolidationSuggestion(
            title: $"Consolidate {ruleIds.Count} rules",
            description: $"Merge rules '{string.Join("', '", ruleNames)}' into a single rule.",
            reasoning: item.Reasoning ?? "Multiple rules can be combined for simplicity.",
            confidence: 0.7m,
            ruleIds: ruleIds,
            consolidatedPattern: item.SuggestedPattern);
    }

    private async Task<IReadOnlyList<RuleSuggestion>> ParseConflictSuggestions(
        string jsonContent,
        IReadOnlyList<CategorizationRule> rules,
        CancellationToken ct)
    {
        try
        {
            var parsed = JsonSerializer.Deserialize<ConflictDetectionResponse>(jsonContent, JsonOptions);
            if (parsed?.Conflicts is null || parsed.Conflicts.Count == 0)
            {
                return Array.Empty<RuleSuggestion>();
            }

            var ruleLookup = rules.ToDictionary(r => r.Id, r => r);
            var suggestions = new List<RuleSuggestion>();

            foreach (var item in parsed.Conflicts)
            {
                var suggestion = await CreateConflictSuggestion(item, ruleLookup, ct);
                if (suggestion is not null)
                {
                    suggestions.Add(suggestion);
                }
            }

            return suggestions;
        }
        catch (JsonException)
        {
            return Array.Empty<RuleSuggestion>();
        }
    }

    private async Task<RuleSuggestion?> CreateConflictSuggestion(
        ConflictItem item,
        Dictionary<Guid, CategorizationRule> ruleLookup,
        CancellationToken ct)
    {
        if (item.RuleIds is null || item.RuleIds.Count < 2)
        {
            return null;
        }

        var ruleIds = new List<Guid>();
        var ruleNames = new List<string>();

        foreach (var idString in item.RuleIds)
        {
            if (Guid.TryParse(idString, out var id) && ruleLookup.TryGetValue(id, out var rule))
            {
                ruleIds.Add(id);
                ruleNames.Add(rule.Name);
            }
        }

        if (ruleIds.Count < 2)
        {
            return null;
        }

        // Check for existing suggestion
        if (await _suggestionRepository.ExistsPendingForRulesAsync(ruleIds, SuggestionType.RuleConflict, ct))
        {
            return null;
        }

        var conflictType = item.ConflictType ?? "overlap";
        var title = conflictType.ToLowerInvariant() switch
        {
            "contradiction" => $"Contradictory rules: {string.Join(" & ", ruleNames)}",
            "shadowed" => $"Shadowed rule: {ruleNames.LastOrDefault()}",
            _ => $"Overlapping rules: {string.Join(" & ", ruleNames)}",
        };

        return RuleSuggestion.CreateConflictSuggestion(
            title: title,
            description: item.Description ?? $"Rules '{string.Join("', '", ruleNames)}' may conflict.",
            reasoning: item.Resolution ?? "Review these rules and adjust patterns or priorities.",
            conflictingRuleIds: ruleIds);
    }
}
