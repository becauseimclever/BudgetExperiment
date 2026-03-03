// <copyright file="ILocationReportBuilder.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Application.Reports;

/// <summary>
/// Builds location-based spending reports.
/// </summary>
public interface ILocationReportBuilder
{
    /// <summary>
    /// Gets spending broken down by geographic location.
    /// </summary>
    /// <param name="startDate">Start date for the report period.</param>
    /// <param name="endDate">End date for the report period.</param>
    /// <param name="accountId">Optional account filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The location spending report.</returns>
    Task<LocationSpendingReportDto> GetSpendingByLocationAsync(
        DateOnly startDate,
        DateOnly endDate,
        Guid? accountId = null,
        CancellationToken cancellationToken = default);
}
