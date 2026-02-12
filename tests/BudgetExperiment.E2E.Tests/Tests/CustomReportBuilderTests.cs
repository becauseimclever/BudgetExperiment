// <copyright file="CustomReportBuilderTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.E2E.Tests.Fixtures;
using BudgetExperiment.E2E.Tests.Helpers;

namespace BudgetExperiment.E2E.Tests.Tests;

/// <summary>
/// E2E tests for the custom report builder page.
/// </summary>
[Collection("Playwright")]
public class CustomReportBuilderTests
{
    private readonly PlaywrightFixture fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomReportBuilderTests"/> class.
    /// </summary>
    /// <param name="fixture">The Playwright fixture.</param>
    public CustomReportBuilderTests(PlaywrightFixture fixture)
    {
        this.fixture = fixture;
    }

    /// <summary>
    /// Verifies widgets can be dragged onto the canvas and configured.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Reports")]
    [Trait("Category", "CustomBuilder")]
    public async Task Builder_Allows_Widget_Drag_And_Select()
    {
        // Arrange
        var page = fixture.Page;
        await AuthenticationHelper.LoginAsync(page, fixture.BaseUrl);

        // Act
        await page.GotoAsync($"{fixture.BaseUrl}/reports/custom-builder");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var paletteItem = page.Locator(".widget-palette-item").First;
        var canvas = page.Locator(".report-canvas-grid");
        await paletteItem.DragToAsync(canvas);

        // Assert
        var widgets = page.Locator(".report-widget");
        await Expect(widgets).ToHaveCountAsync(1);

        await widgets.First.ClickAsync();
        var configTitle = page.Locator("#widget-title");
        await Expect(configTitle).ToBeVisibleAsync(new() { Timeout = 5000 });
    }

    /// <summary>
    /// Verifies layout changes can be saved and reloaded.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Reports")]
    [Trait("Category", "CustomBuilder")]
    public async Task Builder_Can_Save_And_Reload_Layout()
    {
        // Arrange
        var page = fixture.Page;
        await AuthenticationHelper.LoginAsync(page, fixture.BaseUrl);
        await page.GotoAsync($"{fixture.BaseUrl}/reports/custom-builder");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var layoutSelect = page.Locator("#layout-select");
        var options = await layoutSelect.Locator("option[value]:not([value=''])").AllAsync();
        if (options.Count == 0)
        {
            await page.GetByRole(AriaRole.Button, new() { Name = "New Layout" }).ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }

        var nameInput = page.Locator("#layout-name");
        var originalName = await nameInput.InputValueAsync();
        var updatedName = $"{originalName} (Updated)";

        // Act
        await nameInput.FillAsync(updatedName);
        await page.GetByRole(AriaRole.Button, new() { Name = "Save" }).ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var selectedValue = await layoutSelect.InputValueAsync();
        if (!string.IsNullOrWhiteSpace(selectedValue))
        {
            await layoutSelect.SelectOptionAsync(selectedValue);
        }

        // Assert
        Assert.Equal(updatedName, await nameInput.InputValueAsync());

        // Cleanup - restore original name
        await nameInput.FillAsync(originalName);
        await page.GetByRole(AriaRole.Button, new() { Name = "Save" }).ClickAsync();
    }
}
