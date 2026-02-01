// <copyright file="ReconciliationDtos.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// DTO representing a reconciliation match between an imported transaction and a recurring instance.
/// </summary>
public sealed record ReconciliationMatchDto
{
    /// <summary>
    /// Gets the match identifier.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Gets the imported transaction identifier.
    /// </summary>
    public Guid ImportedTransactionId { get; init; }

    /// <summary>
    /// Gets the recurring transaction identifier.
    /// </summary>
    public Guid RecurringTransactionId { get; init; }

    /// <summary>
    /// Gets the recurring instance date.
    /// </summary>
    public DateOnly RecurringInstanceDate { get; init; }

    /// <summary>
    /// Gets the confidence score (0.0 to 1.0).
    /// </summary>
    public decimal ConfidenceScore { get; init; }

    /// <summary>
    /// Gets the confidence level (High, Medium, Low).
    /// </summary>
    public string ConfidenceLevel { get; init; } = string.Empty;

    /// <summary>
    /// Gets the match status (Suggested, Accepted, Rejected, AutoMatched).
    /// </summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>
    /// Gets the match source (Auto, Manual).
    /// </summary>
    public string Source { get; init; } = "Auto";

    /// <summary>
    /// Gets the variance between expected and actual amount.
    /// </summary>
    public decimal AmountVariance { get; init; }

