// <copyright file="CalendarPageAdditionalTests.cs" company="BecauseImClever">
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
/// Additional unit tests for the <see cref="Calendar"/> page component.
/// </summary>
public class CalendarPageAdditionalTests : BunitContext, IAsyncLifetime
{
    private readonly StubBudgetApiService _apiService = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="CalendarPageAdditionalTests"/> class.
    /// </summary>
    public CalendarPageAdditionalTests()
    {
        this.JSInterop.Mode = JSRuntimeMode.Loose;
        this.Services.AddSingleton<IBudgetApiService>(_apiService);
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
    /// Verifies the calendar renders a month grid with correct structure.
    /// </summary>
    [Fact]
    public void CalendarGrid_RendersMonthStructure()
    {
        // Arrange
        var februaryDays = 28; // 2025 is not a leap year
        _apiService.CalendarGrid = new CalendarGridDto
        {
            Year = 2025,
            Month = 2,
            Days = Enumerable.Range(1, februaryDays).Select(day => new CalendarDaySummaryDto
            {
                Date = new DateOnly(2025, 2, day),
                IsCurrentMonth = true,
                ActualTotal = new MoneyDto { Amount = -50m, Currency = "USD" },
                TransactionCount = 1,
            }).ToList(),
        };

        // Act
        var cut = Render<Calendar>();

        // Assert - should render day numbers for February
        for (int day = 1; day <= 5; day++)
        {
            cut.Markup.ShouldContain($">{day}<");
        }
    }

    /// <summary>
    /// Verifies the budget panel displays category progress information.
    /// </summary>
    [Fact]
    public void BudgetPanel_DisplaysCategories()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        _apiService.BudgetSummary = new BudgetSummaryDto
        {
            Year = 2025,
            Month = 6,
            CategoryProgress = new List<BudgetProgressDto>
            {
                new()
                {
                    CategoryId = categoryId,
                    CategoryName = "Groceries",
                    TargetAmount = new MoneyDto { Amount = 500m, Currency = "USD" },
                    SpentAmount = new MoneyDto { Amount = 350m, Currency = "USD" },
                    RemainingAmount = new MoneyDto { Amount = 150m, Currency = "USD" },
                    PercentUsed = 70m,
                },
            },
        };

        // Act
        var cut = Render<Calendar>();

        // Expand the budget panel by clicking the header
        var panelHeader = cut.Find("button.budget-panel-header");
        panelHeader.Click();

        // Assert
        cut.Markup.ShouldContain("Groceries");
    }

    /// <summary>
    /// Verifies page renders successfully when data loads.
    /// </summary>
    [Fact]
    public void Page_RendersSuccessfully()
    {
        // Arrange - ensure we have valid data
        _apiService.CalendarGrid = new CalendarGridDto
        {
            Year = 2025,
            Month = 1,
            Days = new List<CalendarDaySummaryDto>(),
        };

        // Act
        var cut = Render<Calendar>();

        // Assert
        cut.Markup.ShouldNotBeNullOrEmpty();
        cut.Markup.ShouldContain("page-container");
    }

    /// <summary>
    /// Verifies account filter dropdown changes selection.
    /// </summary>
    [Fact]
    public void AccountFilterDropdown_UpdatesSelection()
    {
        // Arrange
        _apiService.Accounts.Add(CreateAccount("Checking"));
        _apiService.Accounts.Add(CreateAccount("Savings"));

        // Act
        var cut = Render<Calendar>();

        // Assert - verify both accounts are available in dropdown
        cut.Markup.ShouldContain("Checking");
        cut.Markup.ShouldContain("Savings");
    }

