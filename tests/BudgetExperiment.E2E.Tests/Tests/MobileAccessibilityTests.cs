// <copyright file="MobileAccessibilityTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.E2E.Tests.Fixtures;
using BudgetExperiment.E2E.Tests.Helpers;

namespace BudgetExperiment.E2E.Tests.Tests;

/// <summary>
/// Accessibility tests for mobile-specific views using axe-core.
/// Validates WCAG compliance on mobile viewport.
/// </summary>
[Collection("MobilePlaywright")]
public class MobileAccessibilityTests
{
    private readonly MobilePlaywrightFixture fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="MobileAccessibilityTests"/> class.
    /// </summary>
    /// <param name="fixture">The mobile Playwright fixture.</param>
    public MobileAccessibilityTests(MobilePlaywrightFixture fixture)
    {
        this.fixture = fixture;
    }

    /// <summary>
    /// Verifies the Calendar page on mobile has no serious accessibility violations.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Mobile")]
    [Trait("Category", "Accessibility")]
    public async Task CalendarMobile_ShouldHaveNoSeriousAccessibilityViolations()
    {
        // Arrange
        var page = fixture.Page;
        await AuthenticationHelper.LoginAsync(page, fixture.BaseUrl);
        await page.GotoAsync(fixture.BaseUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act
        var result = await AccessibilityHelper.AnalyzePageAsync(page);

        // Assert
        AccessibilityHelper.AssertNoSeriousViolations(result, "Calendar (Mobile)");
    }

    /// <summary>
    /// Verifies the FAB component passes axe-core checks.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Mobile")]
    [Trait("Category", "Accessibility")]
    public async Task Fab_ShouldPassAxeAccessibilityCheck()
    {
        // Arrange
        var page = fixture.Page;
        await AuthenticationHelper.LoginAsync(page, fixture.BaseUrl);
        await page.GotoAsync(fixture.BaseUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act - analyze just the FAB area
        var result = await AccessibilityHelper.AnalyzePageAsync(
            page,
            include: [".fab-container"]);

        // Assert
        AccessibilityHelper.AssertNoSeriousViolations(result, "FAB (Mobile)");
    }

    /// <summary>
    /// Verifies the FAB expanded (speed dial) passes axe-core checks.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Mobile")]
    [Trait("Category", "Accessibility")]
    public async Task FabExpanded_ShouldPassAxeAccessibilityCheck()
    {
        // Arrange
        var page = fixture.Page;
        await AuthenticationHelper.LoginAsync(page, fixture.BaseUrl);
        await page.GotoAsync(fixture.BaseUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act - expand the FAB
        await page.Locator(".fab-primary").ClickAsync();
        await Task.Delay(300); // Wait for animation

        var result = await AccessibilityHelper.AnalyzePageAsync(
            page,
            include: [".fab-container"]);

        // Assert
        AccessibilityHelper.AssertNoSeriousViolations(result, "FAB Expanded (Mobile)");
    }

    /// <summary>
    /// Verifies the BottomSheet (Quick Add) passes axe-core checks.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Mobile")]
    [Trait("Category", "Accessibility")]
    public async Task QuickAddBottomSheet_ShouldPassAxeAccessibilityCheck()
    {
        // Arrange
        var page = fixture.Page;
        await AuthenticationHelper.LoginAsync(page, fixture.BaseUrl);
        await page.GotoAsync(fixture.BaseUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act - open Quick Add BottomSheet
        await page.Locator(".fab-primary").ClickAsync();
        await page.Locator("#fab-quickadd-label").ClickAsync();
        await Expect(page.Locator(".bottom-sheet.is-visible")).ToBeVisibleAsync(new() { Timeout = 5000 });

        var result = await AccessibilityHelper.AnalyzePageAsync(
            page,
            include: [".bottom-sheet"]);

        // Assert
        AccessibilityHelper.AssertNoSeriousViolations(result, "Quick Add BottomSheet (Mobile)");
    }

    /// <summary>
    /// Verifies the week view passes axe-core checks.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Mobile")]
    [Trait("Category", "Accessibility")]
    public async Task WeekView_ShouldPassAxeAccessibilityCheck()
    {
        // Arrange
        var page = fixture.Page;
        await AuthenticationHelper.LoginAsync(page, fixture.BaseUrl);
        await page.GotoAsync(fixture.BaseUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act - switch to week view
        await page.GetByLabel("Week view").ClickAsync();
        await Expect(page.Locator(".week-view")).ToBeVisibleAsync(new() { Timeout = 5000 });

        var result = await AccessibilityHelper.AnalyzePageAsync(
            page,
            include: [".week-view"]);

        // Assert
        AccessibilityHelper.AssertNoSeriousViolations(result, "Week View (Mobile)");
    }

    /// <summary>
    /// Verifies the view toggle passes axe-core checks.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Mobile")]
    [Trait("Category", "Accessibility")]
    public async Task ViewToggle_ShouldPassAxeAccessibilityCheck()
    {
        // Arrange
        var page = fixture.Page;
        await AuthenticationHelper.LoginAsync(page, fixture.BaseUrl);
        await page.GotoAsync(fixture.BaseUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act
        var result = await AccessibilityHelper.AnalyzePageAsync(
            page,
            include: [".calendar-view-toggle"]);

        // Assert
        AccessibilityHelper.AssertNoSeriousViolations(result, "View Toggle (Mobile)");
    }

    /// <summary>
    /// Verifies the AI chat BottomSheet passes axe-core checks.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Mobile")]
    [Trait("Category", "Accessibility")]
    public async Task AiChatBottomSheet_ShouldPassAxeAccessibilityCheck()
    {
        // Arrange
        var page = fixture.Page;
        await AuthenticationHelper.LoginAsync(page, fixture.BaseUrl);
        await page.GotoAsync(fixture.BaseUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act - open AI chat BottomSheet
        await page.Locator(".fab-primary").ClickAsync();
        await page.Locator(".fab-ai").ClickAsync();
        await Expect(page.GetByRole(AriaRole.Dialog)).ToBeVisibleAsync(new() { Timeout = 5000 });

        var result = await AccessibilityHelper.AnalyzePageAsync(
            page,
            include: [".bottom-sheet"]);

        // Assert
        AccessibilityHelper.AssertNoSeriousViolations(result, "AI Chat BottomSheet (Mobile)");
    }
}
