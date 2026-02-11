// <copyright file="MobileAiChatTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Text.RegularExpressions;

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

    /// <summary>
    /// Verifies the chat welcome screen shows calendar context after selecting a date.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Mobile")]
    [Trait("Category", "DemoSafe")]
    public async Task AiChat_ShouldShowCalendarContextHint_AfterDateSelection()
    {
        // Arrange
        var page = fixture.Page;
        await AuthenticationHelper.LoginAsync(page, fixture.BaseUrl);
        await page.GotoAsync(fixture.BaseUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act - select today's date on the calendar
        var todayButton = page.GetByRole(AriaRole.Button, new()
        {
            NameRegex = new Regex("Today", RegexOptions.IgnoreCase),
        });
        await Expect(todayButton).ToBeVisibleAsync(new() { Timeout = 5000 });
        await todayButton.ClickAsync();

        // Open AI chat
        await page.Locator(".fab-primary").ClickAsync();
        await page.Locator(".fab-ai").ClickAsync();
        await Expect(page.GetByRole(AriaRole.Dialog)).ToBeVisibleAsync(new() { Timeout = 5000 });

        // Start a new session to ensure welcome screen shows context hint
        var newChatButton = page.GetByTitle("New conversation");
        await newChatButton.ClickAsync();

        // Assert - context hint should include selected date
        var contextHint = page.Locator(".mobile-chat-context");
        await Expect(contextHint).ToBeVisibleAsync(new() { Timeout = 5000 });
        var hintText = await contextHint.TextContentAsync();
        Assert.NotNull(hintText);
        Assert.Contains("Selected:", hintText, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies the context hint updates when navigating to a different month.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Mobile")]
    [Trait("Category", "DemoSafe")]
    public async Task AiChat_ContextHint_ShouldUpdate_WhenMonthChanges()
    {
        // Arrange
        var page = fixture.Page;
        await AuthenticationHelper.LoginAsync(page, fixture.BaseUrl);
        await page.GotoAsync(fixture.BaseUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var header = page.Locator("h2.text-secondary");
        await Expect(header).ToBeVisibleAsync(new() { Timeout = 5000 });
        var initialMonth = await header.TextContentAsync();

        // Act - move to next month
        await page.GetByRole(AriaRole.Button, new() { Name = "Next >" }).ClickAsync();
        await Expect(header).Not.ToHaveTextAsync(initialMonth ?? string.Empty);
        var updatedMonth = await header.TextContentAsync();

        // Open AI chat
        await page.Locator(".fab-primary").ClickAsync();
        await page.Locator(".fab-ai").ClickAsync();
        await Expect(page.GetByRole(AriaRole.Dialog)).ToBeVisibleAsync(new() { Timeout = 5000 });

        // Start a new session to ensure welcome screen shows context hint
        var newChatButton = page.GetByTitle("New conversation");
        await newChatButton.ClickAsync();

        // Assert - context hint should reflect the new month
        var contextHint = page.Locator(".mobile-chat-context");
        await Expect(contextHint).ToBeVisibleAsync(new() { Timeout = 5000 });
        var hintText = await contextHint.TextContentAsync();
        Assert.NotNull(hintText);
        Assert.Contains("Viewing", hintText, StringComparison.Ordinal);
        if (!string.IsNullOrWhiteSpace(updatedMonth))
        {
            Assert.Contains(updatedMonth.Trim(), hintText, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// Verifies the context hint does not show a selected date when none is chosen.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Mobile")]
    [Trait("Category", "DemoSafe")]
    public async Task AiChat_ContextHint_ShouldNotShowSelectedDate_WhenNoneSelected()
    {
        // Arrange
        var page = fixture.Page;
        await AuthenticationHelper.LoginAsync(page, fixture.BaseUrl);
        await page.GotoAsync(fixture.BaseUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act - open AI chat without selecting a date
        await page.Locator(".fab-primary").ClickAsync();
        await page.Locator(".fab-ai").ClickAsync();
        await Expect(page.GetByRole(AriaRole.Dialog)).ToBeVisibleAsync(new() { Timeout = 5000 });

        // Start a new session to ensure welcome screen shows context hint
        var newChatButton = page.GetByTitle("New conversation");
        await newChatButton.ClickAsync();

        // Assert - hint should not include a selected date label
        var contextHint = page.Locator(".mobile-chat-context");
        await Expect(contextHint).ToBeVisibleAsync(new() { Timeout = 5000 });
        var hintText = await contextHint.TextContentAsync();
        Assert.NotNull(hintText);
        Assert.DoesNotContain("Selected:", hintText, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies context is cleared when navigating away from the calendar.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [Trait("Category", "Mobile")]
    [Trait("Category", "DemoSafe")]
    public async Task AiChat_ContextHint_ShouldClear_WhenLeavingCalendar()
    {
        // Arrange
        var page = fixture.Page;
        await AuthenticationHelper.LoginAsync(page, fixture.BaseUrl);
        await page.GotoAsync(fixture.BaseUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Select today's date
        var todayButton = page.GetByRole(AriaRole.Button, new()
        {
            NameRegex = new Regex("Today", RegexOptions.IgnoreCase),
        });
        await Expect(todayButton).ToBeVisibleAsync(new() { Timeout = 5000 });
        await todayButton.ClickAsync();

        // Navigate away to Settings
        await page.GetByTitle("Settings").ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Open AI chat
        await page.Locator(".fab-primary").ClickAsync();
        await page.Locator(".fab-ai").ClickAsync();
        await Expect(page.GetByRole(AriaRole.Dialog)).ToBeVisibleAsync(new() { Timeout = 5000 });

        // Start a new session to ensure welcome screen shows context hint if present
        var newChatButton = page.GetByTitle("New conversation");
        await newChatButton.ClickAsync();

        // Assert - context hint should not render
        var contextHint = page.Locator(".mobile-chat-context");
        await Expect(contextHint).Not.ToBeVisibleAsync(new() { Timeout = 5000 });
    }
}
