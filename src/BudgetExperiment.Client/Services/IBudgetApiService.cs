// <copyright file="IBudgetApiService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Models;

namespace BudgetExperiment.Client.Services;

/// <summary>
/// Service interface for communicating with the Budget API.
/// </summary>
public interface IBudgetApiService
{
    /// <summary>
    /// Gets all accounts.
    /// </summary>
    /// <returns>List of accounts.</returns>
    Task<IReadOnlyList<AccountModel>> GetAccountsAsync();

    /// <summary>
    /// Gets an account by ID with its transactions.
    /// </summary>
    /// <param name="id">The account ID.</param>
    /// <returns>The account or null if not found.</returns>
    Task<AccountModel?> GetAccountAsync(Guid id);

    /// <summary>
    /// Creates a new account.
    /// </summary>
    /// <param name="model">The account creation data.</param>
    /// <returns>The created account.</returns>
    Task<AccountModel?> CreateAccountAsync(AccountCreateModel model);

    /// <summary>
    /// Deletes an account.
    /// </summary>
    /// <param name="id">The account ID.</param>
    /// <returns>True if deleted successfully.</returns>
    Task<bool> DeleteAccountAsync(Guid id);

    /// <summary>
    /// Gets transactions within a date range.
    /// </summary>
    /// <param name="startDate">Start date (inclusive).</param>
    /// <param name="endDate">End date (inclusive).</param>
    /// <param name="accountId">Optional account filter.</param>
    /// <returns>List of transactions.</returns>
    Task<IReadOnlyList<TransactionModel>> GetTransactionsAsync(DateOnly startDate, DateOnly endDate, Guid? accountId = null);

    /// <summary>
    /// Gets a transaction by ID.
    /// </summary>
    /// <param name="id">The transaction ID.</param>
    /// <returns>The transaction or null if not found.</returns>
    Task<TransactionModel?> GetTransactionAsync(Guid id);

    /// <summary>
    /// Creates a new transaction.
    /// </summary>
    /// <param name="model">The transaction creation data.</param>
    /// <returns>The created transaction.</returns>
    Task<TransactionModel?> CreateTransactionAsync(TransactionCreateModel model);

    /// <summary>
    /// Gets calendar summary (daily totals) for a month.
    /// </summary>
    /// <param name="year">The year.</param>
    /// <param name="month">The month (1-12).</param>
    /// <param name="accountId">Optional account filter.</param>
    /// <returns>List of daily totals.</returns>
    Task<IReadOnlyList<DailyTotalModel>> GetCalendarSummaryAsync(int year, int month, Guid? accountId = null);

    /// <summary>
    /// Gets all recurring transactions.
    /// </summary>
    /// <returns>List of recurring transactions.</returns>
    Task<IReadOnlyList<RecurringTransactionModel>> GetRecurringTransactionsAsync();

    /// <summary>
    /// Gets a recurring transaction by ID.
    /// </summary>
    /// <param name="id">The recurring transaction ID.</param>
    /// <returns>The recurring transaction or null if not found.</returns>
    Task<RecurringTransactionModel?> GetRecurringTransactionAsync(Guid id);

    /// <summary>
    /// Creates a new recurring transaction.
    /// </summary>
    /// <param name="model">The creation data.</param>
    /// <returns>The created recurring transaction.</returns>
    Task<RecurringTransactionModel?> CreateRecurringTransactionAsync(RecurringTransactionCreateModel model);

    /// <summary>
    /// Updates a recurring transaction.
    /// </summary>
    /// <param name="id">The recurring transaction ID.</param>
    /// <param name="model">The update data.</param>
    /// <returns>The updated recurring transaction.</returns>
    Task<RecurringTransactionModel?> UpdateRecurringTransactionAsync(Guid id, RecurringTransactionUpdateModel model);

    /// <summary>
    /// Deletes a recurring transaction.
    /// </summary>
    /// <param name="id">The recurring transaction ID.</param>
    /// <returns>True if deleted successfully.</returns>
    Task<bool> DeleteRecurringTransactionAsync(Guid id);

    /// <summary>
    /// Pauses a recurring transaction.
    /// </summary>
    /// <param name="id">The recurring transaction ID.</param>
    /// <returns>The updated recurring transaction.</returns>
    Task<RecurringTransactionModel?> PauseRecurringTransactionAsync(Guid id);

    /// <summary>
    /// Resumes a paused recurring transaction.
    /// </summary>
    /// <param name="id">The recurring transaction ID.</param>
    /// <returns>The updated recurring transaction.</returns>
    Task<RecurringTransactionModel?> ResumeRecurringTransactionAsync(Guid id);

    /// <summary>
    /// Skips the next occurrence of a recurring transaction.
    /// </summary>
    /// <param name="id">The recurring transaction ID.</param>
    /// <returns>The updated recurring transaction.</returns>
    Task<RecurringTransactionModel?> SkipNextRecurringAsync(Guid id);

    /// <summary>
    /// Gets projected recurring transaction instances for a date range.
    /// </summary>
    /// <param name="from">Start date.</param>
    /// <param name="to">End date.</param>
    /// <param name="accountId">Optional account filter.</param>
    /// <returns>List of projected instances.</returns>
    Task<IReadOnlyList<RecurringInstanceModel>> GetProjectedRecurringAsync(DateOnly from, DateOnly to, Guid? accountId = null);

    /// <summary>
    /// Skips a specific instance of a recurring transaction.
    /// </summary>
    /// <param name="id">The recurring transaction ID.</param>
    /// <param name="date">The scheduled date to skip.</param>
    /// <returns>True if skipped successfully.</returns>
    Task<bool> SkipRecurringInstanceAsync(Guid id, DateOnly date);

    /// <summary>
    /// Modifies a specific instance of a recurring transaction.
    /// </summary>
    /// <param name="id">The recurring transaction ID.</param>
    /// <param name="date">The scheduled date to modify.</param>
    /// <param name="model">The modification data.</param>
    /// <returns>The modified instance.</returns>
    Task<RecurringInstanceModel?> ModifyRecurringInstanceAsync(Guid id, DateOnly date, RecurringInstanceModifyModel model);
}
