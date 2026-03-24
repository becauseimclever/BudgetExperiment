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
    /// <param name="version">Optional ETag version for optimistic concurrency.</param>
    /// <returns>The update result (success, conflict, or failure).</returns>
    Task<ApiResult<AccountDto>> UpdateAccountAsync(Guid id, AccountUpdateDto model, string? version = null);

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
    /// Updates an existing transaction.
    /// </summary>
    /// <param name="id">The transaction ID.</param>
    /// <param name="model">The transaction update data.</param>
    /// <param name="version">Optional ETag version for optimistic concurrency.</param>
    /// <returns>The update result (success, conflict, or failure).</returns>
    Task<ApiResult<TransactionDto>> UpdateTransactionAsync(Guid id, TransactionUpdateDto model, string? version = null);

    /// <summary>
    /// Deletes a transaction by ID.
    /// </summary>
    /// <param name="id">The transaction ID.</param>
    /// <returns>True if successfully deleted; false otherwise.</returns>
    Task<bool> DeleteTransactionAsync(Guid id);

    /// <summary>
    /// Updates the location data for a transaction.
    /// </summary>
    /// <param name="id">The transaction ID.</param>
    /// <param name="dto">The location update data.</param>
    /// <param name="version">Optional ETag version for optimistic concurrency.</param>
    /// <returns>The update result (success, conflict, or failure).</returns>
    Task<ApiResult<TransactionDto>> UpdateTransactionLocationAsync(Guid id, TransactionLocationUpdateDto dto, string? version = null);

    /// <summary>
    /// Clears the location data for a transaction.
    /// </summary>
    /// <param name="id">The transaction ID.</param>
    /// <returns>True if successfully cleared; false otherwise.</returns>
    Task<bool> ClearTransactionLocationAsync(Guid id);

    /// <summary>
    /// Performs reverse geocoding to resolve GPS coordinates to an address.
    /// </summary>
    /// <param name="latitude">The latitude coordinate.</param>
    /// <param name="longitude">The longitude coordinate.</param>
    /// <returns>The geocoded address, or null if unavailable.</returns>
    Task<ReverseGeocodeResponseDto?> ReverseGeocodeAsync(decimal latitude, decimal longitude);

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
    /// <param name="version">Optional ETag version for optimistic concurrency.</param>
    /// <returns>The update result (success, conflict, or failure).</returns>
    Task<ApiResult<RecurringTransactionDto>> UpdateRecurringTransactionAsync(Guid id, RecurringTransactionUpdateDto model, string? version = null);

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
    /// <param name="version">Optional ETag version for optimistic concurrency.</param>
    /// <returns>The update result (success, conflict, or failure).</returns>
    Task<ApiResult<RecurringInstanceDto>> ModifyRecurringInstanceAsync(Guid id, DateOnly date, RecurringInstanceModifyDto model, string? version = null);

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
    /// <param name="version">Optional ETag version for optimistic concurrency.</param>
    /// <returns>The update result (success, conflict, or failure).</returns>
    Task<ApiResult<RecurringTransferDto>> UpdateRecurringTransferAsync(Guid id, RecurringTransferUpdateDto model, string? version = null);

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
    /// <param name="version">Optional ETag version for optimistic concurrency.</param>
    /// <returns>The update result (success, conflict, or failure).</returns>
    Task<ApiResult<RecurringTransferInstanceDto>> ModifyRecurringTransferInstanceAsync(Guid id, DateOnly date, RecurringTransferInstanceModifyDto model, string? version = null);

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

    // Settings Operations

    /// <summary>
    /// Gets the current application settings.
    /// </summary>
    /// <returns>The application settings.</returns>
    Task<AppSettingsDto?> GetSettingsAsync();

    /// <summary>
    /// Updates the application settings.
    /// </summary>
    /// <param name="dto">The settings update data.</param>
    /// <returns>The updated application settings.</returns>
    Task<AppSettingsDto?> UpdateSettingsAsync(AppSettingsUpdateDto dto);

    /// <summary>
    /// Deletes all location data from all transactions (bulk privacy operation).
    /// </summary>
    /// <returns>The result containing the count of cleared transactions, or null on failure.</returns>
    Task<LocationDataClearedDto?> DeleteAllLocationDataAsync();

    // User Settings Operations

    /// <summary>
    /// Gets the current user's settings (currency, first day of week, onboarding status).
    /// </summary>
    /// <returns>The user settings.</returns>
    Task<UserSettingsDto?> GetUserSettingsAsync();

    /// <summary>
    /// Updates the current user's settings.
    /// </summary>
    /// <param name="dto">The settings update data.</param>
    /// <returns>The updated user settings.</returns>
    Task<UserSettingsDto?> UpdateUserSettingsAsync(UserSettingsUpdateDto dto);

    /// <summary>
    /// Completes the onboarding wizard for the current user.
    /// </summary>
    /// <returns>The updated user settings.</returns>
    Task<UserSettingsDto?> CompleteOnboardingAsync();

    // Paycheck Allocation Operations

    /// <summary>
    /// Gets paycheck allocation summary for budgeting.
    /// </summary>
    /// <param name="frequency">The paycheck frequency (Weekly, BiWeekly, Monthly).</param>
    /// <param name="amount">Optional paycheck amount.</param>
    /// <param name="accountId">Optional account filter.</param>
    /// <returns>The allocation summary.</returns>
    Task<PaycheckAllocationSummaryDto?> GetPaycheckAllocationAsync(string frequency, decimal? amount = null, Guid? accountId = null);

    // Budget Category Operations

    /// <summary>
    /// Gets all budget categories.
    /// </summary>
    /// <param name="activeOnly">If true, returns only active categories.</param>
    /// <returns>List of budget categories.</returns>
    Task<IReadOnlyList<BudgetCategoryDto>> GetCategoriesAsync(bool activeOnly = false);

    /// <summary>
    /// Gets a budget category by ID.
    /// </summary>
    /// <param name="id">The category ID.</param>
    /// <returns>The category or null if not found.</returns>
    Task<BudgetCategoryDto?> GetCategoryAsync(Guid id);

    /// <summary>
    /// Creates a new budget category.
    /// </summary>
    /// <param name="model">The category creation data.</param>
    /// <returns>The created category.</returns>
    Task<BudgetCategoryDto?> CreateCategoryAsync(BudgetCategoryCreateDto model);

    /// <summary>
    /// Updates an existing budget category.
    /// </summary>
    /// <param name="id">The category ID.</param>
    /// <param name="model">The category update data.</param>
    /// <param name="version">Optional ETag version for optimistic concurrency.</param>
    /// <returns>The update result (success, conflict, or failure).</returns>
    Task<ApiResult<BudgetCategoryDto>> UpdateCategoryAsync(Guid id, BudgetCategoryUpdateDto model, string? version = null);

    /// <summary>
    /// Deletes a budget category.
    /// </summary>
    /// <param name="id">The category ID.</param>
    /// <returns>True if deleted successfully.</returns>
    Task<bool> DeleteCategoryAsync(Guid id);

    /// <summary>
    /// Activates a budget category.
    /// </summary>
    /// <param name="id">The category ID.</param>
    /// <returns>True if activated successfully.</returns>
    Task<bool> ActivateCategoryAsync(Guid id);

    /// <summary>
    /// Deactivates a budget category.
    /// </summary>
    /// <param name="id">The category ID.</param>
    /// <returns>True if deactivated successfully.</returns>
    Task<bool> DeactivateCategoryAsync(Guid id);

    // Budget Goal Operations

    /// <summary>
    /// Gets all budget goals for a specific month.
    /// </summary>
    /// <param name="year">The year.</param>
    /// <param name="month">The month (1-12).</param>
    /// <returns>List of budget goals.</returns>
    Task<IReadOnlyList<BudgetGoalDto>> GetBudgetGoalsAsync(int year, int month);

    /// <summary>
    /// Gets all budget goals for a specific category.
    /// </summary>
    /// <param name="categoryId">The category ID.</param>
    /// <returns>List of budget goals for the category.</returns>
    Task<IReadOnlyList<BudgetGoalDto>> GetBudgetGoalsByCategoryAsync(Guid categoryId);

    /// <summary>
    /// Sets or updates a budget goal for a category.
    /// </summary>
    /// <param name="categoryId">The category ID.</param>
    /// <param name="model">The goal data.</param>
    /// <param name="version">Optional ETag version for optimistic concurrency.</param>
    /// <returns>The update result (success, conflict, or failure).</returns>
    Task<ApiResult<BudgetGoalDto>> SetBudgetGoalAsync(Guid categoryId, BudgetGoalSetDto model, string? version = null);

    /// <summary>
    /// Deletes a budget goal.
    /// </summary>
    /// <param name="categoryId">The category ID.</param>
    /// <param name="year">The year.</param>
    /// <param name="month">The month (1-12).</param>
    /// <returns>True if deleted successfully.</returns>
    Task<bool> DeleteBudgetGoalAsync(Guid categoryId, int year, int month);

    /// <summary>
    /// Copies budget goals from one month to another.
    /// </summary>
    /// <param name="request">The copy request containing source and target month details.</param>
    /// <returns>A result summarizing the copy operation.</returns>
    Task<CopyBudgetGoalsResult?> CopyBudgetGoalsAsync(CopyBudgetGoalsRequest request);

    // Budget Progress Operations

    /// <summary>
    /// Gets the budget summary for a specific month.
    /// </summary>
    /// <param name="year">The year.</param>
    /// <param name="month">The month (1-12).</param>
    /// <returns>The budget summary with progress for all categories.</returns>
    Task<BudgetSummaryDto?> GetBudgetSummaryAsync(int year, int month);

    /// <summary>
    /// Gets the budget progress for a specific category and month.
    /// </summary>
    /// <param name="categoryId">The category ID.</param>
    /// <param name="year">The year.</param>
    /// <param name="month">The month (1-12).</param>
    /// <returns>The budget progress for the category.</returns>
    Task<BudgetProgressDto?> GetCategoryProgressAsync(Guid categoryId, int year, int month);

    // Categorization Rule Operations

    /// <summary>
    /// Gets all categorization rules.
    /// </summary>
    /// <param name="activeOnly">If true, returns only active rules.</param>
    /// <returns>List of categorization rules ordered by priority.</returns>
    Task<IReadOnlyList<CategorizationRuleDto>> GetCategorizationRulesAsync(bool activeOnly = false);

    /// <summary>
    /// Gets categorization rules with server-side pagination, filtering, and sorting.
    /// </summary>
    /// <param name="request">The paged list request parameters.</param>
    /// <returns>A paged response of categorization rules.</returns>
    Task<CategorizationRulePageResponse> GetCategorizationRulesPagedAsync(CategorizationRuleListRequest request);

    /// <summary>
    /// Gets a categorization rule by ID.
    /// </summary>
    /// <param name="id">The rule ID.</param>
    /// <returns>The rule or null if not found.</returns>
    Task<CategorizationRuleDto?> GetCategorizationRuleAsync(Guid id);

    /// <summary>
    /// Creates a new categorization rule.
    /// </summary>
    /// <param name="model">The rule creation data.</param>
    /// <returns>The created rule.</returns>
    Task<CategorizationRuleDto?> CreateCategorizationRuleAsync(CategorizationRuleCreateDto model);

    /// <summary>
    /// Updates an existing categorization rule.
    /// </summary>
    /// <param name="id">The rule ID.</param>
    /// <param name="model">The rule update data.</param>
    /// <param name="version">Optional ETag version for optimistic concurrency.</param>
    /// <returns>The update result (success, conflict, or failure).</returns>
    Task<ApiResult<CategorizationRuleDto>> UpdateCategorizationRuleAsync(Guid id, CategorizationRuleUpdateDto model, string? version = null);

    /// <summary>
    /// Deletes a categorization rule.
    /// </summary>
    /// <param name="id">The rule ID.</param>
    /// <returns>True if deleted successfully.</returns>
    Task<bool> DeleteCategorizationRuleAsync(Guid id);

    /// <summary>
    /// Activates a categorization rule.
    /// </summary>
    /// <param name="id">The rule ID.</param>
    /// <returns>True if activated successfully.</returns>
    Task<bool> ActivateCategorizationRuleAsync(Guid id);

    /// <summary>
    /// Deactivates a categorization rule.
    /// </summary>
    /// <param name="id">The rule ID.</param>
    /// <returns>True if deactivated successfully.</returns>
    Task<bool> DeactivateCategorizationRuleAsync(Guid id);

    /// <summary>
    /// Tests a pattern against existing transactions.
    /// </summary>
    /// <param name="request">The test pattern request.</param>
    /// <returns>The test results showing matching transactions.</returns>
    Task<TestPatternResponse?> TestPatternAsync(TestPatternRequest request);

    /// <summary>
    /// Applies categorization rules to transactions.
    /// </summary>
    /// <param name="request">The apply rules request.</param>
    /// <returns>The result showing how many transactions were categorized.</returns>
    Task<ApplyRulesResponse?> ApplyCategorizationRulesAsync(ApplyRulesRequest request);

    /// <summary>
    /// Reorders categorization rules.
    /// </summary>
    /// <param name="ruleIds">The ordered list of rule IDs. The index becomes the new priority.</param>
    /// <returns>True if reordered successfully.</returns>
    Task<bool> ReorderCategorizationRulesAsync(IReadOnlyList<Guid> ruleIds);

    /// <summary>
    /// Bulk deletes categorization rules.
    /// </summary>
    /// <param name="ids">The IDs of rules to delete.</param>
    /// <returns>The bulk action response with affected count.</returns>
    Task<BulkRuleActionResponse?> BulkDeleteCategorizationRulesAsync(IReadOnlyList<Guid> ids);

    /// <summary>
    /// Bulk activates categorization rules.
    /// </summary>
    /// <param name="ids">The IDs of rules to activate.</param>
    /// <returns>The bulk action response with affected count.</returns>
    Task<BulkRuleActionResponse?> BulkActivateCategorizationRulesAsync(IReadOnlyList<Guid> ids);

    /// <summary>
    /// Bulk deactivates categorization rules.
    /// </summary>
    /// <param name="ids">The IDs of rules to deactivate.</param>
    /// <returns>The bulk action response with affected count.</returns>
    Task<BulkRuleActionResponse?> BulkDeactivateCategorizationRulesAsync(IReadOnlyList<Guid> ids);

    // Unified Transaction Operations

    /// <summary>
    /// Gets a paged, filtered, sorted list of all transactions.
    /// </summary>
    /// <param name="filter">The filter, sort, and paging parameters.</param>
    /// <returns>A paged result of unified transactions with summary and optional balance info.</returns>
    Task<UnifiedTransactionPageDto> GetUnifiedTransactionsAsync(UnifiedTransactionFilterDto filter);

    /// <summary>
    /// Updates the category on a single transaction (quick inline assignment).
    /// </summary>
    /// <param name="transactionId">The transaction ID.</param>
    /// <param name="categoryId">The category ID to assign, or null to clear.</param>
    /// <returns>The updated transaction DTO, or null if not found.</returns>
    Task<TransactionDto?> UpdateTransactionCategoryAsync(Guid transactionId, Guid? categoryId);

    /// <summary>
    /// Gets batch category suggestions for uncategorized transactions using existing rules.
    /// </summary>
    /// <param name="transactionIds">The transaction IDs to get suggestions for.</param>
    /// <returns>A response with suggestions keyed by transaction ID.</returns>
    Task<BatchSuggestCategoriesResponse> GetBatchCategorySuggestionsAsync(IReadOnlyList<Guid> transactionIds);

    // Uncategorized Transaction Operations

    /// <summary>
    /// Gets a paged list of uncategorized transactions with filtering.
    /// </summary>
    /// <param name="filter">The filter and paging parameters.</param>
    /// <returns>A paged result of uncategorized transactions.</returns>
    Task<UncategorizedTransactionPageDto> GetUncategorizedTransactionsAsync(UncategorizedTransactionFilterDto filter);

    /// <summary>
    /// Bulk categorizes multiple transactions.
    /// </summary>
    /// <param name="request">The request containing transaction IDs and target category.</param>
    /// <returns>The result showing success/failure counts.</returns>
    Task<BulkCategorizeResponse> BulkCategorizeTransactionsAsync(BulkCategorizeRequest request);

    /// <summary>
    /// Gets the monthly category spending report.
    /// </summary>
    /// <param name="year">The year.</param>
    /// <param name="month">The month (1-12).</param>
    /// <returns>The monthly category report.</returns>
    Task<MonthlyCategoryReportDto?> GetMonthlyCategoryReportAsync(int year, int month);

    /// <summary>
    /// Gets the category spending report for an arbitrary date range.
    /// </summary>
    /// <param name="startDate">The start date.</param>
    /// <param name="endDate">The end date.</param>
    /// <param name="accountId">Optional account filter.</param>
    /// <returns>The date range category report.</returns>
    Task<DateRangeCategoryReportDto?> GetCategoryReportByRangeAsync(DateOnly startDate, DateOnly endDate, Guid? accountId = null);

    /// <summary>
    /// Gets the spending trends report over multiple months.
    /// </summary>
    /// <param name="months">Number of months to include (default 6, max 24).</param>
    /// <param name="endYear">Optional end year (defaults to current).</param>
    /// <param name="endMonth">Optional end month (defaults to current).</param>
    /// <param name="categoryId">Optional category filter.</param>
    /// <returns>The spending trends report.</returns>
    Task<SpendingTrendsReportDto?> GetSpendingTrendsAsync(int months = 6, int? endYear = null, int? endMonth = null, Guid? categoryId = null);

    /// <summary>
    /// Gets the day summary analytics for a specific date.
    /// </summary>
    /// <param name="date">The date to get the summary for.</param>
    /// <param name="accountId">Optional account filter.</param>
    /// <returns>The day summary with income, spending, net, and top categories.</returns>
    Task<DaySummaryDto?> GetDaySummaryAsync(DateOnly date, Guid? accountId = null);

    /// <summary>
    /// Gets spending aggregated by geographic location for a date range.
    /// </summary>
    /// <param name="startDate">The start date (inclusive).</param>
    /// <param name="endDate">The end date (inclusive).</param>
    /// <param name="accountId">Optional account filter.</param>
    /// <returns>The location spending report.</returns>
    Task<LocationSpendingReportDto?> GetSpendingByLocationAsync(DateOnly startDate, DateOnly endDate, Guid? accountId = null);

    /// <summary>
    /// Gets all custom report layouts.
    /// </summary>
    /// <returns>List of custom report layouts.</returns>
    Task<IReadOnlyList<CustomReportLayoutDto>> GetCustomReportLayoutsAsync();

    /// <summary>
    /// Gets a custom report layout by id.
    /// </summary>
    /// <param name="id">Layout id.</param>
    /// <returns>The layout or null.</returns>
    Task<CustomReportLayoutDto?> GetCustomReportLayoutAsync(Guid id);

    /// <summary>
    /// Creates a custom report layout.
    /// </summary>
    /// <param name="dto">Create DTO.</param>
    /// <returns>The created layout.</returns>
    Task<CustomReportLayoutDto?> CreateCustomReportLayoutAsync(CustomReportLayoutCreateDto dto);

    /// <summary>
    /// Updates a custom report layout.
    /// </summary>
    /// <param name="id">Layout id.</param>
    /// <param name="dto">Update DTO.</param>
    /// <param name="version">Optional ETag version for optimistic concurrency.</param>
    /// <returns>The update result (success, conflict, or failure).</returns>
    Task<ApiResult<CustomReportLayoutDto>> UpdateCustomReportLayoutAsync(Guid id, CustomReportLayoutUpdateDto dto, string? version = null);

    /// <summary>
    /// Deletes a custom report layout.
    /// </summary>
    /// <param name="id">Layout id.</param>
    /// <returns>True if deleted.</returns>
    Task<bool> DeleteCustomReportLayoutAsync(Guid id);

    /// <summary>
    /// Gets the import patterns for a recurring transaction.
    /// </summary>
    /// <param name="recurringTransactionId">The recurring transaction ID.</param>
    /// <returns>The import patterns.</returns>
    Task<ImportPatternsDto?> GetImportPatternsAsync(Guid recurringTransactionId);

    /// <summary>
    /// Updates the import patterns for a recurring transaction.
    /// </summary>
    /// <param name="recurringTransactionId">The recurring transaction ID.</param>
    /// <param name="patterns">The patterns to set.</param>
    /// <returns>The updated patterns.</returns>
    Task<ImportPatternsDto?> UpdateImportPatternsAsync(Guid recurringTransactionId, ImportPatternsDto patterns);

    // Statement Reconciliation Operations (Feature 125b)

    /// <summary>Marks a single transaction as cleared.</summary>
    /// <param name="request">The mark cleared request.</param>
    /// <returns>Updated transaction DTO.</returns>
    Task<TransactionDto?> MarkTransactionClearedAsync(MarkClearedRequest request);

    /// <summary>Unclears a single transaction.</summary>
    /// <param name="request">The mark uncleared request.</param>
    /// <returns>Updated transaction DTO.</returns>
    Task<TransactionDto?> MarkTransactionUnclearedAsync(MarkUnclearedRequest request);

    /// <summary>Bulk-marks multiple transactions as cleared.</summary>
    /// <param name="request">The bulk mark cleared request.</param>
    /// <returns>Updated transaction DTOs.</returns>
    Task<IReadOnlyList<TransactionDto>?> BulkMarkTransactionsClearedAsync(BulkMarkClearedRequest request);

    /// <summary>Bulk-unclears multiple transactions (skips locked ones).</summary>
    /// <param name="request">The bulk mark uncleared request.</param>
    /// <returns>Updated transaction DTOs for successfully uncleared transactions.</returns>
    Task<IReadOnlyList<TransactionDto>?> BulkMarkTransactionsUnclearedAsync(BulkMarkUnclearedRequest request);

    /// <summary>Gets the active (non-completed) statement balance for an account.</summary>
    /// <param name="accountId">The account identifier.</param>
    /// <returns>The active statement balance DTO, or null if none.</returns>
    Task<StatementBalanceDto?> GetActiveStatementBalanceAsync(Guid accountId);

    /// <summary>Gets the computed cleared balance for an account up to an optional date.</summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="upToDate">Optional upper bound date.</param>
    /// <returns>Cleared balance DTO.</returns>
    Task<ClearedBalanceDto?> GetClearedBalanceAsync(Guid accountId, DateOnly? upToDate = null);

    /// <summary>Creates or updates the active statement balance for an account.</summary>
    /// <param name="request">The set statement balance request.</param>
    /// <returns>The created or updated statement balance DTO.</returns>
    Task<StatementBalanceDto?> SetStatementBalanceAsync(SetStatementBalanceRequest request);

    /// <summary>Completes reconciliation for an account.</summary>
    /// <param name="request">The complete reconciliation request.</param>
    /// <returns>The created reconciliation record, or failure if unbalanced.</returns>
    Task<ApiResult<ReconciliationRecordDto>> CompleteReconciliationAsync(CompleteReconciliationRequest request);

    /// <summary>Gets the reconciliation history for an account.</summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="page">1-based page number.</param>
    /// <param name="pageSize">Number of items per page.</param>
    /// <returns>Paged list of reconciliation record DTOs.</returns>
    Task<IReadOnlyList<ReconciliationRecordDto>?> GetReconciliationHistoryAsync(Guid accountId, int page = 1, int pageSize = 20);

    /// <summary>Gets transactions locked to a reconciliation record.</summary>
    /// <param name="reconciliationRecordId">The reconciliation record identifier.</param>
    /// <returns>Transaction DTOs locked to the reconciliation record.</returns>
    Task<IReadOnlyList<TransactionDto>?> GetReconciliationTransactionsAsync(Guid reconciliationRecordId);

    // Data Health Operations

    /// <summary>Gets the full data health report.</summary>
    /// <param name="accountId">Optional account filter.</param>
    /// <returns>Data health report DTO, or null on failure.</returns>
    Task<DataHealthReportDto?> GetDataHealthReportAsync(Guid? accountId = null);

    /// <summary>Gets duplicate transaction clusters.</summary>
    /// <param name="accountId">Optional account filter.</param>
    /// <returns>Duplicate clusters, or null on failure.</returns>
    Task<IReadOnlyList<DuplicateClusterDto>?> GetDuplicatesAsync(Guid? accountId = null);

    /// <summary>Gets amount outlier transactions.</summary>
    /// <param name="accountId">Optional account filter.</param>
    /// <returns>Amount outliers, or null on failure.</returns>
    Task<IReadOnlyList<AmountOutlierDto>?> GetOutliersAsync(Guid? accountId = null);

    /// <summary>Gets date gaps in transaction history.</summary>
    /// <param name="accountId">Optional account filter.</param>
    /// <param name="minGapDays">Minimum gap size to report.</param>
    /// <returns>Date gaps, or null on failure.</returns>
    Task<IReadOnlyList<DateGapDto>?> GetDateGapsAsync(Guid? accountId = null, int minGapDays = 7);

    /// <summary>Gets summary of uncategorized transactions.</summary>
    /// <returns>Uncategorized summary, or null on failure.</returns>
    Task<UncategorizedSummaryDto?> GetUncategorizedSummaryAsync();

    /// <summary>Merges duplicate transactions into a primary transaction.</summary>
    /// <param name="request">The merge request.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task MergeDuplicatesAsync(MergeDuplicatesRequest request);

    /// <summary>Dismisses a transaction outlier.</summary>
    /// <param name="transactionId">The transaction identifier to dismiss.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DismissOutlierAsync(Guid transactionId);
}
