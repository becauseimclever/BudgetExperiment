// <copyright file="CalendarViewToggleTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using Bunit;

using BudgetExperiment.Client.Components.Calendar;
using BudgetExperiment.Client.Models;
using BudgetExperiment.Client.Services;

using Microsoft.Extensions.DependencyInjection;

namespace BudgetExperiment.Client.Tests.Components.Calendar;

/// <summary>
/// Unit tests for the CalendarViewToggle component.
/// </summary>
public class CalendarViewToggleTests : BunitContext, IAsyncLifetime
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CalendarViewToggleTests"/> class.
    /// </summary>
    public CalendarViewToggleTests()
    {
        this.JSInterop.Mode = JSRuntimeMode.Loose;
        this.Services.AddSingleton<ThemeService>();
    }

    /// <inheritdoc/>
    public Task InitializeAsync() => Task.CompletedTask;

    /// <inheritdoc/>
    public new Task DisposeAsync() => base.DisposeAsync().AsTask();

    /// <summary>
    /// Verifies the toggle renders two buttons (Month and Week).
    /// </summary>
    [Fact]
    public void Toggle_RendersTwoButtons()
    {
        // Arrange & Act
        var cut = Render<CalendarViewToggle>(p => p
            .Add(x => x.CurrentView, CalendarViewMode.Month));

        // Assert
        var buttons = cut.FindAll(".toggle-btn");
        Assert.Equal(2, buttons.Count);
        Assert.Equal("Month", buttons[0].TextContent.Trim());
        Assert.Equal("Week", buttons[1].TextContent.Trim());
    }

    /// <summary>
    /// Verifies Month button has active class when CurrentView is Month.
    /// </summary>
    [Fact]
    public void Toggle_MonthActive_WhenCurrentViewIsMonth()
    {
        // Arrange & Act
        var cut = Render<CalendarViewToggle>(p => p
            .Add(x => x.CurrentView, CalendarViewMode.Month));

        // Assert
        var buttons = cut.FindAll(".toggle-btn");
        Assert.Contains("active", buttons[0].ClassList);
        Assert.DoesNotContain("active", buttons[1].ClassList);
    }

    /// <summary>
    /// Verifies Week button has active class when CurrentView is Week.
    /// </summary>
    [Fact]
    public void Toggle_WeekActive_WhenCurrentViewIsWeek()
    {
        // Arrange & Act
        var cut = Render<CalendarViewToggle>(p => p
            .Add(x => x.CurrentView, CalendarViewMode.Week));

        // Assert
        var buttons = cut.FindAll(".toggle-btn");
        Assert.DoesNotContain("active", buttons[0].ClassList);
        Assert.Contains("active", buttons[1].ClassList);
    }

    /// <summary>
    /// Verifies clicking Week button fires CurrentViewChanged.
    /// </summary>
    [Fact]
    public void Toggle_ClickWeek_FiresCurrentViewChanged()
    {
        // Arrange
        CalendarViewMode? newMode = null;
        var cut = Render<CalendarViewToggle>(p => p
            .Add(x => x.CurrentView, CalendarViewMode.Month)
            .Add(x => x.CurrentViewChanged, mode => { newMode = mode; return Task.CompletedTask; }));

        // Act
        cut.FindAll(".toggle-btn")[1].Click(); // Week button

        // Assert
        Assert.Equal(CalendarViewMode.Week, newMode);
    }

    /// <summary>
    /// Verifies clicking Month button fires CurrentViewChanged.
    /// </summary>
    [Fact]
    public void Toggle_ClickMonth_FiresCurrentViewChanged()
    {
        // Arrange
        CalendarViewMode? newMode = null;
        var cut = Render<CalendarViewToggle>(p => p
            .Add(x => x.CurrentView, CalendarViewMode.Week)
            .Add(x => x.CurrentViewChanged, mode => { newMode = mode; return Task.CompletedTask; }));

        // Act
        cut.FindAll(".toggle-btn")[0].Click(); // Month button

        // Assert
        Assert.Equal(CalendarViewMode.Month, newMode);
    }

    /// <summary>
    /// Verifies clicking the already active button does not fire the callback.
    /// </summary>
    [Fact]
    public void Toggle_ClickActiveButton_DoesNotFireCallback()
    {
        // Arrange
        CalendarViewMode? newMode = null;
        var cut = Render<CalendarViewToggle>(p => p
            .Add(x => x.CurrentView, CalendarViewMode.Month)
            .Add(x => x.CurrentViewChanged, mode => { newMode = mode; return Task.CompletedTask; }));

        // Act
        cut.FindAll(".toggle-btn")[0].Click(); // Month is already active

        // Assert
        Assert.Null(newMode);
    }

    /// <summary>
    /// Verifies clicking Week button persists preference to localStorage.
    /// </summary>
    [Fact]
    public void Toggle_ClickWeek_PersistsToLocalStorage()
    {
        // Arrange
        var cut = Render<CalendarViewToggle>(p => p
            .Add(x => x.CurrentView, CalendarViewMode.Month)
            .Add(x => x.CurrentViewChanged, _ => Task.CompletedTask));

        // Act
        cut.FindAll(".toggle-btn")[1].Click(); // Week button

        // Assert
        var invocations = this.JSInterop.Invocations
            .Where(i => i.Identifier == "localStorage.setItem")
            .ToList();
        Assert.NotEmpty(invocations);
        var invocation = invocations.Last();
        Assert.Equal("budget-experiment-calendar-view", invocation.Arguments[0]);
        Assert.Equal("Week", invocation.Arguments[1]);
    }

    /// <summary>
    /// Verifies buttons have appropriate aria-pressed attributes.
    /// </summary>
    [Fact]
    public void Toggle_Buttons_HaveAriaPressed()
    {
        // Arrange & Act
        var cut = Render<CalendarViewToggle>(p => p
            .Add(x => x.CurrentView, CalendarViewMode.Month));

        // Assert
        var buttons = cut.FindAll(".toggle-btn");
        Assert.Equal("true", buttons[0].GetAttribute("aria-pressed"));
        Assert.Equal("false", buttons[1].GetAttribute("aria-pressed"));
    }

    /// <summary>
    /// Verifies LoadPreferenceAsync loads saved view from localStorage.
    /// </summary>
    [Fact]
    public async Task Toggle_LoadPreference_SetsViewFromLocalStorage()
    {
        // Arrange
        this.JSInterop.Setup<string?>("localStorage.getItem", "budget-experiment-calendar-view")
            .SetResult("Week");

        CalendarViewMode? newMode = null;
        var cut = Render<CalendarViewToggle>(p => p
            .Add(x => x.CurrentView, CalendarViewMode.Month)
            .Add(x => x.CurrentViewChanged, mode => { newMode = mode; return Task.CompletedTask; }));

        // Act
        await cut.Instance.LoadPreferenceAsync();

        // Assert
        Assert.Equal(CalendarViewMode.Week, newMode);
    }
}
