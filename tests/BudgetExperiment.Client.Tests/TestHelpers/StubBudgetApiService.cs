// <copyright file="StubBudgetApiService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Models;
using BudgetExperiment.Client.Services;
using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Client.Tests.TestHelpers;

/// <summary>
/// Shared stub implementation of <see cref="IBudgetApiService"/> for page-level bUnit tests.
/// All methods return safe defaults (empty lists, null, false). Override individual methods
/// via <see langword="virtual"/> to customize behavior for specific tests.
/// </summary>
internal class StubBudgetApiService : IBudgetApiService
{
    /// <summary>
    /// Gets the list of accounts that will be returned by <see cref="GetAccountsAsync"/>.
    /// </summary>
    public List<AccountDto> Accounts { get; } = new();

    /// <summary>
    /// Gets the list of categories that will be returned by <see cref="GetCategoriesAsync"/>.
    /// </summary>
    public List<BudgetCategoryDto> Categories { get; } = new();

    /// <summary>
    /// Gets or sets the calendar grid that will be returned by <see cref="GetCalendarGridAsync"/>.
    /// </summary>
    public CalendarGridDto CalendarGrid { get; set; } = new();

    /// <summary>
    /// Gets or sets the day detail that will be returned by <see cref="GetDayDetailAsync"/>.
    /// </summary>
    public DayDetailDto DayDetail { get; set; } = new();

    /// <summary>
    /// Gets or sets the budget summary that will be returned by <see cref="GetBudgetSummaryAsync"/>.
    /// </summary>
    public BudgetSummaryDto? BudgetSummary
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the past due summary that will be returned by <see cref="GetPastDueItemsAsync"/>.
    /// </summary>
    public PastDueSummaryDto? PastDueSummary
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="DeleteAccountAsync"/> returns true.
    /// </summary>
    public bool DeleteAccountResult
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="DeleteCategoryAsync"/> returns true.
    /// </summary>
    public bool DeleteCategoryResult
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="ActivateCategoryAsync"/> returns true.
    /// </summary>
    public bool ActivateCategoryResult
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="DeactivateCategoryAsync"/> returns true.
    /// </summary>
    public bool DeactivateCategoryResult
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the account returned by <see cref="CreateAccountAsync"/>.
    /// </summary>
    public AccountDto? CreateAccountResult
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the result returned by <see cref="UpdateAccountAsync"/>.
    /// </summary>
    public ApiResult<AccountDto>? UpdateAccountResult
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the category returned by <see cref="CreateCategoryAsync"/>.
    /// </summary>
    public BudgetCategoryDto? CreateCategoryResult
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the result returned by <see cref="UpdateCategoryAsync"/>.
    /// </summary>
    public ApiResult<BudgetCategoryDto>? UpdateCategoryResult
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the category returned by <see cref="GetCategoryAsync"/>.
    /// </summary>
    public BudgetCategoryDto? GetCategoryResult
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets an exception to throw from <see cref="GetAccountsAsync"/>.
    /// </summary>
    public Exception? GetAccountsException
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets an exception to throw from <see cref="GetCategoriesAsync"/>.
    /// </summary>
    public Exception? GetCategoriesException
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets an exception to throw from <see cref="CreateCategoryAsync"/>.
    /// </summary>
    public Exception? CreateCategoryException
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets an exception to throw from <see cref="UpdateCategoryAsync"/>.
    /// </summary>
    public Exception? UpdateCategoryException
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets an exception to throw from <see cref="DeleteCategoryAsync"/>.
    /// </summary>
    public Exception? DeleteCategoryException
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets an exception to throw from <see cref="ActivateCategoryAsync"/>.
    /// </summary>
    public Exception? ActivateCategoryException
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets an exception to throw from <see cref="DeactivateCategoryAsync"/>.
    /// </summary>
    public Exception? DeactivateCategoryException
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the app settings returned by <see cref="GetSettingsAsync"/>.
    /// </summary>
    public AppSettingsDto? AppSettings
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the user settings returned by <see cref="GetUserSettingsAsync"/>.
    /// </summary>
    public UserSettingsDto? UserSettings
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the transaction list returned by <see cref="GetAccountTransactionListAsync"/>.
    /// </summary>
    public TransactionListDto? TransactionList
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="GetAccountTransactionListAsync"/> should throw.
    /// </summary>
    public bool ShouldThrowOnGetTransactionList
    {
        get; set;
    }

    /// <summary>
    /// Gets the list of categorization rules returned by <see cref="GetCategorizationRulesAsync"/>.
    /// </summary>
    public List<CategorizationRuleDto> Rules { get; } = new();

