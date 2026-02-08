// <copyright file="MobileOrientationTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.E2E.Tests.Fixtures;
using BudgetExperiment.E2E.Tests.Helpers;

namespace BudgetExperiment.E2E.Tests.Tests;

/// <summary>
/// E2E tests for orientation changes (portrait ↔ landscape) on mobile viewport.
/// </summary>
[Collection("MobilePlaywright")]
public class MobileOrientationTests
{
    private readonly MobilePlaywrightFixture fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="MobileOrientationTests"/> class.
    /// </summary>
    /// <param name="fixture">The mobile Playwright fixture.</param>
    public MobileOrientationTests(MobilePlaywrightFixture fixture)
    {
        this.fixture = fixture;
    }

    /// <summary>
    /// Verifies the calendar renders correctly in landscape orientation.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Mobile")]
    [Trait("Category", "Orientation")]
    public async Task Calendar_ShouldRender_InLandscapeOrientation()
    {
        // Arrange - create a page with landscape viewport
        var page = await fixture.CreatePageWithViewportAsync(
            MobilePlaywrightFixture.LandscapeWidth,
            MobilePlaywrightFixture.LandscapeHeight);

        try
        {
            await AuthenticationHelper.LoginAsync(page, fixture.BaseUrl);
            await page.GotoAsync(fixture.BaseUrl);
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Assert - calendar grid should still render
            var calendarGrid = page.Locator(".calendar-grid");
            await Expect(calendarGrid).ToBeVisibleAsync(new() { Timeout = 10000 });
        }
        finally
        {
            await page.Context.DisposeAsync();
        }
    }

    /// <summary>
    /// Verifies the FAB is visible in landscape orientation.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Mobile")]
    [Trait("Category", "Orientation")]
    public async Task Fab_ShouldBeVisible_InLandscapeOrientation()
    {
        // Arrange
        var page = await fixture.CreatePageWithViewportAsync(
            MobilePlaywrightFixture.LandscapeWidth,
            MobilePlaywrightFixture.LandscapeHeight);

        try
        {
            await AuthenticationHelper.LoginAsync(page, fixture.BaseUrl);
            await page.GotoAsync(fixture.BaseUrl);
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Assert
            var fab = page.Locator(".fab-primary");
            await Expect(fab).ToBeVisibleAsync(new() { Timeout = 5000 });
        }
        finally
        {
            await page.Context.DisposeAsync();
        }
    }

    /// <summary>
    /// Verifies the view toggle is visible in landscape orientation.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Mobile")]
    [Trait("Category", "Orientation")]
    public async Task ViewToggle_ShouldBeVisible_InLandscapeOrientation()
    {
        // Arrange
        var page = await fixture.CreatePageWithViewportAsync(
            MobilePlaywrightFixture.LandscapeWidth,
            MobilePlaywrightFixture.LandscapeHeight);

        try
        {
            await AuthenticationHelper.LoginAsync(page, fixture.BaseUrl);
            await page.GotoAsync(fixture.BaseUrl);
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Assert
            var viewToggle = page.Locator(".calendar-view-toggle");
            await Expect(viewToggle).ToBeVisibleAsync(new() { Timeout = 5000 });
        }
        finally
        {
            await page.Context.DisposeAsync();
        }
    }

    /// <summary>
    /// Verifies the week view renders correctly in landscape orientation.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Mobile")]
    [Trait("Category", "Orientation")]
    public async Task WeekView_ShouldRender_InLandscapeOrientation()
    {
        // Arrange
        var page = await fixture.CreatePageWithViewportAsync(
            MobilePlaywrightFixture.LandscapeWidth,
            MobilePlaywrightFixture.LandscapeHeight);

        try
        {
            await AuthenticationHelper.LoginAsync(page, fixture.BaseUrl);
            await page.GotoAsync(fixture.BaseUrl);
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Act - switch to week view
            await page.GetByLabel("Week view").ClickAsync();

            // Assert
            var weekView = page.Locator(".week-view");
            await Expect(weekView).ToBeVisibleAsync(new() { Timeout = 5000 });

            var dayCells = page.Locator(".week-day");
            await Expect(dayCells).ToHaveCountAsync(7);
        }
        finally
        {
            await page.Context.DisposeAsync();
        }
    }

    /// <summary>
    /// Verifies the Quick Add BottomSheet works in landscape orientation.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Mobile")]
    [Trait("Category", "Orientation")]
    public async Task QuickAdd_ShouldWork_InLandscapeOrientation()
    {
        // Arrange
        var page = await fixture.CreatePageWithViewportAsync(
            MobilePlaywrightFixture.LandscapeWidth,
            MobilePlaywrightFixture.LandscapeHeight);

        try
        {
            await AuthenticationHelper.LoginAsync(page, fixture.BaseUrl);
            await page.GotoAsync(fixture.BaseUrl);
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Act - open Quick Add
            await page.Locator(".fab-primary").ClickAsync();
            await page.Locator("#fab-quickadd-label").ClickAsync();

            // Assert
            var bottomSheet = page.Locator(".bottom-sheet.is-visible");
            await Expect(bottomSheet).ToBeVisibleAsync(new() { Timeout = 5000 });

            var form = page.Locator(".quick-add-form");
            await Expect(form).ToBeVisibleAsync();
        }
        finally
        {
            await page.Context.DisposeAsync();
        }
    }

    /// <summary>
    /// Verifies the calendar is still functional on a tablet-sized viewport (768px).
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Mobile")]
    [Trait("Category", "Orientation")]
    public async Task Calendar_ShouldRender_OnTabletViewport()
    {
        // Arrange - tablet viewport (768x1024)
        var page = await fixture.CreatePageWithViewportAsync(768, 1024);

        try
        {
            await AuthenticationHelper.LoginAsync(page, fixture.BaseUrl);
            await page.GotoAsync(fixture.BaseUrl);
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Assert - calendar grid should render
            var calendarGrid = page.Locator(".calendar-grid");
            await Expect(calendarGrid).ToBeVisibleAsync(new() { Timeout = 10000 });
        }
        finally
        {
            await page.Context.DisposeAsync();
        }
    }
}
