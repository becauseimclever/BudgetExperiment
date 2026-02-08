// <copyright file="MobileAiChatTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.E2E.Tests.Fixtures;
using BudgetExperiment.E2E.Tests.Helpers;

namespace BudgetExperiment.E2E.Tests.Tests;

/// <summary>
/// E2E tests for the AI Assistant mobile chat experience on mobile viewport.
/// </summary>
[Collection("MobilePlaywright")]
public class MobileAiChatTests
{
    private readonly MobilePlaywrightFixture fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="MobileAiChatTests"/> class.
    /// </summary>
    /// <param name="fixture">The mobile Playwright fixture.</param>
    public MobileAiChatTests(MobilePlaywrightFixture fixture)
    {
        this.fixture = fixture;
    }

    /// <summary>
    /// Verifies the AI chat opens as a BottomSheet dialog on mobile.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Mobile")]
    [Trait("Category", "DemoSafe")]
    public async Task AiChat_ShouldOpenAsBottomSheet_OnMobile()
    {
        // Arrange
        var page = fixture.Page;
        await AuthenticationHelper.LoginAsync(page, fixture.BaseUrl);
        await page.GotoAsync(fixture.BaseUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act - open AI chat via FAB
        await page.Locator(".fab-primary").ClickAsync();
        await page.Locator(".fab-ai").ClickAsync();

        // Assert - should open as a dialog (BottomSheet)
        var dialog = page.GetByRole(AriaRole.Dialog);
        await Expect(dialog).ToBeVisibleAsync(new() { Timeout = 5000 });
    }

    /// <summary>
    /// Verifies the mobile chat shows a welcome screen with example messages.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Mobile")]
    [Trait("Category", "DemoSafe")]
    public async Task AiChat_ShouldShowWelcomeScreen()
    {
        // Arrange
        var page = fixture.Page;
        await AuthenticationHelper.LoginAsync(page, fixture.BaseUrl);
        await page.GotoAsync(fixture.BaseUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act - open AI chat
        await page.Locator(".fab-primary").ClickAsync();
        await page.Locator(".fab-ai").ClickAsync();
        await Expect(page.GetByRole(AriaRole.Dialog)).ToBeVisibleAsync(new() { Timeout = 5000 });

        // Assert - welcome screen elements should be present
        var welcomeHeading = page.GetByText("Budget Assistant");
        await Expect(welcomeHeading).ToBeVisibleAsync(new() { Timeout = 5000 });

        // Example messages should be visible
        var examples = page.Locator(".mobile-chat-examples li");
        var count = await examples.CountAsync();
        Assert.True(count >= 2, $"Expected at least 2 example messages, got {count}");
    }

    /// <summary>
    /// Verifies the chat input field is visible.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Mobile")]
    [Trait("Category", "DemoSafe")]
    public async Task AiChat_ShouldShowInputField()
    {
        // Arrange
        var page = fixture.Page;
        await AuthenticationHelper.LoginAsync(page, fixture.BaseUrl);
        await page.GotoAsync(fixture.BaseUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act - open AI chat
        await page.Locator(".fab-primary").ClickAsync();
        await page.Locator(".fab-ai").ClickAsync();
        await Expect(page.GetByRole(AriaRole.Dialog)).ToBeVisibleAsync(new() { Timeout = 5000 });

        // Assert
        var chatInput = page.GetByPlaceholder("Type a message...");
        await Expect(chatInput).ToBeVisibleAsync(new() { Timeout = 5000 });
    }

    /// <summary>
    /// Verifies the Expand button is present in the chat footer.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Mobile")]
    [Trait("Category", "DemoSafe")]
    public async Task AiChat_ShouldShowExpandButton()
    {
        // Arrange
        var page = fixture.Page;
        await AuthenticationHelper.LoginAsync(page, fixture.BaseUrl);
        await page.GotoAsync(fixture.BaseUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act - open AI chat
        await page.Locator(".fab-primary").ClickAsync();
        await page.Locator(".fab-ai").ClickAsync();
        await Expect(page.GetByRole(AriaRole.Dialog)).ToBeVisibleAsync(new() { Timeout = 5000 });

        // Assert
        var expandButton = page.GetByTitle("Expand chat");
        await Expect(expandButton).ToBeVisibleAsync(new() { Timeout = 5000 });
    }

    /// <summary>
    /// Verifies the New Chat button is present.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Mobile")]
    [Trait("Category", "DemoSafe")]
    public async Task AiChat_ShouldShowNewChatButton()
    {
        // Arrange
        var page = fixture.Page;
        await AuthenticationHelper.LoginAsync(page, fixture.BaseUrl);
        await page.GotoAsync(fixture.BaseUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act - open AI chat
        await page.Locator(".fab-primary").ClickAsync();
        await page.Locator(".fab-ai").ClickAsync();
        await Expect(page.GetByRole(AriaRole.Dialog)).ToBeVisibleAsync(new() { Timeout = 5000 });

        // Assert
        var newChatButton = page.GetByTitle("New conversation");
        await Expect(newChatButton).ToBeVisibleAsync(new() { Timeout = 5000 });
    }

    /// <summary>
    /// Verifies closing the AI chat BottomSheet returns to normal view.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Mobile")]
    [Trait("Category", "DemoSafe")]
    public async Task AiChat_CloseButton_ShouldDismissSheet()
    {
        // Arrange
        var page = fixture.Page;
        await AuthenticationHelper.LoginAsync(page, fixture.BaseUrl);
        await page.GotoAsync(fixture.BaseUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act - open AI chat then close
        await page.Locator(".fab-primary").ClickAsync();
        await page.Locator(".fab-ai").ClickAsync();
        await Expect(page.GetByRole(AriaRole.Dialog)).ToBeVisibleAsync(new() { Timeout = 5000 });

        var closeButton = page.Locator(".bottom-sheet__close");
        await closeButton.ClickAsync();

        // Assert - dialog should disappear
        await Expect(page.GetByRole(AriaRole.Dialog)).Not.ToBeVisibleAsync(new() { Timeout = 5000 });

        // FAB should reappear
        var fab = page.Locator(".fab-primary");
        await Expect(fab).ToBeVisibleAsync(new() { Timeout = 3000 });
    }

    /// <summary>
    /// Verifies the BottomSheet has aria-modal set for accessibility.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Mobile")]
    [Trait("Category", "Accessibility")]
    public async Task AiChat_BottomSheet_ShouldBeAriaModal()
    {
        // Arrange
        var page = fixture.Page;
        await AuthenticationHelper.LoginAsync(page, fixture.BaseUrl);
        await page.GotoAsync(fixture.BaseUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act - open AI chat
        await page.Locator(".fab-primary").ClickAsync();
        await page.Locator(".fab-ai").ClickAsync();

        // Assert
        var dialog = page.GetByRole(AriaRole.Dialog);
        await Expect(dialog).ToBeVisibleAsync(new() { Timeout = 5000 });
        await Expect(dialog).ToHaveAttributeAsync("aria-modal", "true");
    }
}
