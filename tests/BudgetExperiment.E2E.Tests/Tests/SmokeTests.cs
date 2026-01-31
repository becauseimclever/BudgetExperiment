// <copyright file="SmokeTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Text.RegularExpressions;
using BudgetExperiment.E2E.Tests.Fixtures;
using BudgetExperiment.E2E.Tests.Helpers;

namespace BudgetExperiment.E2E.Tests.Tests;

/// <summary>
/// Smoke tests to verify basic application functionality.
/// </summary>
[Collection("Playwright")]
public class SmokeTests
{
    private readonly PlaywrightFixture fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="SmokeTests"/> class.
    /// </summary>
    /// <param name="fixture">The Playwright fixture.</param>
    public SmokeTests(PlaywrightFixture fixture)
    {
        this.fixture = fixture;
    }

    /// <summary>
    /// Verifies the home page loads successfully.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Smoke")]
    public async Task HomePage_ShouldLoad_WhenServerIsRunning()
    {
        // Arrange
        var page = fixture.Page;

        // Act
        var response = await page.GotoAsync(fixture.BaseUrl);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Ok, $"Expected OK response, got {response.Status}");
    }

    /// <summary>
    /// Verifies the page has an appropriate title.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Smoke")]
    public async Task HomePage_ShouldHaveBudgetTitle()
    {
        // Arrange
        var page = fixture.Page;

        // Act
        await page.GotoAsync(fixture.BaseUrl);

        // Assert
        await Expect(page).ToHaveTitleAsync(new Regex("Budget", RegexOptions.IgnoreCase));
    }

    /// <summary>
    /// Verifies login succeeds with test credentials.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Smoke")]
    public async Task Login_ShouldSucceed_WithTestCredentials()
    {
        // Arrange
        var page = fixture.Page;

        // Act
        await AuthenticationHelper.LoginAsync(page, fixture.BaseUrl);

        // Assert
        var isLoggedIn = await AuthenticationHelper.IsLoggedInAsync(page);
        Assert.True(isLoggedIn, "User should be logged in after authentication");
    }

    /// <summary>
    /// Verifies the main navigation menu renders after login.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Smoke")]
    public async Task Navigation_ShouldRender_WhenAuthenticated()
    {
        // Arrange
        var page = fixture.Page;
        await AuthenticationHelper.LoginAsync(page, fixture.BaseUrl);

        // Act & Assert
        var navMenu = page.Locator("nav.nav-menu");
        await Expect(navMenu).ToBeVisibleAsync();

        // Verify some key navigation items are present
        await Expect(page.GetByTitle("Calendar")).ToBeVisibleAsync();
        await Expect(page.GetByTitle("Settings")).ToBeVisibleAsync();
    }
}
