// <copyright file="RuleSuggestion.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Categorization;

/// <summary>
/// Represents an AI-generated suggestion for a categorization rule.
/// </summary>
public sealed class RuleSuggestion
{
    /// <summary>
    /// Maximum length for suggestion title.
    /// </summary>
    public const int MaxTitleLength = 200;

    /// <summary>
    /// Maximum length for pattern.
    /// </summary>
    public const int MaxPatternLength = 500;

    /// <summary>
    /// Maximum length for dismissal reason.
    /// </summary>
    public const int MaxDismissalReasonLength = 500;

    private List<Guid> _conflictingRuleIds = new();
    private List<string> _sampleDescriptions = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="RuleSuggestion"/> class.
    /// </summary>
    /// <remarks>
    /// Private constructor for EF Core and factory methods.
    /// </remarks>
    private RuleSuggestion()
    {
    }

    /// <summary>
    /// Gets the unique identifier.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the type of suggestion.
    /// </summary>
    public SuggestionType Type { get; private set; }

    /// <summary>
    /// Gets the review status of the suggestion.
    /// </summary>
    public SuggestionStatus Status { get; private set; }

    /// <summary>
    /// Gets the suggestion title for display.
    /// </summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the detailed description of the suggestion.
    /// </summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the AI's reasoning for this suggestion.
    /// </summary>
    public string Reasoning { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the confidence score (0.0 to 1.0).
    /// </summary>
    public decimal Confidence { get; private set; }

    /// <summary>
    /// Gets the suggested pattern for new rule suggestions.
    /// </summary>
    public string? SuggestedPattern { get; private set; }

    /// <summary>
    /// Gets the suggested match type for new rule suggestions.
    /// </summary>
    public RuleMatchType? SuggestedMatchType { get; private set; }

    /// <summary>
    /// Gets the suggested category ID for new rule suggestions.
    /// </summary>
    public Guid? SuggestedCategoryId { get; private set; }

    /// <summary>
    /// Gets the target rule ID for optimization and unused rule suggestions.
    /// </summary>
    public Guid? TargetRuleId { get; private set; }

    /// <summary>
    /// Gets the optimized pattern for optimization suggestions.
    /// </summary>
    public string? OptimizedPattern { get; private set; }

    /// <summary>
    /// Gets the IDs of conflicting rules or rules to consolidate.
    /// </summary>
    public IReadOnlyList<Guid> ConflictingRuleIds => _conflictingRuleIds.AsReadOnly();

    /// <summary>
    /// Gets the count of transactions affected by this suggestion.
    /// </summary>
    public int AffectedTransactionCount { get; private set; }

    /// <summary>
    /// Gets sample transaction descriptions that would match.
    /// </summary>
    public IReadOnlyList<string> SampleDescriptions => _sampleDescriptions.AsReadOnly();

    /// <summary>
    /// Gets the UTC timestamp when the suggestion was created.
    /// </summary>
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp when the suggestion was reviewed.
    /// </summary>
    public DateTime? ReviewedAtUtc { get; private set; }

    /// <summary>
    /// Gets the reason for dismissal, if dismissed.
    /// </summary>
    public string? DismissalReason { get; private set; }

    /// <summary>
    /// Gets the user's feedback on the suggestion quality.
    /// </summary>
    public bool? UserFeedbackPositive { get; private set; }

    /// <summary>
    /// Creates a suggestion to add a new categorization rule.
    /// </summary>
    /// <param name="title">The suggestion title.</param>
    /// <param name="description">The detailed description.</param>
    /// <param name="reasoning">The AI's reasoning.</param>
    /// <param name="confidence">The confidence score (0.0 to 1.0).</param>
    /// <param name="suggestedPattern">The pattern for the new rule.</param>
    /// <param name="suggestedMatchType">The match type for the new rule.</param>
    /// <param name="suggestedCategoryId">The category ID for the new rule.</param>
    /// <param name="affectedTransactionCount">Number of transactions that would be affected.</param>
    /// <param name="sampleDescriptions">Sample transaction descriptions.</param>
    /// <returns>A new rule suggestion.</returns>
    public static RuleSuggestion CreateNewRuleSuggestion(
        string title,
        string description,
        string reasoning,
        decimal confidence,
        string suggestedPattern,
        RuleMatchType suggestedMatchType,
        Guid suggestedCategoryId,
        int affectedTransactionCount,
        IReadOnlyList<string> sampleDescriptions)
    {
        ValidateTitle(title);
        ValidateConfidence(confidence);
        ValidatePattern(suggestedPattern);
        ValidateCategoryId(suggestedCategoryId);
        ValidateAffectedCount(affectedTransactionCount);

        return new RuleSuggestion
        {
            Id = Guid.NewGuid(),
            Type = SuggestionType.NewRule,
            Status = SuggestionStatus.Pending,
            Title = title.Trim(),
            Description = description ?? string.Empty,
            Reasoning = reasoning ?? string.Empty,
            Confidence = confidence,
            SuggestedPattern = suggestedPattern.Trim(),
            SuggestedMatchType = suggestedMatchType,
            SuggestedCategoryId = suggestedCategoryId,
            AffectedTransactionCount = affectedTransactionCount,
            _sampleDescriptions = sampleDescriptions?.ToList() ?? new List<string>(),
            CreatedAtUtc = DateTime.UtcNow,
        };
    }

    /// <summary>
    /// Creates a suggestion to optimize an existing rule's pattern.
    /// </summary>
    /// <param name="title">The suggestion title.</param>
    /// <param name="description">The detailed description.</param>
    /// <param name="reasoning">The AI's reasoning.</param>
    /// <param name="confidence">The confidence score (0.0 to 1.0).</param>
    /// <param name="targetRuleId">The ID of the rule to optimize.</param>
    /// <param name="optimizedPattern">The optimized pattern.</param>
    /// <returns>An optimization suggestion.</returns>
    public static RuleSuggestion CreateOptimizationSuggestion(
        string title,
        string description,
        string reasoning,
        decimal confidence,
        Guid targetRuleId,
        string optimizedPattern)
    {
        ValidateTitle(title);
        ValidateConfidence(confidence);
        ValidateTargetRuleId(targetRuleId);
        ValidateOptimizedPattern(optimizedPattern);

        return new RuleSuggestion
        {
            Id = Guid.NewGuid(),
            Type = SuggestionType.PatternOptimization,
            Status = SuggestionStatus.Pending,
            Title = title.Trim(),
            Description = description ?? string.Empty,
            Reasoning = reasoning ?? string.Empty,
            Confidence = confidence,
            TargetRuleId = targetRuleId,
            OptimizedPattern = optimizedPattern.Trim(),
            CreatedAtUtc = DateTime.UtcNow,
        };
    }

    /// <summary>
    /// Creates a suggestion alerting to conflicting rules.
    /// </summary>
    /// <param name="title">The suggestion title.</param>
    /// <param name="description">The detailed description.</param>
    /// <param name="reasoning">The AI's reasoning.</param>
    /// <param name="conflictingRuleIds">The IDs of the conflicting rules.</param>
    /// <returns>A conflict suggestion.</returns>
    public static RuleSuggestion CreateConflictSuggestion(
        string title,
        string description,
        string reasoning,
        IReadOnlyList<Guid> conflictingRuleIds)
    {
        ValidateTitle(title);
        ValidateConflictingRuleIds(conflictingRuleIds);

        return new RuleSuggestion
        {
            Id = Guid.NewGuid(),
            Type = SuggestionType.RuleConflict,
            Status = SuggestionStatus.Pending,
            Title = title.Trim(),
            Description = description ?? string.Empty,
            Reasoning = reasoning ?? string.Empty,
            Confidence = 0m,
            _conflictingRuleIds = conflictingRuleIds.ToList(),
            CreatedAtUtc = DateTime.UtcNow,
        };
    }

    /// <summary>
    /// Creates a suggestion alerting to an unused rule.
    /// </summary>
    /// <param name="title">The suggestion title.</param>
    /// <param name="description">The detailed description.</param>
    /// <param name="reasoning">The AI's reasoning.</param>
    /// <param name="targetRuleId">The ID of the unused rule.</param>
    /// <returns>An unused rule suggestion.</returns>
    public static RuleSuggestion CreateUnusedRuleSuggestion(
        string title,
        string description,
        string reasoning,
        Guid targetRuleId)
    {
        ValidateTitle(title);
        ValidateTargetRuleId(targetRuleId);

        return new RuleSuggestion
        {
            Id = Guid.NewGuid(),
            Type = SuggestionType.UnusedRule,
            Status = SuggestionStatus.Pending,
            Title = title.Trim(),
            Description = description ?? string.Empty,
            Reasoning = reasoning ?? string.Empty,
            Confidence = 0m,
            TargetRuleId = targetRuleId,
            AffectedTransactionCount = 0,
            CreatedAtUtc = DateTime.UtcNow,
        };
    }

    /// <summary>
    /// Creates a suggestion to consolidate multiple rules.
    /// </summary>
    /// <param name="title">The suggestion title.</param>
    /// <param name="description">The detailed description.</param>
    /// <param name="reasoning">The AI's reasoning.</param>
    /// <param name="confidence">The confidence score (0.0 to 1.0).</param>
    /// <param name="ruleIds">The IDs of the rules to consolidate.</param>
    /// <param name="consolidatedPattern">The consolidated pattern.</param>
    /// <returns>A consolidation suggestion.</returns>
    public static RuleSuggestion CreateConsolidationSuggestion(
        string title,
        string description,
        string reasoning,
        decimal confidence,
        IReadOnlyList<Guid> ruleIds,
        string consolidatedPattern)
    {
        ValidateTitle(title);
        ValidateConfidence(confidence);
        ValidateConflictingRuleIds(ruleIds);
        ValidateOptimizedPattern(consolidatedPattern);

        return new RuleSuggestion
        {
            Id = Guid.NewGuid(),
            Type = SuggestionType.RuleConsolidation,
            Status = SuggestionStatus.Pending,
            Title = title.Trim(),
            Description = description ?? string.Empty,
            Reasoning = reasoning ?? string.Empty,
            Confidence = confidence,
            _conflictingRuleIds = ruleIds.ToList(),
            OptimizedPattern = consolidatedPattern.Trim(),
            CreatedAtUtc = DateTime.UtcNow,
        };
    }

    /// <summary>
    /// Accepts the suggestion.
    /// </summary>
    public void Accept()
    {
        if (Status != SuggestionStatus.Pending)
        {
            throw new DomainException("Only pending suggestions can be accepted.");
        }

        Status = SuggestionStatus.Accepted;
        ReviewedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Dismisses the suggestion.
    /// </summary>
    /// <param name="reason">Optional reason for dismissal.</param>
    public void Dismiss(string? reason = null)
    {
        if (Status != SuggestionStatus.Pending)
        {
            throw new DomainException("Only pending suggestions can be dismissed.");
        }

        Status = SuggestionStatus.Dismissed;
        ReviewedAtUtc = DateTime.UtcNow;
        DismissalReason = reason;
    }

    /// <summary>
    /// Provides user feedback on the suggestion quality.
    /// </summary>
    /// <param name="positive">True for positive feedback, false for negative.</param>
    public void ProvideFeedback(bool positive)
    {
        if (Status == SuggestionStatus.Pending)
        {
            throw new DomainException("Cannot provide feedback on suggestions that have not been reviewed.");
        }

        UserFeedbackPositive = positive;
    }

    private static void ValidateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainException("Suggestion title is required.");
        }

        if (title.Trim().Length > MaxTitleLength)
        {
            throw new DomainException($"Suggestion title cannot exceed {MaxTitleLength} characters.");
        }
    }

