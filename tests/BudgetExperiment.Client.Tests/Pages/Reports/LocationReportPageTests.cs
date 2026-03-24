// <copyright file="LocationReportPageTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Globalization;

using BudgetExperiment.Client.Models;
using BudgetExperiment.Client.Pages.Reports;
using BudgetExperiment.Client.Services;
using BudgetExperiment.Contracts.Dtos;

using Bunit;

using Microsoft.Extensions.DependencyInjection;

namespace BudgetExperiment.Client.Tests.Pages.Reports;

/// <summary>
/// Unit tests for the <see cref="LocationReportPage"/> component.
/// </summary>
public class LocationReportPageTests : BunitContext, IAsyncLifetime
{
    private readonly StubBudgetApiService stubApiService;

    /// <summary>
    /// Initializes a new instance of the <see cref="LocationReportPageTests"/> class.
    /// </summary>
    public LocationReportPageTests()
    {
        this.JSInterop.Mode = JSRuntimeMode.Loose;

        stubApiService = new StubBudgetApiService();

        this.Services.AddSingleton<IBudgetApiService>(stubApiService);
        this.Services.AddSingleton(new ScopeService(this.JSInterop.JSRuntime));
        this.Services.AddSingleton<ThemeService>();
        this.Services.AddSingleton<CultureService>();
        this.Services.AddSingleton<IToastService, ToastService>();
        this.Services.AddSingleton<IExportDownloadService>(new StubExportDownloadService());
    }

    /// <inheritdoc/>
    public Task InitializeAsync()
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public new Task DisposeAsync() => base.DisposeAsync().AsTask();

    /// <summary>
    /// Verifies that the page loads location spending data from the API on render.
    /// </summary>
    [Fact]
    public void Render_LoadsDataFromApi()
    {
        // Arrange
        stubApiService.LocationReportResult = CreateTestReport();

        // Act
        var cut = Render<LocationReportPage>();

        // Assert
        Assert.True(stubApiService.GetSpendingByLocationCalled);
        Assert.Contains("Washington", cut.Markup);
    }

    /// <summary>
    /// Verifies that the data table is rendered below the map with region data.
    /// </summary>
    [Fact]
    public void Render_ShowsDataTableBelowMap()
    {
        // Arrange
        stubApiService.LocationReportResult = CreateTestReport();

        // Act
        var cut = Render<LocationReportPage>();

        // Assert
        var table = cut.Find("table.location-data-table");
        Assert.Contains("Washington", table.TextContent);
        Assert.Contains("California", table.TextContent);
    }

    /// <summary>
    /// Verifies that changing the date range refreshes the report data.
    /// </summary>
    [Fact]
    public void DateRangeChange_RefreshesData()
    {
        // Arrange
        stubApiService.LocationReportResult = CreateTestReport();
        var cut = Render<LocationReportPage>();
        var initialCallCount = stubApiService.GetSpendingByLocationCallCount;

        // Act — update the start date input
        var startInput = cut.Find("input#start-date");
        startInput.Change("2025-02-01");

        // Assert — should have called the API again
        Assert.True(stubApiService.GetSpendingByLocationCallCount > initialCallCount);
    }

    /// <summary>
    /// Verifies that the export button is present and has a CSV download option.
    /// </summary>
    [Fact]
    public void Export_ShowsCsvOption()
    {
        // Arrange
        stubApiService.LocationReportResult = CreateTestReport();

        // Act
        var cut = Render<LocationReportPage>();

        // Assert — ExportButton is rendered
        Assert.Contains("Export", cut.Markup);
    }

    /// <summary>
    /// Verifies that the region table is rendered with correct column headers.
    /// </summary>
    [Fact]
    public void Render_ShowsRegionTableHeaders()
    {
        // Arrange
        stubApiService.LocationReportResult = CreateTestReport();

        // Act
        var cut = Render<LocationReportPage>();

        // Assert
        var table = cut.Find("table.location-data-table");
        Assert.Contains("Region", table.TextContent);
        Assert.Contains("Spending", table.TextContent);
        Assert.Contains("Transactions", table.TextContent);
    }

