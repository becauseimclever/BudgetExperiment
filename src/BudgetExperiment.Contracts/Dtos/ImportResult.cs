// <copyright file="ImportResult.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// Result of an import execution.
/// </summary>
public sealed record ImportResult
{
    /// <summary>
    /// Gets the created import batch ID.
    /// </summary>
    public Guid BatchId { get; init; }

    /// <summary>
    /// Gets the count of successfully imported transactions.
    /// </summary>
    public int ImportedCount { get; init; }

    /// <summary>
    /// Gets the count of skipped transactions.
    /// </summary>
    public int SkippedCount { get; init; }

    /// <summary>
    /// Gets the count of failed transactions.
    /// </summary>
    public int ErrorCount { get; init; }

    /// <summary>
    /// Gets the IDs of created transactions.
    /// </summary>
    public IReadOnlyList<Guid> CreatedTransactionIds { get; init; } = [];

    /// <summary>
    /// Gets the count of transactions auto-categorized by rules.
    /// </summary>
    public int AutoCategorizedCount { get; init; }

    /// <summary>
    /// Gets the count of transactions categorized from CSV column.
    /// </summary>
    public int CsvCategorizedCount { get; init; }

    /// <summary>
    /// Gets the count of uncategorized transactions.
    /// </summary>
    public int UncategorizedCount { get; init; }

    /// <summary>
    /// Gets the count of transactions matched to recurring transactions.
    /// </summary>
    public int ReconciliationMatchCount { get; init; }

    /// <summary>
    /// Gets the count of high-confidence auto-matched transactions.
    /// </summary>
    public int AutoMatchedCount { get; init; }

    /// <summary>
    /// Gets the count of transactions with pending match suggestions for review.
    /// </summary>
    public int PendingMatchCount { get; init; }

    /// <summary>
    /// Gets the reconciliation match suggestions (if reconciliation was performed).
    /// </summary>
    public IReadOnlyList<ReconciliationMatchDto> MatchSuggestions { get; init; } = [];

    /// <summary>
    /// Gets the count of transactions enriched with location data.
    /// </summary>
    public int LocationEnrichedCount { get; init; }
}
