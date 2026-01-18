// <copyright file="RuleSuggestionDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// Data transfer object for an AI-generated rule suggestion.
/// </summary>
public sealed class RuleSuggestionDto
{
    /// <summary>
    /// Gets or sets the suggestion identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the suggestion type (NewRule, PatternOptimization, RuleConsolidation, RuleConflict, UnusedRule).
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the suggestion status (Pending, Accepted, Dismissed).
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the suggestion title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the detailed description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the AI's reasoning for this suggestion.
    /// </summary>
    public string Reasoning { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the confidence level (0.0 to 1.0).
    /// </summary>
    public decimal Confidence { get; set; }

    /// <summary>
    /// Gets or sets the suggested pattern for new rules.
    /// </summary>
    public string? SuggestedPattern { get; set; }

    /// <summary>
    /// Gets or sets the suggested match type (Contains, Exact, StartsWith, EndsWith, Regex).
    /// </summary>
    public string? SuggestedMatchType { get; set; }

    /// <summary>
    /// Gets or sets the suggested category ID for new rules.
    /// </summary>
    public Guid? SuggestedCategoryId { get; set; }

    /// <summary>
    /// Gets or sets the suggested category name for display.
    /// </summary>
    public string? SuggestedCategoryName { get; set; }

    /// <summary>
    /// Gets or sets the target rule ID for optimization suggestions.
    /// </summary>
    public Guid? TargetRuleId { get; set; }

    /// <summary>
    /// Gets or sets the target rule name for display.
    /// </summary>
    public string? TargetRuleName { get; set; }

    /// <summary>
    /// Gets or sets the optimized pattern for pattern optimization suggestions.
    /// </summary>
    public string? OptimizedPattern { get; set; }

    /// <summary>
    /// Gets or sets the conflicting rule IDs for conflict/consolidation suggestions.
    /// </summary>
    public IReadOnlyList<Guid> ConflictingRuleIds { get; set; } = Array.Empty<Guid>();

    /// <summary>
    /// Gets or sets the number of transactions affected by this suggestion.
    /// </summary>
    public int AffectedTransactionCount { get; set; }

    /// <summary>
    /// Gets or sets sample transaction descriptions that match this suggestion.
    /// </summary>
    public IReadOnlyList<string> SampleDescriptions { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets when the suggestion was created (UTC).
    /// </summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets when the suggestion was reviewed (UTC), if applicable.
    /// </summary>
    public DateTime? ReviewedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the user's feedback (true = positive, false = negative, null = no feedback).
    /// </summary>
    public bool? UserFeedbackPositive { get; set; }
}
