// <copyright file="CategorizationMapper.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Application.Categorization;

/// <summary>
/// Mappers for categorization-related domain entities to DTOs.
/// </summary>
public static class CategorizationMapper
{
    /// <summary>
    /// Maps a <see cref="CategorizationRule"/> to a <see cref="CategorizationRuleDto"/>.
    /// </summary>
    /// <param name="rule">The categorization rule entity.</param>
    /// <returns>The mapped DTO.</returns>
    public static CategorizationRuleDto ToDto(CategorizationRule rule)
    {
        return new CategorizationRuleDto
        {
            Id = rule.Id,
            Name = rule.Name,
            Pattern = rule.Pattern,
            MatchType = rule.MatchType.ToString(),
            CaseSensitive = rule.CaseSensitive,
            CategoryId = rule.CategoryId,
            CategoryName = rule.Category?.Name,
            Priority = rule.Priority,
            IsActive = rule.IsActive,
            CreatedAt = rule.CreatedAtUtc,
            UpdatedAt = rule.UpdatedAtUtc,
        };
    }

    /// <summary>
    /// Maps a <see cref="RuleSuggestion"/> to a <see cref="RuleSuggestionDto"/>.
    /// </summary>
    /// <param name="suggestion">The rule suggestion entity.</param>
    /// <param name="categoryName">The suggested category name (optional).</param>
    /// <param name="targetRuleName">The target rule name (optional).</param>
    /// <returns>The mapped DTO.</returns>
    public static RuleSuggestionDto ToDto(
        RuleSuggestion suggestion,
        string? categoryName = null,
        string? targetRuleName = null)
    {
        return new RuleSuggestionDto
        {
            Id = suggestion.Id,
            Type = suggestion.Type.ToString(),
            Status = suggestion.Status.ToString(),
            Title = suggestion.Title,
            Description = suggestion.Description,
            Reasoning = suggestion.Reasoning,
            Confidence = suggestion.Confidence,
            SuggestedPattern = suggestion.SuggestedPattern,
            SuggestedMatchType = suggestion.SuggestedMatchType?.ToString(),
            SuggestedCategoryId = suggestion.SuggestedCategoryId,
            SuggestedCategoryName = categoryName,
            TargetRuleId = suggestion.TargetRuleId,
            TargetRuleName = targetRuleName,
            OptimizedPattern = suggestion.OptimizedPattern,
            ConflictingRuleIds = suggestion.ConflictingRuleIds,
            AffectedTransactionCount = suggestion.AffectedTransactionCount,
            SampleDescriptions = suggestion.SampleDescriptions,
            CreatedAtUtc = suggestion.CreatedAtUtc,
            ReviewedAtUtc = suggestion.ReviewedAtUtc,
            UserFeedbackPositive = suggestion.UserFeedbackPositive,
        };
    }
}
