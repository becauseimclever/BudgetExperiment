// <copyright file="RuleSuggestionPromptBuilder.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.Categorization;

/// <summary>
/// Builds AI prompts for rule suggestion analysis.
/// </summary>
public static class RuleSuggestionPromptBuilder
{
    /// <summary>
    /// Builds an AI prompt for suggesting new categorization rules.
    /// </summary>
    /// <param name="uncategorized">Uncategorized transactions to analyze.</param>
    /// <param name="categories">Available budget categories.</param>
    /// <param name="existingRules">Currently configured categorization rules.</param>
    /// <returns>The AI prompt.</returns>
    public static AiPrompt BuildNewRulePrompt(
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

    /// <summary>
    /// Builds an AI prompt for suggesting rule optimizations.
    /// </summary>
    /// <param name="rules">Existing categorization rules.</param>
    /// <param name="categories">Available budget categories.</param>
    /// <param name="matchStats">Rule match statistics.</param>
    /// <returns>The AI prompt.</returns>
    public static AiPrompt BuildOptimizationPrompt(
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

    /// <summary>
    /// Builds an AI prompt for detecting rule conflicts.
    /// </summary>
    /// <param name="rules">Existing categorization rules.</param>
    /// <param name="categories">Available budget categories.</param>
    /// <returns>The AI prompt.</returns>
    public static AiPrompt BuildConflictDetectionPrompt(
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

    /// <summary>
    /// Calculates how many transaction descriptions each rule matches.
    /// </summary>
    /// <param name="rules">Categorization rules to evaluate.</param>
    /// <param name="descriptions">Transaction descriptions to match against.</param>
    /// <returns>A list of rule names and their match counts.</returns>
    public static IReadOnlyList<(string RuleName, int MatchCount)> CalculateMatchStats(
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
}
