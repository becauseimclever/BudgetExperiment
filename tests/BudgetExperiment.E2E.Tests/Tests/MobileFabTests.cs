// <copyright file="MobileFabTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.E2E.Tests.Fixtures;
using BudgetExperiment.E2E.Tests.Helpers;

namespace BudgetExperiment.E2E.Tests.Tests;

/// <summary>
/// E2E tests for the Mobile Floating Action Button on mobile viewport.
/// </summary>
[Collection("MobilePlaywright")]
public class MobileFabTests
{
    private readonly MobilePlaywrightFixture fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="MobileFabTests"/> class.
    /// </summary>
    /// <param name="fixture">The mobile Playwright fixture.</param>
    public MobileFabTests(MobilePlaywrightFixture fixture)
    {
        this.fixture = fixture;
    }

    /// <summary>
    /// Verifies the FAB is visible on mobile viewport.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Mobile")]
    [Trait("Category", "DemoSafe")]
    public async Task Fab_ShouldBeVisible_OnMobileViewport()
    {
        // Arrange
        var page = fixture.Page;
        await AuthenticationHelper.LoginAsync(page, fixture.BaseUrl);

        // Act
        await page.GotoAsync(fixture.BaseUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert
        var fab = page.Locator(".fab-primary");
        await Expect(fab).ToBeVisibleAsync();
    }

    /// <summary>
    /// Verifies the FAB has proper accessibility attributes.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Mobile")]
    [Trait("Category", "Accessibility")]
    public async Task Fab_ShouldHaveAccessibleName()
    {
        // Arrange
        var page = fixture.Page;
        await AuthenticationHelper.LoginAsync(page, fixture.BaseUrl);
        await page.GotoAsync(fixture.BaseUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act
        var fab = page.Locator(".fab-primary");

        // Assert - should have aria-label when collapsed
        await Expect(fab).ToHaveAttributeAsync("aria-label", "Open quick actions menu");
        await Expect(fab).ToHaveAttributeAsync("aria-expanded", "false");
    }

    /// <summary>
    /// Verifies the FAB expands to show Quick Add and AI buttons.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Mobile")]
    [Trait("Category", "DemoSafe")]
    public async Task Fab_ShouldExpandSpeedDial_WhenTapped()
    {
        // Arrange
        var page = fixture.Page;
        await AuthenticationHelper.LoginAsync(page, fixture.BaseUrl);
        await page.GotoAsync(fixture.BaseUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act
        var fab = page.Locator(".fab-primary");
        await fab.ClickAsync();

        // Assert - speed dial should be expanded
        await Expect(fab).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(fab).ToHaveAttributeAsync("aria-label", "Close menu");

        // Quick Add and AI buttons should be visible
        var quickAddLabel = page.Locator("#fab-quickadd-label");
        var aiLabel = page.Locator("#fab-ai-label");
        await Expect(quickAddLabel).ToBeVisibleAsync();
        await Expect(aiLabel).ToBeVisibleAsync();
    }

    /// <summary>
    /// Verifies the backdrop appears when FAB is expanded.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Mobile")]
    [Trait("Category", "DemoSafe")]
    public async Task Fab_ShouldShowBackdrop_WhenExpanded()
    {
        // Arrange
        var page = fixture.Page;
        await AuthenticationHelper.LoginAsync(page, fixture.BaseUrl);
        await page.GotoAsync(fixture.BaseUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act
        var fab = page.Locator(".fab-primary");
        await fab.ClickAsync();

        // Assert
        var backdrop = page.Locator(".fab-backdrop.is-visible");
        await Expect(backdrop).ToBeVisibleAsync();
    }

    /// <summary>
    /// Verifies tapping Quick Add in FAB opens the BottomSheet.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Mobile")]
    [Trait("Category", "DemoSafe")]
    public async Task Fab_QuickAddTap_ShouldOpenBottomSheet()
    {
        // Arrange
        var page = fixture.Page;
        await AuthenticationHelper.LoginAsync(page, fixture.BaseUrl);
        await page.GotoAsync(fixture.BaseUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act - expand FAB then tap Quick Add
        await page.Locator(".fab-primary").ClickAsync();
        await page.Locator("#fab-quickadd-label").ClickAsync();

        // Assert - BottomSheet with Quick Add form should appear
        var bottomSheet = page.Locator(".bottom-sheet.is-visible");
        await Expect(bottomSheet).ToBeVisibleAsync(new() { Timeout = 5000 });
    }

    /// <summary>
    /// Verifies tapping AI Assistant in FAB opens the mobile chat sheet.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Mobile")]
    [Trait("Category", "DemoSafe")]
    public async Task Fab_AiTap_ShouldOpenMobileChatSheet()
    {
        // Arrange
        var page = fixture.Page;
        await AuthenticationHelper.LoginAsync(page, fixture.BaseUrl);
        await page.GotoAsync(fixture.BaseUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act - expand FAB then tap AI Assistant
        await page.Locator(".fab-primary").ClickAsync();
        await page.Locator(".fab-ai").ClickAsync();

        // Assert - Mobile chat BottomSheet should appear
        var dialog = page.GetByRole(AriaRole.Dialog);
        await Expect(dialog).ToBeVisibleAsync(new() { Timeout = 5000 });
    }

    /// <summary>
    /// Verifies FAB is hidden when a BottomSheet is open.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Mobile")]
    [Trait("Category", "DemoSafe")]
    public async Task Fab_ShouldBeHidden_WhenBottomSheetIsOpen()
    {
        // Arrange
        var page = fixture.Page;
        await AuthenticationHelper.LoginAsync(page, fixture.BaseUrl);
        await page.GotoAsync(fixture.BaseUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act - open Quick Add
        await page.Locator(".fab-primary").ClickAsync();
        await page.Locator("#fab-quickadd-label").ClickAsync();

        // Wait for BottomSheet to appear
        await Expect(page.Locator(".bottom-sheet.is-visible")).ToBeVisibleAsync(new() { Timeout = 5000 });

        // Assert - FAB container should be hidden
        var fabContainer = page.Locator(".fab-container.is-hidden");
        await Expect(fabContainer).ToBeAttachedAsync();
    }
}
