// <copyright file="ReportExportTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.E2E.Tests.Fixtures;
using BudgetExperiment.E2E.Tests.Helpers;

namespace BudgetExperiment.E2E.Tests.Tests;

/// <summary>
/// E2E tests for report export workflows.
/// </summary>
[Collection("Playwright")]
public class ReportExportTests
{
    private readonly PlaywrightFixture fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReportExportTests"/> class.
    /// </summary>
    /// <param name="fixture">The Playwright fixture.</param>
    public ReportExportTests(PlaywrightFixture fixture)
    {
        this.fixture = fixture;
    }

    /// <summary>
    /// Verifies report export triggers the export endpoint.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Reports")]
    [Trait("Category", "Export")]
    public async Task CategoryReport_Export_TriggersEndpoint()
    {
        // Arrange
        var page = fixture.Page;
        await AuthenticationHelper.LoginAsync(page, fixture.BaseUrl);
        await page.GotoAsync($"{fixture.BaseUrl}/reports/categories");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act
        await page.Locator(".export-trigger").ClickAsync();

        var response = await page.RunAndWaitForResponseAsync(
            async () =>
            {
                await page.Locator(".export-item").First.ClickAsync();
            },
            response => response.Url.Contains("/api/v1/exports/") && response.Status == 200);

        // Assert
        Assert.NotNull(response);
        response.Headers.TryGetValue("content-type", out var contentType);
        Assert.Contains("text/csv", contentType ?? string.Empty);
    }
}