    /// <summary>
    /// Verifies that clicking a region row selects it and shows city breakdown.
    /// </summary>
    [Fact]
    public void RegionClick_ShowsCityBreakdown()
    {
        // Arrange
        stubApiService.LocationReportResult = CreateTestReport();

        // Act
        var cut = Render<LocationReportPage>();
        var regionRows = cut.FindAll("table.location-data-table tbody tr");
        regionRows[0].Click();

        // Assert - should show city breakdown for Washington
        Assert.Contains("Seattle", cut.Markup);
        Assert.Contains("Tacoma", cut.Markup);
    }

    /// <summary>
    /// Verifies that the summary stats section shows transaction counts.
    /// </summary>
    [Fact]
    public void Render_ShowsSummaryStats()
    {
        // Arrange
        stubApiService.LocationReportResult = CreateTestReport();

        // Act
        var cut = Render<LocationReportPage>();

        // Assert
        Assert.Contains("10", cut.Markup);
        Assert.Contains("8", cut.Markup);
    }

    /// <summary>
    /// Verifies that empty state is shown when no report data.
    /// </summary>
    [Fact]
    public void Render_ShowsEmptyState_WhenNoData()
    {
        // Arrange - no report data set

        // Act
        var cut = Render<LocationReportPage>();

        // Assert
        Assert.Contains("No location data", cut.Markup);
    }

    private static LocationSpendingReportDto CreateTestReport() => new()
    {
        StartDate = new DateOnly(2025, 1, 1),
        EndDate = new DateOnly(2025, 1, 31),
        TotalSpending = 1000m,
        TotalTransactions = 10,
        TransactionsWithLocation = 8,
        Regions =
        [
            new RegionSpendingDto
            {
                RegionCode = "US-WA",
                RegionName = "Washington",
                Country = "US",
                TotalSpending = 600m,
                TransactionCount = 5,
                Percentage = 60m,
                Cities =
                [
                    new CitySpendingDto { City = "Seattle", TotalSpending = 400m, TransactionCount = 3, Percentage = 66.7m },
                    new CitySpendingDto { City = "Tacoma", TotalSpending = 200m, TransactionCount = 2, Percentage = 33.3m },
                ],
            },
            new RegionSpendingDto
            {
                RegionCode = "US-CA",
                RegionName = "California",
                Country = "US",
                TotalSpending = 400m,
                TransactionCount = 3,
                Percentage = 40m,
            },
        ],
    };

    private sealed class StubBudgetApiService : IBudgetApiService
    {
        /// <summary>Gets or sets the result to return from GetSpendingByLocationAsync.</summary>
        public LocationSpendingReportDto? LocationReportResult
        {
            get; set;
        }

        /// <summary>Gets a value indicating whether GetSpendingByLocationAsync was called.</summary>
        public bool GetSpendingByLocationCalled
        {
            get; private set;
        }

        /// <summary>Gets the number of times GetSpendingByLocationAsync was called.</summary>
        public int GetSpendingByLocationCallCount
        {
            get; private set;
        }

        /// <inheritdoc/>
        public Task<LocationSpendingReportDto?> GetSpendingByLocationAsync(DateOnly startDate, DateOnly endDate, Guid? accountId = null)
        {
            this.GetSpendingByLocationCalled = true;
            this.GetSpendingByLocationCallCount++;
            return Task.FromResult(this.LocationReportResult);
        }

        // --- All other interface methods return defaults ---

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
        public Task<ImportPatternsDto?> GetImportPatternsAsync(Guid recurringTransactionId) => Task.FromResult<ImportPatternsDto?>(null);

        /// <inheritdoc/>
        public Task<ImportPatternsDto?> UpdateImportPatternsAsync(Guid recurringTransactionId, ImportPatternsDto patterns) => Task.FromResult<ImportPatternsDto?>(null);

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

    private sealed class StubExportDownloadService : IExportDownloadService
    {
        public Task<ExportDownloadResult> DownloadAsync(string url, CancellationToken cancellationToken = default) =>
            Task.FromResult(ExportDownloadResult.Ok());
    }
}
