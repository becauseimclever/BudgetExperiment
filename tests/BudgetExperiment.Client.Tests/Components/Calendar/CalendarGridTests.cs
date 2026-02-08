// <copyright file="CalendarGridTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using Bunit;

using BudgetExperiment.Client.Components.Calendar;
using BudgetExperiment.Client.Services;
using BudgetExperiment.Contracts.Dtos;

using Microsoft.Extensions.DependencyInjection;

namespace BudgetExperiment.Client.Tests.Components.Calendar;

/// <summary>
/// Unit tests for the CalendarGrid component.
/// </summary>
public class CalendarGridTests : BunitContext, IAsyncLifetime
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CalendarGridTests"/> class.
    /// </summary>
    public CalendarGridTests()
    {
        this.JSInterop.Mode = JSRuntimeMode.Loose;
        this.Services.AddSingleton<ThemeService>();
    }

    /// <inheritdoc/>
    public Task InitializeAsync() => Task.CompletedTask;

    /// <inheritdoc/>
    public new Task DisposeAsync() => base.DisposeAsync().AsTask();

    /// <summary>
    /// Verifies that clicking a week number button fires OnWeekSelected.
    /// </summary>
    [Fact]
    public void CalendarGrid_WeekNumberClick_FiresOnWeekSelected()
    {
        // Arrange
        var days = CreateDays(42);
        var selectedWeek = -1;

        // Act
        var cut = Render<CalendarGrid>(p => p
            .Add(x => x.Days, days)
            .Add(x => x.OnWeekSelected, (int idx) => { selectedWeek = idx; }));

        var weekButtons = cut.FindAll(".week-number-btn");
        weekButtons[2].Click(); // Click third week (index 2)

        // Assert
        Assert.Equal(2, selectedWeek);
    }

    /// <summary>
    /// Verifies that 6 week number buttons are rendered for a 42-day grid.
    /// </summary>
    [Fact]
    public void CalendarGrid_Renders_SixWeekNumberButtons()
    {
        // Arrange
        var days = CreateDays(42);

        // Act
        var cut = Render<CalendarGrid>(p => p
            .Add(x => x.Days, days));

        // Assert
        var weekButtons = cut.FindAll(".week-number-btn");
        Assert.Equal(6, weekButtons.Count);
    }

    /// <summary>
    /// Verifies that the selected week row has the week-selected CSS class.
    /// </summary>
    [Fact]
    public void CalendarGrid_SelectedWeek_HasSelectedClass()
    {
        // Arrange
        var days = CreateDays(42);

        // Act
        var cut = Render<CalendarGrid>(p => p
            .Add(x => x.Days, days)
            .Add(x => x.SelectedWeekIndex, 1));

        // Assert
        var weekRows = cut.FindAll(".calendar-week-row");
        Assert.Contains("week-selected", weekRows[1].ClassList);
        Assert.DoesNotContain("week-selected", weekRows[0].ClassList);
    }

    /// <summary>
    /// Verifies that the selected week button has aria-pressed true.
    /// </summary>
    [Fact]
    public void CalendarGrid_SelectedWeekButton_HasAriaPressed()
    {
        // Arrange
        var days = CreateDays(42);

        // Act
        var cut = Render<CalendarGrid>(p => p
            .Add(x => x.Days, days)
            .Add(x => x.SelectedWeekIndex, 0));

        // Assert
        var weekButtons = cut.FindAll(".week-number-btn");
        Assert.Equal("true", weekButtons[0].GetAttribute("aria-pressed"));
        Assert.Equal("false", weekButtons[1].GetAttribute("aria-pressed"));
    }

    /// <summary>
    /// Verifies that the grid renders 8 header cells (1 blank + 7 day names).
    /// </summary>
    [Fact]
    public void CalendarGrid_Renders_EightHeaderCells()
    {
        // Arrange
        var days = CreateDays(42);

        // Act
        var cut = Render<CalendarGrid>(p => p
            .Add(x => x.Days, days));

        // Assert
        var headers = cut.FindAll(".calendar-day-header");
        Assert.Equal(8, headers.Count); // 1 blank + 7 days
        Assert.Contains("Sun", headers[1].TextContent);
        Assert.Contains("Sat", headers[7].TextContent);
    }

    /// <summary>
    /// Verifies that week rows have accessible ARIA labels.
    /// </summary>
    [Fact]
    public void CalendarGrid_WeekRows_HaveAriaLabels()
    {
        // Arrange
        var days = CreateDays(42);

        // Act
        var cut = Render<CalendarGrid>(p => p
            .Add(x => x.Days, days));

        // Assert
        var weekRows = cut.FindAll("[role='row']");
        Assert.Equal(6, weekRows.Count);
        foreach (var row in weekRows)
        {
            var ariaLabel = row.GetAttribute("aria-label");
            Assert.NotNull(ariaLabel);
            Assert.Contains("Week of", ariaLabel);
        }
    }

    /// <summary>
    /// Verifies that clicking a day still fires OnDaySelected.
    /// </summary>
    [Fact]
    public void CalendarGrid_DayClick_StillFiresOnDaySelected()
    {
        // Arrange
        var days = CreateDays(42);
        DateOnly? selectedDate = null;

        // Act
        var cut = Render<CalendarGrid>(p => p
            .Add(x => x.Days, days)
            .Add(x => x.OnDaySelected, (DateOnly d) => { selectedDate = d; }));

        var dayCells = cut.FindAll(".calendar-day");
        dayCells[0].Click();

        // Assert
        Assert.NotNull(selectedDate);
    }

    /// <summary>
    /// Verifies that no week is selected by default (SelectedWeekIndex = -1).
    /// </summary>
    [Fact]
    public void CalendarGrid_NoWeekSelected_ByDefault()
    {
        // Arrange
        var days = CreateDays(42);

        // Act
        var cut = Render<CalendarGrid>(p => p
            .Add(x => x.Days, days));

        // Assert
        var selectedRows = cut.FindAll(".calendar-week-row.week-selected");
        Assert.Empty(selectedRows);
    }

    private static List<CalendarDaySummaryDto> CreateDays(int count)
    {
        var startDate = new DateOnly(2026, 2, 1);
        var firstDayOfWeek = startDate.AddDays(-(int)startDate.DayOfWeek);

        var days = new List<CalendarDaySummaryDto>();
        for (int i = 0; i < count; i++)
        {
            var date = firstDayOfWeek.AddDays(i);
            days.Add(new CalendarDaySummaryDto
            {
                Date = date,
                IsCurrentMonth = date.Month == 2,
                IsToday = false,
                ActualTotal = new MoneyDto { Amount = -10m, Currency = "USD" },
                ProjectedTotal = new MoneyDto { Amount = 0m, Currency = "USD" },
                CombinedTotal = new MoneyDto { Amount = -10m, Currency = "USD" },
                TransactionCount = 1,
                RecurringCount = 0,
                HasRecurring = false,
                EndOfDayBalance = new MoneyDto { Amount = 1000m, Currency = "USD" },
                IsBalanceNegative = false,
            });
        }

        return days;
    }
}
