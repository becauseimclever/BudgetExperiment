// <copyright file="IRecurringTransferService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Application.Services;

/// <summary>
/// Application service interface for recurring transfer use cases.
/// </summary>
public interface IRecurringTransferService
{
    /// <summary>
    /// Gets a recurring transfer by its identifier.
    /// </summary>
    /// <param name="id">The recurring transfer identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The recurring transfer DTO, or null if not found.</returns>
    Task<RecurringTransferDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all recurring transfers.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of recurring transfer DTOs.</returns>
    Task<IReadOnlyList<RecurringTransferDto>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets recurring transfers for a specific account.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of recurring transfer DTOs.</returns>
    Task<IReadOnlyList<RecurringTransferDto>> GetByAccountIdAsync(Guid accountId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active recurring transfers.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of active recurring transfer DTOs.</returns>
    Task<IReadOnlyList<RecurringTransferDto>> GetActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new recurring transfer.
    /// </summary>
    /// <param name="dto">The creation data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created recurring transfer DTO.</returns>
    Task<RecurringTransferDto> CreateAsync(RecurringTransferCreateDto dto, CancellationToken cancellationToken = default);
}
