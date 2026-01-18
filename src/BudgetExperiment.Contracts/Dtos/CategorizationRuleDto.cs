// <copyright file="CategorizationRuleDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// Data transfer object for a categorization rule.
/// </summary>
public sealed class CategorizationRuleDto
{
    /// <summary>
    /// Gets or sets the rule identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the rule name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the pattern to match against transaction descriptions.
    /// </summary>
    public string Pattern { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the match type (Exact, Contains, StartsWith, EndsWith, Regex).
    /// </summary>
    public string MatchType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether matching is case-sensitive.
    /// </summary>
    public bool CaseSensitive { get; set; }

    /// <summary>
    /// Gets or sets the target category identifier.
    /// </summary>
    public Guid CategoryId { get; set; }

    /// <summary>
    /// Gets or sets the target category name.
    /// </summary>
    public string? CategoryName { get; set; }

    /// <summary>
    /// Gets or sets the priority (lower = evaluated first).
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the rule is active.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Gets or sets the creation timestamp (UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the last modification timestamp (UTC).
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Data transfer object for creating a categorization rule.
/// </summary>
public sealed class CategorizationRuleCreateDto
{
    /// <summary>
    /// Gets or sets the rule name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the pattern to match against transaction descriptions.
    /// </summary>
    public string Pattern { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the match type (Exact, Contains, StartsWith, EndsWith, Regex).
    /// </summary>
    public string MatchType { get; set; } = "Contains";

    /// <summary>
    /// Gets or sets a value indicating whether matching is case-sensitive.
    /// </summary>
    public bool CaseSensitive { get; set; }

    /// <summary>
    /// Gets or sets the target category identifier.
    /// </summary>
    public Guid CategoryId { get; set; }

    /// <summary>
    /// Gets or sets the optional priority. If not provided, next available priority is used.
    /// </summary>
    public int? Priority { get; set; }
}

/// <summary>
/// Data transfer object for updating a categorization rule.
/// </summary>
public sealed class CategorizationRuleUpdateDto
{
    /// <summary>
    /// Gets or sets the rule name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the pattern to match against transaction descriptions.
    /// </summary>
    public string Pattern { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the match type (Exact, Contains, StartsWith, EndsWith, Regex).
    /// </summary>
    public string MatchType { get; set; } = "Contains";

    /// <summary>
    /// Gets or sets a value indicating whether matching is case-sensitive.
    /// </summary>
    public bool CaseSensitive { get; set; }

    /// <summary>
    /// Gets or sets the target category identifier.
    /// </summary>
    public Guid CategoryId { get; set; }
}

/// <summary>
/// Request for testing a pattern against existing transactions.
/// </summary>
public sealed class TestPatternRequest
{
    /// <summary>
    /// Gets or sets the pattern to test.
    /// </summary>
    public string Pattern { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the match type (Exact, Contains, StartsWith, EndsWith, Regex).
    /// </summary>
    public string MatchType { get; set; } = "Contains";

    /// <summary>
    /// Gets or sets a value indicating whether matching is case-sensitive.
    /// </summary>
    public bool CaseSensitive { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of matching descriptions to return.
    /// </summary>
    public int Limit { get; set; } = 10;
}

/// <summary>
/// Response from testing a pattern.
/// </summary>
public sealed class TestPatternResponse
{
    /// <summary>
    /// Gets or sets the list of matching transaction descriptions.
    /// </summary>
    public IReadOnlyList<string> MatchingDescriptions { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the total count of matches.
    /// </summary>
    public int MatchCount { get; set; }
}

/// <summary>
/// Request for bulk applying categorization rules.
/// </summary>
public sealed class ApplyRulesRequest
{
    /// <summary>
    /// Gets or sets the transaction IDs to process. If null, all uncategorized transactions are processed.
    /// </summary>
    public IEnumerable<Guid>? TransactionIds { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to overwrite existing categories.
    /// </summary>
    public bool OverwriteExisting { get; set; }
}

/// <summary>
/// Response from bulk applying categorization rules.
/// </summary>
public sealed class ApplyRulesResponse
{
    /// <summary>
    /// Gets or sets the total number of transactions processed.
    /// </summary>
    public int TotalProcessed { get; set; }

    /// <summary>
    /// Gets or sets the number of transactions categorized.
    /// </summary>
    public int Categorized { get; set; }

    /// <summary>
    /// Gets or sets the number of transactions skipped.
    /// </summary>
    public int Skipped { get; set; }

    /// <summary>
    /// Gets or sets the number of errors encountered.
    /// </summary>
    public int Errors { get; set; }

    /// <summary>
    /// Gets or sets the error messages, if any.
    /// </summary>
    public IReadOnlyList<string> ErrorMessages { get; set; } = Array.Empty<string>();
}

/// <summary>
/// Request for reordering rule priorities.
/// </summary>
public sealed class ReorderRulesRequest
{
    /// <summary>
    /// Gets or sets the ordered list of rule IDs. The index becomes the new priority.
    /// </summary>
    public IReadOnlyList<Guid> RuleIds { get; set; } = Array.Empty<Guid>();
}
