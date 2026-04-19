// <copyright file="IKaizenDashboardService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Application.Reports;

/// <summary>
/// Application service for the 12-week rolling Kaizen Dashboard report.
/// </summary>
public interface IKaizenDashboardService
{
    /// <summary>
    /// Builds the Kaizen Dashboard for the given user over the specified number of rolling weeks.
    /// Results are cached for one hour per (userId, weeks) pair.
    /// </summary>
    /// <param name="userId">The authenticated user's identifier.</param>
    /// <param name="weeks">Number of ISO weeks to include (default 12, min 1, max 52).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A <see cref="KaizenDashboardDto"/> containing one entry per week.</returns>
    Task<KaizenDashboardDto> GetDashboardAsync(Guid userId, int weeks = 12, CancellationToken ct = default);
}
