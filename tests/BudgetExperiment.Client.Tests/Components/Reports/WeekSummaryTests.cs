// <copyright file="WeekSummaryTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using Bunit;

using BudgetExperiment.Client.Components.Reports;
using BudgetExperiment.Client.Services;
using BudgetExperiment.Contracts.Dtos;

using Microsoft.Extensions.DependencyInjection;

namespace BudgetExperiment.Client.Tests.Components.Reports;

/// <summary>
/// Unit tests for the WeekSummary component.
/// </summary>
public class WeekSummaryTests : BunitContext, IAsyncLifetime
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WeekSummaryTests"/> class.
    /// </summary>
    public WeekSummaryTests()
    {
        this.JSInterop.Mode = JSRuntimeMode.Loose;
        this.Services.AddSingleton<ThemeService>();
    }

    /// <inheritdoc/>
    public Task InitializeAsync() => Task.CompletedTask;

    /// <inheritdoc/>
    public new Task DisposeAsync() => base.DisposeAsync().AsTask();

    /// <summary>
    /// Verifies that the component renders the week summary title when visible.
    /// </summary>
    [Fact]
    public void WeekSummary_RendersTitle_WhenVisibleWithData()
    {
        // Arrange
        var days = CreateWeekDays(new DateOnly(2026, 2, 1));

        // Act
        var cut = Render<WeekSummary>(p => p
            .Add(x => x.Days, days)
            .Add(x => x.IsVisible, true));

        // Assert
        Assert.Contains("Week Summary", cut.Markup);
    }

    /// <summary>
    /// Verifies that the component does not render when IsVisible is false.
    /// </summary>
    [Fact]
    public void WeekSummary_DoesNotRender_WhenNotVisible()
    {
        // Arrange
        var days = CreateWeekDays(new DateOnly(2026, 2, 1));

        // Act
        var cut = Render<WeekSummary>(p => p
            .Add(x => x.Days, days)
            .Add(x => x.IsVisible, false));

        // Assert
        Assert.DoesNotContain("Week Summary", cut.Markup);
    }

    /// <summary>
    /// Verifies that the component does not render when Days is empty.
    /// </summary>
    [Fact]
    public void WeekSummary_DoesNotRender_WhenDaysEmpty()
    {
        // Act
        var cut = Render<WeekSummary>(p => p
            .Add(x => x.Days, Array.Empty<CalendarDaySummaryDto>())
            .Add(x => x.IsVisible, true));

        // Assert
        Assert.DoesNotContain("Week Summary", cut.Markup);
    }

    /// <summary>
    /// Verifies that the component displays income and spending totals.
    /// </summary>
    [Fact]
    public void WeekSummary_ShowsIncomeAndSpending_Correctly()
    {
        // Arrange — 3 income days ($100 each) + 2 spending days (-$50 each)
        var days = new List<CalendarDaySummaryDto>
        {
            CreateDay(new DateOnly(2026, 2, 1), actualAmount: 100m, txnCount: 1),
            CreateDay(new DateOnly(2026, 2, 2), actualAmount: 100m, txnCount: 1),
            CreateDay(new DateOnly(2026, 2, 3), actualAmount: 100m, txnCount: 1),
            CreateDay(new DateOnly(2026, 2, 4), actualAmount: -50m, txnCount: 1),
            CreateDay(new DateOnly(2026, 2, 5), actualAmount: -50m, txnCount: 1),
            CreateDay(new DateOnly(2026, 2, 6), actualAmount: 0m, txnCount: 0),
            CreateDay(new DateOnly(2026, 2, 7), actualAmount: 0m, txnCount: 0),
        };

        // Act
        var cut = Render<WeekSummary>(p => p
            .Add(x => x.Days, days)
            .Add(x => x.IsVisible, true));

        // Assert
        Assert.Contains("$300.00", cut.Markup); // Income
        Assert.Contains("$100.00", cut.Markup); // Spending
    }

    /// <summary>
    /// Verifies that net amount is calculated correctly.
    /// </summary>
    [Fact]
    public void WeekSummary_ShowsNetAmount_Correctly()
    {
        // Arrange — net = 200 - 75 = +125
        var days = new List<CalendarDaySummaryDto>
        {
            CreateDay(new DateOnly(2026, 2, 1), actualAmount: 200m, txnCount: 1),
            CreateDay(new DateOnly(2026, 2, 2), actualAmount: -75m, txnCount: 2),
            CreateDay(new DateOnly(2026, 2, 3), actualAmount: 0m, txnCount: 0),
            CreateDay(new DateOnly(2026, 2, 4), actualAmount: 0m, txnCount: 0),
            CreateDay(new DateOnly(2026, 2, 5), actualAmount: 0m, txnCount: 0),
            CreateDay(new DateOnly(2026, 2, 6), actualAmount: 0m, txnCount: 0),
            CreateDay(new DateOnly(2026, 2, 7), actualAmount: 0m, txnCount: 0),
        };

        // Act
        var cut = Render<WeekSummary>(p => p
            .Add(x => x.Days, days)
            .Add(x => x.IsVisible, true));

        // Assert — net = +125
        Assert.Contains("+$125.00", cut.Markup);
    }

    /// <summary>
    /// Verifies that negative net shows negative class.
    /// </summary>
    [Fact]
    public void WeekSummary_ShowsNegativeNet_WhenSpendingGreater()
    {
        // Arrange — net = 50 - 200 = -150
        var days = new List<CalendarDaySummaryDto>
        {
            CreateDay(new DateOnly(2026, 2, 1), actualAmount: 50m, txnCount: 1),
            CreateDay(new DateOnly(2026, 2, 2), actualAmount: -200m, txnCount: 1),
            CreateDay(new DateOnly(2026, 2, 3), actualAmount: 0m, txnCount: 0),
            CreateDay(new DateOnly(2026, 2, 4), actualAmount: 0m, txnCount: 0),
            CreateDay(new DateOnly(2026, 2, 5), actualAmount: 0m, txnCount: 0),
            CreateDay(new DateOnly(2026, 2, 6), actualAmount: 0m, txnCount: 0),
            CreateDay(new DateOnly(2026, 2, 7), actualAmount: 0m, txnCount: 0),
        };

        // Act
        var cut = Render<WeekSummary>(p => p
            .Add(x => x.Days, days)
            .Add(x => x.IsVisible, true));

        // Assert
        Assert.Contains("-$150.00", cut.Markup);
        var netValues = cut.FindAll(".stat-item.net .stat-value");
        Assert.Single(netValues);
        Assert.Contains("negative", netValues[0].ClassList);
    }

    /// <summary>
    /// Verifies that the daily breakdown shows 7 daily items.
    /// </summary>
    [Fact]
    public void WeekSummary_ShowsDailyBreakdown_ForAllDays()
    {
        // Arrange
        var days = CreateWeekDays(new DateOnly(2026, 2, 1));

        // Act
        var cut = Render<WeekSummary>(p => p
            .Add(x => x.Days, days)
            .Add(x => x.IsVisible, true));

        // Assert
        var dailyItems = cut.FindAll(".daily-item");
        Assert.Equal(7, dailyItems.Count);
    }

    /// <summary>
    /// Verifies that the date range label is displayed correctly.
    /// </summary>
    [Fact]
    public void WeekSummary_ShowsDateRange_Correctly()
    {
        // Arrange
        var days = CreateWeekDays(new DateOnly(2026, 2, 1));

        // Act
        var cut = Render<WeekSummary>(p => p
            .Add(x => x.Days, days)
            .Add(x => x.IsVisible, true));

        // Assert
        Assert.Contains("Feb 1", cut.Markup);
        Assert.Contains("Feb 7, 2026", cut.Markup);
    }

    /// <summary>
    /// Verifies that the close button invokes the OnClose callback.
    /// </summary>
    [Fact]
    public void WeekSummary_CloseButton_InvokesOnClose()
    {
        // Arrange
        var days = CreateWeekDays(new DateOnly(2026, 2, 1));
        var closeClicked = false;

        // Act
        var cut = Render<WeekSummary>(p => p
            .Add(x => x.Days, days)
            .Add(x => x.IsVisible, true)
            .Add(x => x.OnClose, () => { closeClicked = true; }));

        cut.Find(".week-summary-close").Click();

        // Assert
        Assert.True(closeClicked);
    }

    /// <summary>
    /// Verifies that the component has an accessible region label.
    /// </summary>
    [Fact]
    public void WeekSummary_HasAccessibleRegionLabel()
    {
        // Arrange
        var days = CreateWeekDays(new DateOnly(2026, 2, 1));

        // Act
        var cut = Render<WeekSummary>(p => p
            .Add(x => x.Days, days)
            .Add(x => x.IsVisible, true));

        // Assert
        var region = cut.Find("[role='region']");
        Assert.Equal("Weekly spending summary", region.GetAttribute("aria-label"));
    }

    /// <summary>
    /// Verifies that the daily breakdown list has proper ARIA label.
    /// </summary>
    [Fact]
    public void WeekSummary_DailyBreakdown_HasAriaLabel()
    {
        // Arrange
        var days = CreateWeekDays(new DateOnly(2026, 2, 1));

        // Act
        var cut = Render<WeekSummary>(p => p
            .Add(x => x.Days, days)
            .Add(x => x.IsVisible, true));

        // Assert
        var list = cut.Find("[role='list']");
        Assert.Equal("Daily spending breakdown", list.GetAttribute("aria-label"));
    }

    /// <summary>
    /// Verifies that the today highlight is applied to the correct day.
    /// </summary>
    [Fact]
    public void WeekSummary_HighlightsToday_InDailyBreakdown()
    {
        // Arrange — make the 3rd day "today"
        var startDate = new DateOnly(2026, 2, 1);
        var days = new List<CalendarDaySummaryDto>();
        for (int i = 0; i < 7; i++)
        {
            var day = CreateDay(startDate.AddDays(i), actualAmount: -10m * (i + 1), txnCount: i + 1);
            day.IsToday = i == 2;
            days.Add(day);
        }

        // Act
        var cut = Render<WeekSummary>(p => p
            .Add(x => x.Days, days)
            .Add(x => x.IsVisible, true));

        // Assert
        var todayItems = cut.FindAll(".daily-item.today");
        Assert.Single(todayItems);
    }

    /// <summary>
    /// Verifies that the footer shows the total transaction count.
    /// </summary>
    [Fact]
    public void WeekSummary_ShowsTotalTransactionCount()
    {
        // Arrange — total = 1 + 2 + 3 = 6 transactions
        var days = new List<CalendarDaySummaryDto>
        {
            CreateDay(new DateOnly(2026, 2, 1), actualAmount: -10m, txnCount: 1),
            CreateDay(new DateOnly(2026, 2, 2), actualAmount: -20m, txnCount: 2),
            CreateDay(new DateOnly(2026, 2, 3), actualAmount: -30m, txnCount: 3),
            CreateDay(new DateOnly(2026, 2, 4), actualAmount: 0m, txnCount: 0),
            CreateDay(new DateOnly(2026, 2, 5), actualAmount: 0m, txnCount: 0),
            CreateDay(new DateOnly(2026, 2, 6), actualAmount: 0m, txnCount: 0),
            CreateDay(new DateOnly(2026, 2, 7), actualAmount: 0m, txnCount: 0),
        };

        // Act
        var cut = Render<WeekSummary>(p => p
            .Add(x => x.Days, days)
            .Add(x => x.IsVisible, true));

        // Assert
        Assert.Contains("6 transactions this week", cut.Markup);
    }

    /// <summary>
    /// Verifies that the transaction count uses singular form for 1 transaction.
    /// </summary>
    [Fact]
    public void WeekSummary_UsesSingularTransaction_ForOne()
    {
        // Arrange
        var days = new List<CalendarDaySummaryDto>
        {
            CreateDay(new DateOnly(2026, 2, 1), actualAmount: -10m, txnCount: 1),
            CreateDay(new DateOnly(2026, 2, 2), actualAmount: 0m, txnCount: 0),
            CreateDay(new DateOnly(2026, 2, 3), actualAmount: 0m, txnCount: 0),
            CreateDay(new DateOnly(2026, 2, 4), actualAmount: 0m, txnCount: 0),
            CreateDay(new DateOnly(2026, 2, 5), actualAmount: 0m, txnCount: 0),
            CreateDay(new DateOnly(2026, 2, 6), actualAmount: 0m, txnCount: 0),
            CreateDay(new DateOnly(2026, 2, 7), actualAmount: 0m, txnCount: 0),
        };

        // Act
        var cut = Render<WeekSummary>(p => p
            .Add(x => x.Days, days)
            .Add(x => x.IsVisible, true));

        // Assert
        Assert.Contains("1 transaction this week", cut.Markup);
    }

    /// <summary>
    /// Verifies that the average daily spending is calculated correctly.
    /// </summary>
    [Fact]
    public void WeekSummary_CalculatesDailyAverage_FromDaysWithTransactions()
    {
        // Arrange — 2 spending days: $50 + $100 = $150 total, avg = $75
        var days = new List<CalendarDaySummaryDto>
        {
            CreateDay(new DateOnly(2026, 2, 1), actualAmount: -50m, txnCount: 1),
            CreateDay(new DateOnly(2026, 2, 2), actualAmount: -100m, txnCount: 2),
            CreateDay(new DateOnly(2026, 2, 3), actualAmount: 0m, txnCount: 0),
            CreateDay(new DateOnly(2026, 2, 4), actualAmount: 0m, txnCount: 0),
            CreateDay(new DateOnly(2026, 2, 5), actualAmount: 0m, txnCount: 0),
            CreateDay(new DateOnly(2026, 2, 6), actualAmount: 0m, txnCount: 0),
            CreateDay(new DateOnly(2026, 2, 7), actualAmount: 0m, txnCount: 0),
        };

        // Act
        var cut = Render<WeekSummary>(p => p
            .Add(x => x.Days, days)
            .Add(x => x.IsVisible, true));

        // Assert — average = $150 / 2 days = $75
        Assert.Contains("$75.00", cut.Markup);
    }

    private static List<CalendarDaySummaryDto> CreateWeekDays(DateOnly startDate)
    {
        var days = new List<CalendarDaySummaryDto>();
        for (int i = 0; i < 7; i++)
        {
            days.Add(CreateDay(startDate.AddDays(i), actualAmount: -10m * (i + 1), txnCount: i + 1));
        }

        return days;
    }

    private static CalendarDaySummaryDto CreateDay(DateOnly date, decimal actualAmount, int txnCount)
    {
        return new CalendarDaySummaryDto
        {
            Date = date,
            IsCurrentMonth = true,
            IsToday = false,
            ActualTotal = new MoneyDto { Amount = actualAmount, Currency = "USD" },
            ProjectedTotal = new MoneyDto { Amount = 0m, Currency = "USD" },
            CombinedTotal = new MoneyDto { Amount = actualAmount, Currency = "USD" },
            TransactionCount = txnCount,
            RecurringCount = 0,
            HasRecurring = false,
            EndOfDayBalance = new MoneyDto { Amount = 1000m + actualAmount, Currency = "USD" },
            IsBalanceNegative = false,
        };
    }
}
