// <copyright file="IBudgetProgressService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Application.Budgeting;

/// <summary>
/// Service interface for budget progress tracking operations.
/// </summary>
public interface IBudgetProgressService
{
    /// <summary>
    /// Gets the budget progress for a specific category and month.
    /// </summary>
    /// <param name="categoryId">The category identifier.</param>
    /// <param name="year">The year.</param>
    /// <param name="month">The month (1-12).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The progress DTO, or null if no goal is set.</returns>
    Task<BudgetProgressDto?> GetProgressAsync(Guid categoryId, int year, int month, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the budget summary for a specific month, including progress for all categories.
    /// </summary>
    /// <param name="year">The year.</param>
    /// <param name="month">The month (1-12).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The monthly budget summary DTO.</returns>
    Task<BudgetSummaryDto> GetMonthlySummaryAsync(int year, int month, CancellationToken cancellationToken = default);
}
