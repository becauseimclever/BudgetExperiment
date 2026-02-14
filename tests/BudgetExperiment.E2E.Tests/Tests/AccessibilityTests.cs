// <copyright file="AccessibilityTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.E2E.Tests.Fixtures;
using BudgetExperiment.E2E.Tests.Helpers;

namespace BudgetExperiment.E2E.Tests.Tests;

/// <summary>
/// Accessibility tests using axe-core to verify WCAG 2.0 AA compliance.
/// These tests scan key pages for accessibility violations.
/// </summary>
[Collection("Playwright")]
public class AccessibilityTests
{
    private readonly PlaywrightFixture fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="AccessibilityTests"/> class.
    /// </summary>
    /// <param name="fixture">The Playwright fixture.</param>
    public AccessibilityTests(PlaywrightFixture fixture)
    {
        this.fixture = fixture;
    }

    /// <summary>
    /// Verifies the Calendar page has no serious accessibility violations.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Accessibility")]
    public async Task CalendarPage_ShouldHaveNoSeriousAccessibilityViolations()
    {
        // Arrange
        var page = fixture.Page;
        await AuthenticationHelper.LoginAsync(page, fixture.BaseUrl);
        await page.GotoAsync(fixture.BaseUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act
        var result = await AccessibilityHelper.AnalyzePageAsync(page);

        // Assert
        AccessibilityHelper.AssertNoSeriousViolations(result, "Calendar");
    }

    /// <summary>
    /// Verifies the Accounts page has no serious accessibility violations.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Accessibility")]
    public async Task AccountsPage_ShouldHaveNoSeriousAccessibilityViolations()
    {
        // Arrange
        var page = fixture.Page;
        await AuthenticationHelper.LoginAsync(page, fixture.BaseUrl);
        await page.GotoAsync($"{fixture.BaseUrl}/accounts");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act
        var result = await AccessibilityHelper.AnalyzePageAsync(page);

        // Assert
        AccessibilityHelper.AssertNoSeriousViolations(result, "Accounts");
    }

    /// <summary>
    /// Verifies the Categories page has no serious accessibility violations.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Accessibility")]
    public async Task CategoriesPage_ShouldHaveNoSeriousAccessibilityViolations()
    {
        // Arrange
        var page = fixture.Page;
        await AuthenticationHelper.LoginAsync(page, fixture.BaseUrl);
        await page.GotoAsync($"{fixture.BaseUrl}/categories");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act
        var result = await AccessibilityHelper.AnalyzePageAsync(page);

        // Assert
        AccessibilityHelper.AssertNoSeriousViolations(result, "Categories");
    }

    /// <summary>
    /// Verifies the Recurring Bills page has no serious accessibility violations.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Accessibility")]
    public async Task RecurringPage_ShouldHaveNoSeriousAccessibilityViolations()
    {
        // Arrange
        var page = fixture.Page;
        await AuthenticationHelper.LoginAsync(page, fixture.BaseUrl);
        await page.GotoAsync($"{fixture.BaseUrl}/recurring");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act
        var result = await AccessibilityHelper.AnalyzePageAsync(page);

        // Assert
        AccessibilityHelper.AssertNoSeriousViolations(result, "Recurring Bills");
    }

    /// <summary>
    /// Verifies the Transfers page has no serious accessibility violations.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Accessibility")]
    public async Task TransfersPage_ShouldHaveNoSeriousAccessibilityViolations()
    {
        // Arrange
        var page = fixture.Page;
        await AuthenticationHelper.LoginAsync(page, fixture.BaseUrl);
        await page.GotoAsync($"{fixture.BaseUrl}/transfers");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act
        var result = await AccessibilityHelper.AnalyzePageAsync(page);

        // Assert
        AccessibilityHelper.AssertNoSeriousViolations(result, "Transfers");
    }

    /// <summary>
    /// Verifies the Import page has no serious accessibility violations.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Accessibility")]
    public async Task ImportPage_ShouldHaveNoSeriousAccessibilityViolations()
    {
        // Arrange
        var page = fixture.Page;
        await AuthenticationHelper.LoginAsync(page, fixture.BaseUrl);
        await page.GotoAsync($"{fixture.BaseUrl}/import");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act
        var result = await AccessibilityHelper.AnalyzePageAsync(page);

        // Assert
        AccessibilityHelper.AssertNoSeriousViolations(result, "Import");
    }

    /// <summary>
    /// Verifies the Settings page has no serious accessibility violations.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Accessibility")]
    public async Task SettingsPage_ShouldHaveNoSeriousAccessibilityViolations()
    {
        // Arrange
        var page = fixture.Page;
        await AuthenticationHelper.LoginAsync(page, fixture.BaseUrl);
        await page.GotoAsync($"{fixture.BaseUrl}/settings");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act
        var result = await AccessibilityHelper.AnalyzePageAsync(page);

        // Assert
        AccessibilityHelper.AssertNoSeriousViolations(result, "Settings");
    }

    /// <summary>
    /// Verifies the Reports page has no serious accessibility violations.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Accessibility")]
    public async Task ReportsPage_ShouldHaveNoSeriousAccessibilityViolations()
    {
        // Arrange
        var page = fixture.Page;
        await AuthenticationHelper.LoginAsync(page, fixture.BaseUrl);
        await page.GotoAsync($"{fixture.BaseUrl}/reports");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act
        var result = await AccessibilityHelper.AnalyzePageAsync(page);

        // Assert
        AccessibilityHelper.AssertNoSeriousViolations(result, "Reports");
    }

    /// <summary>
    /// Verifies the report detail pages have no serious accessibility violations.
    /// </summary>
    /// <param name="route">Report route.</param>
    /// <param name="name">Report name.</param>
    /// <returns>A task representing the async test.</returns>
    [Theory]
    [Trait("Category", "Accessibility")]
    [InlineData("reports/categories", "Category Spending")]
    [InlineData("reports/trends", "Monthly Trends")]
    [InlineData("reports/budget-comparison", "Budget Comparison")]
    public async Task ReportPages_ShouldHaveNoSeriousAccessibilityViolations(string route, string name)
    {
        // Arrange
        var page = fixture.Page;
        await AuthenticationHelper.LoginAsync(page, fixture.BaseUrl);
        await page.GotoAsync($"{fixture.BaseUrl}/{route}");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act
        var result = await AccessibilityHelper.AnalyzePageAsync(page);

        // Assert
        AccessibilityHelper.AssertNoSeriousViolations(result, name);
    }

    /// <summary>
    /// Verifies the Budget page has no serious accessibility violations.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Accessibility")]
    public async Task BudgetPage_ShouldHaveNoSeriousAccessibilityViolations()
    {
        // Arrange
        var page = fixture.Page;
        await AuthenticationHelper.LoginAsync(page, fixture.BaseUrl);
        await page.GotoAsync($"{fixture.BaseUrl}/budget");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act
        var result = await AccessibilityHelper.AnalyzePageAsync(page);

        // Assert
        AccessibilityHelper.AssertNoSeriousViolations(result, "Budget");
    }

    /// <summary>
    /// Verifies the Paycheck Planner page has no serious accessibility violations.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Accessibility")]
    public async Task PaycheckPlannerPage_ShouldHaveNoSeriousAccessibilityViolations()
    {
        // Arrange
        var page = fixture.Page;
        await AuthenticationHelper.LoginAsync(page, fixture.BaseUrl);
        await page.GotoAsync($"{fixture.BaseUrl}/paycheck-planner");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act
        var result = await AccessibilityHelper.AnalyzePageAsync(page);

        // Assert
        AccessibilityHelper.AssertNoSeriousViolations(result, "Paycheck Planner");
    }

    /// <summary>
    /// Verifies the skip link is present and functional.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Accessibility")]
    public async Task SkipLink_ShouldBePresent()
    {
        // Arrange
        var page = fixture.Page;
        await AuthenticationHelper.LoginAsync(page, fixture.BaseUrl);
        await page.GotoAsync(fixture.BaseUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act
        var skipLink = page.Locator("a.skip-link");

        // Assert
        await Expect(skipLink).ToHaveAttributeAsync("href", "#main-content");
        await Expect(skipLink).ToContainTextAsync("Skip to main content");
    }

    /// <summary>
    /// Verifies the main content landmark is present.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Accessibility")]
    public async Task MainContentLandmark_ShouldBePresent()
    {
        // Arrange
        var page = fixture.Page;
        await AuthenticationHelper.LoginAsync(page, fixture.BaseUrl);
        await page.GotoAsync(fixture.BaseUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act
        var mainContent = page.Locator("main#main-content");

        // Assert
        await Expect(mainContent).ToBeVisibleAsync();
        await Expect(mainContent).ToHaveAttributeAsync("role", "main");
    }

    /// <summary>
    /// Verifies the navigation landmark is present.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Accessibility")]
    public async Task NavigationLandmark_ShouldBePresent()
    {
        // Arrange
        var page = fixture.Page;
        await AuthenticationHelper.LoginAsync(page, fixture.BaseUrl);
        await page.GotoAsync(fixture.BaseUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act
        var navigation = page.Locator("aside[role='navigation']");

        // Assert
        await Expect(navigation).ToBeVisibleAsync();
        await Expect(navigation).ToHaveAttributeAsync("aria-label", "Main navigation");
    }

    /// <summary>
    /// Verifies the header/banner landmark is present.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Accessibility")]
    public async Task BannerLandmark_ShouldBePresent()
    {
        // Arrange
        var page = fixture.Page;
        await AuthenticationHelper.LoginAsync(page, fixture.BaseUrl);
        await page.GotoAsync(fixture.BaseUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act
        var header = page.Locator("header[role='banner']");

        // Assert
        await Expect(header).ToBeVisibleAsync();
    }
}
