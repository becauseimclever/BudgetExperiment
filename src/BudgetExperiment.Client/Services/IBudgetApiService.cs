// <copyright file="IBudgetApiService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Models;
using BudgetExperiment.Contracts.Dtos;

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
    Task<IReadOnlyList<AccountDto>> GetAccountsAsync();

    /// <summary>
    /// Gets an account by ID with its transactions.
    /// </summary>
    /// <param name="id">The account ID.</param>
    /// <returns>The account or null if not found.</returns>
    Task<AccountDto?> GetAccountAsync(Guid id);

    /// <summary>
    /// Creates a new account.
    /// </summary>
    /// <param name="model">The account creation data.</param>
    /// <returns>The created account.</returns>
    Task<AccountDto?> CreateAccountAsync(AccountCreateDto model);

    /// <summary>
    /// Updates an existing account.
    /// </summary>
    /// <param name="id">The account ID.</param>
    /// <param name="model">The account update data.</param>
    /// <returns>The updated account.</returns>
    Task<AccountDto?> UpdateAccountAsync(Guid id, AccountUpdateDto model);

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
    Task<IReadOnlyList<TransactionDto>> GetTransactionsAsync(DateOnly startDate, DateOnly endDate, Guid? accountId = null);

    /// <summary>
    /// Gets a transaction by ID.
    /// </summary>
    /// <param name="id">The transaction ID.</param>
    /// <returns>The transaction or null if not found.</returns>
    Task<TransactionDto?> GetTransactionAsync(Guid id);

    /// <summary>
    /// Creates a new transaction.
    /// </summary>
    /// <param name="model">The transaction creation data.</param>
    /// <returns>The created transaction.</returns>
    Task<TransactionDto?> CreateTransactionAsync(TransactionCreateDto model);

    /// <summary>
    /// Gets a complete calendar grid with all data pre-computed.
    /// </summary>
    /// <param name="year">The year.</param>
    /// <param name="month">The month (1-12).</param>
    /// <param name="accountId">Optional account filter.</param>
    /// <returns>The complete calendar grid.</returns>
    Task<CalendarGridDto> GetCalendarGridAsync(int year, int month, Guid? accountId = null);

    /// <summary>
    /// Gets detailed information for a specific day.
    /// </summary>
    /// <param name="date">The date to get details for.</param>
    /// <param name="accountId">Optional account filter.</param>
    /// <returns>The day detail.</returns>
    Task<DayDetailDto> GetDayDetailAsync(DateOnly date, Guid? accountId = null);

    /// <summary>
    /// Gets a pre-merged transaction list for an account over a date range.
    /// </summary>
    /// <param name="accountId">The account ID.</param>
    /// <param name="startDate">The start date of the range.</param>
    /// <param name="endDate">The end date of the range.</param>
    /// <param name="includeRecurring">Whether to include recurring transaction instances.</param>
    /// <returns>The transaction list with pre-computed summaries.</returns>
    Task<TransactionListDto> GetAccountTransactionListAsync(
        Guid accountId,
        DateOnly startDate,
        DateOnly endDate,
        bool includeRecurring = true);

    /// <summary>
    /// Gets calendar summary (daily totals) for a month.
    /// </summary>
    /// <param name="year">The year.</param>
    /// <param name="month">The month (1-12).</param>
    /// <param name="accountId">Optional account filter.</param>
    /// <returns>List of daily totals.</returns>
    [Obsolete("Use GetCalendarGridAsync instead.")]
    Task<IReadOnlyList<DailyTotalDto>> GetCalendarSummaryAsync(int year, int month, Guid? accountId = null);

    /// <summary>
    /// Gets all recurring transactions.
    /// </summary>
    /// <returns>List of recurring transactions.</returns>
    Task<IReadOnlyList<RecurringTransactionDto>> GetRecurringTransactionsAsync();

    /// <summary>
    /// Gets a recurring transaction by ID.
    /// </summary>
    /// <param name="id">The recurring transaction ID.</param>
    /// <returns>The recurring transaction or null if not found.</returns>
    Task<RecurringTransactionDto?> GetRecurringTransactionAsync(Guid id);

    /// <summary>
    /// Creates a new recurring transaction.
    /// </summary>
    /// <param name="model">The creation data.</param>
    /// <returns>The created recurring transaction.</returns>
    Task<RecurringTransactionDto?> CreateRecurringTransactionAsync(RecurringTransactionCreateDto model);

    /// <summary>
    /// Updates a recurring transaction.
    /// </summary>
    /// <param name="id">The recurring transaction ID.</param>
    /// <param name="model">The update data.</param>
    /// <returns>The updated recurring transaction.</returns>
    Task<RecurringTransactionDto?> UpdateRecurringTransactionAsync(Guid id, RecurringTransactionUpdateDto model);

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
    Task<RecurringTransactionDto?> PauseRecurringTransactionAsync(Guid id);

    /// <summary>
    /// Resumes a paused recurring transaction.
    /// </summary>
    /// <param name="id">The recurring transaction ID.</param>
    /// <returns>The updated recurring transaction.</returns>
    Task<RecurringTransactionDto?> ResumeRecurringTransactionAsync(Guid id);

    /// <summary>
    /// Skips the next occurrence of a recurring transaction.
    /// </summary>
    /// <param name="id">The recurring transaction ID.</param>
    /// <returns>The updated recurring transaction.</returns>
    Task<RecurringTransactionDto?> SkipNextRecurringAsync(Guid id);

    /// <summary>
    /// Gets projected recurring transaction instances for a date range.
    /// </summary>
    /// <param name="from">Start date.</param>
    /// <param name="to">End date.</param>
    /// <param name="accountId">Optional account filter.</param>
    /// <returns>List of projected instances.</returns>
    Task<IReadOnlyList<RecurringInstanceDto>> GetProjectedRecurringAsync(DateOnly from, DateOnly to, Guid? accountId = null);

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
    Task<RecurringInstanceDto?> ModifyRecurringInstanceAsync(Guid id, DateOnly date, RecurringInstanceModifyDto model);

    /// <summary>
    /// Creates a new transfer between accounts.
    /// </summary>
    /// <param name="model">The transfer creation data.</param>
    /// <returns>The created transfer.</returns>
    Task<TransferResponse?> CreateTransferAsync(CreateTransferRequest model);

    /// <summary>
    /// Gets a transfer by its identifier.
    /// </summary>
    /// <param name="transferId">The transfer identifier.</param>
    /// <returns>The transfer or null if not found.</returns>
    Task<TransferResponse?> GetTransferAsync(Guid transferId);

    /// <summary>
    /// Gets a list of transfers with optional filtering.
    /// </summary>
    /// <param name="accountId">Optional filter by account.</param>
    /// <param name="from">Optional start date filter.</param>
    /// <param name="to">Optional end date filter.</param>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Page size.</param>
    /// <returns>List of transfer items.</returns>
    Task<IReadOnlyList<TransferListItemResponse>> GetTransfersAsync(
        Guid? accountId = null,
        DateOnly? from = null,
        DateOnly? to = null,
        int page = 1,
        int pageSize = 20);

    /// <summary>
    /// Updates an existing transfer.
    /// </summary>
    /// <param name="transferId">The transfer identifier.</param>
    /// <param name="model">The update data.</param>
    /// <returns>The updated transfer or null if not found.</returns>
    Task<TransferResponse?> UpdateTransferAsync(Guid transferId, UpdateTransferRequest model);

    /// <summary>
    /// Deletes a transfer.
    /// </summary>
    /// <param name="transferId">The transfer identifier.</param>
    /// <returns>True if deleted successfully.</returns>
    Task<bool> DeleteTransferAsync(Guid transferId);

    // Recurring Transfers

    /// <summary>
    /// Gets all recurring transfers.
    /// </summary>
    /// <param name="accountId">Optional filter by account (source or destination).</param>
    /// <returns>List of recurring transfers.</returns>
    Task<IReadOnlyList<RecurringTransferDto>> GetRecurringTransfersAsync(Guid? accountId = null);

    /// <summary>
    /// Gets a recurring transfer by ID.
    /// </summary>
    /// <param name="id">The recurring transfer ID.</param>
    /// <returns>The recurring transfer or null if not found.</returns>
    Task<RecurringTransferDto?> GetRecurringTransferAsync(Guid id);

    /// <summary>
    /// Creates a new recurring transfer.
    /// </summary>
    /// <param name="model">The creation data.</param>
    /// <returns>The created recurring transfer.</returns>
    Task<RecurringTransferDto?> CreateRecurringTransferAsync(RecurringTransferCreateDto model);

    /// <summary>
    /// Updates a recurring transfer.
    /// </summary>
    /// <param name="id">The recurring transfer ID.</param>
    /// <param name="model">The update data.</param>
    /// <returns>The updated recurring transfer.</returns>
    Task<RecurringTransferDto?> UpdateRecurringTransferAsync(Guid id, RecurringTransferUpdateDto model);

    /// <summary>
    /// Deletes a recurring transfer.
    /// </summary>
    /// <param name="id">The recurring transfer ID.</param>
    /// <returns>True if deleted successfully.</returns>
    Task<bool> DeleteRecurringTransferAsync(Guid id);

    /// <summary>
    /// Pauses a recurring transfer.
    /// </summary>
    /// <param name="id">The recurring transfer ID.</param>
    /// <returns>The updated recurring transfer.</returns>
    Task<RecurringTransferDto?> PauseRecurringTransferAsync(Guid id);

    /// <summary>
    /// Resumes a paused recurring transfer.
    /// </summary>
    /// <param name="id">The recurring transfer ID.</param>
    /// <returns>The updated recurring transfer.</returns>
    Task<RecurringTransferDto?> ResumeRecurringTransferAsync(Guid id);

    /// <summary>
    /// Skips the next occurrence of a recurring transfer.
    /// </summary>
    /// <param name="id">The recurring transfer ID.</param>
    /// <returns>The updated recurring transfer.</returns>
    Task<RecurringTransferDto?> SkipNextRecurringTransferAsync(Guid id);

    /// <summary>
    /// Gets projected recurring transfer instances for a date range.
    /// </summary>
    /// <param name="from">Start date.</param>
    /// <param name="to">End date.</param>
    /// <param name="accountId">Optional account filter.</param>
    /// <returns>List of projected instances.</returns>
    Task<IReadOnlyList<RecurringTransferInstanceDto>> GetProjectedRecurringTransfersAsync(DateOnly from, DateOnly to, Guid? accountId = null);

    /// <summary>
    /// Skips a specific instance of a recurring transfer.
    /// </summary>
    /// <param name="id">The recurring transfer ID.</param>
    /// <param name="date">The scheduled date to skip.</param>
    /// <returns>True if skipped successfully.</returns>
    Task<bool> SkipRecurringTransferInstanceAsync(Guid id, DateOnly date);

    /// <summary>
    /// Modifies a specific instance of a recurring transfer.
    /// </summary>
    /// <param name="id">The recurring transfer ID.</param>
    /// <param name="date">The scheduled date to modify.</param>
    /// <param name="model">The modification data.</param>
    /// <returns>The modified instance.</returns>
    Task<RecurringTransferInstanceDto?> ModifyRecurringTransferInstanceAsync(Guid id, DateOnly date, RecurringTransferInstanceModifyDto model);

    // Realize Recurring Items

    /// <summary>
    /// Realizes a recurring transaction instance, creating an actual transaction.
    /// </summary>
    /// <param name="recurringTransactionId">The recurring transaction ID.</param>
    /// <param name="request">The realization request with instance date and optional overrides.</param>
    /// <returns>The created transaction.</returns>
    Task<TransactionDto?> RealizeRecurringTransactionAsync(Guid recurringTransactionId, RealizeRecurringTransactionRequest request);

    /// <summary>
    /// Realizes a recurring transfer instance, creating actual transfer transactions.
    /// </summary>
    /// <param name="recurringTransferId">The recurring transfer ID.</param>
    /// <param name="request">The realization request with instance date and optional overrides.</param>
    /// <returns>The created transfer.</returns>
    Task<TransferResponse?> RealizeRecurringTransferAsync(Guid recurringTransferId, RealizeRecurringTransferRequest request);

    // Past-Due Operations

    /// <summary>
    /// Gets a summary of all past-due recurring items.
    /// </summary>
    /// <param name="accountId">Optional account filter.</param>
    /// <returns>Summary of past-due items.</returns>
    Task<PastDueSummaryDto?> GetPastDueItemsAsync(Guid? accountId = null);

    /// <summary>
    /// Realizes multiple past-due items in batch.
    /// </summary>
    /// <param name="request">The batch realize request.</param>
    /// <returns>Results of the batch operation.</returns>
    Task<BatchRealizeResultDto?> RealizeBatchAsync(BatchRealizeRequest request);
}
