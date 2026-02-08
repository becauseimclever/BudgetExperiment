// <copyright file="DateRangePickerTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using Bunit;

using BudgetExperiment.Client.Components.Reports;

using Microsoft.AspNetCore.Components;

namespace BudgetExperiment.Client.Tests.Components;

/// <summary>
/// Unit tests for the DateRangePicker component.
/// </summary>
public class DateRangePickerTests : BunitContext
{
    [Fact]
    public void DateRangePicker_Renders_WithDefaultPresets()
    {
        // Act
        var cut = Render<DateRangePicker>();

        // Assert
        var presetButtons = cut.FindAll(".preset-button");
        Assert.Equal(5, presetButtons.Count);
        Assert.Contains("This Month", presetButtons[0].TextContent);
        Assert.Contains("Last Month", presetButtons[1].TextContent);
        Assert.Contains("Last 7 Days", presetButtons[2].TextContent);
        Assert.Contains("Last 30 Days", presetButtons[3].TextContent);
        Assert.Contains("Custom", presetButtons[4].TextContent);
    }

    [Fact]
    public void DateRangePicker_HidesPresets_WhenShowPresetsIsFalse()
    {
        // Act
        var cut = Render<DateRangePicker>(parameters => parameters
            .Add(p => p.ShowPresets, false));

        // Assert
        var presetButtons = cut.FindAll(".preset-button");
        Assert.Empty(presetButtons);
    }

    [Fact]
    public void DateRangePicker_Renders_DateInputs()
    {
        // Arrange
        var start = new DateOnly(2026, 1, 1);
        var end = new DateOnly(2026, 1, 31);

        // Act
        var cut = Render<DateRangePicker>(parameters => parameters
            .Add(p => p.StartDate, start)
            .Add(p => p.EndDate, end));

        // Assert
        var startInput = cut.Find("#start-date");
        var endInput = cut.Find("#end-date");
        Assert.Equal("2026-01-01", startInput.GetAttribute("value"));
        Assert.Equal("2026-01-31", endInput.GetAttribute("value"));
    }

    [Fact]
    public async Task DateRangePicker_ThisMonth_EmitsCorrectRange()
    {
        // Arrange
        var today = DateOnly.FromDateTime(DateTime.Today);
        var expectedStart = new DateOnly(today.Year, today.Month, 1);
        var expectedEnd = expectedStart.AddMonths(1).AddDays(-1);
        (DateOnly Start, DateOnly End)? emittedRange = null;

        var cut = Render<DateRangePicker>(parameters => parameters
            .Add(p => p.StartDate, today.AddDays(-10))
            .Add(p => p.EndDate, today)
            .Add(p => p.OnRangeChanged, range => emittedRange = range));

        // Act
        var thisMonthButton = cut.Find(".preset-button");
        await thisMonthButton.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Assert
        Assert.NotNull(emittedRange);
        Assert.Equal(expectedStart, emittedRange.Value.Start);
        Assert.Equal(expectedEnd, emittedRange.Value.End);
    }

    [Fact]
    public async Task DateRangePicker_LastMonth_EmitsCorrectRange()
    {
        // Arrange
        var today = DateOnly.FromDateTime(DateTime.Today);
        var expectedStart = new DateOnly(today.Year, today.Month, 1).AddMonths(-1);
        var expectedEnd = new DateOnly(today.Year, today.Month, 1).AddDays(-1);
        (DateOnly Start, DateOnly End)? emittedRange = null;

        var cut = Render<DateRangePicker>(parameters => parameters
            .Add(p => p.OnRangeChanged, range => emittedRange = range));

        // Act
        var buttons = cut.FindAll(".preset-button");
        var lastMonthButton = buttons[1]; // "Last Month"
        await lastMonthButton.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Assert
        Assert.NotNull(emittedRange);
        Assert.Equal(expectedStart, emittedRange.Value.Start);
        Assert.Equal(expectedEnd, emittedRange.Value.End);
    }

    [Fact]
    public async Task DateRangePicker_Last7Days_EmitsCorrectRange()
    {
        // Arrange
        var today = DateOnly.FromDateTime(DateTime.Today);
        (DateOnly Start, DateOnly End)? emittedRange = null;

        var cut = Render<DateRangePicker>(parameters => parameters
            .Add(p => p.OnRangeChanged, range => emittedRange = range));

        // Act
        var buttons = cut.FindAll(".preset-button");
        await buttons[2].ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Assert
        Assert.NotNull(emittedRange);
        Assert.Equal(today.AddDays(-6), emittedRange.Value.Start);
        Assert.Equal(today, emittedRange.Value.End);
    }

    [Fact]
    public async Task DateRangePicker_Last30Days_EmitsCorrectRange()
    {
        // Arrange
        var today = DateOnly.FromDateTime(DateTime.Today);
        (DateOnly Start, DateOnly End)? emittedRange = null;

        var cut = Render<DateRangePicker>(parameters => parameters
            .Add(p => p.OnRangeChanged, range => emittedRange = range));

        // Act
        var buttons = cut.FindAll(".preset-button");
        await buttons[3].ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Assert
        Assert.NotNull(emittedRange);
        Assert.Equal(today.AddDays(-29), emittedRange.Value.Start);
        Assert.Equal(today, emittedRange.Value.End);
    }

