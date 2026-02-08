// <copyright file="BudgetComparisonReportTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Globalization;

using Bunit;

using BudgetExperiment.Client.Pages.Reports;
using BudgetExperiment.Client.Services;
using BudgetExperiment.Contracts.Dtos;

using Microsoft.Extensions.DependencyInjection;

namespace BudgetExperiment.Client.Tests.Pages.Reports;

/// <summary>
/// Unit tests for the BudgetComparisonReport page component.
/// </summary>
public class BudgetComparisonReportTests : BunitContext, IAsyncLifetime
{
    private readonly StubBudgetApiService stubApiService;

    /// <summary>
    /// Initializes a new instance of the <see cref="BudgetComparisonReportTests"/> class.
    /// </summary>
    public BudgetComparisonReportTests()
    {
        this.JSInterop.Mode = JSRuntimeMode.Loose;

        this.stubApiService = new StubBudgetApiService();

        this.Services.AddSingleton<IBudgetApiService>(this.stubApiService);
        this.Services.AddSingleton(new ScopeService(this.JSInterop.JSRuntime));
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
    /// Verifies that the page calls GetBudgetSummaryAsync (existing method, no new API method).
    /// </summary>
    [Fact]
    public void Page_CallsExistingGetBudgetSummaryAsync()
    {
        // Arrange
        this.stubApiService.BudgetSummaryResult = CreateTestSummary();

        // Act
        var cut = Render<BudgetComparisonReport>();

        // Assert
        Assert.True(this.stubApiService.GetBudgetSummaryCalled);
    }

    /// <summary>
    /// Verifies that loading spinner is shown while data is being fetched.
    /// </summary>
    [Fact]
    public void Page_ShowsLoadingSpinner_WhileLoading()
    {
        // Arrange - set up a task that won't complete immediately
        this.stubApiService.BudgetSummaryTaskSource = new TaskCompletionSource<BudgetSummaryDto?>();

        // Act
        var cut = Render<BudgetComparisonReport>();

        // Assert
        Assert.Contains("Loading", cut.Markup);
    }

    /// <summary>
    /// Verifies that a no-budget-goals message is shown when summary has no category progress.
    /// </summary>
    [Fact]
    public void Page_ShowsNoBudgetGoalsMessage_WhenNoCategoryProgress()
    {
        // Arrange
        var summary = CreateTestSummary();
        summary.CategoryProgress = [];
        summary.TotalBudgeted = new MoneyDto { Amount = 0, Currency = "USD" };
        this.stubApiService.BudgetSummaryResult = summary;

        // Act
        var cut = Render<BudgetComparisonReport>();

        // Assert
        Assert.Contains("No budget goals", cut.Markup);
    }

    /// <summary>
    /// Verifies that null response shows an empty state.
    /// </summary>
    [Fact]
    public void Page_ShowsEmptyState_WhenResponseIsNull()
    {
        // Arrange
        this.stubApiService.BudgetSummaryResult = null;

        // Act
        var cut = Render<BudgetComparisonReport>();

        // Assert
        Assert.Contains("No budget data", cut.Markup);
    }

    /// <summary>
    /// Verifies that the overall summary section displays total budgeted, spent, and remaining.
    /// </summary>
    [Fact]
    public void Page_DisplaysOverallSummary()
    {
        // Arrange
        var summary = CreateTestSummary();
        summary.TotalBudgeted = new MoneyDto { Amount = 3000m, Currency = "USD" };
        summary.TotalSpent = new MoneyDto { Amount = 2500m, Currency = "USD" };
        summary.TotalRemaining = new MoneyDto { Amount = 500m, Currency = "USD" };
        summary.OverallPercentUsed = 83.3m;
        this.stubApiService.BudgetSummaryResult = summary;

        // Act
        var cut = Render<BudgetComparisonReport>();

        // Assert
        Assert.Contains("$3,000.00", cut.Markup);
        Assert.Contains("$2,500.00", cut.Markup);
        Assert.Contains("$500.00", cut.Markup);
    }

    /// <summary>
    /// Verifies that the page renders a BarChart with budget vs. actual per category.
    /// </summary>
    [Fact]
    public void Page_RendersBarChart_WithCategoryData()
    {
        // Arrange
        this.stubApiService.BudgetSummaryResult = CreateTestSummaryWithCategories();

        // Act
        var cut = Render<BudgetComparisonReport>();

        // Assert - BarChart should render with bars
        var chart = cut.Find(".bar-chart-container");
        Assert.NotNull(chart);
    }

    /// <summary>
    /// Verifies that the data table shows per-category budget vs. actual details.
    /// </summary>
    [Fact]
    public void Page_ShowsDataTable_WithPerCategoryDetails()
    {
        // Arrange
        this.stubApiService.BudgetSummaryResult = CreateTestSummaryWithCategories();

        // Act
        var cut = Render<BudgetComparisonReport>();

        // Assert
        Assert.Contains("Groceries", cut.Markup);
        Assert.Contains("Dining", cut.Markup);
        Assert.Contains("Transport", cut.Markup);
    }

    /// <summary>
    /// Verifies that status colors are applied to category rows.
    /// </summary>
    [Fact]
    public void Page_AppliesStatusColors_ToRows()
    {
        // Arrange
        this.stubApiService.BudgetSummaryResult = CreateTestSummaryWithCategories();

        // Act
        var cut = Render<BudgetComparisonReport>();

        // Assert - Check that status CSS classes are present
        Assert.Contains("status-over-budget", cut.Markup);
        Assert.Contains("status-warning", cut.Markup);
        Assert.Contains("status-on-track", cut.Markup);
    }

    /// <summary>
    /// Verifies that status counts are displayed in the summary.
    /// </summary>
    [Fact]
    public void Page_DisplaysStatusCounts()
    {
        // Arrange
        this.stubApiService.BudgetSummaryResult = CreateTestSummaryWithCategories();

        // Act
        var cut = Render<BudgetComparisonReport>();

        // Assert
        Assert.Contains("on track", cut.Markup.ToLowerInvariant());
        Assert.Contains("warning", cut.Markup.ToLowerInvariant());
        Assert.Contains("over budget", cut.Markup.ToLowerInvariant());
    }

    /// <summary>
    /// Verifies that clicking Previous month navigates to the previous month.
    /// </summary>
    [Fact]
    public void Page_PreviousMonthButton_LoadsPreviousMonth()
    {
        // Arrange
        this.stubApiService.BudgetSummaryResult = CreateTestSummary();
        var cut = Render<BudgetComparisonReport>();
        this.stubApiService.GetBudgetSummaryCallCount = 0; // reset counter

        // Act
        var prevButton = cut.Find("[aria-label='Previous month']");
        prevButton.Click();

        // Assert - Should have called the API again
        Assert.True(this.stubApiService.GetBudgetSummaryCallCount > 0);
    }

    /// <summary>
    /// Verifies that clicking Next month navigates to the next month.
    /// </summary>
    [Fact]
    public void Page_NextMonthButton_LoadsNextMonth()
    {
        // Arrange
        this.stubApiService.BudgetSummaryResult = CreateTestSummary();
        var cut = Render<BudgetComparisonReport>();
        this.stubApiService.GetBudgetSummaryCallCount = 0; // reset counter

        // Act
        var nextButton = cut.Find("[aria-label='Next month']");
        nextButton.Click();

        // Assert - Should have called the API again
        Assert.True(this.stubApiService.GetBudgetSummaryCallCount > 0);
    }

    /// <summary>
    /// Verifies that the page has a Back to Calendar link.
    /// </summary>
    [Fact]
    public void Page_HasBackToCalendarLink()
    {
        // Arrange
        this.stubApiService.BudgetSummaryResult = CreateTestSummary();

        // Act
        var cut = Render<BudgetComparisonReport>();

        // Assert
        var backLink = cut.Find(".btn-back-calendar");
        Assert.NotNull(backLink);
        Assert.Contains("Calendar", backLink.TextContent);
    }

    /// <summary>
    /// Verifies that NoBudgetSet categories are shown with appropriate styling.
    /// </summary>
    [Fact]
    public void Page_ShowsNoBudgetSetCategories()
    {
        // Arrange
        var summary = CreateTestSummary();
        summary.CategoriesNoBudgetSet = 1;
        summary.CategoryProgress =
        [
            new BudgetProgressDto
            {
                CategoryId = Guid.NewGuid(),
                CategoryName = "Uncategorized",
                Status = "NoBudgetSet",
                TargetAmount = new MoneyDto { Amount = 0, Currency = "USD" },
                SpentAmount = new MoneyDto { Amount = 75m, Currency = "USD" },
                RemainingAmount = new MoneyDto { Amount = 0, Currency = "USD" },
                PercentUsed = 0,
                TransactionCount = 5,
            },
        ];
        this.stubApiService.BudgetSummaryResult = summary;

        // Act
        var cut = Render<BudgetComparisonReport>();

        // Assert
        Assert.Contains("Uncategorized", cut.Markup);
        Assert.Contains("status-no-budget", cut.Markup);
    }

    /// <summary>
    /// Verifies that an error message shows on API failure.
    /// </summary>
    [Fact]
    public void Page_ShowsError_OnApiFailure()
    {
        // Arrange
        this.stubApiService.ShouldThrow = true;

        // Act
        var cut = Render<BudgetComparisonReport>();

        // Assert
        Assert.Contains("Failed to load", cut.Markup);
    }

    /// <summary>
    /// Verifies that the page displays the correct month and year title.
    /// </summary>
    [Fact]
    public void Page_DisplaysCorrectMonthTitle()
    {
        // Arrange
        var now = DateTime.UtcNow;
        this.stubApiService.BudgetSummaryResult = CreateTestSummary();

        // Act
        var cut = Render<BudgetComparisonReport>();

        // Assert - Should display a month name
        var monthName = new DateOnly(now.Year, now.Month, 1).ToString("MMMM yyyy");
        Assert.Contains(monthName, cut.Markup);
    }

    /// <summary>
    /// Verifies that category rows are sorted by status priority (OverBudget first, then Warning, OnTrack, NoBudgetSet).
    /// </summary>
    [Fact]
    public void Page_SortsCategoriesByStatusPriority()
    {
        // Arrange
        this.stubApiService.BudgetSummaryResult = CreateTestSummaryWithCategories();

        // Act
        var cut = Render<BudgetComparisonReport>();

        // Assert - OverBudget ("Transport") should appear before Warning ("Dining"), which should appear before OnTrack ("Groceries")
        var markup = cut.Markup;
        var overBudgetIndex = markup.IndexOf("Transport", StringComparison.Ordinal);
        var warningIndex = markup.IndexOf("Dining", StringComparison.Ordinal);
        var onTrackIndex = markup.IndexOf("Groceries", StringComparison.Ordinal);

        Assert.True(overBudgetIndex < warningIndex, "OverBudget should appear before Warning");
        Assert.True(warningIndex < onTrackIndex, "Warning should appear before OnTrack");
    }

    private static BudgetSummaryDto CreateTestSummary()
    {
        return new BudgetSummaryDto
        {
            Year = DateTime.UtcNow.Year,
            Month = DateTime.UtcNow.Month,
            TotalBudgeted = new MoneyDto { Amount = 2000m, Currency = "USD" },
            TotalSpent = new MoneyDto { Amount = 1500m, Currency = "USD" },
            TotalRemaining = new MoneyDto { Amount = 500m, Currency = "USD" },
            OverallPercentUsed = 75m,
            CategoriesOnTrack = 3,
            CategoriesWarning = 1,
            CategoriesOverBudget = 0,
            CategoriesNoBudgetSet = 0,
            CategoryProgress =
            [
                new BudgetProgressDto
                {
                    CategoryId = Guid.NewGuid(),
                    CategoryName = "General",
                    Status = "OnTrack",
                    TargetAmount = new MoneyDto { Amount = 500, Currency = "USD" },
                    SpentAmount = new MoneyDto { Amount = 300, Currency = "USD" },
                    RemainingAmount = new MoneyDto { Amount = 200, Currency = "USD" },
                    PercentUsed = 60,
                    TransactionCount = 8,
                },
            ],
        };
    }

    private static BudgetSummaryDto CreateTestSummaryWithCategories()
    {
        return new BudgetSummaryDto
        {
            Year = DateTime.UtcNow.Year,
            Month = DateTime.UtcNow.Month,
            TotalBudgeted = new MoneyDto { Amount = 1500m, Currency = "USD" },
            TotalSpent = new MoneyDto { Amount = 1350m, Currency = "USD" },
            TotalRemaining = new MoneyDto { Amount = 150m, Currency = "USD" },
            OverallPercentUsed = 90m,
            CategoriesOnTrack = 1,
            CategoriesWarning = 1,
            CategoriesOverBudget = 1,
            CategoriesNoBudgetSet = 0,
            CategoryProgress =
            [
                new BudgetProgressDto
                {
                    CategoryId = Guid.NewGuid(),
                    CategoryName = "Groceries",
                    CategoryIcon = "🛒",
                    CategoryColor = "#4CAF50",
                    Status = "OnTrack",
                    TargetAmount = new MoneyDto { Amount = 500m, Currency = "USD" },
                    SpentAmount = new MoneyDto { Amount = 350m, Currency = "USD" },
                    RemainingAmount = new MoneyDto { Amount = 150m, Currency = "USD" },
                    PercentUsed = 70m,
                    TransactionCount = 12,
                },
                new BudgetProgressDto
                {
                    CategoryId = Guid.NewGuid(),
                    CategoryName = "Dining",
                    CategoryIcon = "🍕",
                    CategoryColor = "#FF9800",
                    Status = "Warning",
                    TargetAmount = new MoneyDto { Amount = 300m, Currency = "USD" },
                    SpentAmount = new MoneyDto { Amount = 260m, Currency = "USD" },
                    RemainingAmount = new MoneyDto { Amount = 40m, Currency = "USD" },
                    PercentUsed = 86.7m,
                    TransactionCount = 8,
                },
                new BudgetProgressDto
                {
                    CategoryId = Guid.NewGuid(),
                    CategoryName = "Transport",
                    CategoryIcon = "🚗",
                    CategoryColor = "#F44336",
                    Status = "OverBudget",
                    TargetAmount = new MoneyDto { Amount = 200m, Currency = "USD" },
                    SpentAmount = new MoneyDto { Amount = 240m, Currency = "USD" },
                    RemainingAmount = new MoneyDto { Amount = -40m, Currency = "USD" },
                    PercentUsed = 120m,
                    TransactionCount = 15,
                },
            ],
        };
    }

    /// <summary>
    /// Minimal stub for IBudgetApiService that only implements GetBudgetSummaryAsync.
    /// </summary>
    private sealed class StubBudgetApiService : IBudgetApiService
    {
        /// <summary>Gets or sets the result to return from GetBudgetSummaryAsync.</summary>
        public BudgetSummaryDto? BudgetSummaryResult { get; set; }

        /// <summary>Gets or sets a value indicating whether GetBudgetSummaryAsync should throw.</summary>
        public bool ShouldThrow { get; set; }

        /// <summary>Gets a value indicating whether GetBudgetSummaryAsync was called.</summary>
        public bool GetBudgetSummaryCalled { get; private set; }

        /// <summary>Gets or sets the number of times GetBudgetSummaryAsync was called.</summary>
        public int GetBudgetSummaryCallCount { get; set; }

        /// <summary>Gets or sets a TaskCompletionSource for controlling async completion.</summary>
        public TaskCompletionSource<BudgetSummaryDto?>? BudgetSummaryTaskSource { get; set; }

        /// <inheritdoc/>
        public Task<BudgetSummaryDto?> GetBudgetSummaryAsync(int year, int month)
        {
            this.GetBudgetSummaryCalled = true;
            this.GetBudgetSummaryCallCount++;

            if (this.ShouldThrow)
            {
                throw new HttpRequestException("Network error");
            }

            if (this.BudgetSummaryTaskSource != null)
            {
                return this.BudgetSummaryTaskSource.Task;
            }

            return Task.FromResult(this.BudgetSummaryResult);
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
    }
}
