// <copyright file="BudgetPageTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Pages;
using BudgetExperiment.Client.Services;
using BudgetExperiment.Client.Tests.TestHelpers;
using BudgetExperiment.Contracts.Dtos;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace BudgetExperiment.Client.Tests.Pages;

/// <summary>
/// Unit tests for the <see cref="Budget"/> page component.
/// </summary>
public class BudgetPageTests : BunitContext, IAsyncLifetime
{
    private readonly StubBudgetApiService _apiService = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="BudgetPageTests"/> class.
    /// </summary>
    public BudgetPageTests()
    {
        this.JSInterop.Mode = JSRuntimeMode.Loose;
        this.Services.AddSingleton<IBudgetApiService>(this._apiService);
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
        var cut = Render<Budget>();

        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies the page title is set correctly.
    /// </summary>
    [Fact]
    public void HasCorrectPageTitle()
    {
        var cut = Render<Budget>();

        cut.Markup.ShouldContain("Budget Overview");
    }

    /// <summary>
    /// Verifies empty message when no budget data.
    /// </summary>
    [Fact]
    public void ShowsEmptyMessage_WhenNoBudgetData()
    {
        var cut = Render<Budget>();

        cut.Markup.ShouldContain("No budget data available");
    }

    /// <summary>
    /// Verifies month navigation controls are present.
    /// </summary>
    [Fact]
    public void HasMonthNavigationButtons()
    {
        var cut = Render<Budget>();

        cut.Markup.ShouldContain("month-navigation");
        cut.Markup.ShouldContain("current-month");
    }

    /// <summary>
    /// Verifies current month is displayed.
    /// </summary>
    [Fact]
    public void DisplaysCurrentMonth()
    {
        var cut = Render<Budget>();

        var currentMonth = DateTime.Today.ToString("MMMM yyyy");
        cut.Markup.ShouldContain(currentMonth);
    }

    /// <summary>
    /// Verifies the summary card is shown when data exists.
    /// </summary>
    [Fact]
    public void ShowsSummaryCard_WhenDataExists()
    {
        this._apiService.BudgetSummary = new BudgetSummaryDto
        {
            Year = DateTime.Today.Year,
            Month = DateTime.Today.Month,
            TotalBudgeted = new MoneyDto { Amount = 5000m, Currency = "USD" },
            TotalSpent = new MoneyDto { Amount = 3200m, Currency = "USD" },
            TotalRemaining = new MoneyDto { Amount = 1800m, Currency = "USD" },
            OverallPercentUsed = 64m,
            CategoriesOnTrack = 3,
            CategoriesWarning = 1,
            CategoriesOverBudget = 0,
            CategoriesNoBudgetSet = 2,
            CategoryProgress = [],
        };

        var cut = Render<Budget>();

        cut.Markup.ShouldContain("Monthly Summary");
        cut.Markup.ShouldContain("Total Budgeted");
        cut.Markup.ShouldContain("Total Spent");
        cut.Markup.ShouldContain("Remaining");
    }

    /// <summary>
    /// Verifies on-track status count is shown.
    /// </summary>
    [Fact]
    public void ShowsOnTrackStatusCount()
    {
        this._apiService.BudgetSummary = new BudgetSummaryDto
        {
            Year = DateTime.Today.Year,
            Month = DateTime.Today.Month,
            TotalBudgeted = new MoneyDto { Amount = 5000m, Currency = "USD" },
            TotalSpent = new MoneyDto { Amount = 2000m, Currency = "USD" },
            TotalRemaining = new MoneyDto { Amount = 3000m, Currency = "USD" },
            OverallPercentUsed = 40m,
            CategoriesOnTrack = 5,
            CategoriesWarning = 0,
            CategoriesOverBudget = 0,
            CategoriesNoBudgetSet = 0,
            CategoryProgress = [],
        };

        var cut = Render<Budget>();

        cut.Markup.ShouldContain("5 on track");
    }

    /// <summary>
    /// Verifies warning status count is shown.
    /// </summary>
    [Fact]
    public void ShowsWarningStatusCount()
    {
        this._apiService.BudgetSummary = new BudgetSummaryDto
        {
            Year = DateTime.Today.Year,
            Month = DateTime.Today.Month,
            TotalBudgeted = new MoneyDto { Amount = 5000m, Currency = "USD" },
            TotalSpent = new MoneyDto { Amount = 4200m, Currency = "USD" },
            TotalRemaining = new MoneyDto { Amount = 800m, Currency = "USD" },
            OverallPercentUsed = 84m,
            CategoriesOnTrack = 0,
            CategoriesWarning = 2,
            CategoriesOverBudget = 0,
            CategoriesNoBudgetSet = 0,
            CategoryProgress = [],
        };

        var cut = Render<Budget>();

        cut.Markup.ShouldContain("2 warning");
    }

    /// <summary>
    /// Verifies over-budget status count is shown.
    /// </summary>
    [Fact]
    public void ShowsOverBudgetStatusCount()
    {
        this._apiService.BudgetSummary = new BudgetSummaryDto
        {
            Year = DateTime.Today.Year,
            Month = DateTime.Today.Month,
            TotalBudgeted = new MoneyDto { Amount = 5000m, Currency = "USD" },
            TotalSpent = new MoneyDto { Amount = 5500m, Currency = "USD" },
            TotalRemaining = new MoneyDto { Amount = -500m, Currency = "USD" },
            OverallPercentUsed = 110m,
            CategoriesOnTrack = 0,
            CategoriesWarning = 0,
            CategoriesOverBudget = 1,
            CategoriesNoBudgetSet = 0,
            CategoryProgress = [],
        };

        var cut = Render<Budget>();

        cut.Markup.ShouldContain("1 over budget");
    }

    /// <summary>
    /// Verifies category progress section is shown when progress data exists.
    /// </summary>
    [Fact]
    public void ShowsCategoryProgressSection_WhenDataExists()
    {
        this._apiService.BudgetSummary = new BudgetSummaryDto
        {
            Year = DateTime.Today.Year,
            Month = DateTime.Today.Month,
            TotalBudgeted = new MoneyDto { Amount = 5000m, Currency = "USD" },
            TotalSpent = new MoneyDto { Amount = 3000m, Currency = "USD" },
            TotalRemaining = new MoneyDto { Amount = 2000m, Currency = "USD" },
            OverallPercentUsed = 60m,
            CategoriesOnTrack = 1,
            CategoriesWarning = 0,
            CategoriesOverBudget = 0,
            CategoriesNoBudgetSet = 0,
            CategoryProgress =
            [
                new BudgetProgressDto
                {
                    CategoryId = Guid.NewGuid(),
                    CategoryName = "Groceries",
                    TargetAmount = new MoneyDto { Amount = 500m, Currency = "USD" },
                    SpentAmount = new MoneyDto { Amount = 300m, Currency = "USD" },
                    RemainingAmount = new MoneyDto { Amount = 200m, Currency = "USD" },
                    PercentUsed = 60m,
                    Status = "OnTrack",
                },
            ],
        };

        var cut = Render<Budget>();

        cut.Markup.ShouldContain("By Category");
    }
}
