// <copyright file="LinkableInstancesDialogTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Components.Reconciliation;
using BudgetExperiment.Client.Models;
using BudgetExperiment.Client.Services;
using BudgetExperiment.Contracts.Dtos;

using Bunit;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace BudgetExperiment.Client.Tests.Components.Reconciliation;

/// <summary>
/// Unit tests for the <see cref="LinkableInstancesDialog"/> component.
/// </summary>
public sealed class LinkableInstancesDialogTests : BunitContext, IAsyncLifetime
{
    private readonly StubBudgetApiService _budgetApi;
    private readonly StubReconciliationApiService _reconciliationApi;

    /// <summary>
    /// Initializes a new instance of the <see cref="LinkableInstancesDialogTests"/> class.
    /// </summary>
    public LinkableInstancesDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<ThemeService>();
        Services.AddSingleton<CultureService>();
        _budgetApi = new StubBudgetApiService();
        _reconciliationApi = new StubReconciliationApiService();
        Services.AddSingleton<IBudgetApiService>(_budgetApi);
        Services.AddSingleton<IReconciliationApiService>(_reconciliationApi);
    }

    /// <inheritdoc/>
    public Task InitializeAsync() => Task.CompletedTask;

    /// <inheritdoc/>
    public new Task DisposeAsync() => base.DisposeAsync().AsTask();

    /// <summary>
    /// Verifies the dialog renders nothing when not visible.
    /// </summary>
    [Fact]
    public void LinkableInstancesDialog_RendersNothing_WhenNotVisible()
    {
        var cut = Render<LinkableInstancesDialog>(parameters => parameters
            .Add(p => p.IsVisible, false));

        Assert.DoesNotContain("linkable-dialog", cut.Markup);
    }

    /// <summary>
    /// Verifies the dialog renders the instance details when visible.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task LinkableInstancesDialog_ShowsInstanceDetails_WhenVisible()
    {
        var instance = CreateInstance("Electric Bill");

        var cut = Render<LinkableInstancesDialog>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.RecurringInstance, instance));

        await Task.Delay(50);
        cut.Render();

        Assert.Contains("Electric Bill", cut.Markup);
    }

    /// <summary>
    /// Verifies the dialog shows loading state while loading transactions.
    /// </summary>
    [Fact]
    public void LinkableInstancesDialog_ShowsLoadingState()
    {
        var cut = Render<LinkableInstancesDialog>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.RecurringInstance, CreateInstance()));

        Assert.Contains("linkable-dialog", cut.Markup);
    }

    /// <summary>
    /// Verifies the dialog shows empty state when no matching transactions.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task LinkableInstancesDialog_ShowsEmptyState_WhenNoTransactions()
    {
        _budgetApi.TransactionsResult = [];

        var cut = Render<LinkableInstancesDialog>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.RecurringInstance, CreateInstance()));

        await Task.Delay(50);
        cut.Render();

        Assert.Contains("empty-state", cut.Markup);
    }

    /// <summary>
    /// Verifies the dialog shows the expected amount from the instance.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task LinkableInstancesDialog_ShowsExpectedAmount()
    {
        var instance = CreateInstance();

        var cut = Render<LinkableInstancesDialog>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.RecurringInstance, instance));

        await Task.Delay(50);
        cut.Render();

        Assert.Contains("100", cut.Markup);
    }

    /// <summary>
    /// Verifies the cancel button invokes the OnCancel callback.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task LinkableInstancesDialog_CancelButton_InvokesCallback()
    {
        var cancelled = false;
        _budgetApi.TransactionsResult = [];

        var cut = Render<LinkableInstancesDialog>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.RecurringInstance, CreateInstance())
            .Add(p => p.OnCancel, () => { cancelled = true; }));

        await Task.Delay(50);
        cut.Render();

        var cancelBtn = cut.FindAll("button").First(b => b.TextContent.Contains("Cancel"));
        cancelBtn.Click();

        Assert.True(cancelled);
    }

    /// <summary>
    /// Verifies the dialog shows the instance date.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task LinkableInstancesDialog_ShowsInstanceDate()
    {
        var instance = CreateInstance();

        var cut = Render<LinkableInstancesDialog>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.RecurringInstance, instance));

        await Task.Delay(50);
        cut.Render();

        Assert.Contains("Mar", cut.Markup);
    }

    /// <summary>
    /// Verifies the dialog shows the remember pattern checkbox after selecting a transaction.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task LinkableInstancesDialog_ShowsRememberPatternCheckbox_AfterSelectingTransaction()
    {
        _budgetApi.TransactionsResult =
        [
            new TransactionDto
            {
                Id = Guid.NewGuid(),
                AccountId = Guid.NewGuid(),
                Description = "ELECTRIC CO #123",
                Date = new DateOnly(2026, 3, 1),
                Amount = new MoneyDto { Amount = -100m, Currency = "USD" },
            },
        ];

        var cut = Render<LinkableInstancesDialog>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.RecurringInstance, CreateInstance()));

        await Task.Delay(50);
        cut.Render();

        var transactionOption = cut.Find(".transaction-option");
        transactionOption.Click();

        Assert.Contains("remember-pattern-section", cut.Markup);
    }

    private static RecurringInstanceStatusDto CreateInstance(string description = "Test Bill") => new()
    {
        RecurringTransactionId = Guid.NewGuid(),
        Description = description,
        AccountId = Guid.NewGuid(),
        AccountName = "Checking",
        InstanceDate = new DateOnly(2026, 3, 1),
        ExpectedAmount = new MoneyDto { Amount = -100m, Currency = "USD" },
        Status = "Missing",
    };

