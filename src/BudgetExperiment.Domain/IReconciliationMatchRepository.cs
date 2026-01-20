// <copyright file="IReconciliationMatchRepository.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain;

/// <summary>
/// Repository interface for ReconciliationMatch entity.
/// </summary>
public interface IReconciliationMatchRepository : IReadRepository<ReconciliationMatch>, IWriteRepository<ReconciliationMatch>
{
    /// <summary>
    /// Gets all pending (suggested) matches for the current user.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of pending matches.</returns>
    Task<IReadOnlyList<ReconciliationMatch>> GetPendingMatchesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets matches for a specific recurring transaction within a date range.
    /// </summary>
    /// <param name="recurringTransactionId">The recurring transaction identifier.</param>
    /// <param name="startDate">Start date (inclusive).</param>
    /// <param name="endDate">End date (inclusive).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of matches for the recurring transaction.</returns>
    Task<IReadOnlyList<ReconciliationMatch>> GetByRecurringTransactionAsync(
        Guid recurringTransactionId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets matches for a specific imported transaction.
    /// </summary>
    /// <param name="transactionId">The imported transaction identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of matches for the transaction.</returns>
    Task<IReadOnlyList<ReconciliationMatch>> GetByTransactionIdAsync(
        Guid transactionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the reconciliation status for recurring instances within a period.
    /// </summary>
    /// <param name="year">The year.</param>
    /// <param name="month">The month (1-12).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of matches for the period.</returns>
    Task<IReadOnlyList<ReconciliationMatch>> GetByPeriodAsync(
        int year,
        int month,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a match already exists for a transaction and recurring instance combination.
    /// </summary>
    /// <param name="transactionId">The transaction identifier.</param>
    /// <param name="recurringTransactionId">The recurring transaction identifier.</param>
    /// <param name="instanceDate">The instance date.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if a match exists, false otherwise.</returns>
    Task<bool> ExistsAsync(
        Guid transactionId,
        Guid recurringTransactionId,
        DateOnly instanceDate,
        CancellationToken cancellationToken = default);
}
