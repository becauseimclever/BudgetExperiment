// <copyright file="IAccountService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Application.Accounts;

/// <summary>
/// Provides account use-case operations exposed to the API layer.
/// </summary>
public interface IAccountService
{
    /// <summary>
    /// Gets all accounts.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A read-only list of account DTOs.</returns>
    Task<IReadOnlyList<AccountDto>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an account by its identifier.
    /// </summary>
    /// <param name="id">The account identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The account DTO, or <see langword="null"/> if not found.</returns>
    Task<AccountDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new account.
    /// </summary>
    /// <param name="dto">The account creation data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created account DTO.</returns>
    Task<AccountDto> CreateAsync(AccountCreateDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing account.
    /// </summary>
    /// <param name="id">The account identifier.</param>
    /// <param name="dto">The account update data.</param>
    /// <param name="expectedVersion">The expected concurrency token for optimistic concurrency, or <see langword="null"/> to skip.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated account DTO, or <see langword="null"/> if not found.</returns>
    Task<AccountDto?> UpdateAsync(Guid id, AccountUpdateDto dto, string? expectedVersion = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes an account by its identifier.
    /// </summary>
    /// <param name="id">The account identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see langword="true"/> if removed; <see langword="false"/> if not found.</returns>
    Task<bool> RemoveAsync(Guid id, CancellationToken cancellationToken = default);
}
