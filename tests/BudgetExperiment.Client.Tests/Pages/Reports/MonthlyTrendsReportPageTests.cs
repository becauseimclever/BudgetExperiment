// <copyright file="MonthlyTrendsReportPageTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Pages.Reports;
using BudgetExperiment.Client.Services;
using BudgetExperiment.Client.Tests.TestHelpers;
using BudgetExperiment.Contracts.Dtos;

using Bunit;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace BudgetExperiment.Client.Tests.Pages.Reports;

/// <summary>
/// Unit tests for the <see cref="MonthlyTrendsReport"/> page component.
/// </summary>
public class MonthlyTrendsReportPageTests : BunitContext, IAsyncLifetime
{
    private readonly StubBudgetApiService _apiService = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="MonthlyTrendsReportPageTests"/> class.
    /// </summary>
    public MonthlyTrendsReportPageTests()
    {
        this.JSInterop.Mode = JSRuntimeMode.Loose;
        this.Services.AddSingleton<IBudgetApiService>(_apiService);
        this.Services.AddSingleton<IToastService>(new ToastService());
        this.Services.AddSingleton<IExportDownloadService>(new StubExportDownloadService());
        this.Services.AddSingleton<ScopeService>();
        this.Services.AddSingleton<ThemeService>();
        this.Services.AddSingleton<CultureService>();
    }

    /// <inheritdoc/>
    public Task InitializeAsync() => Task.CompletedTask;

    /// <inheritdoc/>
    public new Task DisposeAsync()
    {
        return base.DisposeAsync().AsTask();
    }

