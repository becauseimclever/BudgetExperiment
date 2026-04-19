// <copyright file="ITransactionAnalyticsRepository.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain.DataHealth;

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
    /// Gets spending totals grouped by category for the given month.
    /// </summary>
    /// <param name="year">The year.</param>
    /// <param name="month">The month (1-12).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A dictionary of category IDs and total spending amounts.</returns>
    Task<Dictionary<Guid, decimal>> GetSpendingByCategoriesAsync(
        int year,
        int month,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets transaction projections for duplicate detection analysis.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Duplicate detection projections.</returns>
    Task<IReadOnlyList<DuplicateDetectionProjection>> GetTransactionProjectionsForDuplicateDetectionAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets account/date projections for date gap analysis.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Date gap projections.</returns>
    Task<IReadOnlyList<DateGapProjection>> GetTransactionDatesForGapAnalysisAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets transaction projections for outlier analysis.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Outlier projections.</returns>
    Task<IReadOnlyList<OutlierProjection>> GetTransactionAmountsForOutlierAnalysisAsync(
        CancellationToken cancellationToken = default);

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
