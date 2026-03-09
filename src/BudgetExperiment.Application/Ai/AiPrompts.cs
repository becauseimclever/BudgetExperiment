// <copyright file="AiPrompts.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Application.Ai;

/// <summary>
/// Contains prompt templates for AI-powered rule suggestion features.
/// </summary>
public static class AiPrompts
{
    /// <summary>
    /// System prompt that sets up the AI's role and constraints.
    /// </summary>
    public const string SystemPrompt = """
        You are a financial transaction categorization assistant specializing in personal banking data.
        Your job is to analyze transaction descriptions from bank and credit card statements and suggest
        categorization rules. You must:
        1. Identify common merchant patterns in transaction descriptions (e.g., WALMART, AMAZON, STARBUCKS)
        2. Recognize that bank descriptions often contain noise — prefixes like "PURCHASE AUTHORIZED ON",
           "POS DEBIT", "RECURRING PAYMENT", and suffixes like card numbers, dates, and reference codes
        3. Suggest specific, accurate matching patterns that target the merchant name, not the noise
        4. Recommend appropriate categories based on the merchant type (grocery, dining, utilities, etc.)
        5. Set higher confidence for patterns that appear frequently (10+ transactions)
        6. Explain your reasoning clearly
        7. Never hallucinate or make up transaction data

        Respond ONLY with valid JSON in the specified format. Do not include any other text, markdown formatting, or code blocks.
        """;

    /// <summary>
    /// User prompt template for suggesting new categorization rules.
    /// Placeholders: {categories}, {existingRules}, {descriptions}.
    /// </summary>
    public const string NewRuleSuggestionPrompt = """
        Analyze these uncategorized transaction descriptions and suggest categorization rules.
        Descriptions are grouped by frequency with transaction counts and amount ranges.
        Prioritize high-frequency, high-value patterns first.

        EXISTING CATEGORIES (use only these category names):
        {categories}

        EXISTING RULES (avoid duplicating these patterns):
        {existingRules}

        UNCATEGORIZED TRANSACTION DESCRIPTIONS TO ANALYZE:
        {descriptions}

        For each distinct pattern you identify, suggest a rule. Respond with this exact JSON structure:
        {
          "suggestions": [
            {
              "pattern": "the pattern to match (use simple text for Contains/StartsWith, or regex for complex patterns)",
              "matchType": "Contains",
              "categoryName": "one of the existing category names",
              "confidence": 0.85,
              "reasoning": "brief explanation of why this pattern and category",
              "sampleMatches": ["example descriptions that would match"]
            }
          ]
        }

        Rules for matchType:
        - "Contains": pattern appears anywhere in description (most common, use for merchant names)
        - "StartsWith": pattern appears at beginning (use for transaction codes)
        - "Exact": exact match (rarely needed)
        - "Regex": regular expression (use sparingly, only for complex patterns)

        GOOD SUGGESTION EXAMPLE (high frequency, specific merchant, correct category):
        Input: "STARBUCKS — 23 transactions, $4.50–$7.25"
        Output: {"pattern": "STARBUCKS", "matchType": "Contains", "categoryName": "Dining", "confidence": 0.90, "reasoning": "Starbucks is a coffee shop; 23 transactions indicate a regular pattern", "sampleMatches": ["STARBUCKS STORE 12345"]}

        BAD SUGGESTION EXAMPLE (too generic, low value):
        Input: "PURCHASE — 5 transactions, $10.00–$500.00"
        Output: {"pattern": "PURCHASE", "matchType": "Contains", "categoryName": "Shopping", "confidence": 0.50, "reasoning": "Generic purchase"}
        Why this is bad: "PURCHASE" is too broad — it would match nearly every transaction. Suggest merchant-specific patterns instead.

        Guidelines:
        - Confidence should be 0.5-1.0 based on how certain you are
        - Patterns appearing 10+ times should have higher confidence (0.8+)
        - Only suggest patterns with at least 2 matching transactions
        - Prefer simple Contains matches over complex regex
        - sampleMatches should contain actual descriptions from the input that would match
        - Avoid overly generic patterns like "PAYMENT", "PURCHASE", "DEBIT" that match too broadly
        - Focus on merchant names and specific identifiers
        """;

    /// <summary>
    /// User prompt template for suggesting rule optimizations.
    /// Placeholders: {rules}, {matchStats}.
    /// </summary>
    public const string OptimizationPrompt = """
        Analyze these categorization rules and suggest optimizations.

        CURRENT RULES:
        {rules}

        TRANSACTION MATCH STATISTICS (rule name: match count):
        {matchStats}

        Look for:
        1. Rules with overly complex patterns that could be simplified
        2. Multiple rules that could be consolidated into one
        3. Rules that never match any transactions (0 matches)
        4. Patterns that might be too broad or too narrow

        Respond with this exact JSON structure:
        {
          "suggestions": [
            {
              "type": "simplify",
              "targetRuleId": "guid-of-rule-to-modify",
              "suggestedPattern": "simplified pattern",
              "reasoning": "explanation of the improvement"
            }
          ]
        }

        Types:
        - "simplify": Make a complex pattern simpler
        - "consolidate": Merge multiple rules (list all in targetRuleIds array)
        - "remove": Suggest removing unused rule
        - "broaden": Make pattern match more transactions
        - "narrow": Make pattern more specific

        For consolidate type, use targetRuleIds (array) instead of targetRuleId.
        """;

