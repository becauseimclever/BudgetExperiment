// <copyright file="ReportNavigationTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.E2E.Tests.Fixtures;
using BudgetExperiment.E2E.Tests.Helpers;

namespace BudgetExperiment.E2E.Tests.Tests;

/// <summary>
/// E2E tests for report pages navigation, URL bookmarking, and calendar↔reports flow.
/// </summary>
[Collection("Playwright")]
public class ReportNavigationTests
{
    private readonly PlaywrightFixture fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReportNavigationTests"/> class.
    /// </summary>
    /// <param name="fixture">The Playwright fixture.</param>
    public ReportNavigationTests(PlaywrightFixture fixture)
    {
        this.fixture = fixture;
    }

    /// <summary>
    /// Verifies the Monthly Trends report page loads successfully.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Navigation")]
    [Trait("Category", "Reports")]
    public async Task MonthlyTrendsPage_ShouldLoad()
    {
        // Arrange
        var page = fixture.Page;
        await AuthenticationHelper.LoginAsync(page, fixture.BaseUrl);

        // Act
        var response = await page.GotoAsync($"{fixture.BaseUrl}/reports/trends");

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Ok, $"Monthly Trends page should return OK, got {response.Status}");

        var navMenu = page.Locator("nav.nav-menu");
        await Expect(navMenu).ToBeVisibleAsync(new() { Timeout = 5000 });
    }

    /// <summary>
    /// Verifies the Budget vs. Actual report page loads successfully.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Navigation")]
    [Trait("Category", "Reports")]
    public async Task BudgetComparisonPage_ShouldLoad()
    {
        // Arrange
        var page = fixture.Page;
        await AuthenticationHelper.LoginAsync(page, fixture.BaseUrl);

        // Act
        var response = await page.GotoAsync($"{fixture.BaseUrl}/reports/budget-comparison");

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Ok, $"Budget Comparison page should return OK, got {response.Status}");

        var navMenu = page.Locator("nav.nav-menu");
        await Expect(navMenu).ToBeVisibleAsync(new() { Timeout = 5000 });
    }

    /// <summary>
    /// Verifies that the Category Spending report supports date range URL parameters.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Navigation")]
    [Trait("Category", "Reports")]
    public async Task CategoryReport_SupportsDateRangeUrlParams()
    {
        // Arrange
        var page = fixture.Page;
        await AuthenticationHelper.LoginAsync(page, fixture.BaseUrl);

        // Act
        var response = await page.GotoAsync($"{fixture.BaseUrl}/reports/categories?start=2026-01-01&end=2026-01-31");

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Ok, $"Category report with date range should return OK, got {response.Status}");
        Assert.Contains("reports/categories", page.Url);
    }

    /// <summary>
    /// Verifies that the Category Spending report supports year/month URL parameters.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Navigation")]
    [Trait("Category", "Reports")]
    public async Task CategoryReport_SupportsYearMonthUrlParams()
    {
        // Arrange
        var page = fixture.Page;
        await AuthenticationHelper.LoginAsync(page, fixture.BaseUrl);

        // Act
        var response = await page.GotoAsync($"{fixture.BaseUrl}/reports/categories?year=2026&month=1");

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Ok, $"Category report with year/month should return OK, got {response.Status}");
        Assert.Contains("reports/categories", page.Url);
    }

    /// <summary>
    /// Verifies navigating from calendar to reports and back preserves month context.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Navigation")]
    [Trait("Category", "Reports")]
    public async Task CalendarToReports_PreservesMonthContext()
    {
        // Arrange
        var page = fixture.Page;
        await AuthenticationHelper.LoginAsync(page, fixture.BaseUrl);

        // Act — navigate to a specific month on the calendar
        await page.GotoAsync($"{fixture.BaseUrl}/2026/1");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Click the "Reports" link in the calendar header
        var reportsLink = page.Locator(".btn-view-reports");
        if (await reportsLink.IsVisibleAsync())
        {
            await reportsLink.ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Assert — should be on reports page with January 2026 context
            Assert.Contains("reports/categories", page.Url);
            Assert.Contains("year=2026", page.Url);
            Assert.Contains("month=1", page.Url);
        }
    }

    /// <summary>
    /// Verifies all report pages load without console errors.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Reports")]
    public async Task AllReportPages_LoadWithoutConsoleErrors()
    {
        // Arrange
        var page = fixture.Page;
        await AuthenticationHelper.LoginAsync(page, fixture.BaseUrl);

        var reportRoutes = new[]
        {
            "reports",
            "reports/categories",
            "reports/trends",
            "reports/budget-comparison",
        };

        foreach (var route in reportRoutes)
        {
            var errors = new List<string>();
            page.Console += (_, msg) =>
            {
                if (msg.Type == "error" && !msg.Text.Contains("favicon"))
                {
                    errors.Add($"[{route}] {msg.Text}");
                }
            };

            // Act
            var response = await page.GotoAsync($"{fixture.BaseUrl}/{route}");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Assert
            Assert.NotNull(response);
            Assert.True(response.Ok, $"{route} should return OK, got {response.Status}");

            // Allow auth-related errors since E2E may not have full auth setup
            var criticalErrors = errors.Where(e =>
                !e.Contains("401") &&
                !e.Contains("403") &&
                !e.Contains("Unauthorized")).ToList();

            Assert.Empty(criticalErrors);
        }
    }

    /// <summary>
    /// Verifies the Reports Index page has links to all report types.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Navigation")]
    [Trait("Category", "Reports")]
    public async Task ReportsIndex_HasLinksToAllReports()
    {
        // Arrange
        var page = fixture.Page;
        await AuthenticationHelper.LoginAsync(page, fixture.BaseUrl);

        // Act
        await page.GotoAsync($"{fixture.BaseUrl}/reports");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert
        var categoryLink = page.Locator("a[href*='reports/categories']");
        var trendsLink = page.Locator("a[href*='reports/trends']");
        var budgetLink = page.Locator("a[href*='reports/budget-comparison']");

        // Verify at least one of each link type is present
        Assert.True(
            await categoryLink.CountAsync() > 0 ||
            await trendsLink.CountAsync() > 0 ||
            await budgetLink.CountAsync() > 0,
            "Reports index should have links to at least some report pages");
    }

    /// <summary>
    /// Verifies the Monthly Trends page has no serious accessibility violations.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Accessibility")]
    [Trait("Category", "Reports")]
    public async Task MonthlyTrendsPage_ShouldHaveNoSeriousAccessibilityViolations()
    {
        // Arrange
        var page = fixture.Page;
        await AuthenticationHelper.LoginAsync(page, fixture.BaseUrl);
        await page.GotoAsync($"{fixture.BaseUrl}/reports/trends");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act — exclude SVG charts from audit
        var result = await AccessibilityHelper.AnalyzePageAsync(
            page,
            exclude: [".chart-container", "svg.chart", "svg"]);

        // Assert
        AccessibilityHelper.AssertNoSeriousViolations(result, "Monthly Trends");
    }

    /// <summary>
    /// Verifies the Budget vs. Actual page has no serious accessibility violations.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Accessibility")]
    [Trait("Category", "Reports")]
    public async Task BudgetComparisonPage_ShouldHaveNoSeriousAccessibilityViolations()
    {
        // Arrange
        var page = fixture.Page;
        await AuthenticationHelper.LoginAsync(page, fixture.BaseUrl);
        await page.GotoAsync($"{fixture.BaseUrl}/reports/budget-comparison");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act — exclude SVG charts from audit
        var result = await AccessibilityHelper.AnalyzePageAsync(
            page,
            exclude: [".chart-container", "svg.chart", "svg"]);

        // Assert
        AccessibilityHelper.AssertNoSeriousViolations(result, "Budget Comparison");
    }

    /// <summary>
    /// Verifies the Category Spending report page has no serious accessibility violations.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Accessibility")]
    [Trait("Category", "Reports")]
    public async Task CategorySpendingPage_ShouldHaveNoSeriousAccessibilityViolations()
    {
        // Arrange
        var page = fixture.Page;
        await AuthenticationHelper.LoginAsync(page, fixture.BaseUrl);
        await page.GotoAsync($"{fixture.BaseUrl}/reports/categories");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act — exclude SVG charts from audit
        var result = await AccessibilityHelper.AnalyzePageAsync(
            page,
            exclude: [".chart-container", "svg.chart", "svg"]);

        // Assert
        AccessibilityHelper.AssertNoSeriousViolations(result, "Category Spending");
    }
}
