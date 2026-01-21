// <copyright file="IRecurringTransferRepository.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Repositories;

/// <summary>
/// Repository interface for RecurringTransfer aggregate root.
/// </summary>
public interface IRecurringTransferRepository : IReadRepository<RecurringTransfer>, IWriteRepository<RecurringTransfer>
{
    /// <summary>
    /// Gets all recurring transfers for a specific account (as either source or destination).
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All recurring transfers involving the account.</returns>
    Task<IReadOnlyList<RecurringTransfer>> GetByAccountIdAsync(Guid accountId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all recurring transfers where the specified account is the source.
    /// </summary>
    /// <param name="sourceAccountId">The source account identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All recurring transfers from the source account.</returns>
    Task<IReadOnlyList<RecurringTransfer>> GetBySourceAccountIdAsync(Guid sourceAccountId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all recurring transfers where the specified account is the destination.
    /// </summary>
    /// <param name="destinationAccountId">The destination account identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All recurring transfers to the destination account.</returns>
    Task<IReadOnlyList<RecurringTransfer>> GetByDestinationAccountIdAsync(Guid destinationAccountId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active recurring transfers.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All active recurring transfers.</returns>
    Task<IReadOnlyList<RecurringTransfer>> GetActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a recurring transfer by ID including its exceptions.
    /// </summary>
    /// <param name="id">The recurring transfer identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The recurring transfer with exceptions or null.</returns>
    Task<RecurringTransfer?> GetByIdWithExceptionsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all recurring transfers (for listing).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All recurring transfers.</returns>
    Task<IReadOnlyList<RecurringTransfer>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets exceptions for a recurring transfer within a date range.
    /// </summary>
    /// <param name="recurringTransferId">The recurring transfer identifier.</param>
    /// <param name="fromDate">Start date (inclusive).</param>
    /// <param name="toDate">End date (inclusive).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Exceptions in the date range.</returns>
    Task<IReadOnlyList<RecurringTransferException>> GetExceptionsByDateRangeAsync(
        Guid recurringTransferId,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an exception by recurring transfer ID and original date.
    /// </summary>
    /// <param name="recurringTransferId">The recurring transfer identifier.</param>
    /// <param name="originalDate">The original scheduled date.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The exception or null.</returns>
    Task<RecurringTransferException?> GetExceptionAsync(
        Guid recurringTransferId,
        DateOnly originalDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds an exception to the store.
    /// </summary>
    /// <param name="exception">The exception to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task AddExceptionAsync(RecurringTransferException exception, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes an exception from the store.
    /// </summary>
    /// <param name="exception">The exception to remove.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RemoveExceptionAsync(RecurringTransferException exception, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes all exceptions for a recurring transfer on or after a specific date.
    /// Used when editing "this and future" occurrences.
    /// </summary>
    /// <param name="recurringTransferId">The recurring transfer identifier.</param>
    /// <param name="fromDate">The date from which to remove exceptions (inclusive).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RemoveExceptionsFromDateAsync(
        Guid recurringTransferId,
        DateOnly fromDate,
        CancellationToken cancellationToken = default);
}
