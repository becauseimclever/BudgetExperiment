// <copyright file="ITransactionAnalyticsRepository.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Repositories;

/// <summary>
/// Focused repository interface for transaction analytics, health analysis, and reconciliation queries.
/// </summary>
public interface ITransactionAnalyticsRepository
{
    /// <summary>
    /// Gets the total spending for a category in a specific month.
    /// </summary>
    /// <param name="categoryId">The budget category identifier.</param>
    /// <param name="year">The year.</param>
    /// <param name="month">The month (1-12).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The total spending amount for the category.</returns>
    Task<MoneyValue> GetSpendingByCategoryAsync(
        Guid categoryId,
        int year,
        int month,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all transactions for health analysis, optionally filtered by account.
    /// Applies scope filtering. Includes Category navigation.
    /// </summary>
    /// <param name="accountId">Optional account filter.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>All transactions for health analysis.</returns>
    Task<IReadOnlyList<Transaction>> GetAllForHealthAnalysisAsync(
        Guid? accountId,
        CancellationToken ct);

    /// <summary>
    /// Gets cleared transactions for an account up to the given date.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="upToDate">Optional upper bound (inclusive); returns all cleared if null.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Cleared transactions for the account.</returns>
    Task<IReadOnlyList<Transaction>> GetClearedByAccountAsync(
        Guid accountId,
        DateOnly? upToDate,
        CancellationToken ct);

    /// <summary>
    /// Computes the sum of cleared transaction amounts for an account up to the given date.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="upToDate">Optional upper bound (inclusive); sums all cleared if null.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Sum of cleared transaction amounts.</returns>
    Task<MoneyValue> GetClearedBalanceSumAsync(
        Guid accountId,
        DateOnly? upToDate,
        CancellationToken ct);

    /// <summary>
    /// Gets all transactions locked to a specific reconciliation record.
    /// </summary>
    /// <param name="reconciliationRecordId">The reconciliation record identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Transactions associated with the reconciliation record.</returns>
    Task<IReadOnlyList<Transaction>> GetByReconciliationRecordAsync(
        Guid reconciliationRecordId,
        CancellationToken ct);

    /// <summary>
    /// Gets all transactions that have location data set.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Transactions with non-null location.</returns>
    Task<IReadOnlyList<Transaction>> GetAllWithLocationAsync(
        CancellationToken cancellationToken = default);
}