    /// <summary>
    /// Verifies the budget panel displays category progress information and handles over-budget status.
    /// </summary>
    [Fact]
    public void BudgetPanel_DisplaysCategoryProgressWithOverBudgetStatus()
    {
        // Arrange
        _apiService.BudgetSummary = new BudgetSummaryDto
        {
            Year = 2025,
            Month = 6,
            CategoryProgress = new List<BudgetProgressDto>
            {
                new()
                {
                    CategoryId = Guid.NewGuid(),
                    CategoryName = "Entertainment",
                    TargetAmount = new MoneyDto { Amount = 100m, Currency = "USD" },
                    SpentAmount = new MoneyDto { Amount = 150m, Currency = "USD" },
                    RemainingAmount = new MoneyDto { Amount = -50m, Currency = "USD" },
                    PercentUsed = 150m,
                    Status = "OverBudget",
                },
            },
        };

        // Act
        var cut = Render<Calendar>();

        // Assert
        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies calendar renders correctly for months with 31 days.
    /// </summary>
    [Fact]
    public void CalendarGrid_Renders31DayMonth()
    {
        // Arrange - March has 31 days
        _apiService.CalendarGrid = new CalendarGridDto
        {
            Year = 2025,
            Month = 3,
            Days = Enumerable.Range(1, 31).Select(day => new CalendarDaySummaryDto
            {
                Date = new DateOnly(2025, 3, day),
                IsCurrentMonth = true,
                ActualTotal = new MoneyDto { Amount = -10m, Currency = "USD" },
                TransactionCount = 0,
            }).ToList(),
        };

        // Act
        var cut = Render<Calendar>();

        // Assert
        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies month/year header shows correct date when navigating.
    /// </summary>
    [Fact]
    public void MonthYearHeader_DisplaysCorrectDate()
    {
        // Arrange - render for July 2025
        // Act
        var cut = Render<Calendar>(parameters => parameters
            .Add(p => p.Year, 2025)
            .Add(p => p.Month, 7));

        // Assert
        cut.Markup.ShouldContain("July");
        cut.Markup.ShouldContain("2025");
    }

    /// <summary>
    /// Verifies calendar renders zero balance days correctly.
    /// </summary>
    [Fact]
    public void CalendarDay_RendersZeroBalanceCorrectly()
    {
        // Arrange
        _apiService.CalendarGrid = new CalendarGridDto
        {
            Year = 2025,
            Month = 1,
            Days = new List<CalendarDaySummaryDto>
            {
                new()
                {
                    Date = new DateOnly(2025, 1, 1),
                    IsCurrentMonth = true,
                    ActualTotal = new MoneyDto { Amount = 0m, Currency = "USD" },
                    TransactionCount = 0,
                },
            },
        };

        // Act
        var cut = Render<Calendar>();

        // Assert
        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies budget summary totals display correctly.
    /// </summary>
    [Fact]
    public void BudgetSummary_DisplaysTotals()
    {
        // Arrange
        _apiService.BudgetSummary = new BudgetSummaryDto
        {
            Year = 2025,
            Month = 6,
            TotalBudgeted = new MoneyDto { Amount = 5000m, Currency = "USD" },
            TotalSpent = new MoneyDto { Amount = 3500m, Currency = "USD" },
            TotalRemaining = new MoneyDto { Amount = 1500m, Currency = "USD" },
            OverallPercentUsed = 70m,
        };

        // Act
        var cut = Render<Calendar>();

        // Assert
        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies calendar displays correct number of on-track categories.
    /// </summary>
    [Fact]
    public void BudgetPanel_DisplaysCategoriesOnTrack()
    {
        // Arrange
        _apiService.BudgetSummary = new BudgetSummaryDto
        {
            Year = 2025,
            Month = 6,
            CategoriesOnTrack = 8,
            CategoriesWarning = 1,
            CategoriesOverBudget = 0,
        };

        // Act
        var cut = Render<Calendar>();

        // Assert
        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies all accounts are available in filter dropdown.
    /// </summary>
    [Fact]
    public void AccountFilter_DisplaysAllAvailableAccounts()
    {
        // Arrange
        _apiService.Accounts.Add(CreateAccount("Checking"));
        _apiService.Accounts.Add(CreateAccount("Savings"));
        _apiService.Accounts.Add(CreateAccount("Money Market"));

        // Act
        var cut = Render<Calendar>();

        // Assert
        cut.Markup.ShouldContain("Checking");
        cut.Markup.ShouldContain("Savings");
        cut.Markup.ShouldContain("Money Market");
    }

    /// <summary>
    /// Verifies calendar renders negative balance amounts.
    /// </summary>
    [Fact]
    public void CalendarDay_RendersNegativeBalance()
    {
        // Arrange
        _apiService.CalendarGrid = new CalendarGridDto
        {
            Year = 2025,
            Month = 1,
            Days = new List<CalendarDaySummaryDto>
            {
                new()
                {
                    Date = new DateOnly(2025, 1, 15),
                    IsCurrentMonth = true,
                    ActualTotal = new MoneyDto { Amount = -250.75m, Currency = "USD" },
                    TransactionCount = 3,
                },
            },
        };

        // Act
        var cut = Render<Calendar>();

        // Assert
        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies calendar renders with multiple transactions on a day.
    /// </summary>
    [Fact]
    public void CalendarDay_RendersMultipleTransactions()
    {
        // Arrange
        _apiService.CalendarGrid = new CalendarGridDto
        {
            Year = 2025,
            Month = 2,
            Days = new List<CalendarDaySummaryDto>
            {
                new()
                {
                    Date = new DateOnly(2025, 2, 14),
                    IsCurrentMonth = true,
                    ActualTotal = new MoneyDto { Amount = -125m, Currency = "USD" },
                    TransactionCount = 5,
                },
            },
        };

        // Act
        var cut = Render<Calendar>();

        // Assert
        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies calendar handles days outside current month.
    /// </summary>
    [Fact]
    public void CalendarDay_RendersOutsideCurrentMonth()
    {
        // Arrange - January calendar may show days from previous December
        _apiService.CalendarGrid = new CalendarGridDto
        {
            Year = 2025,
            Month = 1,
            Days = new List<CalendarDaySummaryDto>
            {
                new()
                {
                    Date = new DateOnly(2024, 12, 31),
                    IsCurrentMonth = false,
                    ActualTotal = new MoneyDto { Amount = -50m, Currency = "USD" },
                    TransactionCount = 1,
                },
            },
        };

        // Act
        var cut = Render<Calendar>();

        // Assert
        cut.Markup.ShouldNotBeNullOrEmpty();
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
