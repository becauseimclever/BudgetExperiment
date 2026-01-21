// <copyright file="IRecurringTransactionRepository.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Repositories;

/// <summary>
/// Repository interface for RecurringTransaction aggregate root.
/// </summary>
public interface IRecurringTransactionRepository : IReadRepository<RecurringTransaction>, IWriteRepository<RecurringTransaction>
{
    /// <summary>
    /// Gets all recurring transactions for a specific account.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All recurring transactions for the account.</returns>
    Task<IReadOnlyList<RecurringTransaction>> GetByAccountIdAsync(Guid accountId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active recurring transactions.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All active recurring transactions.</returns>
    Task<IReadOnlyList<RecurringTransaction>> GetActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a recurring transaction by ID including its exceptions.
    /// </summary>
    /// <param name="id">The recurring transaction identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The recurring transaction with exceptions or null.</returns>
    Task<RecurringTransaction?> GetByIdWithExceptionsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all recurring transactions (for listing).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All recurring transactions.</returns>
    Task<IReadOnlyList<RecurringTransaction>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets exceptions for a recurring transaction within a date range.
    /// </summary>
    /// <param name="recurringTransactionId">The recurring transaction identifier.</param>
    /// <param name="fromDate">Start date (inclusive).</param>
    /// <param name="toDate">End date (inclusive).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Exceptions in the date range.</returns>
    Task<IReadOnlyList<RecurringTransactionException>> GetExceptionsByDateRangeAsync(
        Guid recurringTransactionId,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an exception by recurring transaction ID and original date.
    /// </summary>
    /// <param name="recurringTransactionId">The recurring transaction identifier.</param>
    /// <param name="originalDate">The original scheduled date.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The exception or null.</returns>
    Task<RecurringTransactionException?> GetExceptionAsync(
        Guid recurringTransactionId,
        DateOnly originalDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds an exception to the store.
    /// </summary>
    /// <param name="exception">The exception to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task AddExceptionAsync(RecurringTransactionException exception, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes an exception from the store.
    /// </summary>
    /// <param name="exception">The exception to remove.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RemoveExceptionAsync(RecurringTransactionException exception, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes all exceptions for a recurring transaction on or after a specific date.
    /// Used when editing "this and future" occurrences.
    /// </summary>
    /// <param name="recurringTransactionId">The recurring transaction identifier.</param>
    /// <param name="fromDate">The date from which to remove exceptions (inclusive).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RemoveExceptionsFromDateAsync(
        Guid recurringTransactionId,
        DateOnly fromDate,
        CancellationToken cancellationToken = default);
}