    /// <summary>
    /// Gets or sets the rule returned by <see cref="CreateCategorizationRuleAsync"/>.
    /// </summary>
    public CategorizationRuleDto? CreateRuleResult
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the result returned by <see cref="UpdateCategorizationRuleAsync"/>.
    /// </summary>
    public ApiResult<CategorizationRuleDto>? UpdateRuleResult
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="DeleteCategorizationRuleAsync"/> returns true.
    /// </summary>
    public bool DeleteRuleResult
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="ActivateCategorizationRuleAsync"/> returns true.
    /// </summary>
    public bool ActivateRuleResult
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="DeactivateCategorizationRuleAsync"/> returns true.
    /// </summary>
    public bool DeactivateRuleResult
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets an exception to throw from <see cref="GetCategorizationRulesAsync"/>.
    /// </summary>
    public Exception? GetRulesException
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets an exception to throw from <see cref="CreateCategorizationRuleAsync"/>.
    /// </summary>
    public Exception? CreateRuleException
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets an exception to throw from <see cref="UpdateCategorizationRuleAsync"/>.
    /// </summary>
    public Exception? UpdateRuleException
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets an exception to throw from <see cref="DeleteCategorizationRuleAsync"/>.
    /// </summary>
    public Exception? DeleteRuleException
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets an exception to throw from <see cref="ActivateCategorizationRuleAsync"/>.
    /// </summary>
    public Exception? ActivateRuleException
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets an exception to throw from <see cref="DeactivateCategorizationRuleAsync"/>.
    /// </summary>
    public Exception? DeactivateRuleException
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets an exception to throw from <see cref="TestPatternAsync"/>.
    /// </summary>
    public Exception? TestPatternException
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets an exception to throw from <see cref="GetBudgetSummaryAsync"/>.
    /// </summary>
    public Exception? GetBudgetSummaryException
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets an exception to throw from <see cref="SetBudgetGoalAsync"/>.
    /// </summary>
    public Exception? SetBudgetGoalException
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets an exception to throw from <see cref="DeleteBudgetGoalAsync"/>.
    /// </summary>
    public Exception? DeleteBudgetGoalException
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the result returned by <see cref="SetBudgetGoalAsync"/>.
    /// </summary>
    public ApiResult<BudgetGoalDto>? SetBudgetGoalResult
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="DeleteBudgetGoalAsync"/> returns true.
    /// </summary>
    public bool DeleteBudgetGoalResult
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the transaction returned by <see cref="CreateTransactionAsync"/>.
    /// </summary>
    public TransactionDto? CreateTransactionResult
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the result returned by <see cref="UpdateTransactionAsync"/>.
    /// </summary>
    public ApiResult<TransactionDto>? UpdateTransactionResult
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="DeleteTransactionAsync"/> returns true.
    /// </summary>
    public bool DeleteTransactionResult
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets an exception to throw from <see cref="DeleteTransactionAsync"/>.
    /// </summary>
    public Exception? DeleteTransactionException
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the result returned by <see cref="BulkCategorizeTransactionsAsync"/>.
    /// </summary>
    public BulkCategorizeResponse BulkCategorizeResult { get; set; } = new();

    /// <summary>
    /// Gets or sets an exception to throw from <see cref="BulkCategorizeTransactionsAsync"/>.
    /// </summary>
    public Exception? BulkCategorizeException
    {
        get; set;
    }

    /// <summary>
    /// Gets the last bulk categorize request sent.
    /// </summary>
    public BulkCategorizeRequest? LastBulkCategorizeRequest
    {
        get; private set;
    }

    /// <summary>
    /// Gets or sets the transfer returned by <see cref="CreateTransferAsync"/>.
    /// </summary>
    public TransferResponse? CreateTransferResult
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the result returned by <see cref="DeleteAllLocationDataAsync"/>.
    /// </summary>
    public LocationDataClearedDto? DeleteLocationDataResult
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the result returned by <see cref="CompleteOnboardingAsync"/>.
    /// </summary>
    public UserSettingsDto? CompleteOnboardingResult
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the result returned by <see cref="TestPatternAsync"/>.
    /// </summary>
    public TestPatternResponse? TestPatternResult
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the result returned by <see cref="ApplyCategorizationRulesAsync"/>.
    /// </summary>
    public ApplyRulesResponse? ApplyRulesResult
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the result returned by <see cref="BulkDeleteCategorizationRulesAsync"/>.
    /// </summary>
    public BulkRuleActionResponse? BulkDeleteResult
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the exception thrown by <see cref="BulkDeleteCategorizationRulesAsync"/>.
    /// </summary>
    public Exception? BulkDeleteException
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the result returned by <see cref="BulkActivateCategorizationRulesAsync"/>.
    /// </summary>
    public BulkRuleActionResponse? BulkActivateResult
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the exception thrown by <see cref="BulkActivateCategorizationRulesAsync"/>.
    /// </summary>
    public Exception? BulkActivateException
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the result returned by <see cref="BulkDeactivateCategorizationRulesAsync"/>.
    /// </summary>
    public BulkRuleActionResponse? BulkDeactivateResult
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the exception thrown by <see cref="BulkDeactivateCategorizationRulesAsync"/>.
    /// </summary>
    public Exception? BulkDeactivateException
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the result returned by <see cref="GetTransactionAsync"/>.
    /// </summary>
    public TransactionDto? GetTransactionResult
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the result returned by <see cref="CopyBudgetGoalsAsync"/>.
    /// </summary>
    public CopyBudgetGoalsResult? CopyBudgetGoalsResult
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the result returned by <see cref="RealizeBatchAsync"/>.
    /// </summary>
    public BatchRealizeResultDto? RealizeBatchResult
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the uncategorized transaction page returned by <see cref="GetUncategorizedTransactionsAsync"/>.
    /// </summary>
    public UncategorizedTransactionPageDto UncategorizedPage { get; set; } = new();

    /// <summary>
    /// Gets or sets the unified transaction page returned by <see cref="GetUnifiedTransactionsAsync"/>.
    /// </summary>
    public UnifiedTransactionPageDto UnifiedPage { get; set; } = new();

    /// <summary>
    /// Gets or sets the result returned by <see cref="UpdateTransactionCategoryAsync"/>.
    /// </summary>
    public TransactionDto? UpdateTransactionCategoryResult
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the last filter passed to <see cref="GetUnifiedTransactionsAsync"/>.
    /// </summary>
    public UnifiedTransactionFilterDto? LastUnifiedFilter
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the batch category suggestions returned by <see cref="GetBatchCategorySuggestionsAsync"/>.
    /// </summary>
    public BatchSuggestCategoriesResponse BatchSuggestionsResult { get; set; } = new();

    /// <summary>
    /// Gets or sets the paycheck allocation summary returned by <see cref="GetPaycheckAllocationAsync"/>.
    /// </summary>
    public PaycheckAllocationSummaryDto? AllocationSummary
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the result returned by <see cref="CreateRecurringTransferAsync"/>.
    /// </summary>
    public RecurringTransferDto? CreateRecurringTransferResult
    {
        get; set;
    }

    /// <summary>
    /// Gets the list of recurring transactions returned by <see cref="GetRecurringTransactionsAsync"/>.
    /// </summary>
    public List<RecurringTransactionDto> RecurringTransactions { get; } = new();

    /// <summary>
    /// Gets the list of recurring transfers returned by <see cref="GetRecurringTransfersAsync"/>.
    /// </summary>
    public List<RecurringTransferDto> RecurringTransfers { get; } = new();

    /// <summary>
    /// Gets the list of transfers returned by <see cref="GetTransfersAsync"/>.
    /// </summary>
    public List<TransferListItemResponse> Transfers { get; } = new();

