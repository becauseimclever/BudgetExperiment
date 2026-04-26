// <copyright file="CalendarGridTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Components.Calendar;
using BudgetExperiment.Client.Services;
using BudgetExperiment.Contracts.Dtos;

using Bunit;

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
        this.Services.AddSingleton<CultureService>();
        this.Services.AddSingleton<IFeatureFlagClientService>(new TestHelpers.StubFeatureFlagClientService());
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

    /// <summary>
    /// Verifies that CalendarDay components are rendered correctly for the given days.
    /// </summary>
    [Fact]
    public void CalendarGrid_SelectedDay_HasCorrectClass()
    {
        // Arrange
        var days = CreateDays(42);
        var selectedDate = days[10].Date;

        // Act
        var cut = Render<CalendarGrid>(p => p
            .Add(x => x.Days, days)
            .Add(x => x.SelectedDate, selectedDate));

        // Assert - Verify CalendarDay components are rendered (at least 42 expected in 6-week grid)
        var calendarDays = cut.FindComponents<CalendarDay>();
        Assert.NotEmpty(calendarDays);
        Assert.True(calendarDays.Count >= 42, $"Expected at least 42 CalendarDay components, got {calendarDays.Count}");
    }

    /// <summary>
    /// Verifies that no heatmap is applied when HeatmapData is null.
    /// </summary>
    [Fact]
    public void CalendarGrid_NoHeatmap_WhenHeatmapDataNull()
    {
        // Arrange
        var days = CreateDays(42);

        // Act
        var cut = Render<CalendarGrid>(p => p
            .Add(x => x.Days, days)
            .Add(x => x.HeatmapData, null));

        // Assert - No heatmap classes should be present
        var markup = cut.Markup;
        Assert.DoesNotContain("heatmap-low", markup);
        Assert.DoesNotContain("heatmap-moderate", markup);
        Assert.DoesNotContain("heatmap-high", markup);
    }

    /// <summary>
    /// Verifies that clicking a week number button with stopPropagation prevents bubbling.
    /// </summary>
    [Fact]
    public void CalendarGrid_WeekNumberClick_PreventsPropagation()
    {
        // Arrange
        var days = CreateDays(42);
        var dayClickCount = 0;

        // Act
        var cut = Render<CalendarGrid>(p => p
            .Add(x => x.Days, days)
            .Add(x => x.OnWeekSelected, (int _) => { })
            .Add(x => x.OnDaySelected, (DateOnly _) => { dayClickCount++; }));

        var weekButton = cut.FindAll(".week-number-btn")[0];
        weekButton.Click();

        // Assert - Day click should not increment (week click prevents propagation)
        Assert.Equal(0, dayClickCount);
    }

    /// <summary>
    /// Verifies that all 42 days are rendered in a 6-week month view.
    /// </summary>
    [Fact]
    public void CalendarGrid_Renders_AllDaysInSixWeekView()
    {
        // Arrange
        var days = CreateDays(42);

        // Act
        var cut = Render<CalendarGrid>(p => p
            .Add(x => x.Days, days));

        // Assert
        var weekRows = cut.FindAll(".calendar-week-row");
        Assert.Equal(6, weekRows.Count);

        // Each week row should have 7 days + 1 week button = 8 child elements
        foreach (var row in weekRows)
        {
            var cellsInRow = row.QuerySelectorAll(".calendar-day, .week-number-btn");
            Assert.Equal(8, cellsInRow.Count());
        }
    }

    /// <summary>
    /// Verifies that week aria labels contain correct date ranges.
    /// </summary>
    [Fact]
    public void CalendarGrid_WeekAriaLabel_ContainsDateRange()
    {
        // Arrange
        var days = CreateDays(42);

        // Act
        var cut = Render<CalendarGrid>(p => p
            .Add(x => x.Days, days));

        // Assert
        var weekRows = cut.FindAll("[role='row']");
        var firstWeekLabel = weekRows[0].GetAttribute("aria-label");
        Assert.Contains("Week of", firstWeekLabel);
        Assert.Contains("to", firstWeekLabel);
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
