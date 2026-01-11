// <copyright file="CalendarService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Application.Mapping;
using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.Services;

/// <summary>
/// Application service for calendar-related use cases.
/// </summary>
public sealed class CalendarService
{
    private readonly ITransactionRepository _repository;

    /// <summary>
    /// Initializes a new instance of the <see cref="CalendarService"/> class.
    /// </summary>
    /// <param name="repository">The transaction repository.</param>
    public CalendarService(ITransactionRepository repository)
    {
        this._repository = repository;
    }

    /// <summary>
    /// Gets daily totals for a given month (calendar summary).
    /// </summary>
    /// <param name="year">The year.</param>
    /// <param name="month">The month (1-12).</param>
    /// <param name="accountId">Optional account filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of daily totals for days with transactions.</returns>
    public async Task<IReadOnlyList<DailyTotalDto>> GetMonthlySummaryAsync(
        int year,
        int month,
        Guid? accountId = null,
        CancellationToken cancellationToken = default)
    {
        var dailyTotals = await this._repository.GetDailyTotalsAsync(year, month, accountId, cancellationToken);
        return dailyTotals.Select(DomainToDtoMapper.ToDto).ToList();
    }
}
