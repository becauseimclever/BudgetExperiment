// <copyright file="CategorizationResult.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Application.Services;

/// <summary>
/// Result of a bulk categorization operation.
/// </summary>
public sealed record CategorizationResult
{
    /// <summary>
    /// Gets the total number of transactions processed.
    /// </summary>
    public int TotalProcessed { get; init; }

    /// <summary>
    /// Gets the number of transactions that were successfully categorized.
    /// </summary>
    public int Categorized { get; init; }

    /// <summary>
    /// Gets the number of transactions that were skipped (already categorized or no matching rule).
    /// </summary>
    public int Skipped { get; init; }

    /// <summary>
    /// Gets the number of errors that occurred during categorization.
    /// </summary>
    public int Errors { get; init; }

    /// <summary>
    /// Gets the error messages, if any.
    /// </summary>
    public IReadOnlyList<string> ErrorMessages { get; init; } = Array.Empty<string>();
}
