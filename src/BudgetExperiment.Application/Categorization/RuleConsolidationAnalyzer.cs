// <copyright file="RuleConsolidationAnalyzer.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Text.RegularExpressions;
using BudgetExperiment.Domain.Categorization;

namespace BudgetExperiment.Application.Categorization;

/// <summary>
/// Analyzes a set of categorization rules and produces consolidation suggestions.
/// Detects exact duplicates and substring containment relationships within the same category.
/// </summary>
public sealed class RuleConsolidationAnalyzer
{
    /// <summary>
    /// Analyzes the supplied rules and returns consolidation suggestions.
    /// </summary>
    /// <param name="rules">The active and inactive rules to analyze.</param>
    /// <returns>A list of <see cref="ConsolidationSuggestion"/> instances.</returns>
    public Task<IReadOnlyList<ConsolidationSuggestion>> AnalyzeAsync(IReadOnlyList<CategorizationRule> rules)
    {
        var activeRules = rules.Where(r => r.IsActive).ToList();

        var exactSuggestions = FindExactDuplicates(activeRules);
        var exactDuplicateIds = exactSuggestions.SelectMany(s => s.SourceRuleIds).ToHashSet();
        var substringSuggestions = FindSubstringContainments(activeRules, exactDuplicateIds);

        var allClaimedIds = exactSuggestions
            .Concat(substringSuggestions)
            .SelectMany(s => s.SourceRuleIds)
            .ToHashSet();

        var regexSuggestions = FindRegexAlternations(activeRules, allClaimedIds);

        IReadOnlyList<ConsolidationSuggestion> result = [.. exactSuggestions, .. substringSuggestions, .. regexSuggestions];
        return Task.FromResult(result);
    }

    private static List<ConsolidationSuggestion> FindExactDuplicates(IEnumerable<CategorizationRule> rules)
    {
        return rules
            .GroupBy(r => (r.CategoryId, r.MatchType, NormalizedKey: r.Pattern.ToUpperInvariant()))
            .Where(g => g.Count() >= 2)
            .Select(g => new ConsolidationSuggestion
            {
                SourceRuleIds = g.Select(r => r.Id).ToList(),
                MergedPattern = g.First().Pattern,
                MergedMatchType = g.Key.MatchType,
                Confidence = 1.0,
            })
            .ToList();
    }

    private static List<ConsolidationSuggestion> FindSubstringContainments(
        IEnumerable<CategorizationRule> rules,
        HashSet<Guid> excludedIds)
    {
        var containsRules = rules
            .Where(r => r.MatchType == RuleMatchType.Contains && !excludedIds.Contains(r.Id))
            .ToList();

        var suggestions = new List<ConsolidationSuggestion>();

        foreach (var group in containsRules.GroupBy(r => r.CategoryId))
        {
            AddSubstringPairsForGroup(group.ToList(), suggestions);
        }

        return suggestions;
    }

    private static void AddSubstringPairsForGroup(
        List<CategorizationRule> groupRules,
        List<ConsolidationSuggestion> suggestions)
    {
        for (var i = 0; i < groupRules.Count; i++)
        {
            for (var j = 0; j < groupRules.Count; j++)
            {
                if (i == j)
                {
                    continue;
                }

                var broader = groupRules[i];
                var narrower = groupRules[j];

                if (IsSubstringPair(broader.Pattern, narrower.Pattern))
                {
                    suggestions.Add(BuildSubstringSuggestion(broader, narrower));
                }
            }
        }
    }

    private static bool IsSubstringPair(string broaderPattern, string narrowerPattern)
    {
        return narrowerPattern.Contains(broaderPattern, StringComparison.OrdinalIgnoreCase)
            && !broaderPattern.Equals(narrowerPattern, StringComparison.OrdinalIgnoreCase);
    }

    private static ConsolidationSuggestion BuildSubstringSuggestion(
        CategorizationRule broader,
        CategorizationRule narrower)
    {
        return new ConsolidationSuggestion
        {
            SourceRuleIds = new List<Guid> { broader.Id, narrower.Id },
            MergedPattern = broader.Pattern,
            MergedMatchType = RuleMatchType.Contains,
            Confidence = 1.0,
        };
    }

    private static List<ConsolidationSuggestion> FindRegexAlternations(
        IEnumerable<CategorizationRule> rules,
        HashSet<Guid> excludedIds)
    {
        var containsRules = rules
            .Where(r => r.MatchType == RuleMatchType.Contains && !excludedIds.Contains(r.Id))
            .ToList();

        var suggestions = new List<ConsolidationSuggestion>();

        foreach (var group in containsRules.GroupBy(r => r.CategoryId))
        {
            var groupRules = group.ToList();
            if (groupRules.Count < 2)
            {
                continue;
            }

            AddAlternationSuggestionsForGroup(groupRules, suggestions);
        }

        return suggestions;
    }

    private static void AddAlternationSuggestionsForGroup(
        List<CategorizationRule> groupRules,
        List<ConsolidationSuggestion> suggestions)
    {
        var batch = new List<CategorizationRule>();
        var batchPattern = string.Empty;

        foreach (var rule in groupRules)
        {
            var escaped = Regex.Escape(rule.Pattern);
            var candidate = batch.Count == 0 ? escaped : batchPattern + "|" + escaped;

            if (candidate.Length > 500 && batch.Count > 0)
            {
                suggestions.Add(BuildAlternationSuggestion(batch, batchPattern));
                batch = [rule];
                batchPattern = escaped;
            }
            else
            {
                batch.Add(rule);
                batchPattern = candidate;
            }
        }

        if (batch.Count > 0)
        {
            suggestions.Add(BuildAlternationSuggestion(batch, batchPattern));
        }
    }

    private static ConsolidationSuggestion BuildAlternationSuggestion(
        List<CategorizationRule> rules,
        string mergedPattern)
    {
        return new ConsolidationSuggestion
        {
            SourceRuleIds = rules.Select(r => r.Id).ToList(),
            MergedPattern = mergedPattern,
            MergedMatchType = RuleMatchType.Regex,
            Confidence = 1.0,
        };
    }
}