    /// <summary>
    /// Gets or sets the exception to throw from <see cref="GetTransfersAsync"/>.
    /// </summary>
    public Exception? GetTransfersException
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the spending trends report returned by <see cref="GetSpendingTrendsAsync"/>.
    /// </summary>
    public SpendingTrendsReportDto? SpendingTrends
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the category report returned by <see cref="GetCategoryReportByRangeAsync"/>.
    /// </summary>
    public DateRangeCategoryReportDto? DateRangeCategoryReport
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="SkipRecurringInstanceAsync"/> returns true.
    /// </summary>
    public bool SkipRecurringInstanceResult
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the result returned by <see cref="ModifyRecurringInstanceAsync"/>.
    /// </summary>
    public ApiResult<RecurringInstanceDto>? ModifyRecurringInstanceResult
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the result returned by <see cref="UpdateTransactionLocationAsync"/>.
    /// </summary>
    public ApiResult<TransactionDto>? UpdateTransactionLocationResult
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="ClearTransactionLocationAsync"/> returns true.
    /// </summary>
    public bool ClearTransactionLocationResult
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the result returned by <see cref="RealizeRecurringTransactionAsync"/>.
    /// </summary>
    public TransactionDto? RealizeRecurringTransactionResult
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the exception thrown by <see cref="GetRecurringTransactionsAsync"/>.
    /// </summary>
    public Exception? GetRecurringTransactionsException
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the result returned by <see cref="CreateRecurringTransactionAsync"/>.
    /// </summary>
    public RecurringTransactionDto? CreateRecurringTransactionResult
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the result returned by <see cref="UpdateRecurringTransactionAsync"/>.
    /// </summary>
    public ApiResult<RecurringTransactionDto>? UpdateRecurringTransactionResult
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="DeleteRecurringTransactionAsync"/> returns true.
    /// </summary>
    public bool DeleteRecurringTransactionResult
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the result returned by <see cref="SkipNextRecurringAsync"/>.
    /// </summary>
    public RecurringTransactionDto? SkipNextRecurringResult
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the result returned by <see cref="PauseRecurringTransactionAsync"/>.
    /// </summary>
    public RecurringTransactionDto? PauseRecurringTransactionResult
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the result returned by <see cref="ResumeRecurringTransactionAsync"/>.
    /// </summary>
    public RecurringTransactionDto? ResumeRecurringTransactionResult
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the result returned by <see cref="UpdateRecurringTransferAsync"/>.
    /// </summary>
    public ApiResult<RecurringTransferDto>? UpdateRecurringTransferResult
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="DeleteRecurringTransferAsync"/> returns true.
    /// </summary>
    public bool DeleteRecurringTransferResult
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the result returned by <see cref="PauseRecurringTransferAsync"/>.
    /// </summary>
    public RecurringTransferDto? PauseRecurringTransferTransferResult
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the result returned by <see cref="ResumeRecurringTransferAsync"/>.
    /// </summary>
    public RecurringTransferDto? ResumeRecurringTransferTransferResult
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the result returned by <see cref="SkipNextRecurringTransferAsync"/>.
    /// </summary>
    public RecurringTransferDto? SkipNextRecurringTransferResult
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the exception thrown by <see cref="GetRecurringTransfersAsync"/>.
    /// </summary>
    public Exception? GetRecurringTransfersException
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the result returned by <see cref="UpdateTransferAsync"/>.
    /// </summary>
    public TransferResponse? UpdateTransferResult
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="DeleteTransferAsync"/> returns true.
    /// </summary>
    public bool DeleteTransferResult
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the exception to throw from <see cref="CreateTransferAsync"/>.
    /// </summary>
    public Exception? CreateTransferException
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the exception to throw from <see cref="UpdateTransferAsync"/>.
    /// </summary>
    public Exception? UpdateTransferException
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the exception to throw from <see cref="DeleteTransferException"/>.
    /// </summary>
    public Exception? DeleteTransferException
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the result returned by <see cref="UpdateSettingsAsync"/>.
    /// </summary>
    public AppSettingsDto? UpdateSettingsResult
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the result returned by <see cref="UpdateUserSettingsAsync"/>.
    /// </summary>
    public UserSettingsDto? UpdateUserSettingsResult
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets an exception to throw from <see cref="UpdateUserSettingsAsync"/>.
    /// </summary>
    public Exception? UpdateUserSettingsException
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets an exception to throw from <see cref="CompleteOnboardingAsync"/>.
    /// </summary>
    public Exception? CompleteOnboardingException
    {
        get; set;
    }

    /// <summary>
    /// Gets the list of reconciliation records returned by <see cref="GetReconciliationHistoryAsync"/>.
    /// </summary>
    public List<ReconciliationRecordDto> ReconciliationHistory { get; } = new();

    /// <summary>
    /// Gets the list of transactions returned by <see cref="GetReconciliationTransactionsAsync"/>.
    /// </summary>
    public List<TransactionDto> ReconciliationTransactions { get; } = new();

    /// <inheritdoc/>
    public Task<IReadOnlyList<AccountDto>> GetAccountsAsync()
    {
        if (this.GetAccountsException != null)
        {
            throw this.GetAccountsException;
        }

        return Task.FromResult<IReadOnlyList<AccountDto>>(this.Accounts);
    }

    /// <inheritdoc/>
    public Task<AccountDto?> GetAccountAsync(Guid id) => Task.FromResult<AccountDto?>(this.Accounts.Find(a => a.Id == id));

    /// <inheritdoc/>
    public Task<AccountDto?> CreateAccountAsync(AccountCreateDto model) => Task.FromResult(this.CreateAccountResult);

    /// <inheritdoc/>
    public Task<ApiResult<AccountDto>> UpdateAccountAsync(Guid id, AccountUpdateDto model, string? version = null) => Task.FromResult(this.UpdateAccountResult ?? ApiResult<AccountDto>.Failure());

    /// <inheritdoc/>
    public Task<bool> DeleteAccountAsync(Guid id) => Task.FromResult(this.DeleteAccountResult);

