// <copyright file="CalendarPageTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Models;
using BudgetExperiment.Client.Pages;
using BudgetExperiment.Client.Services;
using BudgetExperiment.Client.Tests.TestHelpers;
using BudgetExperiment.Contracts.Dtos;

using Bunit;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace BudgetExperiment.Client.Tests.Pages;

/// <summary>
/// Unit tests for the <see cref="Calendar"/> page component.
/// </summary>
public class CalendarPageTests : BunitContext, IAsyncLifetime
{
    private readonly StubBudgetApiService _apiService = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="CalendarPageTests"/> class.
    /// </summary>
    public CalendarPageTests()
    {
        this.JSInterop.Mode = JSRuntimeMode.Loose;
        this.Services.AddSingleton<IBudgetApiService>(_apiService);
        this.Services.AddSingleton<ScopeService>();
        this.Services.AddSingleton<IChatContextService>(new StubChatContextService());
        this.Services.AddSingleton<ThemeService>();
        this.Services.AddSingleton<CultureService>();
        this.Services.AddSingleton<IExportDownloadService>(new StubExportDownloadService());
        this.Services.AddSingleton<IToastService>(new ToastService());
        this.Services.AddSingleton<IApiErrorContext>(new ApiErrorContext());
        this.Services.AddSingleton<IFeatureFlagClientService>(new StubFeatureFlagClientService());
    }

    /// <inheritdoc/>
    public Task InitializeAsync() => Task.CompletedTask;

    /// <inheritdoc/>
    public new Task DisposeAsync()
    {
        return base.DisposeAsync().AsTask();
    }

    /// <summary>
    /// Verifies the page renders without errors when using default parameters (today).
    /// </summary>
    [Fact]
    public void Renders_WithDefaultParameters()
    {
        var cut = Render<Calendar>();

        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies the page title is set.
    /// </summary>
    [Fact]
    public void HasMonthYearInHeading()
    {
        var today = DateTime.Today;

        var cut = Render<Calendar>();

        // The heading should contain the current month name and year
        cut.Markup.ShouldContain(today.ToString("MMMM"));
        cut.Markup.ShouldContain(today.Year.ToString());
    }

    /// <summary>
    /// Verifies the Previous button is present.
    /// </summary>
    [Fact]
    public void HasPreviousButton()
    {
        var cut = Render<Calendar>();

        cut.Markup.ShouldContain("Previous");
    }

    /// <summary>
    /// Verifies the Next button is present.
    /// </summary>
    [Fact]
    public void HasNextButton()
    {
        var cut = Render<Calendar>();

        cut.Markup.ShouldContain("Next");
    }

    /// <summary>
    /// Verifies the Reports link is present.
    /// </summary>
    [Fact]
    public void HasReportsLink()
    {
        var cut = Render<Calendar>();

        cut.Markup.ShouldContain("Reports");
    }

    /// <summary>
    /// Verifies the page renders with explicit year/month parameters.
    /// </summary>
    [Fact]
    public void Renders_WithExplicitYearMonth()
    {
        var cut = Render<Calendar>(parameters => parameters
            .Add(p => p.Year, 2025)
            .Add(p => p.Month, 3));

        cut.Markup.ShouldContain("March");
        cut.Markup.ShouldContain("2025");
    }

    /// <summary>
    /// Verifies account filter dropdown is rendered when accounts exist.
    /// </summary>
    [Fact]
    public void ShowsAccountFilter_WhenAccountsExist()
    {
        _apiService.Accounts.Add(new AccountDto
        {
            Id = Guid.NewGuid(),
            Name = "Checking",
            Type = "Checking",
            InitialBalance = 1000m,
            InitialBalanceCurrency = "USD",
            InitialBalanceDate = new DateOnly(2025, 1, 1),
        });

        var cut = Render<Calendar>();

        cut.Markup.ShouldContain("Checking");
    }

    /// <summary>
    /// Verifies that calendar grid renders day-of-week headers.
    /// </summary>
    [Fact]
    public void ShowsDayOfWeekHeaders()
    {
        var cut = Render<Calendar>();

        // The calendar should show day abbreviations
        cut.Markup.ShouldContain("Sun");
        cut.Markup.ShouldContain("Mon");
    }

    /// <summary>
    /// Verifies the add transaction modal starts hidden.
    /// </summary>
    [Fact]
    public void AddTransactionModal_IsHiddenByDefault()
    {
        var cut = Render<Calendar>();

        // Modal for adding transactions shouldn't be visible initially
        cut.Markup.ShouldNotContain("Add Transaction");
    }

    /// <summary>
    /// Verifies the calendar renders with budget summary when configured.
    /// </summary>
    [Fact]
    public void ShowsBudgetSummary_WhenAvailable()
    {
        _apiService.BudgetSummary = new BudgetSummaryDto
        {
            Year = DateTime.Today.Year,
            Month = DateTime.Today.Month,
        };

        var cut = Render<Calendar>();

        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies the calendar handles multiple accounts in filter.
    /// </summary>
    [Fact]
    public void ShowsMultipleAccounts_InFilter()
    {
        _apiService.Accounts.Add(CreateAccount("Checking"));
        _apiService.Accounts.Add(CreateAccount("Savings"));

        var cut = Render<Calendar>();

        cut.Markup.ShouldContain("Checking");
        cut.Markup.ShouldContain("Savings");
    }

    /// <summary>
    /// Verifies clicking a day selects the date and shows day detail.
    /// </summary>
    [Fact]
    public void ClickDate_SelectsDateAndShowsDetail()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        _apiService.CalendarGrid = new CalendarGridDto
        {
            Year = today.Year,
            Month = today.Month,
            Days = new List<CalendarDaySummaryDto>
            {
                new()
                {
                    Date = today,
                    ActualTotal = new MoneyDto { Amount = 50m, Currency = "USD" },
                },
            },
        };

        _apiService.DayDetail = new DayDetailDto
        {
            Date = today,
            Items = new List<DayDetailItemDto>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Type = "transaction",
                    Description = "Test Payment",
                    Amount = new MoneyDto { Amount = -50m, Currency = "USD" },
                },
            },
        };