    /// <summary>
    /// User prompt template for detecting rule conflicts.
    /// Placeholders: {rules}.
    /// </summary>
    public const string ConflictDetectionPrompt = """
        Analyze these categorization rules for conflicts and overlaps.

        CURRENT RULES:
        {rules}

        Look for:
        1. Rules with overlapping patterns that would match the same transactions
        2. Rules that contradict each other (same pattern, different categories)
        3. Priority issues where a lower-priority rule might never match

        Respond with this exact JSON structure:
        {
          "conflicts": [
            {
              "ruleIds": ["guid1", "guid2"],
              "conflictType": "overlap",
              "description": "Both rules match transactions starting with 'AMAZON'",
              "resolution": "Suggested way to resolve the conflict"
            }
          ]
        }

        Conflict types:
        - "overlap": Patterns match same transactions
        - "contradiction": Same pattern maps to different categories
        - "shadowed": Higher priority rule prevents lower rule from ever matching
        """;

    /// <summary>
    /// Formats the categories list for inclusion in prompts.
    /// </summary>
    /// <param name="categories">The category names.</param>
    /// <returns>Formatted string for prompt insertion.</returns>
    public static string FormatCategories(IEnumerable<string> categories)
    {
        return string.Join("\n", categories.Select(c => $"- {c}"));
    }

    /// <summary>
    /// Formats existing rules for inclusion in prompts.
    /// </summary>
    /// <param name="rules">The rules with their patterns.</param>
    /// <returns>Formatted string for prompt insertion.</returns>
    public static string FormatExistingRules(IEnumerable<(string Name, string Pattern, string MatchType)> rules)
    {
        return string.Join("\n", rules.Select(r => $"- {r.Name}: [{r.MatchType}] {r.Pattern}"));
    }

    /// <summary>
    /// Formats transaction descriptions for inclusion in prompts.
    /// </summary>
    /// <param name="descriptions">The transaction descriptions.</param>
    /// <returns>Formatted string for prompt insertion.</returns>
    public static string FormatDescriptions(IEnumerable<string> descriptions)
    {
        return string.Join("\n", descriptions.Select((d, i) => $"{i + 1}. {d}"));
    }

    /// <summary>
    /// Formats rules with match statistics for optimization prompts.
    /// </summary>
    /// <param name="rules">Rules with their properties.</param>
    /// <returns>Formatted string for prompt insertion.</returns>
    public static string FormatRulesForOptimization(
        IEnumerable<(Guid Id, string Name, string Pattern, string MatchType, string CategoryName)> rules)
    {
        return string.Join("\n", rules.Select(r =>
            $"- ID: {r.Id}\n  Name: {r.Name}\n  Pattern: [{r.MatchType}] {r.Pattern}\n  Category: {r.CategoryName}"));
    }

    /// <summary>
    /// Formats match statistics for inclusion in prompts.
    /// </summary>
    /// <param name="stats">The rule name to match count mapping.</param>
    /// <returns>Formatted string for prompt insertion.</returns>
    public static string FormatMatchStats(IEnumerable<(string RuleName, int MatchCount)> stats)
    {
        return string.Join("\n", stats.Select(s => $"- {s.RuleName}: {s.MatchCount} matches"));
    }

    /// <summary>
    /// Formats description groups with frequency and amount ranges for enriched prompts.
    /// </summary>
    /// <param name="groups">The aggregated description groups.</param>
    /// <returns>Formatted string for prompt insertion.</returns>
    public static string FormatDescriptionGroups(IEnumerable<Categorization.DescriptionGroup> groups)
    {
        return string.Join("\n", groups.Select((g, i) =>
        {
            var line = $"{i + 1}. {g.RepresentativeDescription} — ";
            var countLabel = g.Count == 1 ? "1 transaction" : $"{g.Count} transactions";

            if (g.MinAmount.HasValue && g.MaxAmount.HasValue)
            {
                var amountPart = g.MinAmount == g.MaxAmount
                    ? $"${g.MinAmount:F2}"
                    : $"${g.MinAmount:F2}\u2013${g.MaxAmount:F2}";
                return $"{line}{countLabel}, {amountPart}";
            }

            return $"{line}{countLabel}";
        }));
    }

    /// <summary>
    /// Formats previously dismissed patterns as a bullet list for the "DO NOT suggest" section.
    /// </summary>
    /// <param name="patterns">The dismissed patterns.</param>
    /// <returns>Formatted string for prompt insertion.</returns>
    public static string FormatDismissedPatterns(IEnumerable<string> patterns)
    {
        return string.Join("\n", patterns.Select(p => $"- {p}"));
    }

    /// <summary>
    /// Formats previously accepted suggestion examples for the prompt.
    /// </summary>
    /// <param name="examples">The accepted suggestion examples.</param>
    /// <returns>Formatted string for prompt insertion.</returns>
    public static string FormatAcceptedExamples(IEnumerable<Categorization.AcceptedSuggestionExample> examples)
    {
        return string.Join("\n", examples.Select(e =>
            $"- Pattern \"{e.Pattern}\" \u2192 Category \"{e.CategoryName}\" (accepted)"));
    }
}
