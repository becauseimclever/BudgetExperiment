// <copyright file="IRecurringTransferInstanceService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Application.Services;

/// <summary>
/// Service interface for recurring transfer instance operations.
/// </summary>
public interface IRecurringTransferInstanceService
{
    /// <summary>
    /// Gets projected instances for a recurring transfer within a date range.
    /// </summary>
    /// <param name="id">The recurring transfer identifier.</param>
    /// <param name="fromDate">Start date (inclusive).</param>
    /// <param name="toDate">End date (inclusive).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of instance DTOs, or null if recurring transfer not found.</returns>
    Task<IReadOnlyList<RecurringTransferInstanceDto>?> GetInstancesAsync(
        Guid id,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Modifies a single instance of a recurring transfer.
    /// </summary>
    /// <param name="id">The recurring transfer identifier.</param>
    /// <param name="instanceDate">The original scheduled date of the instance.</param>
    /// <param name="dto">The modification data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The modified instance DTO, or null if not found.</returns>
    Task<RecurringTransferInstanceDto?> ModifyInstanceAsync(
        Guid id,
        DateOnly instanceDate,
        RecurringTransferInstanceModifyDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Skips (deletes) a single instance of a recurring transfer.
    /// </summary>
    /// <param name="id">The recurring transfer identifier.</param>
    /// <param name="instanceDate">The original scheduled date of the instance.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if skipped, false if recurring transfer not found.</returns>
    Task<bool> SkipInstanceAsync(
        Guid id,
        DateOnly instanceDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all projected recurring transfer instances across all active recurring transfers.
    /// </summary>
    /// <param name="fromDate">Start date (inclusive).</param>
    /// <param name="toDate">End date (inclusive).</param>
    /// <param name="accountId">Optional account filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of instance DTOs.</returns>
    Task<IReadOnlyList<RecurringTransferInstanceDto>> GetProjectedInstancesAsync(
        DateOnly fromDate,
        DateOnly toDate,
        Guid? accountId = null,
        CancellationToken cancellationToken = default);
}
