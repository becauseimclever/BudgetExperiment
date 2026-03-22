// <copyright file="ReconciliationStatusDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// DTO representing the reconciliation status for a period.
/// </summary>
public sealed record ReconciliationStatusDto
{
    /// <summary>
    /// Gets the year for this status report.
    /// </summary>
    public int Year
    {
        get; init;
    }

    /// <summary>
    /// Gets the month for this status report.
    /// </summary>
    public int Month
    {
        get; init;
    }

    /// <summary>
    /// Gets the total number of recurring instances expected in the period.
    /// </summary>
    public int TotalExpectedInstances
    {
        get; init;
    }

    /// <summary>
    /// Gets the number of matched instances (Accepted or AutoMatched).
    /// </summary>
    public int MatchedCount
    {
        get; init;
    }

    /// <summary>
    /// Gets the number of pending matches awaiting review.
    /// </summary>
    public int PendingCount
    {
        get; init;
    }

    /// <summary>
    /// Gets the number of missing instances (no match found).
    /// </summary>
    public int MissingCount
    {
        get; init;
    }

    /// <summary>
    /// Gets the list of recurring instance statuses.
    /// </summary>
    public IReadOnlyList<RecurringInstanceStatusDto> Instances { get; init; } = [];
}