    /// <inheritdoc/>
    public Task<IReadOnlyList<TransactionDto>> GetTransactionsAsync(DateOnly startDate, DateOnly endDate, Guid? accountId = null, string? kakeiboCategory = null) => Task.FromResult<IReadOnlyList<TransactionDto>>([]);

    /// <inheritdoc/>
    public Task<TransactionDto?> GetTransactionAsync(Guid id) => Task.FromResult(this.GetTransactionResult);

    /// <inheritdoc/>
    public Task<TransactionDto?> CreateTransactionAsync(TransactionCreateDto model) => Task.FromResult(this.CreateTransactionResult);

    /// <inheritdoc/>
    public Task<ApiResult<TransactionDto>> UpdateTransactionAsync(Guid id, TransactionUpdateDto model, string? version = null) => Task.FromResult(this.UpdateTransactionResult ?? ApiResult<TransactionDto>.Failure());

    /// <inheritdoc/>
    public Task<bool> DeleteTransactionAsync(Guid id) => this.DeleteTransactionException != null
        ? Task.FromException<bool>(this.DeleteTransactionException)
        : Task.FromResult(this.DeleteTransactionResult);

    /// <inheritdoc/>
    public Task<ApiResult<TransactionDto>> UpdateTransactionLocationAsync(Guid id, TransactionLocationUpdateDto dto, string? version = null) => Task.FromResult(this.UpdateTransactionLocationResult ?? ApiResult<TransactionDto>.Failure());

    /// <inheritdoc/>
    public Task<bool> ClearTransactionLocationAsync(Guid id) => Task.FromResult(this.ClearTransactionLocationResult);

    /// <inheritdoc/>
    public Task<ReverseGeocodeResponseDto?> ReverseGeocodeAsync(decimal latitude, decimal longitude) => Task.FromResult<ReverseGeocodeResponseDto?>(null);

    /// <inheritdoc/>
    public Task<CalendarGridDto> GetCalendarGridAsync(int year, int month, Guid? accountId = null) => Task.FromResult(this.CalendarGrid);

    /// <inheritdoc/>
    public Task<DayDetailDto> GetDayDetailAsync(DateOnly date, Guid? accountId = null) => Task.FromResult(this.DayDetail);

