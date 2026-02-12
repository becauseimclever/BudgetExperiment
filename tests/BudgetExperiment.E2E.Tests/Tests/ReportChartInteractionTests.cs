// <copyright file="ReportChartInteractionTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.E2E.Tests.Fixtures;
using BudgetExperiment.E2E.Tests.Helpers;
using Xunit.Abstractions;

namespace BudgetExperiment.E2E.Tests.Tests;

/// <summary>
/// E2E tests for chart interactions on report pages.
/// </summary>
[Collection("Playwright")]
public class ReportChartInteractionTests
{
    private readonly PlaywrightFixture fixture;
    private readonly ITestOutputHelper output;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReportChartInteractionTests"/> class.
    /// </summary>
    /// <param name="fixture">The Playwright fixture.</param>
    /// <param name="output">The output helper.</param>
    public ReportChartInteractionTests(PlaywrightFixture fixture, ITestOutputHelper output)
    {
        this.fixture = fixture;
        this.output = output;
    }

    /// <summary>
    /// Verifies the donut chart shows a tooltip on hover.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Reports")]
    [Trait("Category", "Chart")]
    public async Task CategoryReport_DonutChart_ShowsTooltipOnHover()
    {
        // Arrange
        var page = fixture.Page;
        await AuthenticationHelper.LoginAsync(page, fixture.BaseUrl);

        // Act
        await page.GotoAsync($"{fixture.BaseUrl}/reports/categories");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var segments = page.Locator(".donut-segment");
        if (await segments.CountAsync() == 0)
        {
            output.WriteLine("No donut segments found; skipping tooltip assertion.");
            return;
        }

        await segments.First.HoverAsync();

        // Assert
        var tooltip = page.Locator(".donut-tooltip");
        await Expect(tooltip).ToBeVisibleAsync(new() { Timeout = 5000 });
    }

    /// <summary>
    /// Verifies the bar chart shows a tooltip on hover.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Reports")]
    [Trait("Category", "Chart")]
    public async Task TrendsReport_BarChart_ShowsTooltipOnHover()
    {
        // Arrange
        var page = fixture.Page;
        await AuthenticationHelper.LoginAsync(page, fixture.BaseUrl);

        // Act
        await page.GotoAsync($"{fixture.BaseUrl}/reports/trends");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var bars = page.Locator("rect.bar-rect");
        if (await bars.CountAsync() == 0)
        {
            output.WriteLine("No bar segments found; skipping tooltip assertion.");
            return;
        }

        await bars.First.HoverAsync();

        // Assert
        var tooltip = page.Locator(".bar-tooltip");
        await Expect(tooltip).ToBeVisibleAsync(new() { Timeout = 5000 });
    }

    /// <summary>
    /// Verifies clicking a report item navigates to detail view.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Reports")]
    [Trait("Category", "Chart")]
    public async Task CategoryReport_Click_Navigates_To_Detail()
    {
        // Arrange
        var page = fixture.Page;
        await AuthenticationHelper.LoginAsync(page, fixture.BaseUrl);

        // Act
        await page.GotoAsync($"{fixture.BaseUrl}/reports/categories");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var rows = page.Locator(".category-row");
        if (await rows.CountAsync() == 0)
        {
            output.WriteLine("No category rows found; skipping navigation assertion.");
            return;
        }

        await rows.First.ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert
        Assert.Contains("/accounts", page.Url, StringComparison.OrdinalIgnoreCase);
    }
}
