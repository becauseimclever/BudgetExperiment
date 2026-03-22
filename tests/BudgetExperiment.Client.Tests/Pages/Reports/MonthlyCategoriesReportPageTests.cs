// <copyright file="MonthlyCategoriesReportPageTests.cs" company="BecauseImClever">
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
/// Unit tests for the <see cref="MonthlyCategoriesReport"/> page component.
/// </summary>
public class MonthlyCategoriesReportPageTests : BunitContext, IAsyncLifetime
{
    private readonly StubBudgetApiService _apiService = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="MonthlyCategoriesReportPageTests"/> class.
    /// </summary>
    public MonthlyCategoriesReportPageTests()
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
        var cut = Render<MonthlyCategoriesReport>();

        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies the page title is set correctly.
    /// </summary>
    [Fact]
    public void HasCorrectPageTitle()
    {
        var cut = Render<MonthlyCategoriesReport>();

        cut.Markup.ShouldContain("Category Spending");
    }

    /// <summary>
    /// Verifies the empty state when no data.
    /// </summary>
    [Fact]
    public void ShowsEmptyState_WhenNoData()
    {
        var cut = Render<MonthlyCategoriesReport>();

        cut.Markup.ShouldContain("No spending data for the selected period");
    }

    /// <summary>
    /// Verifies the calendar link is present.
    /// </summary>
    [Fact]
    public void HasCalendarLink()
    {
        var cut = Render<MonthlyCategoriesReport>();

        cut.Markup.ShouldContain("Calendar");
    }

    /// <summary>
    /// Verifies report shows summary when data exists.
    /// </summary>
    [Fact]
    public void ShowsSummary_WhenDataExists()
    {
        _apiService.DateRangeCategoryReport = new DateRangeCategoryReportDto
        {
            TotalSpending = new MoneyDto { Amount = 2500m, Currency = "USD" },
            TotalIncome = new MoneyDto { Amount = 5000m, Currency = "USD" },
            Categories =
            [
                new CategorySpendingDto
                {
                    CategoryId = Guid.NewGuid(),
                    CategoryName = "Groceries",
                    Amount = new MoneyDto { Amount = 800m, Currency = "USD" },
                    TransactionCount = 15,
                    Percentage = 32m,
                },
            ],
        };

        var cut = Render<MonthlyCategoriesReport>();

        cut.Markup.ShouldContain("Summary");
        cut.Markup.ShouldContain("Total Spending");
        cut.Markup.ShouldContain("Total Income");
        cut.Markup.ShouldContain("Net");
    }

    /// <summary>
    /// Verifies categories list is shown when data exists.
    /// </summary>
    [Fact]
    public void ShowsCategoriesList_WhenDataExists()
    {
        _apiService.DateRangeCategoryReport = new DateRangeCategoryReportDto
        {
            TotalSpending = new MoneyDto { Amount = 1000m, Currency = "USD" },
            TotalIncome = new MoneyDto { Amount = 3000m, Currency = "USD" },
            Categories =
            [
                new CategorySpendingDto
                {
                    CategoryId = Guid.NewGuid(),
                    CategoryName = "Restaurants",
                    Amount = new MoneyDto { Amount = 400m, Currency = "USD" },
                    TransactionCount = 8,
                    Percentage = 40m,
                },
            ],
        };

        var cut = Render<MonthlyCategoriesReport>();

        cut.Markup.ShouldContain("Categories");
        cut.Markup.ShouldContain("Restaurants");
        cut.Markup.ShouldContain("8 txns");
    }

    /// <summary>
    /// Verifies the empty state hint text.
    /// </summary>
    [Fact]
    public void EmptyState_HasHintText()
    {
        var cut = Render<MonthlyCategoriesReport>();

        cut.Markup.ShouldContain("Transactions will appear here once you have spending in categories");
    }

    /// <summary>
    /// Verifies the category count stat is displayed.
    /// </summary>
    [Fact]
    public void ShowsCategoryCountStat()
    {
        _apiService.DateRangeCategoryReport = new DateRangeCategoryReportDto
        {
            TotalSpending = new MoneyDto { Amount = 500m, Currency = "USD" },
            TotalIncome = new MoneyDto { Amount = 2000m, Currency = "USD" },
            Categories =
            [
                new CategorySpendingDto
                {
                    CategoryId = Guid.NewGuid(),
                    CategoryName = "Food",
                    Amount = new MoneyDto { Amount = 300m, Currency = "USD" },
                    TransactionCount = 5,
                    Percentage = 60m,
                },
                new CategorySpendingDto
                {
                    CategoryId = Guid.NewGuid(),
                    CategoryName = "Transport",
                    Amount = new MoneyDto { Amount = 200m, Currency = "USD" },
                    TransactionCount = 3,
                    Percentage = 40m,
                },
            ],
        };

        var cut = Render<MonthlyCategoriesReport>();

        cut.Markup.ShouldContain("Categories");
    }
}