    /// <inheritdoc/>
    public Task<TransactionListDto> GetAccountTransactionListAsync(Guid accountId, DateOnly startDate, DateOnly endDate, bool includeRecurring = true)
    {
        if (this.ShouldThrowOnGetTransactionList)
        {
            throw new HttpRequestException("Server error");
        }

        return Task.FromResult(this.TransactionList ?? new TransactionListDto());
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<DailyTotalDto>> GetCalendarSummaryAsync(int year, int month, Guid? accountId = null) => Task.FromResult<IReadOnlyList<DailyTotalDto>>([]);

    /// <inheritdoc/>
    public Task<IReadOnlyList<RecurringTransactionDto>> GetRecurringTransactionsAsync()
    {
        if (this.GetRecurringTransactionsException != null)
        {
            throw this.GetRecurringTransactionsException;
        }

        return Task.FromResult<IReadOnlyList<RecurringTransactionDto>>(this.RecurringTransactions);
    }

    /// <inheritdoc/>
    public Task<RecurringTransactionDto?> GetRecurringTransactionAsync(Guid id) => Task.FromResult<RecurringTransactionDto?>(null);

    /// <inheritdoc/>
    public Task<RecurringTransactionDto?> CreateRecurringTransactionAsync(RecurringTransactionCreateDto model) => Task.FromResult(this.CreateRecurringTransactionResult);

    /// <inheritdoc/>
    public Task<ApiResult<RecurringTransactionDto>> UpdateRecurringTransactionAsync(Guid id, RecurringTransactionUpdateDto model, string? version = null) => Task.FromResult(this.UpdateRecurringTransactionResult ?? ApiResult<RecurringTransactionDto>.Failure());

    /// <inheritdoc/>
    public Task<bool> DeleteRecurringTransactionAsync(Guid id) => Task.FromResult(this.DeleteRecurringTransactionResult);

    /// <inheritdoc/>
    public Task<RecurringTransactionDto?> PauseRecurringTransactionAsync(Guid id) => Task.FromResult(this.PauseRecurringTransactionResult);

    /// <inheritdoc/>
    public Task<RecurringTransactionDto?> ResumeRecurringTransactionAsync(Guid id) => Task.FromResult(this.ResumeRecurringTransactionResult);

    /// <inheritdoc/>
    public Task<RecurringTransactionDto?> SkipNextRecurringAsync(Guid id) => Task.FromResult(this.SkipNextRecurringResult);

    /// <inheritdoc/>
    public Task<IReadOnlyList<RecurringInstanceDto>> GetProjectedRecurringAsync(DateOnly from, DateOnly to, Guid? accountId = null) => Task.FromResult<IReadOnlyList<RecurringInstanceDto>>([]);

    /// <inheritdoc/>
    public Task<bool> SkipRecurringInstanceAsync(Guid id, DateOnly date) => Task.FromResult(this.SkipRecurringInstanceResult);

    /// <inheritdoc/>
    public Task<ApiResult<RecurringInstanceDto>> ModifyRecurringInstanceAsync(Guid id, DateOnly date, RecurringInstanceModifyDto model, string? version = null) => Task.FromResult(this.ModifyRecurringInstanceResult ?? ApiResult<RecurringInstanceDto>.Failure());

    /// <inheritdoc/>
    public Task<TransferResponse?> CreateTransferAsync(CreateTransferRequest model)
    {
        if (this.CreateTransferException != null)
        {
            throw this.CreateTransferException;
        }

        return Task.FromResult(this.CreateTransferResult);
    }

    /// <inheritdoc/>
    public Task<TransferResponse?> GetTransferAsync(Guid transferId) => Task.FromResult<TransferResponse?>(null);

    /// <inheritdoc/>
    public Task<IReadOnlyList<TransferListItemResponse>> GetTransfersAsync(Guid? accountId = null, DateOnly? from = null, DateOnly? to = null, int page = 1, int pageSize = 20)
    {
        if (this.GetTransfersException != null)
        {
            throw this.GetTransfersException;
        }

        return Task.FromResult<IReadOnlyList<TransferListItemResponse>>(this.Transfers);
    }

    /// <inheritdoc/>
    public Task<TransferResponse?> UpdateTransferAsync(Guid transferId, UpdateTransferRequest model)
    {
        if (this.UpdateTransferException != null)
        {
            throw this.UpdateTransferException;
        }

        return Task.FromResult(this.UpdateTransferResult);
    }

    /// <inheritdoc/>
    public Task<bool> DeleteTransferAsync(Guid transferId)
    {
        if (this.DeleteTransferException != null)
        {
            throw this.DeleteTransferException;
        }

        return Task.FromResult(this.DeleteTransferResult);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<RecurringTransferDto>> GetRecurringTransfersAsync(Guid? accountId = null)
    {
        if (this.GetRecurringTransfersException != null)
        {
            throw this.GetRecurringTransfersException;
        }

        return Task.FromResult<IReadOnlyList<RecurringTransferDto>>(this.RecurringTransfers);
    }

    /// <inheritdoc/>
    public Task<RecurringTransferDto?> GetRecurringTransferAsync(Guid id) => Task.FromResult<RecurringTransferDto?>(null);

    /// <inheritdoc/>
    public Task<RecurringTransferDto?> CreateRecurringTransferAsync(RecurringTransferCreateDto model) => Task.FromResult(this.CreateRecurringTransferResult);

    /// <inheritdoc/>
    public Task<ApiResult<RecurringTransferDto>> UpdateRecurringTransferAsync(Guid id, RecurringTransferUpdateDto model, string? version = null) => Task.FromResult(this.UpdateRecurringTransferResult ?? ApiResult<RecurringTransferDto>.Failure());

    /// <inheritdoc/>
    public Task<bool> DeleteRecurringTransferAsync(Guid id) => Task.FromResult(this.DeleteRecurringTransferResult);

    /// <inheritdoc/>
    public Task<RecurringTransferDto?> PauseRecurringTransferAsync(Guid id) => Task.FromResult(this.PauseRecurringTransferTransferResult);

    /// <inheritdoc/>
    public Task<RecurringTransferDto?> ResumeRecurringTransferAsync(Guid id) => Task.FromResult(this.ResumeRecurringTransferTransferResult);

    /// <inheritdoc/>
    public Task<RecurringTransferDto?> SkipNextRecurringTransferAsync(Guid id) => Task.FromResult(this.SkipNextRecurringTransferResult);

    /// <inheritdoc/>
    public Task<IReadOnlyList<RecurringTransferInstanceDto>> GetProjectedRecurringTransfersAsync(DateOnly from, DateOnly to, Guid? accountId = null) => Task.FromResult<IReadOnlyList<RecurringTransferInstanceDto>>([]);

    /// <inheritdoc/>
    public Task<bool> SkipRecurringTransferInstanceAsync(Guid id, DateOnly date) => Task.FromResult(false);

    /// <inheritdoc/>
    public Task<ApiResult<RecurringTransferInstanceDto>> ModifyRecurringTransferInstanceAsync(Guid id, DateOnly date, RecurringTransferInstanceModifyDto model, string? version = null) => Task.FromResult(ApiResult<RecurringTransferInstanceDto>.Failure());

    /// <inheritdoc/>
    public Task<TransactionDto?> RealizeRecurringTransactionAsync(Guid recurringTransactionId, RealizeRecurringTransactionRequest request) => Task.FromResult(this.RealizeRecurringTransactionResult);

    /// <inheritdoc/>
    public Task<TransferResponse?> RealizeRecurringTransferAsync(Guid recurringTransferId, RealizeRecurringTransferRequest request) => Task.FromResult<TransferResponse?>(null);

    /// <inheritdoc/>
    public Task<PastDueSummaryDto?> GetPastDueItemsAsync(Guid? accountId = null) => Task.FromResult(this.PastDueSummary);

    /// <inheritdoc/>
    public Task<BatchRealizeResultDto?> RealizeBatchAsync(BatchRealizeRequest request) => Task.FromResult(this.RealizeBatchResult);

    /// <inheritdoc/>
    public Task<AppSettingsDto?> GetSettingsAsync() => Task.FromResult(this.AppSettings);

    /// <inheritdoc/>
    public Task<AppSettingsDto?> UpdateSettingsAsync(AppSettingsUpdateDto dto) => Task.FromResult(this.UpdateSettingsResult);

    /// <inheritdoc/>
    public Task<LocationDataClearedDto?> DeleteAllLocationDataAsync() => Task.FromResult(this.DeleteLocationDataResult);

    /// <inheritdoc/>
    public Task<PaycheckAllocationSummaryDto?> GetPaycheckAllocationAsync(string frequency, decimal? amount = null, Guid? accountId = null) => Task.FromResult(this.AllocationSummary);

    /// <inheritdoc/>
    public Task<IReadOnlyList<BudgetCategoryDto>> GetCategoriesAsync(bool activeOnly = false) =>
        this.GetCategoriesException is not null
            ? Task.FromException<IReadOnlyList<BudgetCategoryDto>>(this.GetCategoriesException)
            : Task.FromResult<IReadOnlyList<BudgetCategoryDto>>(this.Categories);

    /// <inheritdoc/>
    public Task<BudgetCategoryDto?> GetCategoryAsync(Guid id) => Task.FromResult(this.GetCategoryResult ?? this.Categories.Find(c => c.Id == id));

    /// <inheritdoc/>
    public Task<BudgetCategoryDto?> CreateCategoryAsync(BudgetCategoryCreateDto model) =>
        this.CreateCategoryException is not null
            ? Task.FromException<BudgetCategoryDto?>(this.CreateCategoryException)
            : Task.FromResult(this.CreateCategoryResult);

    /// <inheritdoc/>
    public Task<ApiResult<BudgetCategoryDto>> UpdateCategoryAsync(Guid id, BudgetCategoryUpdateDto model, string? version = null) =>
        this.UpdateCategoryException is not null
            ? Task.FromException<ApiResult<BudgetCategoryDto>>(this.UpdateCategoryException)
            : Task.FromResult(this.UpdateCategoryResult ?? ApiResult<BudgetCategoryDto>.Failure());

    /// <inheritdoc/>
    public Task<bool> DeleteCategoryAsync(Guid id) =>
        this.DeleteCategoryException is not null
            ? Task.FromException<bool>(this.DeleteCategoryException)
            : Task.FromResult(this.DeleteCategoryResult);

    /// <inheritdoc/>
    public Task<bool> ActivateCategoryAsync(Guid id) =>
        this.ActivateCategoryException is not null
            ? Task.FromException<bool>(this.ActivateCategoryException)
            : Task.FromResult(this.ActivateCategoryResult);

    /// <inheritdoc/>
    public Task<bool> DeactivateCategoryAsync(Guid id) =>
        this.DeactivateCategoryException is not null
            ? Task.FromException<bool>(this.DeactivateCategoryException)
            : Task.FromResult(this.DeactivateCategoryResult);

    /// <inheritdoc/>
    public Task<IReadOnlyList<BudgetGoalDto>> GetBudgetGoalsAsync(int year, int month) => Task.FromResult<IReadOnlyList<BudgetGoalDto>>([]);

    /// <inheritdoc/>
    public Task<IReadOnlyList<BudgetGoalDto>> GetBudgetGoalsByCategoryAsync(Guid categoryId) => Task.FromResult<IReadOnlyList<BudgetGoalDto>>([]);

    /// <inheritdoc/>
    public Task<ApiResult<BudgetGoalDto>> SetBudgetGoalAsync(Guid categoryId, BudgetGoalSetDto model, string? version = null) =>
        this.SetBudgetGoalException is not null
            ? Task.FromException<ApiResult<BudgetGoalDto>>(this.SetBudgetGoalException)
            : Task.FromResult(this.SetBudgetGoalResult ?? ApiResult<BudgetGoalDto>.Failure());

    /// <inheritdoc/>
    public Task<bool> DeleteBudgetGoalAsync(Guid categoryId, int year, int month) =>
        this.DeleteBudgetGoalException is not null
            ? Task.FromException<bool>(this.DeleteBudgetGoalException)
            : Task.FromResult(this.DeleteBudgetGoalResult);

    /// <inheritdoc/>
    public Task<CopyBudgetGoalsResult?> CopyBudgetGoalsAsync(CopyBudgetGoalsRequest request) => Task.FromResult(this.CopyBudgetGoalsResult);

    /// <inheritdoc/>
    public Task<BudgetSummaryDto?> GetBudgetSummaryAsync(int year, int month) =>
        this.GetBudgetSummaryException is not null
            ? Task.FromException<BudgetSummaryDto?>(this.GetBudgetSummaryException)
            : Task.FromResult(this.BudgetSummary);

    /// <inheritdoc/>
    public Task<BudgetProgressDto?> GetCategoryProgressAsync(Guid categoryId, int year, int month) => Task.FromResult<BudgetProgressDto?>(null);

    /// <inheritdoc/>
    public Task<IReadOnlyList<CategorizationRuleDto>> GetCategorizationRulesAsync(bool activeOnly = false) =>
        this.GetRulesException is not null
            ? Task.FromException<IReadOnlyList<CategorizationRuleDto>>(this.GetRulesException)
            : Task.FromResult<IReadOnlyList<CategorizationRuleDto>>(this.Rules);

    /// <inheritdoc/>
    public Task<CategorizationRulePageResponse> GetCategorizationRulesPagedAsync(CategorizationRuleListRequest request) =>
        this.GetRulesException is not null
            ? Task.FromException<CategorizationRulePageResponse>(this.GetRulesException)
            : Task.FromResult(new CategorizationRulePageResponse
            {
                Items = this.Rules,
                TotalCount = this.Rules.Count,
                Page = request.Page,
                PageSize = request.PageSize,
            });

    /// <inheritdoc/>
    public Task<CategorizationRuleDto?> GetCategorizationRuleAsync(Guid id) => Task.FromResult<CategorizationRuleDto?>(null);

    /// <inheritdoc/>
    public Task<CategorizationRuleDto?> CreateCategorizationRuleAsync(CategorizationRuleCreateDto model) =>
        this.CreateRuleException is not null
            ? Task.FromException<CategorizationRuleDto?>(this.CreateRuleException)
            : Task.FromResult(this.CreateRuleResult);

    /// <inheritdoc/>
    public Task<ApiResult<CategorizationRuleDto>> UpdateCategorizationRuleAsync(Guid id, CategorizationRuleUpdateDto model, string? version = null) =>
        this.UpdateRuleException is not null
            ? Task.FromException<ApiResult<CategorizationRuleDto>>(this.UpdateRuleException)
            : Task.FromResult(this.UpdateRuleResult ?? ApiResult<CategorizationRuleDto>.Failure());

    /// <inheritdoc/>
    public Task<bool> DeleteCategorizationRuleAsync(Guid id) =>
        this.DeleteRuleException is not null
            ? Task.FromException<bool>(this.DeleteRuleException)
            : Task.FromResult(this.DeleteRuleResult);

    /// <inheritdoc/>
    public Task<bool> ActivateCategorizationRuleAsync(Guid id) =>
        this.ActivateRuleException is not null
            ? Task.FromException<bool>(this.ActivateRuleException)
            : Task.FromResult(this.ActivateRuleResult);

    /// <inheritdoc/>
    public Task<bool> DeactivateCategorizationRuleAsync(Guid id) =>
        this.DeactivateRuleException is not null
            ? Task.FromException<bool>(this.DeactivateRuleException)
            : Task.FromResult(this.DeactivateRuleResult);

    /// <inheritdoc/>
    public Task<TestPatternResponse?> TestPatternAsync(TestPatternRequest request) =>
        this.TestPatternException is not null
            ? Task.FromException<TestPatternResponse?>(this.TestPatternException)
            : Task.FromResult(this.TestPatternResult);

    /// <inheritdoc/>
    public Task<ApplyRulesResponse?> ApplyCategorizationRulesAsync(ApplyRulesRequest request) => Task.FromResult(this.ApplyRulesResult);

    /// <inheritdoc/>
    public Task<bool> ReorderCategorizationRulesAsync(IReadOnlyList<Guid> ruleIds) => Task.FromResult(false);

    /// <inheritdoc/>
    public Task<BulkRuleActionResponse?> BulkDeleteCategorizationRulesAsync(IReadOnlyList<Guid> ids) =>
        this.BulkDeleteException is not null
            ? Task.FromException<BulkRuleActionResponse?>(this.BulkDeleteException)
            : Task.FromResult(this.BulkDeleteResult);

    /// <inheritdoc/>
    public Task<BulkRuleActionResponse?> BulkActivateCategorizationRulesAsync(IReadOnlyList<Guid> ids) =>
        this.BulkActivateException is not null
            ? Task.FromException<BulkRuleActionResponse?>(this.BulkActivateException)
            : Task.FromResult(this.BulkActivateResult);

    /// <inheritdoc/>
    public Task<BulkRuleActionResponse?> BulkDeactivateCategorizationRulesAsync(IReadOnlyList<Guid> ids) =>
        this.BulkDeactivateException is not null
            ? Task.FromException<BulkRuleActionResponse?>(this.BulkDeactivateException)
            : Task.FromResult(this.BulkDeactivateResult);

    /// <inheritdoc/>
    public Task<UnifiedTransactionPageDto> GetUnifiedTransactionsAsync(UnifiedTransactionFilterDto filter)
    {
        this.LastUnifiedFilter = filter;
        return Task.FromResult(this.UnifiedPage);
    }

    /// <inheritdoc/>
    public Task<TransactionDto?> UpdateTransactionCategoryAsync(Guid transactionId, Guid? categoryId) => Task.FromResult(this.UpdateTransactionCategoryResult);

    /// <inheritdoc/>
    public Task<BatchSuggestCategoriesResponse> GetBatchCategorySuggestionsAsync(IReadOnlyList<Guid> transactionIds) => Task.FromResult(this.BatchSuggestionsResult);

    /// <inheritdoc/>
    public Task<UncategorizedTransactionPageDto> GetUncategorizedTransactionsAsync(UncategorizedTransactionFilterDto filter) => Task.FromResult(this.UncategorizedPage);

    /// <inheritdoc/>
    public Task<BulkCategorizeResponse> BulkCategorizeTransactionsAsync(BulkCategorizeRequest request)
    {
        this.LastBulkCategorizeRequest = request;
        return this.BulkCategorizeException != null
            ? Task.FromException<BulkCategorizeResponse>(this.BulkCategorizeException)
            : Task.FromResult(this.BulkCategorizeResult);
    }

    /// <inheritdoc/>
    public Task<MonthlyCategoryReportDto?> GetMonthlyCategoryReportAsync(int year, int month, bool groupByKakeibo = false) => Task.FromResult<MonthlyCategoryReportDto?>(null);

    /// <inheritdoc/>
    public Task<DateRangeCategoryReportDto?> GetCategoryReportByRangeAsync(DateOnly startDate, DateOnly endDate, Guid? accountId = null, bool groupByKakeibo = false) => Task.FromResult(this.DateRangeCategoryReport);

    /// <inheritdoc/>
    public Task<SpendingTrendsReportDto?> GetSpendingTrendsAsync(int months = 6, int? endYear = null, int? endMonth = null, Guid? categoryId = null, bool groupByKakeibo = false) => Task.FromResult(this.SpendingTrends);

    /// <inheritdoc/>
    public Task<BudgetSummaryDto?> GetBudgetComparisonReportAsync(int year, int month, bool groupByKakeibo = false) => Task.FromResult<BudgetSummaryDto?>(null);

    /// <inheritdoc/>
    public Task<DaySummaryDto?> GetDaySummaryAsync(DateOnly date, Guid? accountId = null) => Task.FromResult<DaySummaryDto?>(null);

    /// <inheritdoc/>
    public Task<LocationSpendingReportDto?> GetSpendingByLocationAsync(DateOnly startDate, DateOnly endDate, Guid? accountId = null) => Task.FromResult<LocationSpendingReportDto?>(null);

    /// <inheritdoc/>
    public Task<IReadOnlyList<CustomReportLayoutDto>> GetCustomReportLayoutsAsync() => Task.FromResult<IReadOnlyList<CustomReportLayoutDto>>([]);

    /// <inheritdoc/>
    public Task<CustomReportLayoutDto?> GetCustomReportLayoutAsync(Guid id) => Task.FromResult<CustomReportLayoutDto?>(null);

    /// <inheritdoc/>
    public Task<CustomReportLayoutDto?> CreateCustomReportLayoutAsync(CustomReportLayoutCreateDto dto) => Task.FromResult<CustomReportLayoutDto?>(null);

    /// <inheritdoc/>
    public Task<ApiResult<CustomReportLayoutDto>> UpdateCustomReportLayoutAsync(Guid id, CustomReportLayoutUpdateDto dto, string? version = null) => Task.FromResult(ApiResult<CustomReportLayoutDto>.Failure());

    /// <inheritdoc/>
    public Task<bool> DeleteCustomReportLayoutAsync(Guid id) => Task.FromResult(false);

    /// <inheritdoc/>
    public Task<ImportPatternsDto?> GetImportPatternsAsync(Guid recurringTransactionId) => Task.FromResult<ImportPatternsDto?>(null);

    /// <inheritdoc/>
    public Task<ImportPatternsDto?> UpdateImportPatternsAsync(Guid recurringTransactionId, ImportPatternsDto patterns) => Task.FromResult<ImportPatternsDto?>(null);

    /// <inheritdoc/>
    public Task<UserSettingsDto?> GetUserSettingsAsync() => Task.FromResult(this.UserSettings);

    /// <inheritdoc/>
    public Task<UserSettingsDto?> UpdateUserSettingsAsync(UserSettingsUpdateDto dto)
    {
        if (this.UpdateUserSettingsException != null)
        {
            throw this.UpdateUserSettingsException;
        }

        return Task.FromResult(this.UpdateUserSettingsResult);
    }

    /// <inheritdoc/>
    public Task<UserSettingsDto?> CompleteOnboardingAsync()
    {
        if (this.CompleteOnboardingException != null)
        {
            throw this.CompleteOnboardingException;
        }

        return Task.FromResult(this.CompleteOnboardingResult);
    }

    /// <inheritdoc/>
    public Task<TransactionDto?> MarkTransactionClearedAsync(MarkClearedRequest request) => Task.FromResult<TransactionDto?>(null);

    /// <inheritdoc/>
    public Task<TransactionDto?> MarkTransactionUnclearedAsync(MarkUnclearedRequest request) => Task.FromResult<TransactionDto?>(null);

    /// <inheritdoc/>
    public Task<IReadOnlyList<TransactionDto>?> BulkMarkTransactionsClearedAsync(BulkMarkClearedRequest request) => Task.FromResult<IReadOnlyList<TransactionDto>?>(null);

    /// <inheritdoc/>
    public Task<IReadOnlyList<TransactionDto>?> BulkMarkTransactionsUnclearedAsync(BulkMarkUnclearedRequest request) => Task.FromResult<IReadOnlyList<TransactionDto>?>(null);

    /// <inheritdoc/>
    public Task<StatementBalanceDto?> GetActiveStatementBalanceAsync(Guid accountId) => Task.FromResult<StatementBalanceDto?>(null);

    /// <inheritdoc/>
    public Task<ClearedBalanceDto?> GetClearedBalanceAsync(Guid accountId, DateOnly? upToDate = null) => Task.FromResult<ClearedBalanceDto?>(null);

    /// <inheritdoc/>
    public Task<StatementBalanceDto?> SetStatementBalanceAsync(SetStatementBalanceRequest request) => Task.FromResult<StatementBalanceDto?>(null);

    /// <inheritdoc/>
    public Task<ApiResult<ReconciliationRecordDto>> CompleteReconciliationAsync(CompleteReconciliationRequest request) => Task.FromResult(ApiResult<ReconciliationRecordDto>.Failure());

    /// <inheritdoc/>
    public Task<IReadOnlyList<ReconciliationRecordDto>?> GetReconciliationHistoryAsync(Guid accountId, int page = 1, int pageSize = 20) =>
        Task.FromResult<IReadOnlyList<ReconciliationRecordDto>?>(ReconciliationHistory.Count > 0 ? ReconciliationHistory : null);

    /// <inheritdoc/>
    public Task<IReadOnlyList<TransactionDto>?> GetReconciliationTransactionsAsync(Guid reconciliationRecordId) =>
        Task.FromResult<IReadOnlyList<TransactionDto>?>(ReconciliationTransactions.Count > 0 ? ReconciliationTransactions : null);

    /// <inheritdoc/>
    public Task<DataHealthReportDto?> GetDataHealthReportAsync(Guid? accountId = null) => Task.FromResult<DataHealthReportDto?>(null);

    /// <inheritdoc/>
    public Task<IReadOnlyList<DuplicateClusterDto>?> GetDuplicatesAsync(Guid? accountId = null) => Task.FromResult<IReadOnlyList<DuplicateClusterDto>?>(null);

    /// <inheritdoc/>
    public Task<IReadOnlyList<AmountOutlierDto>?> GetOutliersAsync(Guid? accountId = null) => Task.FromResult<IReadOnlyList<AmountOutlierDto>?>(null);

    /// <inheritdoc/>
    public Task<IReadOnlyList<DateGapDto>?> GetDateGapsAsync(Guid? accountId = null, int minGapDays = 7) => Task.FromResult<IReadOnlyList<DateGapDto>?>(null);

    /// <inheritdoc/>
    public Task<UncategorizedSummaryDto?> GetUncategorizedSummaryAsync() => Task.FromResult<UncategorizedSummaryDto?>(null);

    /// <inheritdoc/>
    public Task MergeDuplicatesAsync(MergeDuplicatesRequest request) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task DismissOutlierAsync(Guid transactionId) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task<MonthlyReflectionDto?> GetReflectionByMonthAsync(int year, int month) => Task.FromResult<MonthlyReflectionDto?>(null);

    /// <inheritdoc/>
    public Task<MonthlyReflectionDto?> CreateOrUpdateReflectionAsync(int year, int month, CreateOrUpdateMonthlyReflectionDto dto) => Task.FromResult<MonthlyReflectionDto?>(null);

    /// <inheritdoc/>
    public Task<MonthFinancialSummaryDto?> GetMonthFinancialSummaryAsync(int year, int month) => Task.FromResult<MonthFinancialSummaryDto?>(null);

    /// <inheritdoc/>
    public Task<bool> DeleteReflectionAsync(Guid reflectionId) => Task.FromResult(false);

    /// <inheritdoc/>
    public Task<bool> MarkKakeiboSetupCompleteAsync() => Task.FromResult(true);

    /// <inheritdoc/>
    public Task<KaizenGoalDto?> GetKaizenGoalByWeekAsync(DateOnly weekStart) => Task.FromResult<KaizenGoalDto?>(null);

    /// <inheritdoc/>
    public Task<KaizenGoalDto?> CreateKaizenGoalAsync(DateOnly weekStart, CreateKaizenGoalDto dto) => Task.FromResult<KaizenGoalDto?>(null);

    /// <inheritdoc/>
    public Task<KaizenGoalDto?> UpdateKaizenGoalAsync(Guid goalId, UpdateKaizenGoalDto dto) => Task.FromResult<KaizenGoalDto?>(null);

    /// <inheritdoc/>
    public Task<bool> DeleteKaizenGoalAsync(Guid goalId) => Task.FromResult(false);

    /// <inheritdoc/>
    public Task<IReadOnlyList<KaizenGoalDto>?> GetKaizenGoalsRangeAsync(DateOnly from, DateOnly to) => Task.FromResult<IReadOnlyList<KaizenGoalDto>?>(null);

    /// <inheritdoc />
    public virtual Task<KaizenDashboardDto?> GetKaizenDashboardAsync(int weeks = 12, CancellationToken ct = default)
        => Task.FromResult<KaizenDashboardDto?>(null);

    /// <inheritdoc />
    public virtual Task<HeatmapDataResponse?> GetCalendarHeatmapAsync(int year, int month, CancellationToken ct = default)
        => Task.FromResult<HeatmapDataResponse?>(null);
}
