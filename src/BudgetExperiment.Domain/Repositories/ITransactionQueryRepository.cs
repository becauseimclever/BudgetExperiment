// <copyright file="ITransactionQueryRepository.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Repositories;

/// <summary>
/// Focused repository interface for transaction date-range queries, paged queries, and search operations.
/// </summary>
public interface ITransactionQueryRepository
{
    /// <summary>
    /// Gets transactions within a date range, optionally filtered by account.
    /// </summary>
    /// <param name="startDate">Start date (inclusive).</param>
    /// <param name="endDate">End date (inclusive).</param>
    /// <param name="accountId">Optional account filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Transactions in the date range.</returns>
    Task<IReadOnlyList<Transaction>> GetByDateRangeAsync(
        DateOnly startDate,
        DateOnly endDate,
        Guid? accountId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets daily transaction totals for a month (for calendar summary view).
    /// </summary>
    /// <param name="year">The year.</param>
    /// <param name="month">The month (1-12).</param>
    /// <param name="accountId">Optional account filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Daily totals as (date, total amount).</returns>
    Task<IReadOnlyList<DailyTotalValue>> GetDailyTotalsAsync(
        int year,
        int month,
        Guid? accountId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the paired transactions for a transfer by the transfer identifier.
    /// </summary>
    /// <param name="transferId">The transfer identifier linking the paired transactions.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The source and destination transactions for the transfer, or empty if not found.</returns>
    Task<IReadOnlyList<Transaction>> GetByTransferIdAsync(
        Guid transferId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a transaction by recurring transaction ID and instance date.
    /// </summary>
    /// <param name="recurringTransactionId">The recurring transaction identifier.</param>
    /// <param name="instanceDate">The scheduled instance date.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The realized transaction or null if not found.</returns>
    Task<Transaction?> GetByRecurringInstanceAsync(
        Guid recurringTransactionId,
        DateOnly instanceDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets transactions by recurring transfer ID and instance date.
    /// </summary>
    /// <param name="recurringTransferId">The recurring transfer identifier.</param>
    /// <param name="instanceDate">The scheduled instance date.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The realized transfer transactions (source and destination) or empty if not found.</returns>
    Task<IReadOnlyList<Transaction>> GetByRecurringTransferInstanceAsync(
        Guid recurringTransferId,
        DateOnly instanceDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets transactions that do not have a category assigned.
    /// </summary>
    /// <param name="maxCount">Maximum number of transactions to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of uncategorized transactions.</returns>
    Task<IReadOnlyList<Transaction>> GetUncategorizedAsync(
        int maxCount = 500,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets uncategorized transaction descriptions only.
    /// </summary>
    /// <param name="maxCount">Maximum number of descriptions to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of uncategorized transaction descriptions.</returns>
    Task<IReadOnlyList<string>> GetUncategorizedDescriptionsAsync(
        int maxCount = 500,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets uncategorized transactions with filtering, sorting, and paging.
    /// </summary>
    /// <param name="startDate">Optional start date filter (inclusive).</param>
    /// <param name="endDate">Optional end date filter (inclusive).</param>
    /// <param name="minAmount">Optional minimum amount filter.</param>
    /// <param name="maxAmount">Optional maximum amount filter.</param>
    /// <param name="descriptionContains">Optional description contains filter (case-insensitive).</param>
    /// <param name="accountId">Optional account filter.</param>
    /// <param name="sortBy">Sort field: "Date", "Amount", or "Description".</param>
    /// <param name="sortDescending">Sort direction (true = descending).</param>
    /// <param name="skip">Number of records to skip.</param>
    /// <param name="take">Number of records to take.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A tuple containing the paged items and total count.</returns>
    Task<(IReadOnlyList<Transaction> Items, int TotalCount)> GetUncategorizedPagedAsync(
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        decimal? minAmount = null,
        decimal? maxAmount = null,
        string? descriptionContains = null,
        Guid? accountId = null,
        string sortBy = "Date",
        bool sortDescending = true,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets transactions with filtering, sorting, and paging for the unified transaction list.
    /// Includes Category navigation property. Supports sorting by account name and category name.
    /// In encrypted persistence mode, description/amount filters and account-name sorting are
    /// applied in-memory after base server-side filters to preserve business semantics.
    /// </summary>
    /// <param name="accountId">Optional account filter.</param>
    /// <param name="categoryId">Optional category filter.</param>
    /// <param name="uncategorized">If true, return only uncategorized transactions.</param>
    /// <param name="startDate">Optional start date filter (inclusive).</param>
    /// <param name="endDate">Optional end date filter (inclusive).</param>
    /// <param name="descriptionContains">Optional description contains filter (case-insensitive).</param>
    /// <param name="minAmount">Optional minimum amount filter (absolute value).</param>
    /// <param name="maxAmount">Optional maximum amount filter (absolute value).</param>
    /// <param name="sortBy">Sort field: "date", "description", "amount", "category", "account".</param>
    /// <param name="sortDescending">Sort direction (true = descending).</param>
    /// <param name="skip">Number of records to skip.</param>
    /// <param name="take">Number of records to take.</param>
    /// <param name="kakeiboCategory">Optional Kakeibo category filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A tuple containing the paged items and total count.</returns>
    Task<(IReadOnlyList<Transaction> Items, int TotalCount)> GetUnifiedPagedAsync(
        Guid? accountId = null,
        Guid? categoryId = null,
        bool? uncategorized = null,
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        string? descriptionContains = null,
        decimal? minAmount = null,
        decimal? maxAmount = null,
        string sortBy = "date",
        bool sortDescending = true,
        int skip = 0,
        int take = 50,
        KakeiboCategory? kakeiboCategory = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all unique transaction descriptions for pattern analysis.
    /// </summary>
    /// <param name="searchPrefix">Optional search prefix for narrowing results.</param>
    /// <param name="maxResults">Maximum number of descriptions to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of unique descriptions.</returns>
    Task<IReadOnlyList<string>> GetAllDescriptionsAsync(
        string searchPrefix = "",
        int maxResults = 100,
        CancellationToken cancellationToken = default);
}
