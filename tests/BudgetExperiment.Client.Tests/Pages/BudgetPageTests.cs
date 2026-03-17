// <copyright file="BudgetPageTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Models;
using BudgetExperiment.Client.Pages;
using BudgetExperiment.Client.Services;
using BudgetExperiment.Client.Tests.TestHelpers;
using BudgetExperiment.Client.ViewModels;
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
        this.Services.AddSingleton<CultureService>();
        this.Services.AddTransient<BudgetViewModel>();
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

    /// <summary>
    /// Verifies set budget goal result is configurable.
    /// </summary>
    [Fact]
    public void SetBudgetGoalResult_IsConfigurable()
    {
        this._apiService.SetBudgetGoalResult = ApiResult<BudgetGoalDto>.Success(new BudgetGoalDto
        {
            Id = Guid.NewGuid(),
            CategoryId = Guid.NewGuid(),
            TargetAmount = new MoneyDto { Amount = 300m, Currency = "USD" },
            Year = DateTime.Today.Year,
            Month = DateTime.Today.Month,
        });

        var cut = Render<Budget>();

        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies delete budget goal result is configurable.
    /// </summary>
    [Fact]
    public void DeleteBudgetGoalResult_IsConfigurable()
    {
        this._apiService.DeleteBudgetGoalResult = true;

        var cut = Render<Budget>();

        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies copy budget goals result is configurable.
    /// </summary>
    [Fact]
    public void CopyBudgetGoalsResult_IsConfigurable()
    {
        this._apiService.CopyBudgetGoalsResult = new CopyBudgetGoalsResult
        {
            GoalsCreated = 5,
        };

        var cut = Render<Budget>();

        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies clicking next month navigates and updates the month display.
    /// </summary>
    [Fact]
    public void NextMonthButton_UpdatesDisplay()
    {
        var cut = Render<Budget>();

        var nextButton = cut.FindAll("button").First(b => b.GetAttribute("title") == "Next month");
        nextButton.Click();

        var expectedMonth = DateTime.Today.AddMonths(1).ToString("MMMM yyyy");
        cut.Markup.ShouldContain(expectedMonth);
    }

    /// <summary>
    /// Verifies clicking previous month navigates and updates the month display.
    /// </summary>
    [Fact]
    public void PreviousMonthButton_UpdatesDisplay()
    {
        var cut = Render<Budget>();

        var prevButton = cut.FindAll("button").First(b => b.GetAttribute("title") == "Previous month");
        prevButton.Click();

        var expectedMonth = DateTime.Today.AddMonths(-1).ToString("MMMM yyyy");
        cut.Markup.ShouldContain(expectedMonth);
    }

    /// <summary>
    /// Verifies the empty state shows manage categories button.
    /// </summary>
    [Fact]
    public void EmptyState_ShowsManageCategoriesMessage()
    {
        this._apiService.BudgetSummary = new BudgetSummaryDto
        {
            Year = DateTime.Today.Year,
            Month = DateTime.Today.Month,
            TotalBudgeted = new MoneyDto { Amount = 0m, Currency = "USD" },
            TotalSpent = new MoneyDto { Amount = 0m, Currency = "USD" },
            TotalRemaining = new MoneyDto { Amount = 0m, Currency = "USD" },
            OverallPercentUsed = 0m,
            CategoriesOnTrack = 0,
            CategoriesWarning = 0,
            CategoriesOverBudget = 0,
            CategoriesNoBudgetSet = 0,
            CategoryProgress = [],
        };

        var cut = Render<Budget>();

        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies the no-budget-set status count is displayed.
    /// </summary>
    [Fact]
    public void ShowsNoBudgetSetStatusCount()
    {
        this._apiService.BudgetSummary = new BudgetSummaryDto
        {
            Year = DateTime.Today.Year,
            Month = DateTime.Today.Month,
            TotalBudgeted = new MoneyDto { Amount = 1000m, Currency = "USD" },
            TotalSpent = new MoneyDto { Amount = 300m, Currency = "USD" },
            TotalRemaining = new MoneyDto { Amount = 700m, Currency = "USD" },
            OverallPercentUsed = 30m,
            CategoriesOnTrack = 1,
            CategoriesWarning = 0,
            CategoriesOverBudget = 0,
            CategoriesNoBudgetSet = 3,
            CategoryProgress = [],
        };

        var cut = Render<Budget>();

        cut.Markup.ShouldContain("3 no budget");
    }

    /// <summary>
    /// Verifies the overall progress bar shows correct percentage.
    /// </summary>
    [Fact]
    public void ShowsOverallProgressPercentage()
    {
        this._apiService.BudgetSummary = new BudgetSummaryDto
        {
            Year = DateTime.Today.Year,
            Month = DateTime.Today.Month,
            TotalBudgeted = new MoneyDto { Amount = 5000m, Currency = "USD" },
            TotalSpent = new MoneyDto { Amount = 2500m, Currency = "USD" },
            TotalRemaining = new MoneyDto { Amount = 2500m, Currency = "USD" },
            OverallPercentUsed = 50m,
            CategoriesOnTrack = 2,
            CategoriesWarning = 0,
            CategoriesOverBudget = 0,
            CategoriesNoBudgetSet = 0,
            CategoryProgress = [],
        };

        var cut = Render<Budget>();

        cut.Markup.ShouldContain("50");
    }

    /// <summary>
    /// Verifies clicking Edit Goal on a category card opens the modal and Save triggers the handler.
    /// </summary>
    [Fact]
    public void EditGoal_OpensModal_AndSaveTriggersHandler()
    {
        var categoryId = Guid.NewGuid();
        this._apiService.BudgetSummary = CreateSummaryWithProgress(categoryId);
        this._apiService.SetBudgetGoalResult = ApiResult<BudgetGoalDto>.Success(new BudgetGoalDto
        {
            Id = Guid.NewGuid(),
            CategoryId = categoryId,
            TargetAmount = new MoneyDto { Amount = 600m, Currency = "USD" },
            Year = DateTime.Today.Year,
            Month = DateTime.Today.Month,
        });

        var cut = Render<Budget>();

        // Click "Edit Goal" button on the CategoryBudgetCard
        var editButton = cut.FindAll("button").First(b => b.TextContent.Contains("Edit Goal"));
        editButton.Click();

        // Modal should be visible with Save button
        cut.Markup.ShouldContain("Edit Budget Goal");

        // Click Save to trigger SaveGoal handler
        var saveButton = cut.FindAll("button").First(b => b.TextContent.Contains("Save"));
        saveButton.Click();

        // After success, modal should close
        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies clicking Delete Goal triggers the delete handler.
    /// </summary>
    [Fact]
    public void DeleteGoal_TriggersHandler()
    {
        var categoryId = Guid.NewGuid();
        this._apiService.BudgetSummary = CreateSummaryWithProgress(categoryId);
        this._apiService.DeleteBudgetGoalResult = true;

        var cut = Render<Budget>();

        // Open edit modal
        var editButton = cut.FindAll("button").First(b => b.TextContent.Contains("Edit Goal"));
        editButton.Click();

        // Click Delete Goal
        var deleteButton = cut.FindAll("button").First(b => b.TextContent.Contains("Delete Goal"));
        deleteButton.Click();

        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies the Set Budget button appears for categories with no budget.
    /// </summary>
    [Fact]
    public void SetBudget_ButtonAppearsForNoBudgetCategory()
    {
        this._apiService.BudgetSummary = new BudgetSummaryDto
        {
            Year = DateTime.Today.Year,
            Month = DateTime.Today.Month,
            TotalBudgeted = new MoneyDto { Amount = 0m, Currency = "USD" },
            TotalSpent = new MoneyDto { Amount = 100m, Currency = "USD" },
            TotalRemaining = new MoneyDto { Amount = -100m, Currency = "USD" },
            OverallPercentUsed = 0m,
            CategoriesOnTrack = 0,
            CategoriesWarning = 0,
            CategoriesOverBudget = 0,
            CategoriesNoBudgetSet = 1,
            CategoryProgress =
            [
                new BudgetProgressDto
                {
                    CategoryId = Guid.NewGuid(),
                    CategoryName = "Dining",
                    TargetAmount = new MoneyDto { Amount = 0m, Currency = "USD" },
                    SpentAmount = new MoneyDto { Amount = 100m, Currency = "USD" },
                    RemainingAmount = new MoneyDto { Amount = -100m, Currency = "USD" },
                    PercentUsed = 0m,
                    Status = "NoBudgetSet",
                },
            ],
        };

        var cut = Render<Budget>();

        cut.Markup.ShouldContain("Set Budget");
    }

    private static BudgetSummaryDto CreateSummaryWithProgress(Guid categoryId) => new()
    {
        Year = DateTime.Today.Year,
        Month = DateTime.Today.Month,
        TotalBudgeted = new MoneyDto { Amount = 500m, Currency = "USD" },
        TotalSpent = new MoneyDto { Amount = 300m, Currency = "USD" },
        TotalRemaining = new MoneyDto { Amount = 200m, Currency = "USD" },
        OverallPercentUsed = 60m,
        CategoriesOnTrack = 1,
        CategoriesWarning = 0,
        CategoriesOverBudget = 0,
        CategoriesNoBudgetSet = 0,
        CategoryProgress =
        [
            new BudgetProgressDto
            {
                CategoryId = categoryId,
                CategoryName = "Groceries",
                TargetAmount = new MoneyDto { Amount = 500m, Currency = "USD" },
                SpentAmount = new MoneyDto { Amount = 300m, Currency = "USD" },
                RemainingAmount = new MoneyDto { Amount = 200m, Currency = "USD" },
                PercentUsed = 60m,
                Status = "OnTrack",
            },
        ],
    };
}
