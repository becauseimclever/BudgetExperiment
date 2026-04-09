// <copyright file="ICalendarService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Application.Calendar;

/// <summary>
/// Provides calendar-related use cases exposed to the API layer.
/// </summary>
public interface ICalendarService
{
    /// <summary>
    /// Gets daily transaction totals for a given month.
    /// </summary>
    /// <param name="year">The year.</param>
    /// <param name="month">The month (1-12).</param>
    /// <param name="accountId">Optional account filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A read-only list of daily total DTOs for days that have transactions.</returns>
    Task<IReadOnlyList<DailyTotalDto>> GetMonthlySummaryAsync(
        int year,
        int month,
        Guid? accountId = null,
        CancellationToken cancellationToken = default);
}
