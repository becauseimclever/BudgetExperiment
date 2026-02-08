// <copyright file="CalendarWeekViewTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using Bunit;

using BudgetExperiment.Client.Components.Calendar;
using BudgetExperiment.Client.Services;
using BudgetExperiment.Contracts.Dtos;

using Microsoft.Extensions.DependencyInjection;

namespace BudgetExperiment.Client.Tests.Components.Calendar;

/// <summary>
/// Unit tests for the CalendarWeekView component.
/// </summary>
public class CalendarWeekViewTests : BunitContext, IAsyncLifetime
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CalendarWeekViewTests"/> class.
    /// </summary>
    public CalendarWeekViewTests()
    {
        this.JSInterop.Mode = JSRuntimeMode.Loose;
        this.Services.AddSingleton<ThemeService>();
    }

    /// <inheritdoc/>
    public Task InitializeAsync() => Task.CompletedTask;

    /// <inheritdoc/>
    public new Task DisposeAsync() => base.DisposeAsync().AsTask();

    /// <summary>
    /// Verifies the week view renders 7 day headers.
    /// </summary>
    [Fact]
    public void WeekView_Renders_SevenDayHeaders()
    {
        // Arrange
        var days = CreateDays(42);

        // Act
        var cut = Render<CalendarWeekView>(p => p
            .Add(x => x.Days, days)
            .Add(x => x.WeekIndex, 0));

        // Assert
        var headers = cut.FindAll(".week-day-header");
        Assert.Equal(7, headers.Count);
    }

    /// <summary>
    /// Verifies the week view renders exactly 7 day cells.
    /// </summary>
    [Fact]
    public void WeekView_Renders_SevenDayCells()
    {
        // Arrange
        var days = CreateDays(42);

        // Act
        var cut = Render<CalendarWeekView>(p => p
            .Add(x => x.Days, days)
            .Add(x => x.WeekIndex, 0));

        // Assert
        var dayCells = cut.FindAll(".week-day");
        Assert.Equal(7, dayCells.Count);
    }

    /// <summary>
    /// Verifies that changing the WeekIndex shows different days.
    /// </summary>
    [Fact]
    public void WeekView_ShowsCorrectDays_ForWeekIndex()
    {
        // Arrange
        var days = CreateDays(42);

        // Act (render week 2 which is days 7-13)
        var cut = Render<CalendarWeekView>(p => p
            .Add(x => x.Days, days)
            .Add(x => x.WeekIndex, 1));

        // Assert - verify the rendered day numbers match the expected days
        var dayCells = cut.FindAll(".week-day .day-number");
        Assert.Equal(7, dayCells.Count);
        Assert.Equal(days[7].Date.Day.ToString(), dayCells[0].TextContent.Trim());
        Assert.Equal(days[13].Date.Day.ToString(), dayCells[6].TextContent.Trim());
    }

    /// <summary>
    /// Verifies that clicking a day fires OnDaySelected.
    /// </summary>
    [Fact]
    public void WeekView_ClickDay_FiresOnDaySelected()
    {
        // Arrange
        var days = CreateDays(42);
        DateOnly? selectedDate = null;

        var cut = Render<CalendarWeekView>(p => p
            .Add(x => x.Days, days)
            .Add(x => x.WeekIndex, 0)
            .Add(x => x.OnDaySelected, date => { selectedDate = date; return Task.CompletedTask; }));

        // Act
        cut.FindAll(".week-day")[2].Click();

        // Assert
        Assert.NotNull(selectedDate);
        Assert.Equal(days[2].Date, selectedDate.Value);
    }

    /// <summary>
    /// Verifies the next week button fires WeekIndexChanged when not at end.
    /// </summary>
    [Fact]
    public void WeekView_NextWeek_FiresWeekIndexChanged()
    {
        // Arrange
        var days = CreateDays(42);
        int? newIndex = null;

        var cut = Render<CalendarWeekView>(p => p
            .Add(x => x.Days, days)
            .Add(x => x.WeekIndex, 2)
            .Add(x => x.WeekIndexChanged, idx => { newIndex = idx; return Task.CompletedTask; }));

        // Act
        cut.Find("[aria-label='Next week']").Click();

        // Assert
        Assert.Equal(3, newIndex);
    }

    /// <summary>
    /// Verifies the previous week button fires WeekIndexChanged when not at start.
    /// </summary>
    [Fact]
    public void WeekView_PreviousWeek_FiresWeekIndexChanged()
    {
        // Arrange
        var days = CreateDays(42);
        int? newIndex = null;

        var cut = Render<CalendarWeekView>(p => p
            .Add(x => x.Days, days)
            .Add(x => x.WeekIndex, 3)
            .Add(x => x.WeekIndexChanged, idx => { newIndex = idx; return Task.CompletedTask; }));

        // Act
        cut.Find("[aria-label='Previous week']").Click();

        // Assert
        Assert.Equal(2, newIndex);
    }

    /// <summary>
    /// Verifies that navigating past the last week fires OnWeekOverflow.
    /// </summary>
    [Fact]
    public void WeekView_NextBeyondEnd_FiresOnWeekOverflow()
    {
        // Arrange
        var days = CreateDays(42);
        DateOnly? overflowDate = null;

        var cut = Render<CalendarWeekView>(p => p
            .Add(x => x.Days, days)
            .Add(x => x.WeekIndex, 5) // last week (index 5 of 6 weeks)
            .Add(x => x.OnWeekOverflow, date => { overflowDate = date; return Task.CompletedTask; }));

        // Act
        cut.Find("[aria-label='Next week']").Click();

        // Assert
        Assert.NotNull(overflowDate);
    }

    /// <summary>
    /// Verifies that navigating before the first week fires OnWeekOverflow.
    /// </summary>
    [Fact]
    public void WeekView_PreviousBeyondStart_FiresOnWeekOverflow()
    {
        // Arrange
        var days = CreateDays(42);
        DateOnly? overflowDate = null;

        var cut = Render<CalendarWeekView>(p => p
            .Add(x => x.Days, days)
            .Add(x => x.WeekIndex, 0)
            .Add(x => x.OnWeekOverflow, date => { overflowDate = date; return Task.CompletedTask; }));

        // Act
        cut.Find("[aria-label='Previous week']").Click();

        // Assert
        Assert.NotNull(overflowDate);
    }

    /// <summary>
    /// Verifies that the today class is applied to the correct day.
    /// </summary>
    [Fact]
    public void WeekView_TodayDay_HasTodayClass()
    {
        // Arrange
        var days = CreateDays(42);
        days[3].IsToday = true;
        days[3].IsCurrentMonth = true;

        // Act
        var cut = Render<CalendarWeekView>(p => p
            .Add(x => x.Days, days)
            .Add(x => x.WeekIndex, 0));

        // Assert
        var todayCell = cut.FindAll(".week-day")[3];
        Assert.Contains("today", todayCell.ClassList);
    }

    /// <summary>
    /// Verifies the week label updates with week dates.
    /// </summary>
    [Fact]
    public void WeekView_WeekLabel_ShowsDateRange()
    {
        // Arrange
        var days = CreateDays(42);

        // Act
        var cut = Render<CalendarWeekView>(p => p
            .Add(x => x.Days, days)
            .Add(x => x.WeekIndex, 0));

        // Assert
        var label = cut.Find(".week-label");
        Assert.False(string.IsNullOrWhiteSpace(label.TextContent));
    }

    /// <summary>
    /// Verifies that recurring info shows for days with recurring transactions.
    /// </summary>
    [Fact]
    public void WeekView_RecurringDay_ShowsRecurringIndicator()
    {
        // Arrange
        var days = CreateDays(42);
        days[1].HasRecurring = true;
        days[1].RecurringCount = 2;
        days[1].ProjectedTotal = new MoneyDto { Amount = 150m, Currency = "USD" };

        // Act
        var cut = Render<CalendarWeekView>(p => p
            .Add(x => x.Days, days)
            .Add(x => x.WeekIndex, 0));

        // Assert
        var recurringIndicators = cut.FindAll(".week-day-recurring");
        Assert.Single(recurringIndicators);
    }

    /// <summary>
    /// Verifies balance displays for each day cell.
    /// </summary>
    [Fact]
    public void WeekView_AllDays_ShowBalance()
    {
        // Arrange
        var days = CreateDays(42);

        // Act
        var cut = Render<CalendarWeekView>(p => p
            .Add(x => x.Days, days)
            .Add(x => x.WeekIndex, 0));

        // Assert
        var balances = cut.FindAll(".week-day-balance");
        Assert.Equal(7, balances.Count);
    }

    /// <summary>
    /// Verifies empty days list renders no day cells.
    /// </summary>
    [Fact]
    public void WeekView_EmptyDays_RendersNoDayCells()
    {
        // Arrange & Act
        var cut = Render<CalendarWeekView>(p => p
            .Add(x => x.Days, Array.Empty<CalendarDaySummaryDto>())
            .Add(x => x.WeekIndex, 0));

        // Assert
        var dayCells = cut.FindAll(".week-day");
        Assert.Empty(dayCells);
    }

    /// <summary>
    /// Verifies that accessibility attributes are present on day cells.
    /// </summary>
    [Fact]
    public void WeekView_DayCells_HaveAccessibilityAttributes()
    {
        // Arrange
        var days = CreateDays(42);

        // Act
        var cut = Render<CalendarWeekView>(p => p
            .Add(x => x.Days, days)
            .Add(x => x.WeekIndex, 0));

        // Assert
        var firstDay = cut.FindAll(".week-day")[0];
        Assert.Equal("button", firstDay.GetAttribute("role"));
        Assert.Equal("0", firstDay.GetAttribute("tabindex"));
        Assert.NotNull(firstDay.GetAttribute("aria-label"));
    }

    private static List<CalendarDaySummaryDto> CreateDays(int count)
    {
        var startDate = new DateOnly(2026, 2, 1);

        // Align to the Sunday that starts the grid
        var dayOfWeek = (int)startDate.DayOfWeek;
        var gridStart = startDate.AddDays(-dayOfWeek);

        var days = new List<CalendarDaySummaryDto>();
        for (int i = 0; i < count; i++)
        {
            var date = gridStart.AddDays(i);
            days.Add(new CalendarDaySummaryDto
            {
                Date = date,
                IsCurrentMonth = date.Month == 2,
                IsToday = false,
                ActualTotal = new MoneyDto { Amount = 0, Currency = "USD" },
                ProjectedTotal = new MoneyDto { Amount = 0, Currency = "USD" },
                CombinedTotal = new MoneyDto { Amount = 0, Currency = "USD" },
                EndOfDayBalance = new MoneyDto { Amount = 1000m, Currency = "USD" },
                TransactionCount = 0,
                RecurringCount = 0,
                HasRecurring = false,
                IsBalanceNegative = false,
            });
        }

        return days;
    }
}