    [Fact]
    public async Task DateRangePicker_Custom_DoesNotEmit()
    {
        // Arrange
        (DateOnly Start, DateOnly End)? emittedRange = null;

        var cut = Render<DateRangePicker>(parameters => parameters
            .Add(p => p.OnRangeChanged, range => emittedRange = range));

        // Act
        var buttons = cut.FindAll(".preset-button");
        await buttons[4].ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Assert - clicking "Custom" preset should not emit a range, it just marks as custom
        Assert.Null(emittedRange);
    }

    [Fact]
    public async Task DateRangePicker_StartDateChange_EmitsRange()
    {
        // Arrange
        (DateOnly Start, DateOnly End)? emittedRange = null;
        var end = new DateOnly(2026, 1, 31);

        var cut = Render<DateRangePicker>(parameters => parameters
            .Add(p => p.StartDate, new DateOnly(2026, 1, 1))
            .Add(p => p.EndDate, end)
            .Add(p => p.OnRangeChanged, range => emittedRange = range));

        // Act
        var startInput = cut.Find("#start-date");
        await startInput.ChangeAsync(new ChangeEventArgs { Value = "2026-01-15" });

        // Assert
        Assert.NotNull(emittedRange);
        Assert.Equal(new DateOnly(2026, 1, 15), emittedRange.Value.Start);
        Assert.Equal(end, emittedRange.Value.End);
    }

    [Fact]
    public async Task DateRangePicker_EndDateChange_EmitsRange()
    {
        // Arrange
        (DateOnly Start, DateOnly End)? emittedRange = null;
        var start = new DateOnly(2026, 1, 1);

        var cut = Render<DateRangePicker>(parameters => parameters
            .Add(p => p.StartDate, start)
            .Add(p => p.EndDate, new DateOnly(2026, 1, 31))
            .Add(p => p.OnRangeChanged, range => emittedRange = range));

        // Act
        var endInput = cut.Find("#end-date");
        await endInput.ChangeAsync(new ChangeEventArgs { Value = "2026-01-20" });

        // Assert
        Assert.NotNull(emittedRange);
        Assert.Equal(start, emittedRange.Value.Start);
        Assert.Equal(new DateOnly(2026, 1, 20), emittedRange.Value.End);
    }

    [Fact]
    public async Task DateRangePicker_RejectsEndDateBeforeStart()
    {
        // Arrange
        (DateOnly Start, DateOnly End)? emittedRange = null;

        var cut = Render<DateRangePicker>(parameters => parameters
            .Add(p => p.StartDate, new DateOnly(2026, 1, 15))
            .Add(p => p.EndDate, new DateOnly(2026, 1, 31))
            .Add(p => p.OnRangeChanged, range => emittedRange = range));

        // Act - try to set end date before start date
        var endInput = cut.Find("#end-date");
        await endInput.ChangeAsync(new ChangeEventArgs { Value = "2026-01-10" });

        // Assert - should not emit since end < start
        Assert.Null(emittedRange);
    }

    [Fact]
    public async Task DateRangePicker_RejectsStartDateAfterEnd()
    {
        // Arrange
        (DateOnly Start, DateOnly End)? emittedRange = null;

        var cut = Render<DateRangePicker>(parameters => parameters
            .Add(p => p.StartDate, new DateOnly(2026, 1, 1))
            .Add(p => p.EndDate, new DateOnly(2026, 1, 15))
            .Add(p => p.OnRangeChanged, range => emittedRange = range));

        // Act - try to set start date after end date
        var startInput = cut.Find("#start-date");
        await startInput.ChangeAsync(new ChangeEventArgs { Value = "2026-01-20" });

        // Assert - should not emit since start > end
        Assert.Null(emittedRange);
    }

    [Fact]
    public void DateRangePicker_ActivePreset_HasActiveClass()
    {
        // Arrange
        var today = DateOnly.FromDateTime(DateTime.Today);
        var thisMonthStart = new DateOnly(today.Year, today.Month, 1);
        var thisMonthEnd = thisMonthStart.AddMonths(1).AddDays(-1);

        // Act
        var cut = Render<DateRangePicker>(parameters => parameters
            .Add(p => p.StartDate, thisMonthStart)
            .Add(p => p.EndDate, thisMonthEnd));

        // Assert
        var activeButtons = cut.FindAll(".preset-button.active");
        Assert.Single(activeButtons);
        Assert.Contains("This Month", activeButtons[0].TextContent);
    }

    [Fact]
    public void DateRangePicker_AppliesSmallSize()
    {
        // Act
        var cut = Render<DateRangePicker>(parameters => parameters
            .Add(p => p.Size, "small"));

        // Assert
        var container = cut.Find(".date-range-picker");
        Assert.Contains("small", container.ClassList);
    }
}
