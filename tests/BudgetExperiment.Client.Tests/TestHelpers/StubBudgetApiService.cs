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
    public BudgetSummaryDto? BudgetSummary { get; set; }

    /// <summary>
    /// Gets or sets the past due summary that will be returned by <see cref="GetPastDueItemsAsync"/>.
    /// </summary>
    public PastDueSummaryDto? PastDueSummary { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="DeleteAccountAsync"/> returns true.
    /// </summary>
    public bool DeleteAccountResult { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="DeleteCategoryAsync"/> returns true.
    /// </summary>
    public bool DeleteCategoryResult { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="ActivateCategoryAsync"/> returns true.
    /// </summary>
    public bool ActivateCategoryResult { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="DeactivateCategoryAsync"/> returns true.
    /// </summary>
    public bool DeactivateCategoryResult { get; set; }

    /// <summary>
    /// Gets or sets the account returned by <see cref="CreateAccountAsync"/>.
    /// </summary>
    public AccountDto? CreateAccountResult { get; set; }

    /// <summary>
    /// Gets or sets the result returned by <see cref="UpdateAccountAsync"/>.
    /// </summary>
    public ApiResult<AccountDto>? UpdateAccountResult { get; set; }

    /// <summary>
    /// Gets or sets the category returned by <see cref="CreateCategoryAsync"/>.
    /// </summary>
    public BudgetCategoryDto? CreateCategoryResult { get; set; }

    /// <summary>
    /// Gets or sets the result returned by <see cref="UpdateCategoryAsync"/>.
    /// </summary>
    public ApiResult<BudgetCategoryDto>? UpdateCategoryResult { get; set; }

    /// <summary>
    /// Gets or sets the category returned by <see cref="GetCategoryAsync"/>.
    /// </summary>
    public BudgetCategoryDto? GetCategoryResult { get; set; }

    /// <summary>
    /// Gets or sets the app settings returned by <see cref="GetSettingsAsync"/>.
    /// </summary>
    public AppSettingsDto? AppSettings { get; set; }

    /// <summary>
    /// Gets or sets the user settings returned by <see cref="GetUserSettingsAsync"/>.
    /// </summary>
    public UserSettingsDto? UserSettings { get; set; }

    /// <summary>
    /// Gets the list of categorization rules returned by <see cref="GetCategorizationRulesAsync"/>.
    /// </summary>
    public List<CategorizationRuleDto> Rules { get; } = new();

    /// <summary>
    /// Gets or sets the uncategorized transaction page returned by <see cref="GetUncategorizedTransactionsAsync"/>.
    /// </summary>
    public UncategorizedTransactionPageDto UncategorizedPage { get; set; } = new();

    /// <summary>
    /// Gets or sets the paycheck allocation summary returned by <see cref="GetPaycheckAllocationAsync"/>.
    /// </summary>
    public PaycheckAllocationSummaryDto? AllocationSummary { get; set; }

    /// <inheritdoc/>
    public Task<IReadOnlyList<AccountDto>> GetAccountsAsync() => Task.FromResult<IReadOnlyList<AccountDto>>(this.Accounts);

    /// <inheritdoc/>
    public Task<AccountDto?> GetAccountAsync(Guid id) => Task.FromResult<AccountDto?>(this.Accounts.Find(a => a.Id == id));

    /// <inheritdoc/>
    public Task<AccountDto?> CreateAccountAsync(AccountCreateDto model) => Task.FromResult(this.CreateAccountResult);

    /// <inheritdoc/>
    public Task<ApiResult<AccountDto>> UpdateAccountAsync(Guid id, AccountUpdateDto model, string? version = null) => Task.FromResult(this.UpdateAccountResult ?? ApiResult<AccountDto>.Failure());

    /// <inheritdoc/>
    public Task<bool> DeleteAccountAsync(Guid id) => Task.FromResult(this.DeleteAccountResult);

    /// <inheritdoc/>
    public Task<IReadOnlyList<TransactionDto>> GetTransactionsAsync(DateOnly startDate, DateOnly endDate, Guid? accountId = null) => Task.FromResult<IReadOnlyList<TransactionDto>>([]);

    /// <inheritdoc/>
    public Task<TransactionDto?> GetTransactionAsync(Guid id) => Task.FromResult<TransactionDto?>(null);

    /// <inheritdoc/>
    public Task<TransactionDto?> CreateTransactionAsync(TransactionCreateDto model) => Task.FromResult<TransactionDto?>(null);

    /// <inheritdoc/>
    public Task<ApiResult<TransactionDto>> UpdateTransactionAsync(Guid id, TransactionUpdateDto model, string? version = null) => Task.FromResult(ApiResult<TransactionDto>.Failure());

    /// <inheritdoc/>
    public Task<bool> DeleteTransactionAsync(Guid id) => Task.FromResult(false);

    /// <inheritdoc/>
    public Task<ApiResult<TransactionDto>> UpdateTransactionLocationAsync(Guid id, TransactionLocationUpdateDto dto, string? version = null) => Task.FromResult(ApiResult<TransactionDto>.Failure());

    /// <inheritdoc/>
    public Task<bool> ClearTransactionLocationAsync(Guid id) => Task.FromResult(false);

    /// <inheritdoc/>
    public Task<ReverseGeocodeResponseDto?> ReverseGeocodeAsync(decimal latitude, decimal longitude) => Task.FromResult<ReverseGeocodeResponseDto?>(null);

    /// <inheritdoc/>
    public Task<CalendarGridDto> GetCalendarGridAsync(int year, int month, Guid? accountId = null) => Task.FromResult(this.CalendarGrid);

    /// <inheritdoc/>
    public Task<DayDetailDto> GetDayDetailAsync(DateOnly date, Guid? accountId = null) => Task.FromResult(this.DayDetail);

    /// <inheritdoc/>
    public Task<TransactionListDto> GetAccountTransactionListAsync(Guid accountId, DateOnly startDate, DateOnly endDate, bool includeRecurring = true) => Task.FromResult(new TransactionListDto());

    /// <inheritdoc/>
    public Task<IReadOnlyList<DailyTotalDto>> GetCalendarSummaryAsync(int year, int month, Guid? accountId = null) => Task.FromResult<IReadOnlyList<DailyTotalDto>>([]);

    /// <inheritdoc/>
    public Task<IReadOnlyList<RecurringTransactionDto>> GetRecurringTransactionsAsync() => Task.FromResult<IReadOnlyList<RecurringTransactionDto>>([]);

    /// <inheritdoc/>
    public Task<RecurringTransactionDto?> GetRecurringTransactionAsync(Guid id) => Task.FromResult<RecurringTransactionDto?>(null);

    /// <inheritdoc/>
    public Task<RecurringTransactionDto?> CreateRecurringTransactionAsync(RecurringTransactionCreateDto model) => Task.FromResult<RecurringTransactionDto?>(null);

    /// <inheritdoc/>
    public Task<ApiResult<RecurringTransactionDto>> UpdateRecurringTransactionAsync(Guid id, RecurringTransactionUpdateDto model, string? version = null) => Task.FromResult(ApiResult<RecurringTransactionDto>.Failure());

    /// <inheritdoc/>
    public Task<bool> DeleteRecurringTransactionAsync(Guid id) => Task.FromResult(false);

    /// <inheritdoc/>
    public Task<RecurringTransactionDto?> PauseRecurringTransactionAsync(Guid id) => Task.FromResult<RecurringTransactionDto?>(null);

    /// <inheritdoc/>
    public Task<RecurringTransactionDto?> ResumeRecurringTransactionAsync(Guid id) => Task.FromResult<RecurringTransactionDto?>(null);

    /// <inheritdoc/>
    public Task<RecurringTransactionDto?> SkipNextRecurringAsync(Guid id) => Task.FromResult<RecurringTransactionDto?>(null);

    /// <inheritdoc/>
    public Task<IReadOnlyList<RecurringInstanceDto>> GetProjectedRecurringAsync(DateOnly from, DateOnly to, Guid? accountId = null) => Task.FromResult<IReadOnlyList<RecurringInstanceDto>>([]);

    /// <inheritdoc/>
    public Task<bool> SkipRecurringInstanceAsync(Guid id, DateOnly date) => Task.FromResult(false);

    /// <inheritdoc/>
    public Task<ApiResult<RecurringInstanceDto>> ModifyRecurringInstanceAsync(Guid id, DateOnly date, RecurringInstanceModifyDto model, string? version = null) => Task.FromResult(ApiResult<RecurringInstanceDto>.Failure());

    /// <inheritdoc/>
    public Task<TransferResponse?> CreateTransferAsync(CreateTransferRequest model) => Task.FromResult<TransferResponse?>(null);

    /// <inheritdoc/>
    public Task<TransferResponse?> GetTransferAsync(Guid transferId) => Task.FromResult<TransferResponse?>(null);

    /// <inheritdoc/>
    public Task<IReadOnlyList<TransferListItemResponse>> GetTransfersAsync(Guid? accountId = null, DateOnly? from = null, DateOnly? to = null, int page = 1, int pageSize = 20) => Task.FromResult<IReadOnlyList<TransferListItemResponse>>([]);

    /// <inheritdoc/>
    public Task<TransferResponse?> UpdateTransferAsync(Guid transferId, UpdateTransferRequest model) => Task.FromResult<TransferResponse?>(null);

    /// <inheritdoc/>
    public Task<bool> DeleteTransferAsync(Guid transferId) => Task.FromResult(false);

    /// <inheritdoc/>
    public Task<IReadOnlyList<RecurringTransferDto>> GetRecurringTransfersAsync(Guid? accountId = null) => Task.FromResult<IReadOnlyList<RecurringTransferDto>>([]);

    /// <inheritdoc/>
    public Task<RecurringTransferDto?> GetRecurringTransferAsync(Guid id) => Task.FromResult<RecurringTransferDto?>(null);

    /// <inheritdoc/>
    public Task<RecurringTransferDto?> CreateRecurringTransferAsync(RecurringTransferCreateDto model) => Task.FromResult<RecurringTransferDto?>(null);

    /// <inheritdoc/>
    public Task<ApiResult<RecurringTransferDto>> UpdateRecurringTransferAsync(Guid id, RecurringTransferUpdateDto model, string? version = null) => Task.FromResult(ApiResult<RecurringTransferDto>.Failure());

    /// <inheritdoc/>
    public Task<bool> DeleteRecurringTransferAsync(Guid id) => Task.FromResult(false);

    /// <inheritdoc/>
    public Task<RecurringTransferDto?> PauseRecurringTransferAsync(Guid id) => Task.FromResult<RecurringTransferDto?>(null);

    /// <inheritdoc/>
    public Task<RecurringTransferDto?> ResumeRecurringTransferAsync(Guid id) => Task.FromResult<RecurringTransferDto?>(null);

    /// <inheritdoc/>
    public Task<RecurringTransferDto?> SkipNextRecurringTransferAsync(Guid id) => Task.FromResult<RecurringTransferDto?>(null);

    /// <inheritdoc/>
    public Task<IReadOnlyList<RecurringTransferInstanceDto>> GetProjectedRecurringTransfersAsync(DateOnly from, DateOnly to, Guid? accountId = null) => Task.FromResult<IReadOnlyList<RecurringTransferInstanceDto>>([]);

    /// <inheritdoc/>
    public Task<bool> SkipRecurringTransferInstanceAsync(Guid id, DateOnly date) => Task.FromResult(false);

    /// <inheritdoc/>
    public Task<ApiResult<RecurringTransferInstanceDto>> ModifyRecurringTransferInstanceAsync(Guid id, DateOnly date, RecurringTransferInstanceModifyDto model, string? version = null) => Task.FromResult(ApiResult<RecurringTransferInstanceDto>.Failure());

    /// <inheritdoc/>
    public Task<TransactionDto?> RealizeRecurringTransactionAsync(Guid recurringTransactionId, RealizeRecurringTransactionRequest request) => Task.FromResult<TransactionDto?>(null);

    /// <inheritdoc/>
    public Task<TransferResponse?> RealizeRecurringTransferAsync(Guid recurringTransferId, RealizeRecurringTransferRequest request) => Task.FromResult<TransferResponse?>(null);

    /// <inheritdoc/>
    public Task<PastDueSummaryDto?> GetPastDueItemsAsync(Guid? accountId = null) => Task.FromResult(this.PastDueSummary);

    /// <inheritdoc/>
    public Task<BatchRealizeResultDto?> RealizeBatchAsync(BatchRealizeRequest request) => Task.FromResult<BatchRealizeResultDto?>(null);

    /// <inheritdoc/>
    public Task<AppSettingsDto?> GetSettingsAsync() => Task.FromResult(this.AppSettings);

    /// <inheritdoc/>
    public Task<AppSettingsDto?> UpdateSettingsAsync(AppSettingsUpdateDto dto) => Task.FromResult<AppSettingsDto?>(null);

    /// <inheritdoc/>
    public Task<LocationDataClearedDto?> DeleteAllLocationDataAsync() => Task.FromResult<LocationDataClearedDto?>(null);

    /// <inheritdoc/>
    public Task<PaycheckAllocationSummaryDto?> GetPaycheckAllocationAsync(string frequency, decimal? amount = null, Guid? accountId = null) => Task.FromResult(this.AllocationSummary);

    /// <inheritdoc/>
    public Task<IReadOnlyList<BudgetCategoryDto>> GetCategoriesAsync(bool activeOnly = false) => Task.FromResult<IReadOnlyList<BudgetCategoryDto>>(this.Categories);

    /// <inheritdoc/>
    public Task<BudgetCategoryDto?> GetCategoryAsync(Guid id) => Task.FromResult(this.GetCategoryResult ?? this.Categories.Find(c => c.Id == id));

    /// <inheritdoc/>
    public Task<BudgetCategoryDto?> CreateCategoryAsync(BudgetCategoryCreateDto model) => Task.FromResult(this.CreateCategoryResult);

    /// <inheritdoc/>
    public Task<ApiResult<BudgetCategoryDto>> UpdateCategoryAsync(Guid id, BudgetCategoryUpdateDto model, string? version = null) => Task.FromResult(this.UpdateCategoryResult ?? ApiResult<BudgetCategoryDto>.Failure());

    /// <inheritdoc/>
    public Task<bool> DeleteCategoryAsync(Guid id) => Task.FromResult(this.DeleteCategoryResult);

    /// <inheritdoc/>
    public Task<bool> ActivateCategoryAsync(Guid id) => Task.FromResult(this.ActivateCategoryResult);

    /// <inheritdoc/>
    public Task<bool> DeactivateCategoryAsync(Guid id) => Task.FromResult(this.DeactivateCategoryResult);

    /// <inheritdoc/>
    public Task<IReadOnlyList<BudgetGoalDto>> GetBudgetGoalsAsync(int year, int month) => Task.FromResult<IReadOnlyList<BudgetGoalDto>>([]);

    /// <inheritdoc/>
    public Task<IReadOnlyList<BudgetGoalDto>> GetBudgetGoalsByCategoryAsync(Guid categoryId) => Task.FromResult<IReadOnlyList<BudgetGoalDto>>([]);

    /// <inheritdoc/>
    public Task<ApiResult<BudgetGoalDto>> SetBudgetGoalAsync(Guid categoryId, BudgetGoalSetDto model, string? version = null) => Task.FromResult(ApiResult<BudgetGoalDto>.Failure());

    /// <inheritdoc/>
    public Task<bool> DeleteBudgetGoalAsync(Guid categoryId, int year, int month) => Task.FromResult(false);

    /// <inheritdoc/>
    public Task<CopyBudgetGoalsResult?> CopyBudgetGoalsAsync(CopyBudgetGoalsRequest request) => Task.FromResult<CopyBudgetGoalsResult?>(null);

    /// <inheritdoc/>
    public Task<BudgetSummaryDto?> GetBudgetSummaryAsync(int year, int month) => Task.FromResult(this.BudgetSummary);

    /// <inheritdoc/>
    public Task<BudgetProgressDto?> GetCategoryProgressAsync(Guid categoryId, int year, int month) => Task.FromResult<BudgetProgressDto?>(null);

    /// <inheritdoc/>
    public Task<IReadOnlyList<CategorizationRuleDto>> GetCategorizationRulesAsync(bool activeOnly = false) => Task.FromResult<IReadOnlyList<CategorizationRuleDto>>(this.Rules);

    /// <inheritdoc/>
    public Task<CategorizationRuleDto?> GetCategorizationRuleAsync(Guid id) => Task.FromResult<CategorizationRuleDto?>(null);

    /// <inheritdoc/>
    public Task<CategorizationRuleDto?> CreateCategorizationRuleAsync(CategorizationRuleCreateDto model) => Task.FromResult<CategorizationRuleDto?>(null);

    /// <inheritdoc/>
    public Task<ApiResult<CategorizationRuleDto>> UpdateCategorizationRuleAsync(Guid id, CategorizationRuleUpdateDto model, string? version = null) => Task.FromResult(ApiResult<CategorizationRuleDto>.Failure());

    /// <inheritdoc/>
    public Task<bool> DeleteCategorizationRuleAsync(Guid id) => Task.FromResult(false);

    /// <inheritdoc/>
    public Task<bool> ActivateCategorizationRuleAsync(Guid id) => Task.FromResult(false);

    /// <inheritdoc/>
    public Task<bool> DeactivateCategorizationRuleAsync(Guid id) => Task.FromResult(false);

    /// <inheritdoc/>
    public Task<TestPatternResponse?> TestPatternAsync(TestPatternRequest request) => Task.FromResult<TestPatternResponse?>(null);

    /// <inheritdoc/>
    public Task<ApplyRulesResponse?> ApplyCategorizationRulesAsync(ApplyRulesRequest request) => Task.FromResult<ApplyRulesResponse?>(null);

    /// <inheritdoc/>
    public Task<bool> ReorderCategorizationRulesAsync(IReadOnlyList<Guid> ruleIds) => Task.FromResult(false);

    /// <inheritdoc/>
    public Task<UncategorizedTransactionPageDto> GetUncategorizedTransactionsAsync(UncategorizedTransactionFilterDto filter) => Task.FromResult(this.UncategorizedPage);

    /// <inheritdoc/>
    public Task<BulkCategorizeResponse> BulkCategorizeTransactionsAsync(BulkCategorizeRequest request) => Task.FromResult(new BulkCategorizeResponse());

    /// <inheritdoc/>
    public Task<MonthlyCategoryReportDto?> GetMonthlyCategoryReportAsync(int year, int month) => Task.FromResult<MonthlyCategoryReportDto?>(null);

    /// <inheritdoc/>
    public Task<DateRangeCategoryReportDto?> GetCategoryReportByRangeAsync(DateOnly startDate, DateOnly endDate, Guid? accountId = null) => Task.FromResult<DateRangeCategoryReportDto?>(null);

    /// <inheritdoc/>
    public Task<SpendingTrendsReportDto?> GetSpendingTrendsAsync(int months = 6, int? endYear = null, int? endMonth = null, Guid? categoryId = null) => Task.FromResult<SpendingTrendsReportDto?>(null);

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
    public Task<UserSettingsDto?> UpdateUserSettingsAsync(UserSettingsUpdateDto dto) => Task.FromResult<UserSettingsDto?>(null);

    /// <inheritdoc/>
    public Task<UserSettingsDto?> CompleteOnboardingAsync() => Task.FromResult<UserSettingsDto?>(null);
}
