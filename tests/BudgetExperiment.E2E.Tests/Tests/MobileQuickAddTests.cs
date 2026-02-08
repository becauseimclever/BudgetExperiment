// <copyright file="MobileQuickAddTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.E2E.Tests.Fixtures;
using BudgetExperiment.E2E.Tests.Helpers;

namespace BudgetExperiment.E2E.Tests.Tests;

/// <summary>
/// E2E tests for the Quick Add transaction flow on mobile viewport.
/// </summary>
[Collection("MobilePlaywright")]
public class MobileQuickAddTests
{
    private readonly MobilePlaywrightFixture fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="MobileQuickAddTests"/> class.
    /// </summary>
    /// <param name="fixture">The mobile Playwright fixture.</param>
    public MobileQuickAddTests(MobilePlaywrightFixture fixture)
    {
        this.fixture = fixture;
    }

    /// <summary>
    /// Verifies the Quick Add form renders inside a BottomSheet on mobile.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Mobile")]
    [Trait("Category", "DemoSafe")]
    public async Task QuickAdd_ShouldOpenInBottomSheet_OnMobile()
    {
        // Arrange
        var page = fixture.Page;
        await AuthenticationHelper.LoginAsync(page, fixture.BaseUrl);
        await page.GotoAsync(fixture.BaseUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act - open Quick Add via FAB
        await page.Locator(".fab-primary").ClickAsync();
        await page.Locator("#fab-quickadd-label").ClickAsync();

        // Assert - BottomSheet should contain the Quick Add form
        var bottomSheet = page.Locator(".bottom-sheet.is-visible");
        await Expect(bottomSheet).ToBeVisibleAsync(new() { Timeout = 5000 });

        var form = page.Locator(".quick-add-form");
        await Expect(form).ToBeVisibleAsync();
    }

    /// <summary>
    /// Verifies the Quick Add form has required fields visible.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Mobile")]
    [Trait("Category", "DemoSafe")]
    public async Task QuickAdd_ShouldShowRequiredFields()
    {
        // Arrange
        var page = fixture.Page;
        await AuthenticationHelper.LoginAsync(page, fixture.BaseUrl);
        await page.GotoAsync(fixture.BaseUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act - open Quick Add
        await page.Locator(".fab-primary").ClickAsync();
        await page.Locator("#fab-quickadd-label").ClickAsync();
        await Expect(page.Locator(".bottom-sheet.is-visible")).ToBeVisibleAsync(new() { Timeout = 5000 });

        // Assert - all required inputs should be visible
        await Expect(page.Locator("#qa-description")).ToBeVisibleAsync();
        await Expect(page.Locator("#qa-amount")).ToBeVisibleAsync();
        await Expect(page.Locator("#qa-account")).ToBeVisibleAsync();
        await Expect(page.Locator("#qa-date")).ToBeVisibleAsync();
    }

    /// <summary>
    /// Verifies the Quick Add date defaults to today.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Mobile")]
    [Trait("Category", "DemoSafe")]
    public async Task QuickAdd_DateShouldDefaultToToday()
    {
        // Arrange
        var page = fixture.Page;
        await AuthenticationHelper.LoginAsync(page, fixture.BaseUrl);
        await page.GotoAsync(fixture.BaseUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act - open Quick Add
        await page.Locator(".fab-primary").ClickAsync();
        await page.Locator("#fab-quickadd-label").ClickAsync();
        await Expect(page.Locator(".bottom-sheet.is-visible")).ToBeVisibleAsync(new() { Timeout = 5000 });

        // Assert - date should default to today
        var dateInput = page.Locator("#qa-date");
        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        await Expect(dateInput).ToHaveValueAsync(today);
    }

    /// <summary>
    /// Verifies Cancel button closes the Quick Add BottomSheet.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Mobile")]
    [Trait("Category", "DemoSafe")]
    public async Task QuickAdd_CancelButton_ShouldCloseBottomSheet()
    {
        // Arrange
        var page = fixture.Page;
        await AuthenticationHelper.LoginAsync(page, fixture.BaseUrl);
        await page.GotoAsync(fixture.BaseUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act - open Quick Add then cancel
        await page.Locator(".fab-primary").ClickAsync();
        await page.Locator("#fab-quickadd-label").ClickAsync();
        await Expect(page.Locator(".bottom-sheet.is-visible")).ToBeVisibleAsync(new() { Timeout = 5000 });

        await page.GetByRole(AriaRole.Button, new() { Name = "Cancel" }).ClickAsync();

        // Assert - BottomSheet should close
        await Expect(page.Locator(".bottom-sheet.is-visible")).Not.ToBeVisibleAsync(new() { Timeout = 3000 });
    }

    /// <summary>
    /// Verifies the description input uses appropriate mobile input mode.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Mobile")]
    [Trait("Category", "DemoSafe")]
    public async Task QuickAdd_DescriptionInput_ShouldHaveTextInputMode()
    {
        // Arrange
        var page = fixture.Page;
        await AuthenticationHelper.LoginAsync(page, fixture.BaseUrl);
        await page.GotoAsync(fixture.BaseUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act - open Quick Add
        await page.Locator(".fab-primary").ClickAsync();
        await page.Locator("#fab-quickadd-label").ClickAsync();
        await Expect(page.Locator(".bottom-sheet.is-visible")).ToBeVisibleAsync(new() { Timeout = 5000 });

        // Assert
        var descInput = page.Locator("#qa-description");
        await Expect(descInput).ToHaveAttributeAsync("inputmode", "text");
    }

    /// <summary>
    /// Verifies the amount input uses decimal input mode for numeric keyboard.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Mobile")]
    [Trait("Category", "DemoSafe")]
    public async Task QuickAdd_AmountInput_ShouldHaveDecimalInputMode()
    {
        // Arrange
        var page = fixture.Page;
        await AuthenticationHelper.LoginAsync(page, fixture.BaseUrl);
        await page.GotoAsync(fixture.BaseUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act - open Quick Add
        await page.Locator(".fab-primary").ClickAsync();
        await page.Locator("#fab-quickadd-label").ClickAsync();
        await Expect(page.Locator(".bottom-sheet.is-visible")).ToBeVisibleAsync(new() { Timeout = 5000 });

        // Assert
        var amountInput = page.Locator("#qa-amount");
        await Expect(amountInput).ToHaveAttributeAsync("inputmode", "decimal");
    }

    /// <summary>
    /// Verifies the Save and Cancel buttons are visible within the BottomSheet.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Mobile")]
    [Trait("Category", "DemoSafe")]
    public async Task QuickAdd_ActionButtons_ShouldBeVisible()
    {
        // Arrange
        var page = fixture.Page;
        await AuthenticationHelper.LoginAsync(page, fixture.BaseUrl);
        await page.GotoAsync(fixture.BaseUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act - open Quick Add
        await page.Locator(".fab-primary").ClickAsync();
        await page.Locator("#fab-quickadd-label").ClickAsync();
        await Expect(page.Locator(".bottom-sheet.is-visible")).ToBeVisibleAsync(new() { Timeout = 5000 });

        // Assert
        var saveButton = page.GetByRole(AriaRole.Button, new() { Name = "Save" });
        var cancelButton = page.GetByRole(AriaRole.Button, new() { Name = "Cancel" });
        await Expect(saveButton).ToBeVisibleAsync();
        await Expect(cancelButton).ToBeVisibleAsync();
    }
}