    private static void ValidateConfidence(decimal confidence)
    {
        if (confidence < 0 || confidence > 1)
        {
            throw new DomainException("Confidence must be between 0 and 1.");
        }
    }

    private static void ValidatePattern(string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            throw new DomainException("Suggested pattern is required.");
        }

        if (pattern.Trim().Length > MaxPatternLength)
        {
            throw new DomainException($"Pattern cannot exceed {MaxPatternLength} characters.");
        }
    }

    private static void ValidateOptimizedPattern(string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            throw new DomainException("Optimized pattern is required.");
        }

        if (pattern.Trim().Length > MaxPatternLength)
        {
            throw new DomainException($"Pattern cannot exceed {MaxPatternLength} characters.");
        }
    }

    private static void ValidateCategoryId(Guid categoryId)
    {
        if (categoryId == Guid.Empty)
        {
            throw new DomainException("Category ID is required.");
        }
    }

    private static void ValidateTargetRuleId(Guid ruleId)
    {
        if (ruleId == Guid.Empty)
        {
            throw new DomainException("Target rule ID is required.");
        }
    }

    private static void ValidateAffectedCount(int count)
    {
        if (count < 0)
        {
            throw new DomainException("Affected transaction count cannot be negative.");
        }
    }

    private static void ValidateConflictingRuleIds(IReadOnlyList<Guid> ruleIds)
    {
        if (ruleIds == null || ruleIds.Count < 2)
        {
            throw new DomainException("At least two conflicting rule IDs are required.");
        }
    }
}
