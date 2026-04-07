// <copyright file="ReportsDashboardTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

// NOTE: These tests are intentionally RED until Lucius creates:
//   - BudgetExperiment.Client.Pages.Reports.ReportsDashboard (Razor component)
//     located in src/.../Pages/Reports/ReportsDashboard.razor
// They define the expected HTML structure and async-loading contract for
// Feature 127, Slice 10 — the aggregate reports dashboard page.
// JSInterop is set to Loose so Blazor-ApexCharts JS calls do not throw in bUnit.
// FakeChartDataService returns empty data synchronously so tests 3–6 do not
// need WaitForState/WaitForElement: async completes in the same render cycle.
using System.Globalization;

using BudgetExperiment.Client.Components.Charts.Models;
using BudgetExperiment.Client.Models;
using BudgetExperiment.Client.Pages.Reports;
using BudgetExperiment.Client.Services;
using BudgetExperiment.Contracts.Dtos;

using Bunit;

using Microsoft.Extensions.DependencyInjection;

namespace BudgetExperiment.Client.Tests.Pages.Reports;

/// <summary>
/// Unit tests for the <see cref="ReportsDashboard"/> page component (Feature 127, Slice 10).
/// Tests are RED until Lucius implements the component at Pages/Reports/ReportsDashboard.razor.
/// </summary>
public class ReportsDashboardTests : BunitContext, IAsyncLifetime
{
    private readonly FakeBudgetApiService _fakeApiService = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ReportsDashboardTests"/> class.
    /// Registers all services required by ReportsDashboard and sets JSInterop to Loose.
    /// </summary>
    public ReportsDashboardTests()
    {
        this.JSInterop.Mode = JSRuntimeMode.Loose;
        this.Services.AddSingleton<IBudgetApiService>(this._fakeApiService);
        this.Services.AddSingleton<IChartDataService>(new FakeChartDataService());
        this.Services.AddSingleton(new ScopeService(this.JSInterop.JSRuntime));
        this.Services.AddSingleton<ThemeService>();
        this.Services.AddSingleton<CultureService>();
        this.Services.AddSingleton<IToastService, ToastService>();
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
    /// Verifies the dashboard renders without exception and the root container div is present.
    /// </summary>
    [Fact]
    public void ReportsDashboard_Renders_Container()
    {
        // Act
        var cut = Render<ReportsDashboard>();

        // Assert
        var container = cut.Find(".reports-dashboard");
        Assert.NotNull(container);
    }

    /// <summary>
    /// Verifies the loading state element is visible while async data is still pending.
    /// </summary>
    [Fact]
    public void ReportsDashboard_Shows_LoadingState_Initially()
    {
        // Arrange — block the first API call so OnInitializedAsync stays pending
        this._fakeApiService.MonthlyCategoryTaskSource = new TaskCompletionSource<MonthlyCategoryReportDto?>();

        // Act
        var cut = Render<ReportsDashboard>();

        // Assert
        var loading = cut.Find(".dashboard-loading");
        Assert.NotNull(loading);
    }

    /// <summary>
    /// Verifies the treemap section wrapper is present after data loads.
    /// </summary>
    [Fact]
    public void ReportsDashboard_Renders_TreemapSection()
    {
        // Act — fake returns synchronously; async completes in the same render cycle
        var cut = Render<ReportsDashboard>();

        // Assert
        var section = cut.Find(".dashboard-treemap");
        Assert.NotNull(section);
    }

    /// <summary>
    /// Verifies the waterfall section wrapper is present after data loads.
    /// </summary>
    [Fact]
    public void ReportsDashboard_Renders_WaterfallSection()
    {
        // Act
        var cut = Render<ReportsDashboard>();

        // Assert
        var section = cut.Find(".dashboard-waterfall");
        Assert.NotNull(section);
    }

    /// <summary>
    /// Verifies the category filter section is present after data loads.
    /// </summary>
    [Fact]
    public void ReportsDashboard_Renders_FilterSection()
    {
        // Act
        var cut = Render<ReportsDashboard>();

        // Assert
        var filter = cut.Find(".dashboard-filter");
        Assert.NotNull(filter);
    }

    /// <summary>
    /// Verifies the radial bar section wrapper is present after data loads.
    /// </summary>
    [Fact]
    public void ReportsDashboard_Renders_RadialBarSection()
    {
        // Act
        var cut = Render<ReportsDashboard>();

        // Assert
        var section = cut.Find(".dashboard-radial-bar");
        Assert.NotNull(section);
    }

    /// <summary>
    /// Verifies the radar section wrapper is present after data loads.
    /// </summary>
    [Fact]
    public void ReportsDashboard_Renders_RadarSection()
    {
        // Act
        var cut = Render<ReportsDashboard>();

        // Assert
        var section = cut.Find(".dashboard-radar");
        Assert.NotNull(section);
    }

    /// <summary>
    /// Verifies the heatmap section wrapper is present after data loads.
    /// </summary>
    [Fact]
    public void ReportsDashboard_Renders_HeatmapSection()
    {
        // Act
        var cut = Render<ReportsDashboard>();

        // Assert
        var section = cut.Find(".dashboard-heatmap");
        Assert.NotNull(section);
    }

    /// <summary>
    /// Verifies the line chart section wrapper is present after data loads.
    /// The section must contain a LineChart bound to trend monthly net amounts.
    /// </summary>
    [Fact]
    public void ReportsDashboard_Renders_LineChartSection()
    {
        // Act
        var cut = Render<ReportsDashboard>();

        // Assert
        var section = cut.Find(".dashboard-line-chart");
        Assert.NotNull(section);
    }

    /// <summary>
    /// Verifies the candlestick section wrapper is present after data loads.
    /// The section must contain a CandlestickChart bound to trend-derived OHLC candles.
    /// </summary>
    [Fact]
    public void ReportsDashboard_Renders_CandlestickSection()
    {
        // Act
        var cut = Render<ReportsDashboard>();

        // Assert
        var section = cut.Find(".dashboard-candlestick");
        Assert.NotNull(section);
    }

    /// <summary>
    /// Verifies the box plot section wrapper is present after data loads.
    /// The section must contain a BoxPlotChart bound to category distributions from transactions.
    /// </summary>
    [Fact]
    public void ReportsDashboard_Renders_BoxPlotSection()
    {
        // Act
        var cut = Render<ReportsDashboard>();

        // Assert
        var section = cut.Find(".dashboard-box-plot");
        Assert.NotNull(section);
    }

    /// <summary>
    /// Verifies the scatter section wrapper is present after data loads.
    /// The section must contain a ScatterChart bound to transaction scatter points.
    /// </summary>
    [Fact]
    public void ReportsDashboard_Renders_ScatterSection()
    {
        // Act
        var cut = Render<ReportsDashboard>();

        // Assert
        var section = cut.Find(".dashboard-scatter");
        Assert.NotNull(section);
    }

    /// <summary>
    /// Verifies the grouped bar section wrapper is present after data loads.
    /// The section must contain a GroupedBarChart bound to budget vs. actual per category.
    /// </summary>
    [Fact]
    public void ReportsDashboard_Renders_GroupedBarSection()
    {
        // Act
        var cut = Render<ReportsDashboard>();

        // Assert
        var section = cut.Find(".dashboard-grouped-bar");
        Assert.NotNull(section);
    }

    /// <summary>
    /// Verifies the stacked bar section wrapper is present after data loads.
    /// The section must contain a StackedBarChart bound to monthly income vs. spending.
    /// </summary>
    [Fact]
    public void ReportsDashboard_Renders_StackedBarSection()
    {
        // Act
        var cut = Render<ReportsDashboard>();

        // Assert
        var section = cut.Find(".dashboard-stacked-bar");
        Assert.NotNull(section);
    }

    /// <summary>
    /// Verifies the radial gauge section wrapper is present after data loads.
    /// The section must contain a RadialGauge bound to overall budget percentage used.
    /// </summary>
    [Fact]
    public void ReportsDashboard_Renders_RadialGaugeSection()
    {
        // Act
        var cut = Render<ReportsDashboard>();

        // Assert
        var section = cut.Find(".dashboard-radial-gauge");
        Assert.NotNull(section);
    }

    /// <summary>
    /// Verifies the sparkline section wrapper is present after data loads.
    /// The section must contain a SparkLine bound to monthly spending values.
    /// </summary>
    [Fact]
    public void ReportsDashboard_Renders_SparklineSection()
    {
        // Act
        var cut = Render<ReportsDashboard>();

        // Assert
        var section = cut.Find(".dashboard-sparkline");
        Assert.NotNull(section);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Fakes
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Fake <see cref="IChartDataService"/> returning empty data synchronously.
    /// All methods complete immediately so no WaitForState/WaitForElement is required.
    /// </summary>
    private sealed class FakeChartDataService : IChartDataService
    {
        /// <inheritdoc/>
        public HeatmapDataPoint[][] BuildSpendingHeatmap(
            IEnumerable<TransactionDto> transactions,
            HeatmapGrouping grouping = HeatmapGrouping.DayOfWeekByWeek)
            => [[], [], [], [], [], [], []];

        /// <inheritdoc/>
        public WaterfallSegment[] BuildBudgetWaterfall(
            decimal income,
            IEnumerable<CategorySpendingDto> spending)
            => [];

        /// <inheritdoc/>
        public CandlestickDataPoint[] BuildBalanceCandlesticks(
            IEnumerable<DailyBalanceDto> balances,
            CandlestickInterval interval = CandlestickInterval.Monthly)
            => [];

        /// <inheritdoc/>
        public BoxPlotSummary[] BuildCategoryDistributions(
            IEnumerable<TransactionDto> transactions,
            int monthsBack = 6)
            => [];

        /// <inheritdoc/>
        public CandlestickDataPoint[] BuildTrendCandlesticks(
            IEnumerable<MonthlyTrendPointDto> monthlyData)
            => [];
    }

    /// <summary>
    /// Minimal fake <see cref="IBudgetApiService"/> for ReportsDashboard tests.
    /// All non-dashboard methods return safe empty defaults.
    /// <see cref="MonthlyCategoryTaskSource"/> allows blocking
    /// <see cref="GetMonthlyCategoryReportAsync"/> to assert the initial loading state.
    /// </summary>
    private sealed class FakeBudgetApiService : IBudgetApiService
    {
        /// <summary>
        /// Gets or sets a <see cref="TaskCompletionSource{T}"/> that, when non-null,
        /// causes <see cref="GetMonthlyCategoryReportAsync"/> to return an incomplete task,
        /// keeping the component in its loading state for assertion.
        /// </summary>
        public TaskCompletionSource<MonthlyCategoryReportDto?>? MonthlyCategoryTaskSource
        {
            get; set;
        }

        /// <inheritdoc/>
        public Task<MonthlyCategoryReportDto?> GetMonthlyCategoryReportAsync(int year, int month)
        {
            if (this.MonthlyCategoryTaskSource != null)
            {
                return this.MonthlyCategoryTaskSource.Task;
            }

            return Task.FromResult<MonthlyCategoryReportDto?>(null);
        }

        // --- All other interface methods return safe defaults ---

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
        [Obsolete("Use GetCalendarGridAsync instead.")]
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
        public Task<UnifiedTransactionPageDto> GetUnifiedTransactionsAsync(UnifiedTransactionFilterDto filter) => Task.FromResult(new UnifiedTransactionPageDto());

        /// <inheritdoc/>
        public Task<TransactionDto?> UpdateTransactionCategoryAsync(Guid transactionId, Guid? categoryId) => Task.FromResult<TransactionDto?>(null);

        /// <inheritdoc/>
        public Task<BatchSuggestCategoriesResponse> GetBatchCategorySuggestionsAsync(IReadOnlyList<Guid> transactionIds) => Task.FromResult(new BatchSuggestCategoriesResponse());

        /// <inheritdoc/>
        public Task<UncategorizedTransactionPageDto> GetUncategorizedTransactionsAsync(UncategorizedTransactionFilterDto filter) => Task.FromResult(new UncategorizedTransactionPageDto());

        /// <inheritdoc/>
        public Task<BulkCategorizeResponse> BulkCategorizeTransactionsAsync(BulkCategorizeRequest request) => Task.FromResult(new BulkCategorizeResponse());

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
        public Task<UserSettingsDto?> GetUserSettingsAsync() => Task.FromResult<UserSettingsDto?>(null);

        /// <inheritdoc/>
        public Task<UserSettingsDto?> UpdateUserSettingsAsync(UserSettingsUpdateDto dto) => Task.FromResult<UserSettingsDto?>(null);

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
        public Task<HeatmapDataResponse?> GetCalendarHeatmapAsync(int year, int month, CancellationToken ct = default)
            => Task.FromResult<HeatmapDataResponse?>(null);
    }
}
