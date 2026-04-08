// <copyright file="ITrendReportBuilder.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Application.Reports;

/// <summary>
/// Builds spending trend reports across multiple months.
/// </summary>
public interface ITrendReportBuilder
{
    /// <summary>
    /// Gets spending trends over a specified number of months.
    /// </summary>
    /// <param name="months">Number of months to include (1-24).</param>
    /// <param name="endYear">End year (defaults to current year).</param>
    /// <param name="endMonth">End month (defaults to current month).</param>
    /// <param name="categoryId">Optional category filter.</param>
    /// <param name="groupByKakeibo">Whether to include Kakeibo grouped summary.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The spending trends report.</returns>
    Task<SpendingTrendsReportDto> GetSpendingTrendsAsync(
        int months = 6,
        int? endYear = null,
        int? endMonth = null,
        Guid? categoryId = null,
        bool groupByKakeibo = false,
        CancellationToken cancellationToken = default);
}
