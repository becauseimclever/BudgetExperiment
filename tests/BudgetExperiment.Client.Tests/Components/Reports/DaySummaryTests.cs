// <copyright file="DaySummaryTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using Bunit;

using BudgetExperiment.Client.Components.Reports;
using BudgetExperiment.Client.Services;
using BudgetExperiment.Contracts.Dtos;

using Microsoft.Extensions.DependencyInjection;

namespace BudgetExperiment.Client.Tests.Components.Reports;

/// <summary>
/// Unit tests for the DaySummary component.
/// </summary>
public class DaySummaryTests : BunitContext, IAsyncLifetime
{
    private readonly StubBudgetApiService _stubApiService;

    /// <summary>
    /// Initializes a new instance of the <see cref="DaySummaryTests"/> class.
    /// </summary>
    public DaySummaryTests()
    {
        this.JSInterop.Mode = JSRuntimeMode.Loose;
        this._stubApiService = new StubBudgetApiService();
        this.Services.AddSingleton<IBudgetApiService>(this._stubApiService);
        this.Services.AddSingleton<ThemeService>();
    }

    /// <inheritdoc/>
    public Task InitializeAsync() => Task.CompletedTask;

    /// <inheritdoc/>
    public new Task DisposeAsync() => base.DisposeAsync().AsTask();

    /// <summary>
    /// Verifies that the component renders income, spending, and net amounts.
    /// </summary>
    [Fact]
    public void DaySummary_ShowsIncomeSpendingAndNet_WhenDataExists()
    {
        // Arrange
        this._stubApiService.DaySummaryResult = CreateSummaryWithTransactions();

        // Act
        var cut = Render<DaySummary>(parameters => parameters
            .Add(p => p.Date, new DateOnly(2026, 2, 5)));

        // Assert
        Assert.Contains("Daily Summary", cut.Markup);
        Assert.Contains("$500.00", cut.Markup); // Income
        Assert.Contains("$150.00", cut.Markup); // Spending
    }

    /// <summary>
    /// Verifies that the component displays top categories.
    /// </summary>
    [Fact]
    public void DaySummary_ShowsTopCategories_WhenPresent()
    {
        // Arrange
        this._stubApiService.DaySummaryResult = CreateSummaryWithCategories();

        // Act
        var cut = Render<DaySummary>(parameters => parameters
            .Add(p => p.Date, new DateOnly(2026, 2, 5)));

        // Assert
        Assert.Contains("Groceries", cut.Markup);
        Assert.Contains("Dining", cut.Markup);
        Assert.Contains("Transport", cut.Markup);
    }

    /// <summary>
    /// Verifies that the component shows an empty message when there are no transactions.
    /// </summary>
    [Fact]
    public void DaySummary_ShowsEmptyMessage_WhenNoTransactions()
    {
        // Arrange
        this._stubApiService.DaySummaryResult = new DaySummaryDto
        {
            Date = new DateOnly(2026, 2, 5),
            TransactionCount = 0,
        };

        // Act
        var cut = Render<DaySummary>(parameters => parameters
            .Add(p => p.Date, new DateOnly(2026, 2, 5)));

        // Assert
        Assert.Contains("No transaction analytics for this day", cut.Markup);
        Assert.DoesNotContain("Daily Summary", cut.Markup);
    }

    /// <summary>
    /// Verifies that the component handles a null API response gracefully.
    /// </summary>
    [Fact]
    public void DaySummary_HandlesNullResponse_Gracefully()
    {
        // Arrange
        this._stubApiService.DaySummaryResult = null;

        // Act
        var cut = Render<DaySummary>(parameters => parameters
            .Add(p => p.Date, new DateOnly(2026, 2, 5)));

        // Assert — should render without errors ( no summary, no empty message)
        Assert.DoesNotContain("Daily Summary", cut.Markup);
        Assert.DoesNotContain("No transaction analytics", cut.Markup);
    }

    /// <summary>
    /// Verifies that positive net shows positive styling.
    /// </summary>
    [Fact]
    public void DaySummary_ShowsPositiveNet_WhenIncomeExceedsSpending()
    {
        // Arrange
        this._stubApiService.DaySummaryResult = CreateSummaryWithTransactions();

        // Act
        var cut = Render<DaySummary>(parameters => parameters
            .Add(p => p.Date, new DateOnly(2026, 2, 5)));

        // Assert
        var netStat = cut.FindAll(".stat-item.net .stat-value");
        Assert.Single(netStat);
        Assert.Contains("positive", netStat[0].ClassList);
        Assert.Contains("+$350.00", netStat[0].TextContent);
    }

