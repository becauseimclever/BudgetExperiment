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
