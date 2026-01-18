// <copyright file="RuleMatchType.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain;

/// <summary>
/// Defines how a categorization rule matches transaction descriptions.
/// </summary>
public enum RuleMatchType
{
    /// <summary>
    /// Description exactly matches pattern.
    /// </summary>
    Exact,

    /// <summary>
    /// Description contains pattern.
    /// </summary>
    Contains,

    /// <summary>
    /// Description starts with pattern.
    /// </summary>
    StartsWith,

    /// <summary>
    /// Description ends with pattern.
    /// </summary>
    EndsWith,

    /// <summary>
    /// Pattern is a regular expression.
    /// </summary>
    Regex,
}
