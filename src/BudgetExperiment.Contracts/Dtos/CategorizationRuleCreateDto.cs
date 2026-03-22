// <copyright file="CategorizationRuleCreateDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

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
    public bool CaseSensitive
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the target category identifier.
    /// </summary>
    public Guid CategoryId
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the optional priority. If not provided, next available priority is used.
    /// </summary>
    public int? Priority
    {
        get; set;
    }
}
