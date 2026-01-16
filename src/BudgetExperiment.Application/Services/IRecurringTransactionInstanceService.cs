// <copyright file="IRecurringTransactionInstanceService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Application.Services;

/// <summary>
/// Service interface for recurring transaction instance operations.
/// </summary>
public interface IRecurringTransactionInstanceService
{
    /// <summary>
    /// Gets projected instances for a recurring transaction within a date range.
    /// </summary>
    /// <param name="id">The recurring transaction identifier.</param>
    /// <param name="fromDate">Start date (inclusive).</param>
    /// <param name="toDate">End date (inclusive).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of instance DTOs, or null if recurring transaction not found.</returns>
    Task<IReadOnlyList<RecurringInstanceDto>?> GetInstancesAsync(
        Guid id,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Modifies a single instance of a recurring transaction.
    /// </summary>
    /// <param name="id">The recurring transaction identifier.</param>
    /// <param name="instanceDate">The original scheduled date of the instance.</param>
    /// <param name="dto">The modification data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The modified instance DTO, or null if not found.</returns>
    Task<RecurringInstanceDto?> ModifyInstanceAsync(
        Guid id,
        DateOnly instanceDate,
        RecurringInstanceModifyDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Skips (deletes) a single instance of a recurring transaction.
    /// </summary>
    /// <param name="id">The recurring transaction identifier.</param>
    /// <param name="instanceDate">The original scheduled date of the instance.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if skipped, false if recurring transaction not found.</returns>
    Task<bool> SkipInstanceAsync(
        Guid id,
        DateOnly instanceDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all projected recurring transaction instances across all active recurring transactions.
    /// </summary>
    /// <param name="fromDate">Start date (inclusive).</param>
    /// <param name="toDate">End date (inclusive).</param>
    /// <param name="accountId">Optional account filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of instance DTOs.</returns>
    Task<IReadOnlyList<RecurringInstanceDto>> GetProjectedInstancesAsync(
        DateOnly fromDate,
        DateOnly toDate,
        Guid? accountId = null,
        CancellationToken cancellationToken = default);
}
