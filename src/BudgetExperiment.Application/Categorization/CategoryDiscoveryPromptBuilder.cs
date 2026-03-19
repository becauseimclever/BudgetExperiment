// <copyright file="CategoryDiscoveryPromptBuilder.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Application.Categorization;

/// <summary>
/// Builds AI prompts for discovering new categories from unmatched transaction descriptions.
/// </summary>
public static class CategoryDiscoveryPromptBuilder
{
    /// <summary>
    /// Builds an AI prompt for category discovery from unmatched transaction descriptions.
    /// </summary>
    /// <param name="unmatchedGroups">Aggregated unmatched description groups.</param>
    /// <param name="existingCategoryNames">The user's existing category names to avoid duplicates.</param>
    /// <param name="dismissedCategoryNames">Previously dismissed category names to avoid re-suggesting.</param>
    /// <returns>The AI prompt for category discovery.</returns>
    public static AiPrompt Build(
        IReadOnlyList<DescriptionGroup> unmatchedGroups,
        IReadOnlyList<string> existingCategoryNames,
        IReadOnlyList<string>? dismissedCategoryNames = null)
    {
        var dismissedSection = string.Empty;
        if (dismissedCategoryNames is { Count: > 0 })
        {
            dismissedSection = "PREVIOUSLY DISMISSED CATEGORIES (DO NOT re-suggest these):\n"
                + AiPrompts.FormatDismissedPatterns(dismissedCategoryNames);
        }

        var userPrompt = AiPrompts.CategoryDiscoveryPrompt
            .Replace("{existingCategories}", AiPrompts.FormatCategories(existingCategoryNames))
            .Replace("{dismissedSection}", dismissedSection)
            .Replace("{descriptions}", AiPrompts.FormatDescriptionGroups(unmatchedGroups));

        return new AiPrompt(
            SystemPrompt: AiPrompts.CategoryDiscoverySystemPrompt,
            UserPrompt: userPrompt,
            Temperature: 0.3m,
            MaxTokens: 2000);
    }
}
