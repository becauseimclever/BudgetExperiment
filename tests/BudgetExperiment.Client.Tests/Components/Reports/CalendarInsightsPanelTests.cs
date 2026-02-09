// <copyright file="CalendarInsightsPanelTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Globalization;

using Bunit;

using BudgetExperiment.Client.Components.Reports;
using BudgetExperiment.Client.Services;
using BudgetExperiment.Contracts.Dtos;

using Microsoft.Extensions.DependencyInjection;

namespace BudgetExperiment.Client.Tests.Components.Reports;

/// <summary>
/// Unit tests for the CalendarInsightsPanel component.
/// </summary>
public class CalendarInsightsPanelTests : BunitContext, IAsyncLifetime
{
    private readonly StubBudgetApiService _stubApiService;

    /// <summary>
    /// Initializes a new instance of the <see cref="CalendarInsightsPanelTests"/> class.
    /// </summary>
    public CalendarInsightsPanelTests()
    {
        this.JSInterop.Mode = JSRuntimeMode.Loose;
        this._stubApiService = new StubBudgetApiService();
        this.Services.AddSingleton<IBudgetApiService>(this._stubApiService);
        this.Services.AddSingleton<ThemeService>();
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
    /// Verifies that the panel renders in collapsed state by default.
    /// </summary>
    [Fact]
    public void Panel_RendersCollapsedByDefault()
    {
        // Arrange & Act
        var cut = Render<CalendarInsightsPanel>(parameters => parameters
            .Add(p => p.Year, 2026)
            .Add(p => p.Month, 2));

        // Assert
        var panel = cut.Find(".calendar-insights-panel");
        Assert.Contains("collapsed", panel.ClassList);
        Assert.DoesNotContain("expanded", panel.ClassList);
    }

    /// <summary>
    /// Verifies that the panel shows "Month Insights" title.
    /// </summary>
    [Fact]
    public void Panel_ShowsTitle()
    {
        // Arrange & Act
        var cut = Render<CalendarInsightsPanel>(parameters => parameters
            .Add(p => p.Year, 2026)
            .Add(p => p.Month, 2));

        // Assert
        Assert.Contains("Month Insights", cut.Markup);
    }

    /// <summary>
    /// Verifies that clicking header expands the panel and loads data.
    /// </summary>
    [Fact]
    public void Panel_ExpandsOnHeaderClick_AndLoadsData()
    {
        // Arrange
        this._stubApiService.MonthlyCategoryReportResult = CreateTestReport();
        var cut = Render<CalendarInsightsPanel>(parameters => parameters
            .Add(p => p.Year, 2026)
            .Add(p => p.Month, 2));

        // Act
        cut.Find(".insights-panel-header").Click();

        // Assert
        var panel = cut.Find(".calendar-insights-panel");
        Assert.Contains("expanded", panel.ClassList);
        Assert.True(this._stubApiService.GetMonthlyCategoryReportCalled);
    }

    /// <summary>
    /// Verifies that clicking header again collapses the panel.
    /// </summary>
    [Fact]
    public void Panel_CollapsesOnSecondHeaderClick()
    {
        // Arrange
        this._stubApiService.MonthlyCategoryReportResult = CreateTestReport();
        var cut = Render<CalendarInsightsPanel>(parameters => parameters
            .Add(p => p.Year, 2026)
            .Add(p => p.Month, 2));

        // Act - expand then collapse
        cut.Find(".insights-panel-header").Click();
        cut.Find(".insights-panel-header").Click();

        // Assert
        var panel = cut.Find(".calendar-insights-panel");
        Assert.Contains("collapsed", panel.ClassList);
    }

    /// <summary>
    /// Verifies that the panel displays income, spending, and net values.
    /// </summary>
    [Fact]
    public void Panel_DisplaysSummaryValues_WhenExpanded()
    {
        // Arrange
        var report = CreateTestReport();
        report.TotalIncome = new MoneyDto { Amount = 4000m, Currency = "USD" };
        report.TotalSpending = new MoneyDto { Amount = 2500m, Currency = "USD" };
        this._stubApiService.MonthlyCategoryReportResult = report;

        var cut = Render<CalendarInsightsPanel>(parameters => parameters
            .Add(p => p.Year, 2026)
            .Add(p => p.Month, 2));

        // Act
        cut.Find(".insights-panel-header").Click();

        // Assert
        Assert.Contains("$4,000.00", cut.Markup);
        Assert.Contains("$2,500.00", cut.Markup);
        Assert.Contains("$1,500.00", cut.Markup); // Net = 4000 - 2500
    }

    /// <summary>
    /// Verifies that the panel shows top 3 categories.
    /// </summary>
    [Fact]
    public void Panel_ShowsTopThreeCategories()
    {
        // Arrange
        var report = CreateTestReport();
        report.Categories = new List<CategorySpendingDto>
        {
            new() { CategoryId = Guid.NewGuid(), CategoryName = "Groceries", CategoryColor = "#4CAF50", Amount = new MoneyDto { Amount = 450m, Currency = "USD" }, Percentage = 30m, TransactionCount = 12 },
            new() { CategoryId = Guid.NewGuid(), CategoryName = "Dining", CategoryColor = "#FF5722", Amount = new MoneyDto { Amount = 300m, Currency = "USD" }, Percentage = 20m, TransactionCount = 8 },
            new() { CategoryId = Guid.NewGuid(), CategoryName = "Gas", CategoryColor = "#2196F3", Amount = new MoneyDto { Amount = 200m, Currency = "USD" }, Percentage = 13.3m, TransactionCount = 5 },
            new() { CategoryId = Guid.NewGuid(), CategoryName = "Entertainment", CategoryColor = "#9C27B0", Amount = new MoneyDto { Amount = 100m, Currency = "USD" }, Percentage = 6.7m, TransactionCount = 3 },
        };
        this._stubApiService.MonthlyCategoryReportResult = report;

        var cut = Render<CalendarInsightsPanel>(parameters => parameters
            .Add(p => p.Year, 2026)
            .Add(p => p.Month, 2));

        // Act
        cut.Find(".insights-panel-header").Click();

        // Assert - top 3 shown, 4th not shown in the top categories list
        var categoryRows = cut.FindAll(".insights-category-row");
        Assert.Equal(3, categoryRows.Count);
        Assert.Contains("Groceries", cut.Markup);
        Assert.Contains("Dining", cut.Markup);
        Assert.Contains("Gas", cut.Markup);
    }

    /// <summary>
    /// Verifies that the panel shows a "View Full Report" link.
    /// </summary>
    [Fact]
    public void Panel_ShowsViewFullReportLink()
    {
        // Arrange
        this._stubApiService.MonthlyCategoryReportResult = CreateTestReport();
        var cut = Render<CalendarInsightsPanel>(parameters => parameters
            .Add(p => p.Year, 2026)
            .Add(p => p.Month, 2));

        // Act
        cut.Find(".insights-panel-header").Click();

        // Assert
        var link = cut.Find(".view-full-report");
        Assert.Equal("/reports/categories?year=2026&month=2", link.GetAttribute("href"));
        Assert.Contains("View Full Report", link.TextContent);
    }

    /// <summary>
    /// Verifies that the panel shows empty state when no data.
    /// </summary>
    [Fact]
    public void Panel_ShowsEmptyState_WhenNoData()
    {
        // Arrange - leave stub returning null
        var cut = Render<CalendarInsightsPanel>(parameters => parameters
            .Add(p => p.Year, 2026)
            .Add(p => p.Month, 2));

        // Act
        cut.Find(".insights-panel-header").Click();

        // Assert
        Assert.Contains("No spending data available", cut.Markup);
    }

    /// <summary>
    /// Verifies that the OnExpandedChanged callback is fired.
    /// </summary>
    [Fact]
    public void Panel_FiresOnExpandedChanged()
    {
        // Arrange
        var expandedValue = false;
        this._stubApiService.MonthlyCategoryReportResult = CreateTestReport();
        var cut = Render<CalendarInsightsPanel>(parameters => parameters
            .Add(p => p.Year, 2026)
            .Add(p => p.Month, 2)
            .Add(p => p.OnExpandedChanged, val => expandedValue = val));

        // Act
        cut.Find(".insights-panel-header").Click();

        // Assert
        Assert.True(expandedValue);
    }

    /// <summary>
    /// Verifies that trend indicator is rendered when previous month data exists.
    /// </summary>
    [Fact]
    public void Panel_ShowsTrendIndicator_WhenPreviousDataExists()
    {
        // Arrange
        var currentReport = CreateTestReport();
        currentReport.TotalSpending = new MoneyDto { Amount = 2500m, Currency = "USD" };

        var previousReport = CreateTestReport();
        previousReport.TotalSpending = new MoneyDto { Amount = 2000m, Currency = "USD" };

        this._stubApiService.MonthlyCategoryReportResult = currentReport;
        this._stubApiService.PreviousMonthReportResult = previousReport;

        var cut = Render<CalendarInsightsPanel>(parameters => parameters
            .Add(p => p.Year, 2026)
            .Add(p => p.Month, 2));

        // Act
        cut.Find(".insights-panel-header").Click();

        // Assert - TrendIndicator should be rendered
        Assert.Contains("trend-indicator", cut.Markup);
        Assert.Contains("vs. last month", cut.Markup);
    }

    /// <summary>
    /// Verifies that the panel does not fetch data when collapsed.
    /// </summary>
    [Fact]
    public void Panel_DoesNotFetchData_WhenCollapsed()
    {
        // Arrange & Act
        Render<CalendarInsightsPanel>(parameters => parameters
            .Add(p => p.Year, 2026)
            .Add(p => p.Month, 2));

        // Assert
        Assert.False(this._stubApiService.GetMonthlyCategoryReportCalled);
    }

    /// <summary>
    /// Verifies that net amount shows positive class when income exceeds spending.
    /// </summary>
    [Fact]
    public void Panel_ShowsPositiveNetClass_WhenIncomeExceedsSpending()
    {
        // Arrange
        var report = CreateTestReport();
        report.TotalIncome = new MoneyDto { Amount = 5000m, Currency = "USD" };
        report.TotalSpending = new MoneyDto { Amount = 3000m, Currency = "USD" };
        this._stubApiService.MonthlyCategoryReportResult = report;

        var cut = Render<CalendarInsightsPanel>(parameters => parameters
            .Add(p => p.Year, 2026)
            .Add(p => p.Month, 2));

        // Act
        cut.Find(".insights-panel-header").Click();

        // Assert
        Assert.Contains("stat-positive", cut.Markup);
    }

    /// <summary>
    /// Verifies that net amount shows negative class when spending exceeds income.
    /// </summary>
    [Fact]
    public void Panel_ShowsNegativeNetClass_WhenSpendingExceedsIncome()
    {
        // Arrange
        var report = CreateTestReport();
        report.TotalIncome = new MoneyDto { Amount = 2000m, Currency = "USD" };
        report.TotalSpending = new MoneyDto { Amount = 3000m, Currency = "USD" };
        this._stubApiService.MonthlyCategoryReportResult = report;

        var cut = Render<CalendarInsightsPanel>(parameters => parameters
            .Add(p => p.Year, 2026)
            .Add(p => p.Month, 2));

        // Act
        cut.Find(".insights-panel-header").Click();

        // Assert
        Assert.Contains("stat-negative", cut.Markup);
    }

    /// <summary>
    /// Verifies that the panel renders the donut chart.
    /// </summary>
    [Fact]
    public void Panel_RendersDonutChart_WhenCategoriesExist()
    {
        // Arrange
        var report = CreateTestReport();
        report.Categories = new List<CategorySpendingDto>
        {
            new() { CategoryId = Guid.NewGuid(), CategoryName = "Groceries", Amount = new MoneyDto { Amount = 450m, Currency = "USD" }, Percentage = 100m, TransactionCount = 5 },
        };
        this._stubApiService.MonthlyCategoryReportResult = report;

        var cut = Render<CalendarInsightsPanel>(parameters => parameters
            .Add(p => p.Year, 2026)
            .Add(p => p.Month, 2));

        // Act
        cut.Find(".insights-panel-header").Click();

        // Assert - DonutChart should produce an SVG
        Assert.Contains("donut-chart", cut.Markup);
    }

    /// <summary>
    /// Verifies that the panel displays transaction count.
    /// </summary>
    [Fact]
    public void Panel_DisplaysTransactionCount()
    {
        // Arrange
        var report = CreateTestReport();
        report.Categories = new List<CategorySpendingDto>
        {
            new() { CategoryId = Guid.NewGuid(), CategoryName = "Groceries", Amount = new MoneyDto { Amount = 450m, Currency = "USD" }, Percentage = 60m, TransactionCount = 12 },
            new() { CategoryId = Guid.NewGuid(), CategoryName = "Dining", Amount = new MoneyDto { Amount = 300m, Currency = "USD" }, Percentage = 40m, TransactionCount = 8 },
        };
        this._stubApiService.MonthlyCategoryReportResult = report;

        var cut = Render<CalendarInsightsPanel>(parameters => parameters
            .Add(p => p.Year, 2026)
            .Add(p => p.Month, 2));

        // Act
        cut.Find(".insights-panel-header").Click();

        // Assert - 12 + 8 = 20 transactions
        Assert.Contains("20", cut.Markup);
    }

    /// <summary>
    /// Verifies that chart segments exclude zero-amount categories.
    /// </summary>
    [Fact]
    public void Panel_ChartSegments_ExcludeZeroAmountCategories()
    {
        // Arrange
        var report = CreateTestReport();
        report.TotalSpending = new MoneyDto { Amount = 500m, Currency = "USD" };
        report.Categories = new List<CategorySpendingDto>
        {
            new() { CategoryId = Guid.NewGuid(), CategoryName = "Groceries", CategoryColor = "#4CAF50", Amount = new MoneyDto { Amount = 500m, Currency = "USD" }, Percentage = 100m, TransactionCount = 5 },
            new() { CategoryId = Guid.NewGuid(), CategoryName = "EmptyCategory", CategoryColor = "#FF0000", Amount = new MoneyDto { Amount = 0m, Currency = "USD" }, Percentage = 0m, TransactionCount = 0 },
        };
        this._stubApiService.MonthlyCategoryReportResult = report;

        var cut = Render<CalendarInsightsPanel>(parameters => parameters
            .Add(p => p.Year, 2026)
            .Add(p => p.Month, 2));

        // Act
        cut.Find(".insights-panel-header").Click();

        // Assert - only one segment circle should be rendered (zero-amount excluded)
        var segments = cut.FindAll("circle.donut-segment");
        Assert.Single(segments);

        // The zero-amount category color should not appear as a segment stroke
        Assert.DoesNotContain("#FF0000", cut.FindAll("circle.donut-segment").Select(s => s.GetAttribute("stroke")).ToList());
    }

    /// <summary>
    /// Verifies that chart segments are sorted by amount descending.
    /// </summary>
    [Fact]
    public void Panel_ChartSegments_SortedByAmountDescending()
    {
        // Arrange
        var report = CreateTestReport();
        report.TotalSpending = new MoneyDto { Amount = 900m, Currency = "USD" };
        report.Categories = new List<CategorySpendingDto>
        {
            new() { CategoryId = Guid.NewGuid(), CategoryName = "Small", CategoryColor = "#FF0000", Amount = new MoneyDto { Amount = 100m, Currency = "USD" }, Percentage = 11.1m, TransactionCount = 1 },
            new() { CategoryId = Guid.NewGuid(), CategoryName = "Large", CategoryColor = "#00FF00", Amount = new MoneyDto { Amount = 500m, Currency = "USD" }, Percentage = 55.6m, TransactionCount = 5 },
            new() { CategoryId = Guid.NewGuid(), CategoryName = "Medium", CategoryColor = "#0000FF", Amount = new MoneyDto { Amount = 300m, Currency = "USD" }, Percentage = 33.3m, TransactionCount = 3 },
        };
        this._stubApiService.MonthlyCategoryReportResult = report;

        var cut = Render<CalendarInsightsPanel>(parameters => parameters
            .Add(p => p.Year, 2026)
            .Add(p => p.Month, 2));

        // Act
        cut.Find(".insights-panel-header").Click();

        // Assert - segments should be ordered: Large (#00FF00), Medium (#0000FF), Small (#FF0000)
        var segmentColors = cut.FindAll("circle.donut-segment")
            .Select(s => s.GetAttribute("stroke"))
            .ToList();

        Assert.Equal(3, segmentColors.Count);
        Assert.Equal("#00FF00", segmentColors[0]); // Large (500)
        Assert.Equal("#0000FF", segmentColors[1]); // Medium (300)
        Assert.Equal("#FF0000", segmentColors[2]); // Small (100)
    }

    private static MonthlyCategoryReportDto CreateTestReport()
    {
        return new MonthlyCategoryReportDto
        {
            Year = 2026,
            Month = 2,
            TotalSpending = new MoneyDto { Amount = 1500m, Currency = "USD" },
            TotalIncome = new MoneyDto { Amount = 3000m, Currency = "USD" },
            Categories = [],
        };
    }

    /// <summary>
    /// Stub implementation of IBudgetApiService for CalendarInsightsPanel tests.
    /// </summary>
    private sealed class StubBudgetApiService : IBudgetApiService
    {
        /// <summary>Gets or sets the result for the current month report.</summary>
        public MonthlyCategoryReportDto? MonthlyCategoryReportResult { get; set; }

        /// <summary>Gets or sets the result for the previous month report (for trend calculation).</summary>
        public MonthlyCategoryReportDto? PreviousMonthReportResult { get; set; }

        /// <summary>Gets a value indicating whether GetMonthlyCategoryReportAsync was called.</summary>
        public bool GetMonthlyCategoryReportCalled { get; private set; }

        /// <inheritdoc/>
        public Task<MonthlyCategoryReportDto?> GetMonthlyCategoryReportAsync(int year, int month)
        {
            this.GetMonthlyCategoryReportCalled = true;

            // Return previous month data if the requested month is one before the current
            if (this.PreviousMonthReportResult != null &&
                this.MonthlyCategoryReportResult != null &&
                (month != this.MonthlyCategoryReportResult.Month || year != this.MonthlyCategoryReportResult.Year))
            {
                return Task.FromResult<MonthlyCategoryReportDto?>(this.PreviousMonthReportResult);
            }

            return Task.FromResult(this.MonthlyCategoryReportResult);
        }

        // --- All other interface methods return defaults ---

        /// <inheritdoc/>
        public Task<IReadOnlyList<AccountDto>> GetAccountsAsync() => Task.FromResult<IReadOnlyList<AccountDto>>([]);

        /// <inheritdoc/>
        public Task<AccountDto?> GetAccountAsync(Guid id) => Task.FromResult<AccountDto?>(null);

        /// <inheritdoc/>
        public Task<AccountDto?> CreateAccountAsync(AccountCreateDto model) => Task.FromResult<AccountDto?>(null);

        /// <inheritdoc/>
        public Task<AccountDto?> UpdateAccountAsync(Guid id, AccountUpdateDto model) => Task.FromResult<AccountDto?>(null);

        /// <inheritdoc/>
        public Task<bool> DeleteAccountAsync(Guid id) => Task.FromResult(false);

        /// <inheritdoc/>
        public Task<IReadOnlyList<TransactionDto>> GetTransactionsAsync(DateOnly startDate, DateOnly endDate, Guid? accountId = null) => Task.FromResult<IReadOnlyList<TransactionDto>>([]);

        /// <inheritdoc/>
        public Task<TransactionDto?> GetTransactionAsync(Guid id) => Task.FromResult<TransactionDto?>(null);

        /// <inheritdoc/>
        public Task<TransactionDto?> CreateTransactionAsync(TransactionCreateDto model) => Task.FromResult<TransactionDto?>(null);

        /// <inheritdoc/>
        public Task<TransactionDto?> UpdateTransactionAsync(Guid id, TransactionUpdateDto model) => Task.FromResult<TransactionDto?>(null);

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
        public Task<RecurringTransactionDto?> UpdateRecurringTransactionAsync(Guid id, RecurringTransactionUpdateDto model) => Task.FromResult<RecurringTransactionDto?>(null);

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
        public Task<RecurringInstanceDto?> ModifyRecurringInstanceAsync(Guid id, DateOnly date, RecurringInstanceModifyDto model) => Task.FromResult<RecurringInstanceDto?>(null);

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
        public Task<RecurringTransferDto?> UpdateRecurringTransferAsync(Guid id, RecurringTransferUpdateDto model) => Task.FromResult<RecurringTransferDto?>(null);

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
        public Task<RecurringTransferInstanceDto?> ModifyRecurringTransferInstanceAsync(Guid id, DateOnly date, RecurringTransferInstanceModifyDto model) => Task.FromResult<RecurringTransferInstanceDto?>(null);

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
        public Task<PaycheckAllocationSummaryDto?> GetPaycheckAllocationAsync(string frequency, decimal? amount = null, Guid? accountId = null) => Task.FromResult<PaycheckAllocationSummaryDto?>(null);

        /// <inheritdoc/>
        public Task<IReadOnlyList<BudgetCategoryDto>> GetCategoriesAsync(bool activeOnly = false) => Task.FromResult<IReadOnlyList<BudgetCategoryDto>>([]);

        /// <inheritdoc/>
        public Task<BudgetCategoryDto?> GetCategoryAsync(Guid id) => Task.FromResult<BudgetCategoryDto?>(null);

        /// <inheritdoc/>
        public Task<BudgetCategoryDto?> CreateCategoryAsync(BudgetCategoryCreateDto model) => Task.FromResult<BudgetCategoryDto?>(null);

        /// <inheritdoc/>
        public Task<BudgetCategoryDto?> UpdateCategoryAsync(Guid id, BudgetCategoryUpdateDto model) => Task.FromResult<BudgetCategoryDto?>(null);

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
        public Task<BudgetGoalDto?> SetBudgetGoalAsync(Guid categoryId, BudgetGoalSetDto model) => Task.FromResult<BudgetGoalDto?>(null);

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
        public Task<CategorizationRuleDto?> GetCategorizationRuleAsync(Guid id) => Task.FromResult<CategorizationRuleDto?>(null);

        /// <inheritdoc/>
        public Task<CategorizationRuleDto?> CreateCategorizationRuleAsync(CategorizationRuleCreateDto model) => Task.FromResult<CategorizationRuleDto?>(null);

        /// <inheritdoc/>
        public Task<CategorizationRuleDto?> UpdateCategorizationRuleAsync(Guid id, CategorizationRuleUpdateDto model) => Task.FromResult<CategorizationRuleDto?>(null);

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
        public Task<ImportPatternsDto?> GetImportPatternsAsync(Guid recurringTransactionId) => Task.FromResult<ImportPatternsDto?>(null);

        /// <inheritdoc/>
        public Task<ImportPatternsDto?> UpdateImportPatternsAsync(Guid recurringTransactionId, ImportPatternsDto patterns) => Task.FromResult<ImportPatternsDto?>(null);
    }
}
