// <copyright file="ICalendarGridService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Application.Services;

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

    /// <summary>
    /// Gets detailed information for a specific day.
    /// </summary>
    /// <param name="date">The date to get details for.</param>
    /// <param name="accountId">Optional account filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The day detail DTO.</returns>
    Task<DayDetailDto> GetDayDetailAsync(
        DateOnly date,
        Guid? accountId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a pre-merged transaction list for an account over a date range.
    /// </summary>
    /// <param name="accountId">The account ID.</param>
    /// <param name="startDate">The start date of the range.</param>
    /// <param name="endDate">The end date of the range.</param>
    /// <param name="includeRecurring">Whether to include recurring transaction instances.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The transaction list DTO with pre-computed summaries.</returns>
    Task<TransactionListDto> GetAccountTransactionListAsync(
        Guid accountId,
        DateOnly startDate,
        DateOnly endDate,
        bool includeRecurring = true,
        CancellationToken cancellationToken = default);
}
