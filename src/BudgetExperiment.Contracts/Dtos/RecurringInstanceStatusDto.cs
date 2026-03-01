// <copyright file="RecurringInstanceStatusDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

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
