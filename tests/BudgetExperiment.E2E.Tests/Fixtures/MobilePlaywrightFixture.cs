// <copyright file="MobilePlaywrightFixture.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.E2E.Tests.Fixtures;

/// <summary>
/// Playwright fixture configured with a mobile viewport (iPhone SE: 375x667).
/// Uses touch-enabled context to simulate real mobile interactions.
/// </summary>
public class MobilePlaywrightFixture : IAsyncLifetime
{
    /// <summary>
    /// iPhone SE viewport width.
    /// </summary>
    public const int MobileWidth = 375;

    /// <summary>
    /// iPhone SE viewport height.
    /// </summary>
    public const int MobileHeight = 667;

    /// <summary>
    /// iPhone SE landscape width.
    /// </summary>
    public const int LandscapeWidth = 667;

    /// <summary>
    /// iPhone SE landscape height.
    /// </summary>
    public const int LandscapeHeight = 375;

    private IPlaywright? playwright;
    private IBrowser? browser;

    /// <summary>
    /// Gets the browser context configured with mobile viewport and touch.
    /// </summary>
    public IBrowserContext Context { get; private set; } = null!;

    /// <summary>
    /// Gets the page for the current test.
    /// </summary>
    public IPage Page { get; private set; } = null!;

    /// <summary>
    /// Gets the base URL for the application under test.
    /// </summary>
    public string BaseUrl { get; } = Environment.GetEnvironmentVariable("BUDGET_APP_URL")
        ?? "https://budgetdemo.becauseimclever.com";

    /// <summary>
    /// Gets a value indicating whether the browser should run in headed mode.
    /// </summary>
    public bool Headed { get; } = Environment.GetEnvironmentVariable("HEADED")
        ?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false;

    /// <summary>
    /// Gets the slow motion delay in milliseconds for debugging.
    /// </summary>
    public float SlowMo { get; } = float.TryParse(
        Environment.GetEnvironmentVariable("SLOWMO"),
        out var slowMo) ? slowMo : 0;

    /// <summary>
    /// Gets a value indicating whether to capture traces for debugging.
    /// </summary>
    public bool CaptureTraces { get; } = Environment.GetEnvironmentVariable("PLAYWRIGHT_TRACES")
        ?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false;

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        playwright = await Playwright.CreateAsync();
        browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = !Headed,
            SlowMo = SlowMo,
        });

        Context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = MobileWidth, Height = MobileHeight },
            HasTouch = true,
            IsMobile = true,
            UserAgent = "Mozilla/5.0 (iPhone; CPU iPhone OS 16_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/16.0 Mobile/15E148 Safari/604.1",
        });

        if (CaptureTraces)
        {
            await Context.Tracing.StartAsync(new TracingStartOptions
            {
                Screenshots = true,
                Snapshots = true,
                Sources = true,
            });
        }

        Page = await Context.NewPageAsync();

        // Verify server is reachable
        await ValidateServerAsync();
    }

    /// <summary>
    /// Creates a new page with the specified viewport size, useful for orientation change tests.
    /// </summary>
    /// <param name="width">Viewport width in pixels.</param>
    /// <param name="height">Viewport height in pixels.</param>
    /// <returns>A new page with the updated viewport.</returns>
    public async Task<IPage> CreatePageWithViewportAsync(int width, int height)
    {
        var context = await browser!.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = width, Height = height },
            HasTouch = true,
            IsMobile = true,
            UserAgent = "Mozilla/5.0 (iPhone; CPU iPhone OS 16_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/16.0 Mobile/15E148 Safari/604.1",
        });

        return await context.NewPageAsync();
    }

    /// <inheritdoc/>
    public async Task DisposeAsync()
    {
        if (CaptureTraces)
        {
            var tracePath = Path.Combine("playwright-traces", $"trace-mobile-{DateTime.UtcNow:yyyyMMdd-HHmmss}.zip");
            Directory.CreateDirectory("playwright-traces");
            await Context.Tracing.StopAsync(new TracingStopOptions { Path = tracePath });
        }

        await Context.DisposeAsync();
        await browser!.DisposeAsync();
        playwright!.Dispose();
    }

    private async Task ValidateServerAsync()
    {
        var response = await Page.GotoAsync(BaseUrl);
        if (response?.Status >= 400)
        {
            throw new InvalidOperationException($"Server at {BaseUrl} returned {response.Status}");
        }
    }
}