    /// <summary>
    /// Verifies that negative net shows negative styling.
    /// </summary>
    [Fact]
    public void DaySummary_ShowsNegativeNet_WhenSpendingExceedsIncome()
    {
        // Arrange
        this._stubApiService.DaySummaryResult = new DaySummaryDto
        {
            Date = new DateOnly(2026, 2, 5),
            TotalIncome = new MoneyDto { Amount = 100m, Currency = "USD" },
            TotalSpending = new MoneyDto { Amount = 250m, Currency = "USD" },
            NetAmount = new MoneyDto { Amount = -150m, Currency = "USD" },
            TransactionCount = 3,
        };

        // Act
        var cut = Render<DaySummary>(parameters => parameters
            .Add(p => p.Date, new DateOnly(2026, 2, 5)));

        // Assert
        var netStat = cut.FindAll(".stat-item.net .stat-value");
        Assert.Single(netStat);
        Assert.Contains("negative", netStat[0].ClassList);
        Assert.Contains("-$150.00", netStat[0].TextContent);
    }

    /// <summary>
    /// Verifies that the component does not show categories section when no top categories exist.
    /// </summary>
    [Fact]
    public void DaySummary_HidesCategoriesSection_WhenNoCategories()
    {
        // Arrange
        this._stubApiService.DaySummaryResult = new DaySummaryDto
        {
            Date = new DateOnly(2026, 2, 5),
            TotalIncome = new MoneyDto { Amount = 0m, Currency = "USD" },
            TotalSpending = new MoneyDto { Amount = 50m, Currency = "USD" },
            NetAmount = new MoneyDto { Amount = -50m, Currency = "USD" },
            TransactionCount = 1,
            TopCategories = [],
        };

        // Act
        var cut = Render<DaySummary>(parameters => parameters
            .Add(p => p.Date, new DateOnly(2026, 2, 5)));

        // Assert
        Assert.DoesNotContain("Top Categories", cut.Markup);
        Assert.Contains("Daily Summary", cut.Markup);
    }

    /// <summary>
    /// Verifies that the component has an accessible region label.
    /// </summary>
    [Fact]
    public void DaySummary_HasAccessibleRegionLabel()
    {
        // Arrange
        this._stubApiService.DaySummaryResult = CreateSummaryWithTransactions();

        // Act
        var cut = Render<DaySummary>(parameters => parameters
            .Add(p => p.Date, new DateOnly(2026, 2, 5)));

        // Assert
        var region = cut.Find("[role='region']");
        Assert.Equal("Day spending summary", region.GetAttribute("aria-label"));
    }

    /// <summary>
    /// Verifies that the component reloads data when rendered with a different date.
    /// </summary>
    [Fact]
    public void DaySummary_LoadsData_ForDifferentDates()
    {
        // Arrange — first date
        var summary1 = new DaySummaryDto
        {
            Date = new DateOnly(2026, 2, 5),
            TotalIncome = new MoneyDto { Amount = 100m, Currency = "USD" },
            TotalSpending = new MoneyDto { Amount = 50m, Currency = "USD" },
            NetAmount = new MoneyDto { Amount = 50m, Currency = "USD" },
            TransactionCount = 2,
        };
        this._stubApiService.DaySummaryResult = summary1;

        var cut1 = Render<DaySummary>(parameters => parameters
            .Add(p => p.Date, new DateOnly(2026, 2, 5)));

        Assert.Contains("$100.00", cut1.Markup);

        // Act — different date with different data
        var summary2 = new DaySummaryDto
        {
            Date = new DateOnly(2026, 2, 6),
            TotalIncome = new MoneyDto { Amount = 200m, Currency = "USD" },
            TotalSpending = new MoneyDto { Amount = 75m, Currency = "USD" },
            NetAmount = new MoneyDto { Amount = 125m, Currency = "USD" },
            TransactionCount = 3,
        };
        this._stubApiService.DaySummaryResult = summary2;

        var cut2 = Render<DaySummary>(parameters => parameters
            .Add(p => p.Date, new DateOnly(2026, 2, 6)));

        // Assert — should have loaded data for the second date
        Assert.Contains("$200.00", cut2.Markup);
    }

