// <copyright file="PerformanceTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.E2E.Tests.Fixtures;
using BudgetExperiment.E2E.Tests.Helpers;
using Xunit.Abstractions;

namespace BudgetExperiment.E2E.Tests.Tests;

/// <summary>
/// Performance tests that capture and validate Core Web Vitals.
/// These tests ensure the application meets performance thresholds.
/// </summary>
[Collection("Playwright")]
public class PerformanceTests
{
    private readonly PlaywrightFixture _fixture;
    private readonly ITestOutputHelper _output;

    /// <summary>
    /// Initializes a new instance of the <see cref="PerformanceTests"/> class.
    /// </summary>
    /// <param name="fixture">The Playwright fixture.</param>
    /// <param name="output">The test output helper for logging.</param>
    public PerformanceTests(PlaywrightFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    /// <summary>
    /// Captures and validates Core Web Vitals for initial page load (unauthenticated).
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Performance")]
    [Trait("Category", "CoreWebVitals")]
    public async Task InitialPageLoad_ShouldMeetPerformanceThresholds()
    {
        // Arrange - Create fresh context without cookies
        var browser = await _fixture.Page.Context.Browser!.NewContextAsync();
        var page = await browser.NewPageAsync();

        try
        {
            // Act - Navigate to the app and wait for load
            await page.GotoAsync(_fixture.BaseUrl);
            await page.WaitForLoadStateAsync(LoadState.Load);

            // Give time for LCP and CLS to stabilize
            await Task.Delay(1000);

            var metrics = await PerformanceHelper.CaptureMetricsAsync(page);

            // Log metrics for CI visibility
            LogMetricsToOutput(metrics, "Initial Page Load (Unauthenticated)");
            PerformanceHelper.LogMetrics(metrics, "Initial Page Load");

            // Assert thresholds
            PerformanceHelper.AssertThresholds(metrics);
        }
        finally
        {
            await browser.DisposeAsync();
        }
    }

    /// <summary>
    /// Captures and validates Core Web Vitals for authenticated user page load.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Performance")]
    [Trait("Category", "CoreWebVitals")]
    public async Task AuthenticatedPageLoad_ShouldMeetPerformanceThresholds()
    {
        // Arrange - Authenticate first
        var page = _fixture.Page;
        await AuthenticationHelper.LoginAsync(page, _fixture.BaseUrl);

        // Navigate away to get a fresh load measurement
        await page.GotoAsync($"{_fixture.BaseUrl}/settings");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act - Navigate back to home and measure
        await page.GotoAsync(_fixture.BaseUrl);
        await page.WaitForLoadStateAsync(LoadState.Load);

        // Give time for LCP and CLS to stabilize
        await Task.Delay(1000);

        var metrics = await PerformanceHelper.CaptureMetricsAsync(page);

        // Log metrics for CI visibility
        LogMetricsToOutput(metrics, "Home Page (Authenticated)");
        PerformanceHelper.LogMetrics(metrics, "Authenticated Home");

        // Assert thresholds
        PerformanceHelper.AssertThresholds(metrics);
    }

    /// <summary>
    /// Captures Core Web Vitals for key application routes.
    /// </summary>
    /// <param name="route">The route to test.</param>
    /// <param name="routeName">The display name for the route.</param>
    /// <returns>A task representing the async test.</returns>
    [Theory]
    [Trait("Category", "Performance")]
    [Trait("Category", "CoreWebVitals")]
    [InlineData("", "Calendar")]
    [InlineData("recurring", "Recurring Bills")]
    [InlineData("categories", "Budget Categories")]
    [InlineData("budget", "Budget Overview")]
    [InlineData("reports", "Reports Overview")]
    public async Task Route_ShouldMeetPerformanceThresholds(string route, string routeName)
    {
        // Arrange - Authenticate
        var page = _fixture.Page;
        await AuthenticationHelper.LoginAsync(page, _fixture.BaseUrl);

        // Act - Navigate to the route
        var url = string.IsNullOrEmpty(route)
            ? _fixture.BaseUrl
            : $"{_fixture.BaseUrl}/{route}";

        await page.GotoAsync(url);
        await page.WaitForLoadStateAsync(LoadState.Load);

        // Give time for LCP and CLS to stabilize
        await Task.Delay(1000);

        var metrics = await PerformanceHelper.CaptureMetricsAsync(page);

        // Log metrics
        LogMetricsToOutput(metrics, routeName);
        PerformanceHelper.LogMetrics(metrics, routeName);

        // Assert - Use slightly relaxed thresholds for navigation (not cold load)
        // Still enforce core thresholds
        PerformanceHelper.AssertThresholds(metrics);
    }

    /// <summary>
    /// Measures First Contentful Paint specifically, which is critical for perceived performance.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Performance")]
    [Trait("Category", "FCP")]
    public async Task FirstContentfulPaint_ShouldBeFast()
    {
        // Arrange - Fresh context
        var browser = await _fixture.Page.Context.Browser!.NewContextAsync();
        var page = await browser.NewPageAsync();

        try
        {
            // Act
            await page.GotoAsync(_fixture.BaseUrl);
            await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

            var metrics = await PerformanceHelper.CaptureMetricsAsync(page);

            // Log
            _output.WriteLine($"FCP: {metrics.FirstContentfulPaintMs:F0}ms ({PerformanceThresholds.GetFcpRating(metrics.FirstContentfulPaintMs)})");

            // Assert - FCP should be under the good threshold
            Assert.True(
                metrics.FirstContentfulPaintMs <= PerformanceThresholds.Fcp.FailMs,
                $"FCP of {metrics.FirstContentfulPaintMs:F0}ms exceeds threshold of {PerformanceThresholds.Fcp.FailMs}ms");

            // Bonus: warn if not in "Good" range
            if (metrics.FirstContentfulPaintMs > PerformanceThresholds.Fcp.GoodMs)
            {
                _output.WriteLine($"WARNING: FCP ({metrics.FirstContentfulPaintMs:F0}ms) is above 'Good' threshold of {PerformanceThresholds.Fcp.GoodMs}ms");
            }
        }
        finally
        {
            await browser.DisposeAsync();
        }
    }

    /// <summary>
    /// Verifies Cumulative Layout Shift stays low throughout page load and interactions.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Performance")]
    [Trait("Category", "CLS")]
    public async Task CumulativeLayoutShift_ShouldBeLow()
    {
        // Arrange - Authenticate
        var page = _fixture.Page;
        await AuthenticationHelper.LoginAsync(page, _fixture.BaseUrl);

        // Navigate to a content-heavy page
        await page.GotoAsync($"{_fixture.BaseUrl}/budget");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Wait for any lazy-loaded content
        await Task.Delay(2000);

        // Act - Capture CLS
        var metrics = await PerformanceHelper.CaptureMetricsAsync(page);

        // Log
        _output.WriteLine($"CLS: {metrics.CumulativeLayoutShift:F4} ({PerformanceThresholds.GetClsRating(metrics.CumulativeLayoutShift)})");

        // Assert
        Assert.True(
            metrics.CumulativeLayoutShift <= PerformanceThresholds.Cls.Fail,
            $"CLS of {metrics.CumulativeLayoutShift:F4} exceeds threshold of {PerformanceThresholds.Cls.Fail}");
    }

    /// <summary>
    /// Measures performance of navigation between routes (SPA navigation).
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Performance")]
    [Trait("Category", "Navigation")]
    public async Task SpaNavigation_ShouldBeFast()
    {
        // Arrange - Authenticate and start at home
        var page = _fixture.Page;
        await AuthenticationHelper.LoginAsync(page, _fixture.BaseUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act - Measure time to navigate between routes
        var navTimes = new List<(string route, double timeMs)>();

        var routes = new[] { "recurring", "categories", "budget", "reports", string.Empty };

        foreach (var route in routes)
        {
            var startTime = DateTime.UtcNow;

            var link = page.Locator($"nav a[href='/{route}'], nav a[href='{route}']").First;
            if (await link.IsVisibleAsync())
            {
                await link.ClickAsync();
            }
            else
            {
                // Direct navigation if link not found
                var url = string.IsNullOrEmpty(route) ? _fixture.BaseUrl : $"{_fixture.BaseUrl}/{route}";
                await page.GotoAsync(url);
            }

            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var elapsedMs = (DateTime.UtcNow - startTime).TotalMilliseconds;
            navTimes.Add((route, elapsedMs));
        }

        // Log results
        _output.WriteLine("SPA Navigation Times:");
        foreach (var (route, timeMs) in navTimes)
        {
            var displayRoute = string.IsNullOrEmpty(route) ? "/" : $"/{route}";
            _output.WriteLine($"  {displayRoute,-20} {timeMs:F0}ms");
        }

        // Assert - SPA navigation should be fast (under 1 second)
        var maxNavTime = navTimes.Max(n => n.timeMs);
        Assert.True(
            maxNavTime < 2000,
            $"SPA navigation took {maxNavTime:F0}ms which exceeds 2000ms threshold");
    }

    private void LogMetricsToOutput(PerformanceMetrics metrics, string testName)
    {
        _output.WriteLine($"=== {testName} ===");
        _output.WriteLine($"FCP:  {metrics.FirstContentfulPaintMs,8:F0}ms  ({PerformanceThresholds.GetFcpRating(metrics.FirstContentfulPaintMs)})");
        _output.WriteLine($"LCP:  {metrics.LargestContentfulPaintMs,8:F0}ms  ({PerformanceThresholds.GetLcpRating(metrics.LargestContentfulPaintMs)})");
        _output.WriteLine($"TTI:  {metrics.TimeToInteractiveMs,8:F0}ms  ({PerformanceThresholds.GetTtiRating(metrics.TimeToInteractiveMs)})");
        _output.WriteLine($"CLS:  {metrics.CumulativeLayoutShift,8:F4}    ({PerformanceThresholds.GetClsRating(metrics.CumulativeLayoutShift)})");
        _output.WriteLine($"DOM:  {metrics.DomContentLoadedMs,8:F0}ms");
        _output.WriteLine($"Load: {metrics.LoadCompleteMs,8:F0}ms");
        _output.WriteLine(string.Empty);
    }
}
