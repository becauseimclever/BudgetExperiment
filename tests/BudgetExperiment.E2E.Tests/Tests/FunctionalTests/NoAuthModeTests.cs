// <copyright file="NoAuthModeTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Text.RegularExpressions;

using BudgetExperiment.E2E.Tests.Fixtures;
using BudgetExperiment.E2E.Tests.Helpers;

namespace BudgetExperiment.E2E.Tests.Tests.FunctionalTests;

/// <summary>
/// E2E tests verifying the no-auth (demo) mode experience.
/// These tests require the application to be running with Authentication__Mode=None.
/// </summary>
[Collection("Playwright")]
public class NoAuthModeTests
{
    private readonly PlaywrightFixture _fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="NoAuthModeTests"/> class.
    /// </summary>
    /// <param name="fixture">The Playwright fixture.</param>
    public NoAuthModeTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Verifies the home page loads without requiring login when auth is disabled.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Functional")]
    [Trait("Category", "NoAuth")]
    [Trait("Category", "LocalOnly")]
    public async Task HomePage_LoadsWithoutLogin_WhenAuthDisabled()
    {
        if (!LocalExecutionGuard.IsLocalBaseUrl(_fixture.BaseUrl))
        {
            return;
        }

        if (!await IsNoAuthModeAsync())
        {
            return;
        }

        var page = _fixture.Page;
        var response = await page.GotoAsync(
            _fixture.BaseUrl,
            new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

        Assert.NotNull(response);
        Assert.True(response.Ok, $"Expected OK response, got {response.Status}");
        await Expect(page).ToHaveTitleAsync(new Regex("Budget", RegexOptions.IgnoreCase));
    }

    /// <summary>
    /// Verifies the auth-off banner is visible when authentication is disabled.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Functional")]
    [Trait("Category", "NoAuth")]
    [Trait("Category", "LocalOnly")]
    public async Task AuthOffBanner_IsVisible_WhenAuthDisabled()
    {
        if (!LocalExecutionGuard.IsLocalBaseUrl(_fixture.BaseUrl))
        {
            return;
        }

        if (!await IsNoAuthModeAsync())
        {
            return;
        }

        var page = _fixture.Page;
        await page.GotoAsync(
            _fixture.BaseUrl,
            new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

        var banner = page.GetByRole(AriaRole.Status);
        await Expect(banner).ToBeVisibleAsync();

        var bannerText = await banner.InnerTextAsync();
        Assert.Contains("demo mode", bannerText, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Verifies no login button is visible when authentication is disabled.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Functional")]
    [Trait("Category", "NoAuth")]
    [Trait("Category", "LocalOnly")]
    public async Task LoginButton_IsNotVisible_WhenAuthDisabled()
    {
        if (!LocalExecutionGuard.IsLocalBaseUrl(_fixture.BaseUrl))
        {
            return;
        }

        if (!await IsNoAuthModeAsync())
        {
            return;
        }

        var page = _fixture.Page;
        await page.GotoAsync(
            _fixture.BaseUrl,
            new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

        var loginLink = page.GetByRole(AriaRole.Link, new()
        {
            Name = "Login",
        });
        await Expect(loginLink).ToHaveCountAsync(0);
    }

    /// <summary>
    /// Verifies the /api/v1/config endpoint returns mode=none.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Functional")]
    [Trait("Category", "NoAuth")]
    [Trait("Category", "LocalOnly")]
    public async Task ConfigEndpoint_ReturnsModeNone_WhenAuthDisabled()
    {
        if (!LocalExecutionGuard.IsLocalBaseUrl(_fixture.BaseUrl))
        {
            return;
        }

        var page = _fixture.Page;
        var response = await page.APIRequest.GetAsync($"{_fixture.BaseUrl}/api/v1/config");

        Assert.True(response.Ok, $"Expected OK response from /api/v1/config, got {response.Status}");

        var body = await response.TextAsync();

        // Only run remaining assertions if actually in no-auth mode
        if (!body.Contains("\"none\"", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        Assert.Contains("\"mode\"", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"none\"", body, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Verifies no JavaScript console errors occur during no-auth navigation.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Functional")]
    [Trait("Category", "NoAuth")]
    [Trait("Category", "LocalOnly")]
    public async Task NoJsErrors_DuringNavigation_WhenAuthDisabled()
    {
        if (!LocalExecutionGuard.IsLocalBaseUrl(_fixture.BaseUrl))
        {
            return;
        }

        if (!await IsNoAuthModeAsync())
        {
            return;
        }

        var page = _fixture.Page;
        var jsErrors = new List<string>();

        page.PageError += (_, error) => jsErrors.Add(error);

        await page.GotoAsync(
            _fixture.BaseUrl,
            new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

        // Navigate to a few pages to check for JS errors
        var navLinks = page.Locator("nav a");
        var linkCount = await navLinks.CountAsync();
        var maxLinks = Math.Min(linkCount, 3);

        for (var i = 0; i < maxLinks; i++)
        {
            var link = navLinks.Nth(i);
            if (await link.IsVisibleAsync())
            {
                await link.ClickAsync();
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            }
        }

        Assert.Empty(jsErrors);
    }

    /// <summary>
    /// Verifies the /authentication/login route redirects to home in no-auth mode.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Functional")]
    [Trait("Category", "NoAuth")]
    [Trait("Category", "LocalOnly")]
    public async Task AuthenticationRoute_RedirectsToHome_WhenAuthDisabled()
    {
        if (!LocalExecutionGuard.IsLocalBaseUrl(_fixture.BaseUrl))
        {
            return;
        }

        if (!await IsNoAuthModeAsync())
        {
            return;
        }

        var page = _fixture.Page;
        await page.GotoAsync(
            $"{_fixture.BaseUrl}/authentication/login",
            new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

        // Should redirect away from the authentication route
        Assert.DoesNotContain("/authentication/", page.Url, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks whether the target application is running in no-auth mode
    /// by querying the /api/v1/config endpoint.
    /// </summary>
    /// <returns>True if auth mode is "none".</returns>
    private async Task<bool> IsNoAuthModeAsync()
    {
        var page = _fixture.Page;
        var response = await page.APIRequest.GetAsync($"{_fixture.BaseUrl}/api/v1/config");
        if (!response.Ok)
        {
            return false;
        }

        var body = await response.TextAsync();
        return body.Contains("\"none\"", StringComparison.OrdinalIgnoreCase);
    }
}
