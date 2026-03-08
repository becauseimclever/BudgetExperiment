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
        this.Services.AddSingleton<IBudgetApiService>(this._apiService);
        this.Services.AddSingleton<IToastService>(new ToastService());
        this.Services.AddSingleton<IExportDownloadService>(new StubExportDownloadService());
        this.Services.AddSingleton<ScopeService>();
        this.Services.AddSingleton<ThemeService>();
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
        this._apiService.SpendingTrends = new SpendingTrendsReportDto
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
        this._apiService.Categories.Add(new BudgetCategoryDto
        {
            Id = Guid.NewGuid(),
            Name = "Groceries",
            Type = "Expense",
            IsActive = true,
        });

        var cut = Render<MonthlyTrendsReport>();

        cut.Markup.ShouldContain("Groceries");
    }
}
