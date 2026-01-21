// <copyright file="ICalendarGridService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Application.Calendar;

/// <summary>
/// Service interface for building complete calendar grid views.
/// </summary>
public interface ICalendarGridService
{
    /// <summary>
    /// Gets a complete calendar grid with all data pre-computed.
    /// </summary>
    /// <param name="year">The year.</param>
    /// <param name="month">The month (1-12).</param>
    /// <param name="accountId">Optional account filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The complete calendar grid DTO.</returns>
    Task<CalendarGridDto> GetCalendarGridAsync(
        int year,
        int month,
        Guid? accountId = null,
        CancellationToken cancellationToken = default);
}