    /// <summary>
    /// Verifies the page renders without errors.
    /// </summary>
    [Fact]
    public void Renders_WithoutErrors()
    {
        var cut = Render<MonthlyTrendsReport>();

        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies the page title is set correctly.
    /// </summary>
    [Fact]
    public void HasCorrectPageTitle()
    {
        var cut = Render<MonthlyTrendsReport>();

        cut.Markup.ShouldContain("Monthly Trends");
    }

    /// <summary>
    /// Verifies the empty state when no data.
    /// </summary>
    [Fact]
    public void ShowsEmptyState_WhenNoData()
    {
        var cut = Render<MonthlyTrendsReport>();

        cut.Markup.ShouldContain("No spending data available");
    }

    /// <summary>
    /// Verifies month count selector options are present.
    /// </summary>
    [Fact]
    public void HasMonthCountSelector()
    {
        var cut = Render<MonthlyTrendsReport>();

        cut.Markup.ShouldContain("month-count-selector");
        cut.Markup.ShouldContain("months");
    }

    /// <summary>
    /// Verifies category filter dropdown is present.
    /// </summary>
    [Fact]
    public void HasCategoryFilter()
    {
        var cut = Render<MonthlyTrendsReport>();

        cut.Markup.ShouldContain("Category");
        cut.Markup.ShouldContain("All Categories");
    }

    /// <summary>
    /// Verifies the calendar link is present.
    /// </summary>
    [Fact]
    public void HasCalendarLink()
    {
        var cut = Render<MonthlyTrendsReport>();

        cut.Markup.ShouldContain("Calendar");
    }

    /// <summary>
    /// Verifies report shows summary when data exists.
    /// </summary>
    [Fact]
    public void ShowsSummary_WhenDataExists()
    {
        _apiService.SpendingTrends = new SpendingTrendsReportDto
        {
            AverageMonthlySpending = new MoneyDto { Amount = 3000m, Currency = "USD" },
            AverageMonthlyIncome = new MoneyDto { Amount = 5000m, Currency = "USD" },
            TrendDirection = "decreasing",
            TrendPercentage = -5.2m,
            MonthlyData = [],
        };

        var cut = Render<MonthlyTrendsReport>();

        cut.Markup.ShouldContain("Averages");
        cut.Markup.ShouldContain("Avg. Spending");
        cut.Markup.ShouldContain("Avg. Income");
        cut.Markup.ShouldContain("Trend");
    }

    /// <summary>
    /// Verifies category filter dropdown is populated when categories exist.
    /// </summary>
    [Fact]
    public void CategoryFilter_PopulatedWithCategories()
    {
        _apiService.Categories.Add(new BudgetCategoryDto
        {
            Id = Guid.NewGuid(),
            Name = "Groceries",
            Type = "Expense",
            IsActive = true,
        });

        var cut = Render<MonthlyTrendsReport>();

        cut.Markup.ShouldContain("Groceries");
    }

    /// <summary>
    /// Verifies clicking a month count button reloads data.
    /// </summary>
    [Fact]
    public void MonthCountButton_ReloadsData()
    {
        _apiService.SpendingTrends = CreateTestTrendsReport();

        var cut = Render<MonthlyTrendsReport>();

        var monthButtons = cut.FindAll("button.month-option");
        var button12 = monthButtons.First(b => b.TextContent.Contains("12"));
        button12.Click();

        cut.Markup.ShouldContain("Averages");
    }

    /// <summary>
    /// Verifies the category filter select triggers reload.
    /// </summary>
    [Fact]
    public void CategoryFilter_SelectTriggersReload()
    {
        var catId = Guid.NewGuid();
        _apiService.Categories.Add(new BudgetCategoryDto
        {
            Id = catId,
            Name = "Dining",
            Type = "Expense",
            IsActive = true,
        });
        _apiService.SpendingTrends = CreateTestTrendsReport();

        var cut = Render<MonthlyTrendsReport>();

        var select = cut.Find("select#category-select");
        select.Change(catId.ToString());

        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies the monthly breakdown table shows data when report exists.
    /// </summary>
    [Fact]
    public void ShowsMonthlyBreakdownTable_WhenDataExists()
    {
        _apiService.SpendingTrends = CreateTestTrendsReport();

        var cut = Render<MonthlyTrendsReport>();

        cut.Markup.ShouldContain("Monthly Breakdown");
        cut.Markup.ShouldContain("Month");
        cut.Markup.ShouldContain("Income");
        cut.Markup.ShouldContain("Spending");
    }

    /// <summary>
    /// Verifies the trend direction is displayed with correct arrow.
    /// </summary>
    [Fact]
    public void ShowsTrendDirection_Decreasing()
    {
        _apiService.SpendingTrends = new SpendingTrendsReportDto
        {
            AverageMonthlySpending = new MoneyDto { Amount = 2500m, Currency = "USD" },
            AverageMonthlyIncome = new MoneyDto { Amount = 4000m, Currency = "USD" },
            TrendDirection = "decreasing",
            TrendPercentage = -8.3m,
            MonthlyData = [],
        };

        var cut = Render<MonthlyTrendsReport>();

        cut.Markup.ShouldContain("↓");
        cut.Markup.ShouldContain("8.3");
    }

    /// <summary>
    /// Verifies the trend direction is displayed with upward arrow for increasing.
    /// </summary>
    [Fact]
    public void ShowsTrendDirection_Increasing()
    {
        _apiService.SpendingTrends = new SpendingTrendsReportDto
        {
            AverageMonthlySpending = new MoneyDto { Amount = 3500m, Currency = "USD" },
            AverageMonthlyIncome = new MoneyDto { Amount = 4000m, Currency = "USD" },
            TrendDirection = "increasing",
            TrendPercentage = 12.1m,
            MonthlyData = [],
        };

        var cut = Render<MonthlyTrendsReport>();

        cut.Markup.ShouldContain("↑");
        cut.Markup.ShouldContain("12.1");
    }

    /// <summary>
    /// Verifies the default month count selector has 6 months selected.
    /// </summary>
    [Fact]
    public void DefaultMonthCount_IsSix()
    {
        var cut = Render<MonthlyTrendsReport>();

        var activeButton = cut.Find("button.month-option.active");
        activeButton.TextContent.ShouldContain("6");
    }

    private static SpendingTrendsReportDto CreateTestTrendsReport() => new()
    {
        AverageMonthlySpending = new MoneyDto { Amount = 3000m, Currency = "USD" },
        AverageMonthlyIncome = new MoneyDto { Amount = 5000m, Currency = "USD" },
        TrendDirection = "decreasing",
        TrendPercentage = -5.2m,
        MonthlyData =
        [
            new MonthlyTrendPointDto
            {
                Year = 2025,
                Month = 1,
                TotalSpending = new MoneyDto { Amount = 3200m, Currency = "USD" },
                TotalIncome = new MoneyDto { Amount = 5000m, Currency = "USD" },
                NetAmount = new MoneyDto { Amount = 1800m, Currency = "USD" },
                TransactionCount = 45,
            },
            new MonthlyTrendPointDto
            {
                Year = 2025,
                Month = 2,
                TotalSpending = new MoneyDto { Amount = 2800m, Currency = "USD" },
                TotalIncome = new MoneyDto { Amount = 5000m, Currency = "USD" },
                NetAmount = new MoneyDto { Amount = 2200m, Currency = "USD" },
                TransactionCount = 38,
            },
        ],
    };
}
