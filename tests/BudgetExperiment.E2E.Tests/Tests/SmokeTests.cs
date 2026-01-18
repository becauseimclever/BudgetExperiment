// <copyright file="SmokeTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.E2E.Tests.Infrastructure;
using BudgetExperiment.E2E.Tests.PageObjects;

namespace BudgetExperiment.E2E.Tests.Tests;

/// <summary>
/// Basic smoke tests to verify the application is accessible.
/// </summary>
[Collection(PlaywrightCollection.Name)]
public class SmokeTests
{
    private readonly PlaywrightFixture _fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="SmokeTests"/> class.
    /// </summary>
    /// <param name="fixture">The Playwright _fixture.</param>
    public SmokeTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Verifies the application is accessible and loads.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    public async Task Application_IsAccessible()
    {
        var page = await _fixture.CreatePageAsync();

        var response = await page.GotoAsync("/");

        Assert.NotNull(response);
        Assert.True(response.Ok, $"Expected successful response, got {response.Status}");
    }

    /// <summary>
    /// Verifies the page has a title after login.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    public async Task Application_HasPageTitle()
    {
        var page = await _fixture.CreatePageAsync();
        await AuthenticationHelper.GoToWithLoginAsync(page, "/");

        var title = await page.TitleAsync();

        Assert.False(string.IsNullOrWhiteSpace(title), "Page should have a title");
        Assert.Contains("Budget", title, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Verifies the sidebar navigation is visible after login.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    public async Task Application_HasNavigationSidebar()
    {
        var page = await _fixture.CreatePageAsync();
        await AuthenticationHelper.GoToWithLoginAsync(page, "/");

        // Wait for the app to fully load after login
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Debug: Check current URL
        var currentUrl = page.Url;
        Assert.False(
            currentUrl.Contains("authentik", StringComparison.OrdinalIgnoreCase),
            $"Should not be on Authentik login page. Current URL: {currentUrl}");

        var nav = new NavigationComponent(page);

        // Wait for the sidebar to be visible with a timeout
        await nav.Sidebar.WaitForAsync(new LocatorWaitForOptions { Timeout = 10000 });
        var isVisible = await nav.Sidebar.IsVisibleAsync();

        Assert.True(isVisible, "Navigation sidebar should be visible");
    }

    /// <summary>
    /// Verifies no JavaScript errors on initial load.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    public async Task Application_NoJavaScriptErrorsOnLoad()
    {
        var page = await _fixture.CreatePageAsync();
        var jsErrors = new List<string>();

        page.Console += (_, msg) =>
        {
            if (msg.Type == "error")
            {
                jsErrors.Add(msg.Text);
            }
        };

        await AuthenticationHelper.GoToWithLoginAsync(page, "/");

        // Wait a moment for any async errors
        await page.WaitForTimeoutAsync(1000);

        // Filter out known acceptable errors (like favicon 404, etc.)
        var significantErrors = jsErrors
            .Where(e => !e.Contains("favicon", StringComparison.OrdinalIgnoreCase))
            .ToList();

        Assert.Empty(significantErrors);
    }

    /// <summary>
    /// Verifies all main pages are reachable after login.
    /// </summary>
    /// <param name="path">The page path to test.</param>
    /// <returns>A task representing the async test.</returns>
    [Theory]
    [InlineData("/accounts")]
    [InlineData("/budget")]
    [InlineData("/calendar")]
    [InlineData("/categories")]
    [InlineData("/recurring")]
    [InlineData("/rules")]
    [InlineData("/settings")]
    public async Task Page_IsReachable(string path)
    {
        var page = await _fixture.CreatePageAsync();
        await AuthenticationHelper.GoToWithLoginAsync(page, path);

        // Verify we're on the expected page (not redirected to auth)
        Assert.DoesNotContain("authentik", page.Url, StringComparison.OrdinalIgnoreCase);
    }
}