    /// <summary>
    /// Gets the offset in days from scheduled date.
    /// </summary>
    public int DateOffsetDays { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the match was created.
    /// </summary>
    public DateTime CreatedAtUtc { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the match was resolved.
    /// </summary>
    public DateTime? ResolvedAtUtc { get; init; }

    /// <summary>
    /// Gets the imported transaction details (optional, populated when needed).
    /// </summary>
    public TransactionDto? ImportedTransaction { get; init; }

    /// <summary>
    /// Gets the recurring transaction description (optional, for display).
    /// </summary>
    public string? RecurringTransactionDescription { get; init; }

    /// <summary>
    /// Gets the expected amount from the recurring transaction.
    /// </summary>
    public MoneyDto? ExpectedAmount { get; init; }
}

/// <summary>
/// DTO for matching tolerances configuration.
/// </summary>
public sealed record MatchingTolerancesDto
{
    /// <summary>
    /// Gets or sets the maximum days before/after scheduled date to consider a match.
    /// </summary>
    public int DateToleranceDays { get; init; } = 7;

    /// <summary>
    /// Gets or sets the maximum percentage variance in amount (0.0 to 1.0).
    /// </summary>
    public decimal AmountTolerancePercent { get; init; } = 0.10m;

    /// <summary>
    /// Gets or sets the maximum absolute amount variance.
    /// </summary>
    public decimal AmountToleranceAbsolute { get; init; } = 10.00m;

    /// <summary>
    /// Gets or sets the minimum description similarity threshold (0.0 to 1.0).
    /// </summary>
    public decimal DescriptionSimilarityThreshold { get; init; } = 0.6m;

    /// <summary>
    /// Gets or sets the minimum confidence for auto-matching (0.0 to 1.0).
    /// </summary>
    public decimal AutoMatchThreshold { get; init; } = 0.85m;
}

/// <summary>
/// DTO representing the reconciliation status for a period.
/// </summary>
public sealed record ReconciliationStatusDto
{
    /// <summary>
    /// Gets the year for this status report.
    /// </summary>
    public int Year { get; init; }

    /// <summary>
    /// Gets the month for this status report.
    /// </summary>
    public int Month { get; init; }

    /// <summary>
    /// Gets the total number of recurring instances expected in the period.
    /// </summary>
    public int TotalExpectedInstances { get; init; }

    /// <summary>
    /// Gets the number of matched instances (Accepted or AutoMatched).
    /// </summary>
    public int MatchedCount { get; init; }

    /// <summary>
    /// Gets the number of pending matches awaiting review.
    /// </summary>
    public int PendingCount { get; init; }

    /// <summary>
    /// Gets the number of missing instances (no match found).
    /// </summary>
    public int MissingCount { get; init; }

    /// <summary>
    /// Gets the list of recurring instance statuses.
    /// </summary>
    public IReadOnlyList<RecurringInstanceStatusDto> Instances { get; init; } = [];
}

/// <summary>
/// DTO representing the status of a single recurring instance.
/// </summary>
public sealed record RecurringInstanceStatusDto
{
    /// <summary>
    /// Gets the recurring transaction identifier.
    /// </summary>
    public Guid RecurringTransactionId { get; init; }

    /// <summary>
    /// Gets the recurring transaction description.
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Gets the account identifier.
    /// </summary>
    public Guid AccountId { get; init; }

    /// <summary>
    /// Gets the account name.
    /// </summary>
    public string AccountName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the instance date.
    /// </summary>
    public DateOnly InstanceDate { get; init; }

    /// <summary>
    /// Gets the expected amount.
    /// </summary>
    public MoneyDto ExpectedAmount { get; init; } = null!;

    /// <summary>
    /// Gets the reconciliation status.
    /// </summary>
    public string Status { get; init; } = "Missing";

    /// <summary>
    /// Gets the matched transaction ID (if matched).
    /// </summary>
    public Guid? MatchedTransactionId { get; init; }

    /// <summary>
    /// Gets the actual amount (if matched).
    /// </summary>
    public MoneyDto? ActualAmount { get; init; }

    /// <summary>
    /// Gets the amount variance (if matched).
    /// </summary>
    public decimal? AmountVariance { get; init; }

    /// <summary>
    /// Gets the match ID (if a match suggestion exists).
    /// </summary>
    public Guid? MatchId { get; init; }

    /// <summary>
    /// Gets the match source (Auto or Manual) if matched.
    /// </summary>
    public string? MatchSource { get; init; }
}

/// <summary>
/// Request to manually match a transaction to a recurring instance.
/// </summary>
public sealed record ManualMatchRequest
{
    /// <summary>
    /// Gets or sets the transaction ID to match.
    /// </summary>
    public Guid TransactionId { get; init; }

    /// <summary>
    /// Gets or sets the recurring transaction ID.
    /// </summary>
    public Guid RecurringTransactionId { get; init; }

    /// <summary>
    /// Gets or sets the instance date to match.
    /// </summary>
    public DateOnly InstanceDate { get; init; }
}

/// <summary>
/// Request to find matches for transactions.
/// </summary>
public sealed record FindMatchesRequest
{
    /// <summary>
    /// Gets or sets the transaction IDs to find matches for.
    /// </summary>
    public IReadOnlyList<Guid> TransactionIds { get; init; } = [];

    /// <summary>
    /// Gets or sets the date range start for recurring instances to consider.
    /// </summary>
    public DateOnly StartDate { get; init; }

    /// <summary>
    /// Gets or sets the date range end for recurring instances to consider.
    /// </summary>
    public DateOnly EndDate { get; init; }

    /// <summary>
    /// Gets or sets custom tolerances (optional, uses defaults if null).
    /// </summary>
    public MatchingTolerancesDto? Tolerances { get; init; }
}

/// <summary>
/// Result of finding matches for transactions.
/// </summary>
public sealed record FindMatchesResult
{
    /// <summary>
    /// Gets the matches found, grouped by transaction ID.
    /// </summary>
    public IReadOnlyDictionary<Guid, IReadOnlyList<ReconciliationMatchDto>> MatchesByTransaction { get; init; }
        = new Dictionary<Guid, IReadOnlyList<ReconciliationMatchDto>>();

    /// <summary>
    /// Gets the total number of matches found.
    /// </summary>
    public int TotalMatchesFound { get; init; }

    /// <summary>
    /// Gets the number of high confidence matches.
    /// </summary>
    public int HighConfidenceCount { get; init; }
}

/// <summary>
/// Request to bulk accept or reject matches.
/// </summary>
public sealed record BulkMatchActionRequest
{
    /// <summary>
    /// Gets or sets the match IDs to process.
    /// </summary>
    public IReadOnlyList<Guid> MatchIds { get; init; } = [];
}

/// <summary>
/// Result of a bulk match action.
/// </summary>
 public sealed record BulkMatchActionResult
{
    /// <summary>
    /// Gets the number of matches successfully accepted.
    /// </summary>
    public int AcceptedCount { get; init; }

    /// <summary>
    /// Gets the number of matches that failed.
    /// </summary>
    public int FailedCount { get; init; }
}

/// <summary>
/// DTO representing a recurring instance that can be linked to a transaction.
/// </summary>
public sealed record LinkableInstanceDto
{
    /// <summary>
    /// Gets the recurring transaction identifier.
    /// </summary>
    public Guid RecurringTransactionId { get; init; }

    /// <summary>
    /// Gets the recurring transaction description.
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Gets the expected amount for this instance.
    /// </summary>
    public MoneyDto ExpectedAmount { get; init; } = default!;

    /// <summary>
    /// Gets the scheduled date for this instance.
    /// </summary>
    public DateOnly InstanceDate { get; init; }

    /// <summary>
    /// Gets a value indicating whether this instance is already matched to another transaction.
    /// </summary>
    public bool IsAlreadyMatched { get; init; }

    /// <summary>
    /// Gets the confidence score if auto-matched to this transaction (0.0 to 1.0).
    /// </summary>
    public decimal? SuggestedConfidence { get; init; }
}