    /// <summary>
    /// Verifies that the category list has proper ARIA label.
    /// </summary>
    [Fact]
    public void DaySummary_CategoryList_HasAriaLabel()
    {
        // Arrange
        this._stubApiService.DaySummaryResult = CreateSummaryWithCategories();

        // Act
        var cut = Render<DaySummary>(parameters => parameters
            .Add(p => p.Date, new DateOnly(2026, 2, 5)));

        // Assert
        var list = cut.Find("ul[aria-label]");
        Assert.Equal("Top spending categories", list.GetAttribute("aria-label"));
    }

    /// <summary>
    /// Verifies that the component passes the account ID filter to the API call.
    /// </summary>
    [Fact]
    public void DaySummary_PassesAccountId_ToApiCall()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        this._stubApiService.DaySummaryResult = CreateSummaryWithTransactions();

        // Act
        var cut = Render<DaySummary>(parameters => parameters
            .Add(p => p.Date, new DateOnly(2026, 2, 5))
            .Add(p => p.AccountId, accountId));

        // Assert
        Assert.Equal(accountId, this._stubApiService.LastRequestedAccountId);
    }

    private static DaySummaryDto CreateSummaryWithTransactions()
    {
        return new DaySummaryDto
        {
            Date = new DateOnly(2026, 2, 5),
            TotalIncome = new MoneyDto { Amount = 500m, Currency = "USD" },
            TotalSpending = new MoneyDto { Amount = 150m, Currency = "USD" },
            NetAmount = new MoneyDto { Amount = 350m, Currency = "USD" },
            TransactionCount = 5,
            TopCategories = [],
        };
    }

    private static DaySummaryDto CreateSummaryWithCategories()
    {
        return new DaySummaryDto
        {
            Date = new DateOnly(2026, 2, 5),
            TotalIncome = new MoneyDto { Amount = 0m, Currency = "USD" },
            TotalSpending = new MoneyDto { Amount = 120m, Currency = "USD" },
            NetAmount = new MoneyDto { Amount = -120m, Currency = "USD" },
            TransactionCount = 4,
            TopCategories = new List<DayTopCategoryDto>
            {
                new() { CategoryName = "Groceries", Amount = new MoneyDto { Amount = 60m, Currency = "USD" } },
                new() { CategoryName = "Dining", Amount = new MoneyDto { Amount = 35m, Currency = "USD" } },
                new() { CategoryName = "Transport", Amount = new MoneyDto { Amount = 25m, Currency = "USD" } },
            },
        };
    }

    /// <summary>
    /// Stub implementation of IBudgetApiService for DaySummary tests.
    /// </summary>
    private sealed class StubBudgetApiService : IBudgetApiService
    {
        /// <summary>Gets or sets the DaySummaryDto to return from GetDaySummaryAsync.</summary>
        public DaySummaryDto? DaySummaryResult { get; set; }

        /// <summary>Gets or sets the number of times GetDaySummaryAsync was called.</summary>
        public int GetDaySummaryCallCount { get; set; }

        /// <summary>Gets the last account ID passed to GetDaySummaryAsync.</summary>
        public Guid? LastRequestedAccountId { get; private set; }

        /// <inheritdoc/>
        public Task<DaySummaryDto?> GetDaySummaryAsync(DateOnly date, Guid? accountId = null)
        {
            this.GetDaySummaryCallCount++;
            this.LastRequestedAccountId = accountId;
            return Task.FromResult(this.DaySummaryResult);
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
        public Task<MonthlyCategoryReportDto?> GetMonthlyCategoryReportAsync(int year, int month) => Task.FromResult<MonthlyCategoryReportDto?>(null);

        /// <inheritdoc/>
        public Task<DateRangeCategoryReportDto?> GetCategoryReportByRangeAsync(DateOnly startDate, DateOnly endDate, Guid? accountId = null) => Task.FromResult<DateRangeCategoryReportDto?>(null);

        /// <inheritdoc/>
        public Task<SpendingTrendsReportDto?> GetSpendingTrendsAsync(int months = 6, int? endYear = null, int? endMonth = null, Guid? categoryId = null) => Task.FromResult<SpendingTrendsReportDto?>(null);

        /// <inheritdoc/>
        public Task<ImportPatternsDto?> GetImportPatternsAsync(Guid recurringTransactionId) => Task.FromResult<ImportPatternsDto?>(null);

        /// <inheritdoc/>
        public Task<ImportPatternsDto?> UpdateImportPatternsAsync(Guid recurringTransactionId, ImportPatternsDto patterns) => Task.FromResult<ImportPatternsDto?>(null);
    }
}
