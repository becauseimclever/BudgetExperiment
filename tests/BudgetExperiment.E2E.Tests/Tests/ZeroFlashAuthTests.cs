// <copyright file="ZeroFlashAuthTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.E2E.Tests.Fixtures;
using BudgetExperiment.E2E.Tests.Helpers;

namespace BudgetExperiment.E2E.Tests.Tests;

/// <summary>
/// Tests to verify zero-flash authentication experience.
/// These tests ensure no intermediate loading states flash during initial page load.
/// </summary>
[Collection("Playwright")]
public class ZeroFlashAuthTests
{
    private readonly PlaywrightFixture _fixture;

    /// <summary>
    /// Forbidden text that should never be visible during page load.
    /// These indicate incomplete auth state handling or flash content.
    /// </summary>
    private static readonly string[] _forbiddenFlashTexts =
    [
        "Checking authentication",
        "Checking authentication...",
        "Redirecting to login",
        "Redirecting to login...",
        "Please wait...",
        "Initializing...",
    ];

    /// <summary>
    /// Initializes a new instance of the <see cref="ZeroFlashAuthTests"/> class.
    /// </summary>
    /// <param name="fixture">The Playwright fixture.</param>
    public ZeroFlashAuthTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Verifies that no flash messages appear during initial page load for unauthenticated users.
    /// The user should see either the branded loading overlay or a direct redirect to Authentik.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Performance")]
    [Trait("Category", "ZeroFlash")]
    public async Task InitialLoad_ShouldNotShowFlashMessages_WhenUnauthenticated()
    {
        // Arrange - Create fresh context without cookies (unauthenticated)
        var browser = await _fixture.Page.Context.Browser!.NewContextAsync();
        var page = await browser.NewPageAsync();
        var capturedTexts = new List<string>();

        // Set up mutation observer to capture any text that appears
        await page.AddInitScriptAsync(@"
            window.__flashTexts = [];
            const observer = new MutationObserver((mutations) => {
                for (const mutation of mutations) {
                    if (mutation.type === 'childList') {
                        for (const node of mutation.addedNodes) {
                            if (node.textContent) {
                                window.__flashTexts.push(node.textContent.trim());
                            }
                        }
                    }
                }
            });
            observer.observe(document.documentElement, { childList: true, subtree: true });
        ");

        try
        {
            // Act - Navigate to the app
            await page.GotoAsync(_fixture.BaseUrl);

            // Wait for either redirect to Authentik or app to stabilize
            await Task.Delay(2000); // Allow time for any flashes to appear

            // Collect all text that was captured during load
            var flashTexts = await page.EvaluateAsync<string[]>("() => window.__flashTexts || []");
            capturedTexts.AddRange(flashTexts);

            // Assert - Check that none of the forbidden texts appeared
            foreach (var forbiddenText in _forbiddenFlashTexts)
            {
                var foundFlash = capturedTexts.Any(t =>
                    t.Contains(forbiddenText, StringComparison.OrdinalIgnoreCase));

                Assert.False(
                    foundFlash,
                    $"Flash message detected during load: '{forbiddenText}'. " +
                    $"Captured texts: [{string.Join(", ", capturedTexts.Take(10))}]");
            }

            // Also verify current page state doesn't show flash content
            foreach (var forbiddenText in _forbiddenFlashTexts)
            {
                var element = page.GetByText(forbiddenText, new() { Exact = false });
                var isVisible = await element.IsVisibleAsync();

                Assert.False(
                    isVisible,
                    $"Flash message currently visible on page: '{forbiddenText}'");
            }
        }
        finally
        {
            await browser.DisposeAsync();
        }
    }

    /// <summary>
    /// Verifies that authenticated users see content immediately without flash messages.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Performance")]
    [Trait("Category", "ZeroFlash")]
    public async Task PageLoad_ShouldNotShowFlashMessages_WhenAuthenticated()
    {
        // Arrange - First authenticate
        var page = _fixture.Page;
        await AuthenticationHelper.LoginAsync(page, _fixture.BaseUrl);

        // Navigate away and back to test re-entry
        await page.GotoAsync($"{_fixture.BaseUrl}/settings");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var capturedTexts = new List<string>();

        // Set up text capture before navigating
        await page.AddInitScriptAsync(@"
            window.__flashTexts = [];
            const observer = new MutationObserver((mutations) => {
                for (const mutation of mutations) {
                    if (mutation.type === 'childList') {
                        for (const node of mutation.addedNodes) {
                            if (node.textContent) {
                                window.__flashTexts.push(node.textContent.trim());
                            }
                        }
                    }
                }
            });
            observer.observe(document.documentElement, { childList: true, subtree: true });
        ");

        // Act - Navigate back to home
        await page.GotoAsync(_fixture.BaseUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Collect captured texts
        var flashTexts = await page.EvaluateAsync<string[]>("() => window.__flashTexts || []");
        capturedTexts.AddRange(flashTexts);

        // Assert
        foreach (var forbiddenText in _forbiddenFlashTexts)
        {
            var foundFlash = capturedTexts.Any(t =>
                t.Contains(forbiddenText, StringComparison.OrdinalIgnoreCase));

            Assert.False(
                foundFlash,
                $"Flash message detected during authenticated navigation: '{forbiddenText}'");
        }
    }

    /// <summary>
    /// Verifies that the branded loading overlay (if present) is used instead of plain text messages.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Performance")]
    [Trait("Category", "ZeroFlash")]
    public async Task LoadingState_ShouldUseBrandedOverlay_NotPlainText()
    {
        // Arrange - Create fresh context
        var browser = await _fixture.Page.Context.Browser!.NewContextAsync();
        var page = await browser.NewPageAsync();

        try
        {
            // Act - Navigate and check for proper overlay usage
            await page.GotoAsync(_fixture.BaseUrl);

            // The app should either:
            // 1. Show a branded loading overlay with proper styling
            // 2. Redirect to Authentik immediately
            // 3. Show content immediately (if using cached auth)

            // Check for branded overlay (if it appears)
            var brandedOverlay = page.Locator(".loading-overlay, .app-loading, [data-loading-overlay]");
            var plainLoadingText = page.GetByText("Loading...", new() { Exact = true });

            // If there's any loading state, it should be the branded overlay, not plain text
            if (await plainLoadingText.IsVisibleAsync())
            {
                // Plain "Loading..." text is acceptable only inside a styled container
                var isInsideStyledContainer = await page.EvaluateAsync<bool>(@"() => {
                    const loadingText = document.evaluate(
                        ""//*[contains(text(), 'Loading...')]"",
                        document,
                        null,
                        XPathResult.FIRST_ORDERED_NODE_TYPE,
                        null
                    ).singleNodeValue;

                    if (!loadingText) return true; // No plain loading text is good

                    // Check if it has styled parent
                    let parent = loadingText.parentElement;
                    while (parent) {
                        const style = window.getComputedStyle(parent);
                        if (style.backgroundColor !== 'rgba(0, 0, 0, 0)' ||
                            parent.classList.contains('loading-overlay') ||
                            parent.classList.contains('app-loading')) {
                            return true; // Inside styled container
                        }
                        parent = parent.parentElement;
                    }
                    return false; // Plain unstyled loading text
                }");

                Assert.True(
                    isInsideStyledContainer,
                    "Plain 'Loading...' text should be inside a styled loading overlay container");
            }
        }
        finally
        {
            await browser.DisposeAsync();
        }
    }
}
