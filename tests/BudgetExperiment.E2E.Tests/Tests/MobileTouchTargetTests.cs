// <copyright file="MobileTouchTargetTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.E2E.Tests.Fixtures;
using BudgetExperiment.E2E.Tests.Helpers;
using Xunit.Abstractions;

namespace BudgetExperiment.E2E.Tests.Tests;

/// <summary>
/// Tests to verify all touch targets meet WCAG 2.5.5 minimum size (44x44px).
/// </summary>
[Collection("MobilePlaywright")]
public class MobileTouchTargetTests
{
    /// <summary>
    /// Minimum touch target size per WCAG 2.5.5.
    /// </summary>
    private const int MinTouchTargetSize = 44;

    private readonly MobilePlaywrightFixture fixture;
    private readonly ITestOutputHelper output;

    /// <summary>
    /// Initializes a new instance of the <see cref="MobileTouchTargetTests"/> class.
    /// </summary>
    /// <param name="fixture">The mobile Playwright fixture.</param>
    /// <param name="output">The test output helper for logging.</param>
    public MobileTouchTargetTests(MobilePlaywrightFixture fixture, ITestOutputHelper output)
    {
        this.fixture = fixture;
        this.output = output;
    }

    /// <summary>
    /// Verifies the FAB primary button meets minimum touch target size.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Mobile")]
    [Trait("Category", "Accessibility")]
    public async Task FabPrimary_ShouldMeetMinimumTouchTargetSize()
    {
        // Arrange
        var page = fixture.Page;
        await AuthenticationHelper.LoginAsync(page, fixture.BaseUrl);
        await page.GotoAsync(fixture.BaseUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act
        var fab = page.Locator(".fab-primary");
        var box = await fab.BoundingBoxAsync();

        // Assert
        Assert.NotNull(box);
        output.WriteLine($"FAB primary size: {box!.Width}x{box.Height}");
        Assert.True(box.Width >= MinTouchTargetSize, $"FAB width {box.Width}px is less than {MinTouchTargetSize}px minimum");
        Assert.True(box.Height >= MinTouchTargetSize, $"FAB height {box.Height}px is less than {MinTouchTargetSize}px minimum");
    }

    /// <summary>
    /// Verifies the Quick Add speed dial button meets minimum touch target size.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Mobile")]
    [Trait("Category", "Accessibility")]
    public async Task FabQuickAdd_ShouldMeetMinimumTouchTargetSize()
    {
        // Arrange
        var page = fixture.Page;
        await AuthenticationHelper.LoginAsync(page, fixture.BaseUrl);
        await page.GotoAsync(fixture.BaseUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act - expand FAB
        await page.Locator(".fab-primary").ClickAsync();
        await Task.Delay(300); // Wait for animation

        // Check the entire FAB item (icon + label) touch area
        var quickAddItem = page.Locator(".fab-item").First;
        var box = await quickAddItem.BoundingBoxAsync();

        // Assert
        Assert.NotNull(box);
        output.WriteLine($"Quick Add FAB item size: {box!.Width}x{box.Height}");
        Assert.True(box.Height >= MinTouchTargetSize, $"Quick Add height {box.Height}px is less than {MinTouchTargetSize}px minimum");
    }

    /// <summary>
    /// Verifies the AI speed dial button meets minimum touch target size.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Mobile")]
    [Trait("Category", "Accessibility")]
    public async Task FabAiButton_ShouldMeetMinimumTouchTargetSize()
    {
        // Arrange
        var page = fixture.Page;
        await AuthenticationHelper.LoginAsync(page, fixture.BaseUrl);
        await page.GotoAsync(fixture.BaseUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act - expand FAB
        await page.Locator(".fab-primary").ClickAsync();
        await Task.Delay(300);

        var aiButton = page.Locator(".fab-ai");
        var box = await aiButton.BoundingBoxAsync();

        // Assert
        Assert.NotNull(box);
        output.WriteLine($"AI FAB button size: {box!.Width}x{box.Height}");
        Assert.True(box.Width >= MinTouchTargetSize, $"AI button width {box.Width}px is less than {MinTouchTargetSize}px minimum");
        Assert.True(box.Height >= MinTouchTargetSize, $"AI button height {box.Height}px is less than {MinTouchTargetSize}px minimum");
    }

    /// <summary>
    /// Verifies the view toggle buttons meet minimum touch target size.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Mobile")]
    [Trait("Category", "Accessibility")]
    public async Task ViewToggleButtons_ShouldMeetMinimumTouchTargetSize()
    {
        // Arrange
        var page = fixture.Page;
        await AuthenticationHelper.LoginAsync(page, fixture.BaseUrl);
        await page.GotoAsync(fixture.BaseUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act
        var monthButton = page.GetByLabel("Month view");
        var weekButton = page.GetByLabel("Week view");

        var monthBox = await monthButton.BoundingBoxAsync();
        var weekBox = await weekButton.BoundingBoxAsync();

        // Assert
        Assert.NotNull(monthBox);
        Assert.NotNull(weekBox);
        output.WriteLine($"Month button size: {monthBox!.Width}x{monthBox.Height}");
        output.WriteLine($"Week button size: {weekBox!.Width}x{weekBox.Height}");

        Assert.True(monthBox.Height >= MinTouchTargetSize, $"Month button height {monthBox.Height}px is less than {MinTouchTargetSize}px minimum");
        Assert.True(weekBox.Height >= MinTouchTargetSize, $"Week button height {weekBox.Height}px is less than {MinTouchTargetSize}px minimum");
    }

    /// <summary>
    /// Verifies the week view navigation buttons meet minimum touch target size.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Mobile")]
    [Trait("Category", "Accessibility")]
    public async Task WeekNavButtons_ShouldMeetMinimumTouchTargetSize()
    {
        // Arrange
        var page = fixture.Page;
        await AuthenticationHelper.LoginAsync(page, fixture.BaseUrl);
        await page.GotoAsync(fixture.BaseUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act - switch to week view
        await page.GetByLabel("Week view").ClickAsync();
        await Expect(page.Locator(".week-view")).ToBeVisibleAsync(new() { Timeout = 5000 });

        var prevButton = page.GetByLabel("Previous week");
        var nextButton = page.GetByLabel("Next week");

        var prevBox = await prevButton.BoundingBoxAsync();
        var nextBox = await nextButton.BoundingBoxAsync();

        // Assert
        Assert.NotNull(prevBox);
        Assert.NotNull(nextBox);
        output.WriteLine($"Previous week button size: {prevBox!.Width}x{prevBox.Height}");
        output.WriteLine($"Next week button size: {nextBox!.Width}x{nextBox.Height}");

        Assert.True(prevBox.Width >= MinTouchTargetSize, $"Previous button width {prevBox.Width}px is less than {MinTouchTargetSize}px minimum");
        Assert.True(prevBox.Height >= MinTouchTargetSize, $"Previous button height {prevBox.Height}px is less than {MinTouchTargetSize}px minimum");
        Assert.True(nextBox.Width >= MinTouchTargetSize, $"Next button width {nextBox.Width}px is less than {MinTouchTargetSize}px minimum");
        Assert.True(nextBox.Height >= MinTouchTargetSize, $"Next button height {nextBox.Height}px is less than {MinTouchTargetSize}px minimum");
    }

    /// <summary>
    /// Verifies the week view day cells meet minimum touch target size.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Mobile")]
    [Trait("Category", "Accessibility")]
    public async Task WeekDayCells_ShouldMeetMinimumTouchTargetSize()
    {
        // Arrange
        var page = fixture.Page;
        await AuthenticationHelper.LoginAsync(page, fixture.BaseUrl);
        await page.GotoAsync(fixture.BaseUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act - switch to week view
        await page.GetByLabel("Week view").ClickAsync();
        await Expect(page.Locator(".week-view")).ToBeVisibleAsync(new() { Timeout = 5000 });

        var dayCells = page.Locator(".week-day");
        var count = await dayCells.CountAsync();

        // Assert - each day cell should meet minimum size
        for (int i = 0; i < count; i++)
        {
            var cell = dayCells.Nth(i);
            var box = await cell.BoundingBoxAsync();

            Assert.NotNull(box);
            output.WriteLine($"Week day cell {i} size: {box!.Width}x{box.Height}");
            Assert.True(box.Width >= MinTouchTargetSize, $"Day cell {i} width {box.Width}px is less than {MinTouchTargetSize}px minimum");
            Assert.True(box.Height >= MinTouchTargetSize, $"Day cell {i} height {box.Height}px is less than {MinTouchTargetSize}px minimum");
        }
    }

    /// <summary>
    /// Verifies the BottomSheet close button meets minimum touch target size.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Mobile")]
    [Trait("Category", "Accessibility")]
    public async Task BottomSheetCloseButton_ShouldMeetMinimumTouchTargetSize()
    {
        // Arrange
        var page = fixture.Page;
        await AuthenticationHelper.LoginAsync(page, fixture.BaseUrl);
        await page.GotoAsync(fixture.BaseUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act - open Quick Add BottomSheet
        await page.Locator(".fab-primary").ClickAsync();
        await page.Locator("#fab-quickadd-label").ClickAsync();
        await Expect(page.Locator(".bottom-sheet.is-visible")).ToBeVisibleAsync(new() { Timeout = 5000 });

        var closeButton = page.Locator(".bottom-sheet__close");
        var box = await closeButton.BoundingBoxAsync();

        // Assert
        Assert.NotNull(box);
        output.WriteLine($"BottomSheet close button size: {box!.Width}x{box.Height}");
        Assert.True(box.Width >= MinTouchTargetSize, $"Close button width {box.Width}px is less than {MinTouchTargetSize}px minimum");
        Assert.True(box.Height >= MinTouchTargetSize, $"Close button height {box.Height}px is less than {MinTouchTargetSize}px minimum");
    }

    /// <summary>
    /// Verifies the month navigation buttons meet minimum touch target size.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Mobile")]
    [Trait("Category", "Accessibility")]
    public async Task MonthNavButtons_ShouldMeetMinimumTouchTargetSize()
    {
        // Arrange
        var page = fixture.Page;
        await AuthenticationHelper.LoginAsync(page, fixture.BaseUrl);
        await page.GotoAsync(fixture.BaseUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act
        var prevButton = page.GetByRole(AriaRole.Button, new() { Name = "< Previous" });
        var nextButton = page.GetByRole(AriaRole.Button, new() { Name = "Next >" });

        var prevBox = await prevButton.BoundingBoxAsync();
        var nextBox = await nextButton.BoundingBoxAsync();

        // Assert
        Assert.NotNull(prevBox);
        Assert.NotNull(nextBox);
        output.WriteLine($"Previous month button size: {prevBox!.Width}x{prevBox.Height}");
        output.WriteLine($"Next month button size: {nextBox!.Width}x{nextBox.Height}");

        Assert.True(prevBox.Height >= MinTouchTargetSize, $"Previous month button height {prevBox.Height}px is less than {MinTouchTargetSize}px minimum");
        Assert.True(nextBox.Height >= MinTouchTargetSize, $"Next month button height {nextBox.Height}px is less than {MinTouchTargetSize}px minimum");
    }
}
