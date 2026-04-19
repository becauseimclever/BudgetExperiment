// <copyright file="IKakeiboReportService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Application.Reports;

/// <summary>
/// Service interface for generating Kakeibo bucket summaries over arbitrary date ranges.
/// </summary>
public interface IKakeiboReportService
{
    /// <summary>
    /// Gets the Kakeibo bucket summary for a date range.
    /// </summary>
    /// <param name="from">The start date (inclusive).</param>
    /// <param name="to">The end date (inclusive).</param>
    /// <param name="accountId">Optional account filter; null returns all accounts.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A <see cref="KakeiboSummary"/> with daily, weekly, and total bucket aggregations.
    /// All four Kakeibo buckets are always present, including zero-spend buckets.
    /// </returns>
    Task<KakeiboSummary> GetKakeiboSummaryAsync(
        DateOnly from,
        DateOnly to,
        Guid? accountId,
        CancellationToken cancellationToken = default);
}