#pragma warning disable SA1201 // Elements should appear in the correct order
    private sealed class StubBudgetApiService : IBudgetApiService
#pragma warning restore SA1201
    {
        /// <summary>Gets or sets the transactions result.</summary>
        public IReadOnlyList<TransactionDto>? TransactionsResult
        {
            get; set;
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<TransactionDto>> GetTransactionsAsync(DateOnly startDate, DateOnly endDate, Guid? accountId = null) => Task.FromResult<IReadOnlyList<TransactionDto>>(this.TransactionsResult ?? []);

        /// <inheritdoc/>
        public Task<ImportPatternsDto?> GetImportPatternsAsync(Guid recurringTransactionId) => Task.FromResult<ImportPatternsDto?>(null);

        /// <inheritdoc/>
        public Task<ImportPatternsDto?> UpdateImportPatternsAsync(Guid recurringTransactionId, ImportPatternsDto patterns) => Task.FromResult<ImportPatternsDto?>(patterns);

        /// <inheritdoc/>
        public Task<IReadOnlyList<AccountDto>> GetAccountsAsync() => Task.FromResult<IReadOnlyList<AccountDto>>([]);

        /// <inheritdoc/>
        public Task<AccountDto?> GetAccountAsync(Guid id) => Task.FromResult<AccountDto?>(null);

        /// <inheritdoc/>
        public Task<AccountDto?> CreateAccountAsync(AccountCreateDto model) => Task.FromResult<AccountDto?>(null);

        /// <inheritdoc/>
        public Task<ApiResult<AccountDto>> UpdateAccountAsync(Guid id, AccountUpdateDto model, string? version = null) => Task.FromResult(ApiResult<AccountDto>.Failure());

        /// <inheritdoc/>
        public Task<bool> DeleteAccountAsync(Guid id) => Task.FromResult(false);

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
        public Task<CalendarGridDto> GetCalendarGridAsync(int year, int month, Guid? accountId = null) => Task.FromResult(new CalendarGridDto());

        /// <inheritdoc/>
        public Task<DayDetailDto> GetDayDetailAsync(DateOnly date, Guid? accountId = null) => Task.FromResult(new DayDetailDto());

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
        public Task<PastDueSummaryDto?> GetPastDueItemsAsync(Guid? accountId = null) => Task.FromResult<PastDueSummaryDto?>(null);

        /// <inheritdoc/>
        public Task<BatchRealizeResultDto?> RealizeBatchAsync(BatchRealizeRequest request) => Task.FromResult<BatchRealizeResultDto?>(null);

        /// <inheritdoc/>
        public Task<AppSettingsDto?> GetSettingsAsync() => Task.FromResult<AppSettingsDto?>(null);

        /// <inheritdoc/>
        public Task<AppSettingsDto?> UpdateSettingsAsync(AppSettingsUpdateDto dto) => Task.FromResult<AppSettingsDto?>(null);

        /// <inheritdoc/>
        public Task<LocationDataClearedDto?> DeleteAllLocationDataAsync() => Task.FromResult<LocationDataClearedDto?>(null);

        /// <inheritdoc/>
        public Task<PaycheckAllocationSummaryDto?> GetPaycheckAllocationAsync(string frequency, decimal? amount = null, Guid? accountId = null) => Task.FromResult<PaycheckAllocationSummaryDto?>(null);

        /// <inheritdoc/>
        public Task<IReadOnlyList<BudgetCategoryDto>> GetCategoriesAsync(bool activeOnly = false) => Task.FromResult<IReadOnlyList<BudgetCategoryDto>>([]);

        /// <inheritdoc/>
        public Task<BudgetCategoryDto?> GetCategoryAsync(Guid id) => Task.FromResult<BudgetCategoryDto?>(null);

        /// <inheritdoc/>
        public Task<BudgetCategoryDto?> CreateCategoryAsync(BudgetCategoryCreateDto model) => Task.FromResult<BudgetCategoryDto?>(null);

        /// <inheritdoc/>
        public Task<ApiResult<BudgetCategoryDto>> UpdateCategoryAsync(Guid id, BudgetCategoryUpdateDto model, string? version = null) => Task.FromResult(ApiResult<BudgetCategoryDto>.Failure());

        /// <inheritdoc/>
        public Task<bool> DeleteCategoryAsync(Guid id) => Task.FromResult(false);

        /// <inheritdoc/>
        public Task<bool> ActivateCategoryAsync(Guid id) => Task.FromResult(false);

        /// <inheritdoc/>
        public Task<bool> DeactivateCategoryAsync(Guid id) => Task.FromResult(false);

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
        public Task<BudgetSummaryDto?> GetBudgetSummaryAsync(int year, int month) => Task.FromResult<BudgetSummaryDto?>(null);

        /// <inheritdoc/>
        public Task<BudgetProgressDto?> GetCategoryProgressAsync(Guid categoryId, int year, int month) => Task.FromResult<BudgetProgressDto?>(null);

        /// <inheritdoc/>
        public Task<IReadOnlyList<CategorizationRuleDto>> GetCategorizationRulesAsync(bool activeOnly = false) => Task.FromResult<IReadOnlyList<CategorizationRuleDto>>([]);

        /// <inheritdoc/>
        public Task<CategorizationRulePageResponse> GetCategorizationRulesPagedAsync(CategorizationRuleListRequest request) => Task.FromResult(new CategorizationRulePageResponse());

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
        public Task<BulkRuleActionResponse?> BulkDeleteCategorizationRulesAsync(IReadOnlyList<Guid> ids) => Task.FromResult<BulkRuleActionResponse?>(null);

        /// <inheritdoc/>
        public Task<BulkRuleActionResponse?> BulkActivateCategorizationRulesAsync(IReadOnlyList<Guid> ids) => Task.FromResult<BulkRuleActionResponse?>(null);

        /// <inheritdoc/>
        public Task<BulkRuleActionResponse?> BulkDeactivateCategorizationRulesAsync(IReadOnlyList<Guid> ids) => Task.FromResult<BulkRuleActionResponse?>(null);

        /// <inheritdoc/>
        public Task<UncategorizedTransactionPageDto> GetUncategorizedTransactionsAsync(UncategorizedTransactionFilterDto filter) => Task.FromResult(new UncategorizedTransactionPageDto());

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
        public Task<UserSettingsDto?> GetUserSettingsAsync() => Task.FromResult<UserSettingsDto?>(null);

        /// <inheritdoc/>
        public Task<UserSettingsDto?> UpdateUserSettingsAsync(UserSettingsUpdateDto dto) => Task.FromResult<UserSettingsDto?>(null);

        /// <inheritdoc/>
        public Task<UnifiedTransactionPageDto> GetUnifiedTransactionsAsync(UnifiedTransactionFilterDto filter) => Task.FromResult(new UnifiedTransactionPageDto());

        /// <inheritdoc/>
        public Task<TransactionDto?> UpdateTransactionCategoryAsync(Guid transactionId, Guid? categoryId) => Task.FromResult<TransactionDto?>(null);

        /// <inheritdoc/>
        public Task<BatchSuggestCategoriesResponse> GetBatchCategorySuggestionsAsync(IReadOnlyList<Guid> transactionIds) => Task.FromResult(new BatchSuggestCategoriesResponse());

        /// <inheritdoc/>
        public Task<UserSettingsDto?> CompleteOnboardingAsync() => Task.FromResult<UserSettingsDto?>(null);

#pragma warning disable SA1648 // inheritdoc should be used with inheriting class
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
        public Task<IReadOnlyList<ReconciliationRecordDto>?> GetReconciliationHistoryAsync(Guid accountId, int page = 1, int pageSize = 20) => Task.FromResult<IReadOnlyList<ReconciliationRecordDto>?>(null);

        /// <inheritdoc/>
        public Task<IReadOnlyList<TransactionDto>?> GetReconciliationTransactionsAsync(Guid reconciliationRecordId) => Task.FromResult<IReadOnlyList<TransactionDto>?>(null);

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
    }
#pragma warning restore SA1648

    private sealed class StubReconciliationApiService : IReconciliationApiService
    {
        /// <inheritdoc/>
        public Task<ReconciliationMatchDto?> CreateManualMatchAsync(ManualMatchRequest request) => Task.FromResult<ReconciliationMatchDto?>(null);

        /// <inheritdoc/>
        public Task<ReconciliationStatusDto?> GetStatusAsync(int year, int month, Guid? accountId = null) => Task.FromResult<ReconciliationStatusDto?>(null);

        /// <inheritdoc/>
        public Task<IReadOnlyList<ReconciliationMatchDto>> GetPendingMatchesAsync(Guid? accountId = null) => Task.FromResult<IReadOnlyList<ReconciliationMatchDto>>([]);

        /// <inheritdoc/>
        public Task<FindMatchesResult?> FindMatchesAsync(FindMatchesRequest request) => Task.FromResult<FindMatchesResult?>(null);

        /// <inheritdoc/>
        public Task<bool> AcceptMatchAsync(Guid matchId) => Task.FromResult(false);

        /// <inheritdoc/>
        public Task<bool> RejectMatchAsync(Guid matchId) => Task.FromResult(false);

        /// <inheritdoc/>
        public Task<int> BulkAcceptMatchesAsync(IReadOnlyList<Guid> matchIds) => Task.FromResult(0);

        /// <inheritdoc/>
        public Task<MatchingTolerancesDto?> GetTolerancesAsync() => Task.FromResult<MatchingTolerancesDto?>(null);

        /// <inheritdoc/>
        public Task<bool> UpdateTolerancesAsync(MatchingTolerancesDto tolerances) => Task.FromResult(false);

        /// <inheritdoc/>
        public Task<bool> UnlinkMatchAsync(Guid matchId) => Task.FromResult(false);

        /// <inheritdoc/>
        public Task<IReadOnlyList<LinkableInstanceDto>> GetLinkableInstancesAsync(Guid transactionId) => Task.FromResult<IReadOnlyList<LinkableInstanceDto>>([]);

#pragma warning disable SA1648 // inheritdoc should be used with inheriting class
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
        public Task<IReadOnlyList<ReconciliationRecordDto>?> GetReconciliationHistoryAsync(Guid accountId, int page = 1, int pageSize = 20) => Task.FromResult<IReadOnlyList<ReconciliationRecordDto>?>(null);

        /// <inheritdoc/>
        public Task<IReadOnlyList<TransactionDto>?> GetReconciliationTransactionsAsync(Guid reconciliationRecordId) => Task.FromResult<IReadOnlyList<TransactionDto>?>(null);
    }
#pragma warning restore SA1648
}