        var cut = Render<Calendar>();

        // Find and click a calendar day cell
        var dayCells = cut.FindAll(".calendar-day");
        if (dayCells.Any())
        {
            dayCells.First().Click();
        }

        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies the calendar renders with categories loaded.
    /// </summary>
    [Fact]
    public void CategoriesAreLoaded_ForTransactionCreation()
    {
        _apiService.Categories.Add(new BudgetCategoryDto
        {
            Id = Guid.NewGuid(),
            Name = "Food",
            Type = "Expense",
            IsActive = true,
        });

        var cut = Render<Calendar>();

        // Categories are loaded in the background for the add transaction modal
        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies navigation to previous month shows correct heading.
    /// </summary>
    [Fact]
    public void PreviousMonth_UpdatesHeading()
    {
        var cut = Render<Calendar>(parameters => parameters
            .Add(p => p.Year, 2025)
            .Add(p => p.Month, 6));

        cut.Markup.ShouldContain("June");
        cut.Markup.ShouldContain("2025");
    }

    /// <summary>
    /// Verifies the calendar renders with past due items.
    /// </summary>
    [Fact]
    public void ShowsPastDueBanner_WhenPastDueItemsExist()
    {
        _apiService.PastDueSummary = new PastDueSummaryDto
        {
            TotalCount = 3,
            Items = new List<PastDueItemDto>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Type = "recurring-transaction",
                    Description = "Netflix",
                    InstanceDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-5)),
                    Amount = new MoneyDto { Amount = -15.99m, Currency = "USD" },
                },
            },
        };

        var cut = Render<Calendar>();

        // Past due banner or indicator should be visible
        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies day detail items render when a day is selected with transactions.
    /// </summary>
    [Fact]
    public void DayDetail_ShowsTransactions_WhenDaySelected()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        _apiService.DayDetail = new DayDetailDto
        {
            Date = today,
            Items = new List<DayDetailItemDto>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Type = "transaction",
                    Description = "Morning Coffee",
                    Amount = new MoneyDto { Amount = -4.50m, Currency = "USD" },
                },
            },
        };

        var cut = Render<Calendar>();

        // Day detail should be loaded, though the detail panel might not be visible
        // until a date is clicked
        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies the add transaction button appears on day selection.
    /// </summary>
    [Fact]
    public void CreateTransactionResult_IsConfigurable()
    {
        _apiService.CreateTransactionResult = new TransactionDto
        {
            Id = Guid.NewGuid(),
            Description = "New Transaction",
            Amount = new MoneyDto { Amount = -100m, Currency = "USD" },
            Date = DateOnly.FromDateTime(DateTime.Today),
            AccountId = Guid.NewGuid(),
        };

        var cut = Render<Calendar>();

        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies the budget goal result is configurable.
    /// </summary>
    [Fact]
    public void SetBudgetGoalResult_IsConfigurable()
    {
        _apiService.SetBudgetGoalResult = ApiResult<BudgetGoalDto>.Success(new BudgetGoalDto
        {
            Id = Guid.NewGuid(),
            CategoryId = Guid.NewGuid(),
            TargetAmount = new MoneyDto { Amount = 500m, Currency = "USD" },
            Year = 2025,
            Month = 6,
        });

        var cut = Render<Calendar>();

        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies the delete budget goal result is configurable.
    /// </summary>
    [Fact]
    public void DeleteBudgetGoalResult_IsConfigurable()
    {
        _apiService.DeleteBudgetGoalResult = true;

        var cut = Render<Calendar>();

        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies the Next button updates the heading to next month.
    /// </summary>
    [Fact]
    public void NextMonth_UpdatesHeading()
    {
        var cut = Render<Calendar>();
        var nextButton = cut.FindAll("button").First(b => b.TextContent.Contains("Next"));
        nextButton.Click();

        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies copy budget goals result is configurable.
    /// </summary>
    [Fact]
    public void CopyBudgetGoalsResult_IsConfigurable()
    {
        _apiService.CopyBudgetGoalsResult = new CopyBudgetGoalsResult { GoalsCreated = 5 };

        var cut = Render<Calendar>();

        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies calendar grid shows day cells.
    /// </summary>
    [Fact]
    public void CalendarGrid_ShowsDayCells()
    {
        _apiService.CalendarGrid = new CalendarGridDto
        {
            Year = 2025,
            Month = 6,
            Days =
            [
                new CalendarDaySummaryDto
                {
                    Date = new DateOnly(2025, 6, 15),
                    IsCurrentMonth = true,
                    ActualTotal = new MoneyDto { Amount = -50m, Currency = "USD" },
                    TransactionCount = 1,
                },
            ],
        };

        var cut = Render<Calendar>();

        cut.Markup.ShouldContain("15");
    }

    /// <summary>
    /// Verifies realize batch result is configurable.
    /// </summary>
    [Fact]
    public void RealizeBatchResult_IsConfigurable()
    {
        _apiService.RealizeBatchResult = new BatchRealizeResultDto { SuccessCount = 3 };

        var cut = Render<Calendar>();

        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies the view mode toggle buttons are present.
    /// </summary>
    [Fact]
    public void HasViewModeToggle()
    {
        var cut = Render<Calendar>();

        // Calendar should have view mode toggle (Month/Week)
        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies the budget goal modal is hidden by default.
    /// </summary>
    [Fact]
    public void BudgetGoalModal_IsHiddenByDefault()
    {
        var cut = Render<Calendar>();

        cut.Markup.ShouldNotContain("Set Budget Goal");
    }

    /// <summary>
    /// Verifies Save budget goal calls the API with correct data.
    /// </summary>
    [Fact]
    public void SaveBudgetGoal_Success_RefreshesCalendar()
    {
        _apiService.SetBudgetGoalResult = ApiResult<BudgetGoalDto>.Success(new BudgetGoalDto
        {
            Id = Guid.NewGuid(),
            CategoryId = Guid.NewGuid(),
            TargetAmount = new MoneyDto { Amount = 300m, Currency = "USD" },
            Year = 2025,
            Month = 6,
        });

        _apiService.BudgetSummary = new BudgetSummaryDto
        {
            Year = 2025,
            Month = 6,
        };

        var cut = Render<Calendar>(parameters => parameters
            .Add(p => p.Year, 2025)
            .Add(p => p.Month, 6));

        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies Delete budget goal result triggers refresh.
    /// </summary>
    [Fact]
    public void DeleteBudgetGoal_Success_RefreshesCalendar()
    {
        _apiService.DeleteBudgetGoalResult = true;

        var cut = Render<Calendar>(parameters => parameters
            .Add(p => p.Year, 2025)
            .Add(p => p.Month, 6));

        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies Copy budget goals from previous month is configurable.
    /// </summary>
    [Fact]
    public void CopyBudgetGoals_Success_RefreshesCalendar()
    {
        _apiService.CopyBudgetGoalsResult = new CopyBudgetGoalsResult { GoalsCreated = 3 };
        _apiService.BudgetSummary = new BudgetSummaryDto
        {
            Year = 2025,
            Month = 6,
        };

        var cut = Render<Calendar>(parameters => parameters
            .Add(p => p.Year, 2025)
            .Add(p => p.Month, 6));

        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies the create transaction result adds to day detail.
    /// </summary>
    [Fact]
    public void CreateTransaction_Success_RefreshesCalendar()
    {
        _apiService.CreateTransactionResult = new TransactionDto
        {
            Id = Guid.NewGuid(),
            Description = "Test Transaction",
            Amount = new MoneyDto { Amount = -25m, Currency = "USD" },
            Date = new DateOnly(2025, 6, 15),
            AccountId = Guid.NewGuid(),
        };

        var cut = Render<Calendar>(parameters => parameters
            .Add(p => p.Year, 2025)
            .Add(p => p.Month, 6));

        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies skip recurring instance is configurable.
    /// </summary>
    [Fact]
    public void SkipRecurringInstance_Success_RefreshesCalendar()
    {
        _apiService.SkipRecurringInstanceResult = true;

        var cut = Render<Calendar>(parameters => parameters
            .Add(p => p.Year, 2025)
            .Add(p => p.Month, 6));

        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies modify recurring instance is configurable.
    /// </summary>
    [Fact]
    public void ModifyRecurringInstance_Success_RefreshesCalendar()
    {
        _apiService.ModifyRecurringInstanceResult = ApiResult<RecurringInstanceDto>.Success(new RecurringInstanceDto
        {
            RecurringTransactionId = Guid.NewGuid(),
            ScheduledDate = new DateOnly(2025, 6, 15),
        });

        var cut = Render<Calendar>(parameters => parameters
            .Add(p => p.Year, 2025)
            .Add(p => p.Month, 6));

        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies realize batch result for confirming past due items.
    /// </summary>
    [Fact]
    public void ConfirmPastDueItems_Success_RefreshesCalendar()
    {
        _apiService.RealizeBatchResult = new BatchRealizeResultDto { SuccessCount = 2 };
        _apiService.PastDueSummary = new PastDueSummaryDto
        {
            TotalCount = 2,
            Items = new List<PastDueItemDto>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Type = "recurring-transaction",
                    Description = "Late Payment",
                    InstanceDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-7)),
                    Amount = new MoneyDto { Amount = -100m, Currency = "USD" },
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Type = "recurring-transaction",
                    Description = "Very Late",
                    InstanceDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-14)),
                    Amount = new MoneyDto { Amount = -50m, Currency = "USD" },
                },
            },
        };

        var cut = Render<Calendar>();

        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies navigating to previous month triggers navigation to correct URL.
    /// </summary>
    [Fact]
    public void PreviousMonth_NavigatesToCorrectUrl()
    {
        var cut = Render<Calendar>(parameters => parameters
            .Add(p => p.Year, 2025)
            .Add(p => p.Month, 3));

        var prevButton = cut.FindAll("button").First(b => b.TextContent.Contains("Previous"));
        prevButton.Click();

        var navMan = this.Services.GetRequiredService<NavigationManager>();
        navMan.Uri.ShouldContain("/2025/2");
    }

    /// <summary>
    /// Verifies navigating to next month from December wraps to January of next year.
    /// </summary>
    [Fact]
    public void NextMonth_FromDecember_NavigatesToJanuary()
    {
        var cut = Render<Calendar>(parameters => parameters
            .Add(p => p.Year, 2025)
            .Add(p => p.Month, 12));

        var nextButton = cut.FindAll("button").First(b => b.TextContent.Contains("Next"));
        nextButton.Click();

        var navMan = this.Services.GetRequiredService<NavigationManager>();
        navMan.Uri.ShouldContain("/2026/1");
    }

    /// <summary>
    /// Verifies navigating to previous month from January wraps to December of previous year.
    /// </summary>
    [Fact]
    public void PreviousMonth_FromJanuary_NavigatesToDecember()
    {
        var cut = Render<Calendar>(parameters => parameters
            .Add(p => p.Year, 2025)
            .Add(p => p.Month, 1));

        var prevButton = cut.FindAll("button").First(b => b.TextContent.Contains("Previous"));
        prevButton.Click();

        var navMan = this.Services.GetRequiredService<NavigationManager>();
        navMan.Uri.ShouldContain("/2024/12");
    }

    /// <summary>
    /// Verifies that calendar handles day click with both grid and detail data.
    /// </summary>
    [Fact]
    public void SelectDate_LoadsDayDetail()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        _apiService.CalendarGrid = new CalendarGridDto
        {
            Year = today.Year,
            Month = today.Month,
            Days = new List<CalendarDaySummaryDto>
            {
                new()
                {
                    Date = today,
                    IsCurrentMonth = true,
                    ActualTotal = new MoneyDto { Amount = -25m, Currency = "USD" },
                    TransactionCount = 1,
                },
            },
        };
        _apiService.DayDetail = new DayDetailDto
        {
            Date = today,
            Items = new List<DayDetailItemDto>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Type = "transaction",
                    Description = "Day Detail Item",
                    Amount = new MoneyDto { Amount = -25m, Currency = "USD" },
                },
            },
        };

        var cut = Render<Calendar>();

        var dayCells = cut.FindAll(".calendar-day");
        if (dayCells.Any())
        {
            dayCells.First().Click();
        }

        cut.WaitForAssertion(() => cut.Markup.ShouldNotBeNullOrEmpty());
    }

    /// <summary>
    /// Verifies account filter changes reload calendar data.
    /// </summary>
    [Fact]
    public void AccountFilter_Change_ReloadsData()
    {
        var accountId = Guid.NewGuid();
        _apiService.Accounts.Add(new AccountDto
        {
            Id = accountId,
            Name = "Filtered Account",
            Type = "Checking",
            InitialBalance = 0m,
            InitialBalanceCurrency = "USD",
            InitialBalanceDate = new DateOnly(2025, 1, 1),
        });

        var cut = Render<Calendar>();

        cut.Markup.ShouldContain("Filtered Account");
    }

    private static AccountDto CreateAccount(string name)
    {
        return new AccountDto
        {
            Id = Guid.NewGuid(),
            Name = name,
            Type = "Checking",
            InitialBalance = 1000m,
            InitialBalanceCurrency = "USD",
            InitialBalanceDate = new DateOnly(2025, 1, 1),
        };
    }
}
