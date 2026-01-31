// <copyright file="ImportPattern.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Recurring;

/// <summary>
/// Value object representing an import description pattern for matching transactions.
/// Supports exact matching and wildcard patterns (prefix *, suffix *, or both).
/// </summary>
public sealed record ImportPattern
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ImportPattern"/> class.
    /// </summary>
    /// <param name="pattern">The normalized pattern string.</param>
    private ImportPattern(string pattern)
    {
        this.Pattern = pattern;
    }

    /// <summary>
    /// Gets the pattern string (uppercase, trimmed).
    /// </summary>
    public string Pattern { get; }

    /// <summary>
    /// Creates a new import pattern with validation and normalization.
    /// </summary>
    /// <param name="pattern">The pattern string. Can contain * as prefix and/or suffix wildcards.</param>
    /// <returns>A new <see cref="ImportPattern"/> instance.</returns>
    /// <exception cref="DomainException">Thrown when pattern is null or empty.</exception>
    public static ImportPattern Create(string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            throw new DomainException("Import pattern is required.");
        }

        var normalized = pattern.Trim().ToUpperInvariant();
        return new ImportPattern(normalized);
    }

    /// <summary>
    /// Checks if the given transaction description matches this pattern.
    /// Matching is case-insensitive and supports wildcards.
    /// </summary>
    /// <param name="description">The transaction description to match against.</param>
    /// <returns>True if the description matches the pattern; otherwise, false.</returns>
    public bool Matches(string description)
    {
        if (string.IsNullOrEmpty(description))
        {
            return false;
        }

        var normalizedDescription = description.ToUpperInvariant();
        var patternText = this.Pattern;

        var hasWildcardPrefix = patternText.StartsWith('*');
        var hasWildcardSuffix = patternText.EndsWith('*');

        // Remove wildcards for comparison
        var searchText = patternText;
        if (hasWildcardPrefix)
        {
            searchText = searchText[1..];
        }

        if (hasWildcardSuffix)
        {
            searchText = searchText[..^1];
        }

        // Exact match (no wildcards)
        if (!hasWildcardPrefix && !hasWildcardSuffix)
        {
            return string.Equals(normalizedDescription, searchText, StringComparison.Ordinal);
        }

        // Both prefix and suffix wildcards - contains check
        if (hasWildcardPrefix && hasWildcardSuffix)
        {
            return normalizedDescription.Contains(searchText, StringComparison.Ordinal);
        }

        // Only prefix wildcard - description must end with pattern
        if (hasWildcardPrefix)
        {
            return normalizedDescription.EndsWith(searchText, StringComparison.Ordinal);
        }

        // Only suffix wildcard - description must start with pattern
        return normalizedDescription.StartsWith(searchText, StringComparison.Ordinal);
    }

    /// <inheritdoc />
    public override string ToString() => this.Pattern;
}
