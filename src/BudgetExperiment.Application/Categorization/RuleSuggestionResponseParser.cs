// <copyright file="RuleSuggestionResponseParser.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Text.Json;
using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.Categorization;

/// <summary>
/// Parses AI response JSON into <see cref="RuleSuggestion"/> domain objects.
/// Handles JSON extraction, deserialization, and duplicate filtering.
/// </summary>
public sealed class RuleSuggestionResponseParser : IRuleSuggestionResponseParser
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly IRuleSuggestionRepository _suggestionRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="RuleSuggestionResponseParser"/> class.
    /// </summary>
    /// <param name="suggestionRepository">The rule suggestion repository for duplicate checking.</param>
    public RuleSuggestionResponseParser(IRuleSuggestionRepository suggestionRepository)
    {
        _suggestionRepository = suggestionRepository;
    }

    /// <summary>
    /// Extracts the first complete JSON object from raw AI response text.
    /// AI models frequently wrap JSON in markdown code blocks or add preamble
    /// text despite being instructed not to. This method strips that wrapping
    /// so <see cref="JsonSerializer"/> can parse the content.
    /// </summary>
    /// <param name="content">The raw AI response text.</param>
    /// <returns>The extracted JSON string.</returns>
    /// <exception cref="JsonException">Thrown when no JSON object is found in the content.</exception>
    public static string ExtractJson(string content)
    {
        // Try direct parse first — fastest path for well-formed responses
        var trimmed = content.Trim();
        if (trimmed.StartsWith('{') && trimmed.EndsWith('}'))
        {
            return trimmed;
        }

        // Strip markdown code fences: ```json ... ``` or ``` ... ```
        var fenceStart = content.IndexOf("```", StringComparison.Ordinal);
        if (fenceStart >= 0)
        {
            var lineEnd = content.IndexOf('\n', fenceStart);
            if (lineEnd >= 0)
            {
                var fenceEnd = content.IndexOf("```", lineEnd, StringComparison.Ordinal);
                if (fenceEnd > lineEnd)
                {
                    content = content.Substring(lineEnd + 1, fenceEnd - lineEnd - 1);
                }
            }
        }

        // Fall back to bracket matching
        var jsonStart = content.IndexOf('{');
        var jsonEnd = content.LastIndexOf('}');

        if (jsonStart < 0 || jsonEnd < 0 || jsonEnd <= jsonStart)
        {
            throw new JsonException("No JSON object found in AI response.");
        }

        return content.Substring(jsonStart, jsonEnd - jsonStart + 1);
    }

    /// <inheritdoc/>
    public ParseResult<IReadOnlyList<RuleSuggestion>> ParseNewRuleSuggestions(
        string jsonContent,
        IReadOnlyList<BudgetCategory> categories,
        int transactionCount)
    {
        var diagnostics = new List<string>();

        try
        {
            var extracted = ExtractJson(jsonContent);
            var parsed = JsonSerializer.Deserialize<NewRuleSuggestionResponse>(extracted, JsonOptions);
            if (parsed?.Suggestions is null || parsed.Suggestions.Count == 0)
            {
                return ParseResult<IReadOnlyList<RuleSuggestion>>.Ok(Array.Empty<RuleSuggestion>());
            }

            var categoryLookup = categories.ToDictionary(c => c.Name, c => c.Id, StringComparer.OrdinalIgnoreCase);
            var suggestions = new List<RuleSuggestion>();

            foreach (var item in parsed.Suggestions)
            {
                var suggestion = CreateNewRuleSuggestion(item, categoryLookup, diagnostics);
                if (suggestion is not null)
                {
                    suggestions.Add(suggestion);
                }
            }

            return ParseResult<IReadOnlyList<RuleSuggestion>>.Ok(suggestions, diagnostics.Count > 0 ? diagnostics : null);
        }
        catch (JsonException ex)
        {
            diagnostics.Add($"AI response was not valid JSON: {ex.Message}");
            return ParseResult<IReadOnlyList<RuleSuggestion>>.Fail(Array.Empty<RuleSuggestion>(), diagnostics);
        }
    }

    /// <inheritdoc/>
    public async Task<ParseResult<IReadOnlyList<RuleSuggestion>>> ParseOptimizationSuggestionsAsync(
        string jsonContent,
        IReadOnlyList<CategorizationRule> rules,
        CancellationToken ct = default)
    {
        var diagnostics = new List<string>();

        try
        {
            var extracted = ExtractJson(jsonContent);
            var parsed = JsonSerializer.Deserialize<OptimizationSuggestionResponse>(extracted, JsonOptions);
            if (parsed?.Suggestions is null || parsed.Suggestions.Count == 0)
            {
                return ParseResult<IReadOnlyList<RuleSuggestion>>.Ok(Array.Empty<RuleSuggestion>());
            }

            var ruleLookup = rules.ToDictionary(r => r.Id, r => r);
            var suggestions = new List<RuleSuggestion>();

            foreach (var item in parsed.Suggestions)
            {
                var suggestion = await CreateOptimizationSuggestionAsync(item, ruleLookup, ct);
                if (suggestion is not null)
                {
                    suggestions.Add(suggestion);
                }
            }

            return ParseResult<IReadOnlyList<RuleSuggestion>>.Ok(suggestions, diagnostics.Count > 0 ? diagnostics : null);
        }
        catch (JsonException ex)
        {
            diagnostics.Add($"AI response was not valid JSON: {ex.Message}");
            return ParseResult<IReadOnlyList<RuleSuggestion>>.Fail(Array.Empty<RuleSuggestion>(), diagnostics);
        }
    }

    /// <inheritdoc/>
    public async Task<ParseResult<IReadOnlyList<RuleSuggestion>>> ParseConflictSuggestionsAsync(
        string jsonContent,
        IReadOnlyList<CategorizationRule> rules,
        CancellationToken ct = default)
    {
        var diagnostics = new List<string>();

        try
        {
            var extracted = ExtractJson(jsonContent);
            var parsed = JsonSerializer.Deserialize<ConflictDetectionResponse>(extracted, JsonOptions);
            if (parsed?.Conflicts is null || parsed.Conflicts.Count == 0)
            {
                return ParseResult<IReadOnlyList<RuleSuggestion>>.Ok(Array.Empty<RuleSuggestion>());
            }

            var ruleLookup = rules.ToDictionary(r => r.Id, r => r);
            var suggestions = new List<RuleSuggestion>();

            foreach (var item in parsed.Conflicts)
            {
                var suggestion = await CreateConflictSuggestionAsync(item, ruleLookup, ct);
                if (suggestion is not null)
                {
                    suggestions.Add(suggestion);
                }
            }

            return ParseResult<IReadOnlyList<RuleSuggestion>>.Ok(suggestions, diagnostics.Count > 0 ? diagnostics : null);
        }
        catch (JsonException ex)
        {
            diagnostics.Add($"AI response was not valid JSON: {ex.Message}");
            return ParseResult<IReadOnlyList<RuleSuggestion>>.Fail(Array.Empty<RuleSuggestion>(), diagnostics);
        }
    }

    private static RuleSuggestion? CreateNewRuleSuggestion(
        NewRuleSuggestionItem item,
        Dictionary<string, Guid> categoryLookup,
        List<string> diagnostics)
    {
        // Skip if category not found
        if (!categoryLookup.TryGetValue(item.CategoryName ?? string.Empty, out var categoryId))
        {
            diagnostics.Add($"Skipped suggestion for pattern '{item.Pattern}': unknown category '{item.CategoryName}'");
            return null;
        }

        // Parse match type
        if (!Enum.TryParse<RuleMatchType>(item.MatchType, ignoreCase: true, out var matchType))
        {
            matchType = RuleMatchType.Contains; // Default
        }

        return RuleSuggestion.CreateNewRuleSuggestion(
            title: $"Create rule for {item.Pattern}",
            description: item.Reasoning ?? string.Empty,
            reasoning: item.Reasoning ?? string.Empty,
            confidence: Math.Clamp(item.Confidence, 0m, 1m),
            suggestedPattern: item.Pattern ?? string.Empty,
            suggestedMatchType: matchType,
            suggestedCategoryId: categoryId,
            affectedTransactionCount: item.SampleMatches?.Count ?? 0,
            sampleDescriptions: item.SampleMatches ?? Array.Empty<string>());
    }

    private async Task<RuleSuggestion?> CreateOptimizationSuggestionAsync(
        OptimizationSuggestionItem item,
        Dictionary<Guid, CategorizationRule> ruleLookup,
        CancellationToken ct)
    {
        var type = item.Type?.ToLowerInvariant();

        return type switch
        {
            "remove" => await CreateUnusedRuleSuggestionAsync(item, ruleLookup, ct),
            "simplify" or "broaden" or "narrow" => await CreatePatternOptimizationSuggestionAsync(item, ruleLookup, ct),
            "consolidate" => await CreateConsolidationSuggestionAsync(item, ruleLookup, ct),
            _ => null,
        };
    }

    private async Task<RuleSuggestion?> CreateUnusedRuleSuggestionAsync(
        OptimizationSuggestionItem item,
        Dictionary<Guid, CategorizationRule> ruleLookup,
        CancellationToken ct)
    {
        if (!Guid.TryParse(item.TargetRuleId, out var ruleId) || !ruleLookup.TryGetValue(ruleId, out var rule))
        {
            return null;
        }

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

    private async Task<RuleSuggestion?> CreatePatternOptimizationSuggestionAsync(
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

    private async Task<RuleSuggestion?> CreateConsolidationSuggestionAsync(
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

    private async Task<RuleSuggestion?> CreateConflictSuggestionAsync(
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
}
