// <copyright file="IRecurringTransactionService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Application.Recurring;

/// <summary>
/// Application service interface for recurring transaction use cases.
/// </summary>
public interface IRecurringTransactionService
{
    /// <summary>
    /// Gets a recurring transaction by its identifier.
    /// </summary>
    /// <param name="id">The recurring transaction identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The recurring transaction DTO, or null if not found.</returns>
    Task<RecurringTransactionDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all recurring transactions.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of recurring transaction DTOs.</returns>
    Task<IReadOnlyList<RecurringTransactionDto>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets recurring transactions for a specific account.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of recurring transaction DTOs.</returns>
    Task<IReadOnlyList<RecurringTransactionDto>> GetByAccountIdAsync(Guid accountId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new recurring transaction.
    /// </summary>
    /// <param name="dto">The creation data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created recurring transaction DTO.</returns>
    Task<RecurringTransactionDto> CreateAsync(RecurringTransactionCreateDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a recurring transaction.
    /// </summary>
    /// <param name="id">The recurring transaction identifier.</param>
    /// <param name="dto">The update data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated recurring transaction DTO, or null if not found.</returns>
    Task<RecurringTransactionDto?> UpdateAsync(Guid id, RecurringTransactionUpdateDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a recurring transaction.
    /// </summary>
    /// <param name="id">The recurring transaction identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if deleted, false if not found.</returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pauses a recurring transaction.
    /// </summary>
    /// <param name="id">The recurring transaction identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated recurring transaction DTO, or null if not found.</returns>
    Task<RecurringTransactionDto?> PauseAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resumes a paused recurring transaction.
    /// </summary>
    /// <param name="id">The recurring transaction identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated recurring transaction DTO, or null if not found.</returns>
    Task<RecurringTransactionDto?> ResumeAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets import patterns for a recurring transaction.
    /// </summary>
    /// <param name="id">The recurring transaction identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The import patterns DTO, or null if not found.</returns>
    Task<ImportPatternsDto?> GetImportPatternsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates import patterns for a recurring transaction.
    /// </summary>
    /// <param name="id">The recurring transaction identifier.</param>
    /// <param name="dto">The import patterns to set.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated import patterns DTO, or null if not found.</returns>
    Task<ImportPatternsDto?> UpdateImportPatternsAsync(Guid id, ImportPatternsDto dto, CancellationToken cancellationToken = default);
}
