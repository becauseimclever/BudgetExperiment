// <copyright file="MobileCalendarTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.E2E.Tests.Fixtures;
using BudgetExperiment.E2E.Tests.Helpers;

namespace BudgetExperiment.E2E.Tests.Tests;

/// <summary>
/// E2E tests for Calendar navigation and view modes on mobile viewport.
/// </summary>
[Collection("MobilePlaywright")]
public class MobileCalendarTests
{
    private readonly MobilePlaywrightFixture fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="MobileCalendarTests"/> class.
    /// </summary>
    /// <param name="fixture">The mobile Playwright fixture.</param>
    public MobileCalendarTests(MobilePlaywrightFixture fixture)
    {
        this.fixture = fixture;
    }

    /// <summary>
    /// Verifies the calendar grid renders on mobile viewport.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Mobile")]
    [Trait("Category", "DemoSafe")]
    public async Task Calendar_ShouldRenderGrid_OnMobile()
    {
        // Arrange
        var page = fixture.Page;
        await AuthenticationHelper.LoginAsync(page, fixture.BaseUrl);

        // Act
        await page.GotoAsync(fixture.BaseUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert
        var calendarGrid = page.Locator(".calendar-grid");
        await Expect(calendarGrid).ToBeVisibleAsync(new() { Timeout = 10000 });
    }

    /// <summary>
    /// Verifies the view toggle is visible on mobile viewport.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Mobile")]
    [Trait("Category", "DemoSafe")]
    public async Task Calendar_ViewToggle_ShouldBeVisible_OnMobile()
    {
        // Arrange
        var page = fixture.Page;
        await AuthenticationHelper.LoginAsync(page, fixture.BaseUrl);

        // Act
        await page.GotoAsync(fixture.BaseUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert
        var viewToggle = page.Locator(".calendar-view-toggle");
        await Expect(viewToggle).ToBeVisibleAsync(new() { Timeout = 5000 });
    }

    /// <summary>
    /// Verifies the Month button is active by default.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Mobile")]
    [Trait("Category", "DemoSafe")]
    public async Task Calendar_MonthView_ShouldBeDefaultActive()
    {
        // Arrange
        var page = fixture.Page;
        await AuthenticationHelper.LoginAsync(page, fixture.BaseUrl);
        await page.GotoAsync(fixture.BaseUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act
        var monthButton = page.GetByLabel("Month view");

        // Assert
        await Expect(monthButton).ToHaveAttributeAsync("aria-pressed", "true");
    }

    /// <summary>
    /// Verifies tapping Week button switches to week view.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Mobile")]
    [Trait("Category", "DemoSafe")]
    public async Task Calendar_WeekToggle_ShouldSwitchToWeekView()
    {
        // Arrange
        var page = fixture.Page;
        await AuthenticationHelper.LoginAsync(page, fixture.BaseUrl);
        await page.GotoAsync(fixture.BaseUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act - tap Week button
        var weekButton = page.GetByLabel("Week view");
        await weekButton.ClickAsync();

        // Assert - week view should appear, month grid should not
        var weekView = page.Locator(".week-view");
        await Expect(weekView).ToBeVisibleAsync(new() { Timeout = 5000 });

        var monthGrid = page.Locator(".calendar-grid");
        await Expect(monthGrid).Not.ToBeVisibleAsync();

        // Week button should now be active
        await Expect(weekButton).ToHaveAttributeAsync("aria-pressed", "true");
    }

    /// <summary>
    /// Verifies the week view shows 7 day headers.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Mobile")]
    [Trait("Category", "DemoSafe")]
    public async Task Calendar_WeekView_ShouldShowSevenDayHeaders()
    {
        // Arrange
        var page = fixture.Page;
        await AuthenticationHelper.LoginAsync(page, fixture.BaseUrl);
        await page.GotoAsync(fixture.BaseUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act - switch to week view
        await page.GetByLabel("Week view").ClickAsync();
        await Expect(page.Locator(".week-view")).ToBeVisibleAsync(new() { Timeout = 5000 });

        // Assert - should have 7 day headers
        var dayHeaders = page.Locator(".week-day-header");
        await Expect(dayHeaders).ToHaveCountAsync(7);
    }

    /// <summary>
    /// Verifies the week view shows 7 day cells.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Mobile")]
    [Trait("Category", "DemoSafe")]
    public async Task Calendar_WeekView_ShouldShowSevenDayCells()
    {
        // Arrange
        var page = fixture.Page;
        await AuthenticationHelper.LoginAsync(page, fixture.BaseUrl);
        await page.GotoAsync(fixture.BaseUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act - switch to week view
        await page.GetByLabel("Week view").ClickAsync();
        await Expect(page.Locator(".week-view")).ToBeVisibleAsync(new() { Timeout = 5000 });

        // Assert - should have exactly 7 day cells
        var dayCells = page.Locator(".week-day");
        await Expect(dayCells).ToHaveCountAsync(7);
    }

    /// <summary>
    /// Verifies the week view has navigation buttons.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Mobile")]
    [Trait("Category", "DemoSafe")]
    public async Task Calendar_WeekView_ShouldHaveNavigationButtons()
    {
        // Arrange
        var page = fixture.Page;
        await AuthenticationHelper.LoginAsync(page, fixture.BaseUrl);
        await page.GotoAsync(fixture.BaseUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act - switch to week view
        await page.GetByLabel("Week view").ClickAsync();
        await Expect(page.Locator(".week-view")).ToBeVisibleAsync(new() { Timeout = 5000 });

        // Assert
        var prevButton = page.GetByLabel("Previous week");
        var nextButton = page.GetByLabel("Next week");
        await Expect(prevButton).ToBeVisibleAsync();
        await Expect(nextButton).ToBeVisibleAsync();
    }

    /// <summary>
    /// Verifies the week label shows a date range.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Mobile")]
    [Trait("Category", "DemoSafe")]
    public async Task Calendar_WeekView_ShouldShowWeekLabel()
    {
        // Arrange
        var page = fixture.Page;
        await AuthenticationHelper.LoginAsync(page, fixture.BaseUrl);
        await page.GotoAsync(fixture.BaseUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act - switch to week view
        await page.GetByLabel("Week view").ClickAsync();
        await Expect(page.Locator(".week-view")).ToBeVisibleAsync(new() { Timeout = 5000 });

        // Assert - label should contain a dash/en-dash (date range)
        var weekLabel = page.Locator(".week-label");
        await Expect(weekLabel).ToBeVisibleAsync();
        var text = await weekLabel.TextContentAsync();
        Assert.NotNull(text);
        Assert.True(text!.Contains("–") || text.Contains("-"), $"Week label should show date range, got: '{text}'");
    }

    /// <summary>
    /// Verifies switching back to Month view from Week view.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Mobile")]
    [Trait("Category", "DemoSafe")]
    public async Task Calendar_ToggleBackToMonth_ShouldShowMonthGrid()
    {
        // Arrange
        var page = fixture.Page;
        await AuthenticationHelper.LoginAsync(page, fixture.BaseUrl);
        await page.GotoAsync(fixture.BaseUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act - switch to week then back to month
        await page.GetByLabel("Week view").ClickAsync();
        await Expect(page.Locator(".week-view")).ToBeVisibleAsync(new() { Timeout = 5000 });

        await page.GetByLabel("Month view").ClickAsync();

        // Assert - month grid should reappear
        var calendarGrid = page.Locator(".calendar-grid");
        await Expect(calendarGrid).ToBeVisibleAsync(new() { Timeout = 5000 });

        var weekView = page.Locator(".week-view");
        await Expect(weekView).Not.ToBeVisibleAsync();
    }

    /// <summary>
    /// Verifies the Previous/Next month buttons work on mobile.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Mobile")]
    [Trait("Category", "DemoSafe")]
    public async Task Calendar_MonthNavigation_ShouldWork_OnMobile()
    {
        // Arrange
        var page = fixture.Page;
        await AuthenticationHelper.LoginAsync(page, fixture.BaseUrl);

        // Navigate to a known month
        var now = DateTime.UtcNow;
        await page.GotoAsync($"{fixture.BaseUrl}/{now.Year}/{now.Month}");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Get the current month heading text
        var heading = page.Locator("h2.text-secondary");
        await Expect(heading).ToBeVisibleAsync(new() { Timeout = 5000 });
        var originalText = await heading.TextContentAsync();

        // Act - click Next
        await page.GetByRole(AriaRole.Button, new() { Name = "Next >" }).ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert - heading should change
        var newText = await heading.TextContentAsync();
        Assert.NotEqual(originalText, newText);
    }

    /// <summary>
    /// Verifies day cells in week view have button role for accessibility.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Mobile")]
    [Trait("Category", "Accessibility")]
    public async Task Calendar_WeekDayCells_ShouldHaveButtonRole()
    {
        // Arrange
        var page = fixture.Page;
        await AuthenticationHelper.LoginAsync(page, fixture.BaseUrl);
        await page.GotoAsync(fixture.BaseUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act - switch to week view
        await page.GetByLabel("Week view").ClickAsync();
        await Expect(page.Locator(".week-view")).ToBeVisibleAsync(new() { Timeout = 5000 });

        // Assert - day cells should have button role
        var firstDayCell = page.Locator(".week-day").First;
        await Expect(firstDayCell).ToHaveAttributeAsync("role", "button");
    }
}
