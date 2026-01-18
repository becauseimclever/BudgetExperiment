// <copyright file="CategorizationRule.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Text.RegularExpressions;

namespace BudgetExperiment.Domain;

/// <summary>
/// Represents a rule for automatically categorizing transactions based on description matching.
/// </summary>
public sealed class CategorizationRule
{
    /// <summary>
    /// Maximum length for rule name.
    /// </summary>
    public const int MaxNameLength = 200;

    /// <summary>
    /// Maximum length for pattern.
    /// </summary>
    public const int MaxPatternLength = 500;

    /// <summary>
    /// Default priority for new rules.
    /// </summary>
    public const int DefaultPriority = 100;

    /// <summary>
    /// Timeout for regex matching to prevent catastrophic backtracking.
    /// </summary>
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(1);

    private Regex? _compiledRegex;

    /// <summary>
    /// Initializes a new instance of the <see cref="CategorizationRule"/> class.
    /// </summary>
    /// <remarks>
    /// Private constructor for EF Core and factory method.
    /// </remarks>
    private CategorizationRule()
    {
    }

    /// <summary>
    /// Gets the unique identifier.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the rule name for display purposes.
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the match type that determines how the pattern is applied.
    /// </summary>
    public RuleMatchType MatchType { get; private set; }

    /// <summary>
    /// Gets the pattern to match against transaction descriptions.
    /// </summary>
    public string Pattern { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the category ID to assign when the rule matches.
    /// </summary>
    public Guid CategoryId { get; private set; }

    /// <summary>
    /// Gets the priority for rule evaluation. Lower number = higher priority (evaluated first).
    /// </summary>
    public int Priority { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the rule is active.
    /// </summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>
    /// Gets a value indicating whether matching is case-sensitive.
    /// </summary>
    public bool CaseSensitive { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp when the rule was created.
    /// </summary>
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp when the rule was last updated.
    /// </summary>
    public DateTime UpdatedAtUtc { get; private set; }

    /// <summary>
    /// Gets the associated budget category (navigation property for queries).
    /// </summary>
    public BudgetCategory? Category { get; private set; }

    /// <summary>
    /// Creates a new categorization rule.
    /// </summary>
    /// <param name="name">The rule name for display purposes.</param>
    /// <param name="matchType">How the pattern should be matched.</param>
    /// <param name="pattern">The pattern to match against descriptions.</param>
    /// <param name="categoryId">The category ID to assign when matched.</param>
    /// <param name="priority">Priority for evaluation order (lower = first). Defaults to 100.</param>
    /// <param name="caseSensitive">Whether matching is case-sensitive. Defaults to false.</param>
    /// <returns>A new <see cref="CategorizationRule"/> instance.</returns>
    /// <exception cref="DomainException">Thrown when validation fails.</exception>
    public static CategorizationRule Create(
        string name,
        RuleMatchType matchType,
        string pattern,
        Guid categoryId,
        int priority = DefaultPriority,
        bool caseSensitive = false)
    {
        ValidateName(name);
        ValidatePattern(pattern, matchType);
        ValidateCategoryId(categoryId);
        ValidatePriority(priority);

        var now = DateTime.UtcNow;
        var rule = new CategorizationRule
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            MatchType = matchType,
            Pattern = pattern.Trim(),
            CategoryId = categoryId,
            Priority = priority,
            IsActive = true,
            CaseSensitive = caseSensitive,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };

        // Pre-compile regex if applicable
        if (matchType == RuleMatchType.Regex)
        {
            rule._compiledRegex = BuildRegex(rule.Pattern, caseSensitive);
        }

        return rule;
    }

    /// <summary>
    /// Updates the rule properties.
    /// </summary>
    /// <param name="name">The new rule name.</param>
    /// <param name="matchType">The new match type.</param>
    /// <param name="pattern">The new pattern.</param>
    /// <param name="categoryId">The new category ID.</param>
    /// <param name="caseSensitive">Whether matching is case-sensitive.</param>
    /// <exception cref="DomainException">Thrown when validation fails.</exception>
    public void Update(
        string name,
        RuleMatchType matchType,
        string pattern,
        Guid categoryId,
        bool caseSensitive)
    {
        ValidateName(name);
        ValidatePattern(pattern, matchType);
        ValidateCategoryId(categoryId);

        this.Name = name.Trim();
        this.MatchType = matchType;
        this.Pattern = pattern.Trim();
        this.CategoryId = categoryId;
        this.CaseSensitive = caseSensitive;
        this.UpdatedAtUtc = DateTime.UtcNow;

        // Update compiled regex
        this._compiledRegex = matchType == RuleMatchType.Regex
            ? BuildRegex(this.Pattern, caseSensitive)
            : null;
    }

    /// <summary>
    /// Sets the rule priority.
    /// </summary>
    /// <param name="priority">The new priority value (lower = higher priority).</param>
    /// <exception cref="DomainException">Thrown when priority is negative.</exception>
    public void SetPriority(int priority)
    {
        ValidatePriority(priority);

        this.Priority = priority;
        this.UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Deactivates the rule so it won't be evaluated.
    /// </summary>
    public void Deactivate()
    {
        this.IsActive = false;
        this.UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Activates the rule so it will be evaluated.
    /// </summary>
    public void Activate()
    {
        this.IsActive = true;
        this.UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Tests whether the given description matches this rule's pattern.
    /// </summary>
    /// <param name="description">The transaction description to test.</param>
    /// <returns>True if the description matches; otherwise, false.</returns>
    public bool Matches(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return false;
        }

        return this.MatchType switch
        {
            RuleMatchType.Exact => this.MatchesExact(description),
            RuleMatchType.Contains => this.MatchesContains(description),
            RuleMatchType.StartsWith => this.MatchesStartsWith(description),
            RuleMatchType.EndsWith => this.MatchesEndsWith(description),
            RuleMatchType.Regex => this.MatchesRegex(description),
            _ => false,
        };
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Rule name is required.");
        }

        if (name.Trim().Length > MaxNameLength)
        {
            throw new DomainException($"Rule name cannot exceed {MaxNameLength} characters.");
        }
    }

    private static void ValidatePattern(string pattern, RuleMatchType matchType)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            throw new DomainException("Rule pattern is required.");
        }

        if (pattern.Trim().Length > MaxPatternLength)
        {
            throw new DomainException($"Rule pattern cannot exceed {MaxPatternLength} characters.");
        }

        if (matchType == RuleMatchType.Regex)
        {
            try
            {
                _ = new Regex(pattern.Trim(), RegexOptions.None, RegexTimeout);
            }
            catch (ArgumentException)
            {
                throw new DomainException("Invalid regex pattern.");
            }
        }
    }

    private static void ValidateCategoryId(Guid categoryId)
    {
        if (categoryId == Guid.Empty)
        {
            throw new DomainException("Category ID is required.");
        }
    }

    private static void ValidatePriority(int priority)
    {
        if (priority < 0)
        {
            throw new DomainException("Priority cannot be negative.");
        }
    }

    private static Regex BuildRegex(string pattern, bool caseSensitive)
    {
        var options = caseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;
        return new Regex(pattern, options, RegexTimeout);
    }

    private bool MatchesExact(string description)
    {
        var comparison = this.CaseSensitive
            ? StringComparison.Ordinal
            : StringComparison.OrdinalIgnoreCase;

        return description.Equals(this.Pattern, comparison);
    }

    private bool MatchesContains(string description)
    {
        var comparison = this.CaseSensitive
            ? StringComparison.Ordinal
            : StringComparison.OrdinalIgnoreCase;

        return description.Contains(this.Pattern, comparison);
    }

    private bool MatchesStartsWith(string description)
    {
        var comparison = this.CaseSensitive
            ? StringComparison.Ordinal
            : StringComparison.OrdinalIgnoreCase;

        return description.StartsWith(this.Pattern, comparison);
    }

    private bool MatchesEndsWith(string description)
    {
        var comparison = this.CaseSensitive
            ? StringComparison.Ordinal
            : StringComparison.OrdinalIgnoreCase;

        return description.EndsWith(this.Pattern, comparison);
    }

    private bool MatchesRegex(string description)
    {
        try
        {
            // Use pre-compiled regex if available, otherwise build one
            var regex = this._compiledRegex ?? BuildRegex(this.Pattern, this.CaseSensitive);
            return regex.IsMatch(description);
        }
        catch (RegexMatchTimeoutException)
        {
            // Regex took too long, treat as no match
            return false;
        }
    }
}
