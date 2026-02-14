// <copyright file="ModalTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using Bunit;

using BudgetExperiment.Client.Components.Common;
using BudgetExperiment.Client.Services;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace BudgetExperiment.Client.Tests.Components.Common;

/// <summary>
/// Unit tests for the Modal component.
/// </summary>
public class ModalTests : BunitContext, IAsyncLifetime
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ModalTests"/> class.
    /// Sets up JSInterop mocks and required services for the component.
    /// </summary>
    public ModalTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<ThemeService>();
    }

    /// <inheritdoc/>
    public Task InitializeAsync() => Task.CompletedTask;

    /// <inheritdoc/>
    public new Task DisposeAsync() => base.DisposeAsync().AsTask();

    /// <summary>
    /// Verifies that the modal does not render when IsVisible is false.
    /// </summary>
    [Fact]
    public void Modal_DoesNotRender_WhenNotVisible()
    {
        // Arrange & Act
        var cut = Render<Modal>(parameters => parameters
            .Add(p => p.IsVisible, false)
            .Add(p => p.Title, "Test")
            .AddChildContent("<p>Content</p>"));

        // Assert
        Assert.Empty(cut.Markup.Trim());
    }

    /// <summary>
    /// Verifies that the modal renders with backdrop and content when visible.
    /// </summary>
    [Fact]
    public void Modal_Renders_WhenVisible()
    {
        // Arrange & Act
        var cut = Render<Modal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Title, "Test Title")
            .AddChildContent("<p>Body text</p>"));

        // Assert
        Assert.Contains("modal-backdrop", cut.Markup);
        Assert.Contains("modal-dialog", cut.Markup);
        Assert.Contains("Test Title", cut.Markup);
        Assert.Contains("Body text", cut.Markup);
    }

    /// <summary>
    /// Verifies that the modal focuses the dialog element once when it first opens.
    /// </summary>
    [Fact]
    public void Modal_FocusesDialog_OnFirstOpen()
    {
        // Arrange & Act
        var cut = Render<Modal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Title, "Focus Test")
            .AddChildContent("<p>Content</p>"));

        // Assert – the eval JS call that focuses the modal should have been invoked exactly once
        var focusInvocations = JSInterop.Invocations
            .Where(i => i.Identifier == "eval"
                && i.Arguments.Any(a => a?.ToString()?.Contains("modal-dialog") == true))
            .ToList();
        Assert.Single(focusInvocations);
    }

    /// <summary>
    /// Regression test: verifies that re-rendering the modal while it remains open
    /// does NOT steal focus again (the bug that prevented keyboard input).
    /// </summary>
    [Fact]
    public void Modal_DoesNotRestealFocus_OnSubsequentRenders()
    {
        // Arrange – render the modal in the visible state
        var cut = Render<Modal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Title, "Input Test")
            .AddChildContent("<input type=\"text\" />"));

        // Record how many focus calls happened on initial render
        int initialFocusCount = JSInterop.Invocations
            .Count(i => i.Identifier == "eval"
                && i.Arguments.Any(a => a?.ToString()?.Contains("modal-dialog") == true));
        Assert.Equal(1, initialFocusCount);

        // Act – trigger a re-render while the modal stays visible (simulates typing causing re-render)
        cut.Render(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Title, "Input Test")
            .AddChildContent("<input type=\"text\" value=\"typed\" />"));

        // Assert – focus should NOT have been called again
        int totalFocusCount = JSInterop.Invocations
            .Count(i => i.Identifier == "eval"
                && i.Arguments.Any(a => a?.ToString()?.Contains("modal-dialog") == true));
        Assert.Equal(1, totalFocusCount);
    }

    /// <summary>
    /// Verifies that closing and reopening the modal focuses the dialog again.
    /// </summary>
    [Fact]
    public void Modal_FocusesDialog_WhenReopened()
    {
        // Arrange – open the modal
        var cut = Render<Modal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Title, "Reopen Test")
            .AddChildContent("<p>Content</p>"));

        // Act – close then reopen the modal
        cut.Render(parameters => parameters
            .Add(p => p.IsVisible, false)
            .Add(p => p.Title, "Reopen Test")
            .AddChildContent("<p>Content</p>"));

        cut.Render(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Title, "Reopen Test")
            .AddChildContent("<p>Content</p>"));

        // Assert – focus should have been called exactly twice (once per open)
        int totalFocusCount = JSInterop.Invocations
            .Count(i => i.Identifier == "eval"
                && i.Arguments.Any(a => a?.ToString()?.Contains("modal-dialog") == true));
        Assert.Equal(2, totalFocusCount);
    }

    /// <summary>
    /// Verifies that pressing Escape triggers the OnClose callback.
    /// </summary>
    [Fact]
    public void Modal_ClosesOnEscape()
    {
        // Arrange
        var closed = false;
        var cut = Render<Modal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Title, "Escape Test")
            .Add(p => p.OnClose, () => { closed = true; })
            .AddChildContent("<p>Content</p>"));

        // Act – press Escape on the backdrop
        cut.Find(".modal-backdrop").KeyDown(key: "Escape");

        // Assert
        Assert.True(closed);
    }

    /// <summary>
    /// Verifies that clicking the overlay closes the modal when CloseOnOverlayClick is true.
    /// </summary>
    [Fact]
    public void Modal_ClosesOnOverlayClick_WhenEnabled()
    {
        // Arrange
        var closed = false;
        var cut = Render<Modal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Title, "Overlay Test")
            .Add(p => p.CloseOnOverlayClick, true)
            .Add(p => p.OnClose, () => { closed = true; })
            .AddChildContent("<p>Content</p>"));

        // Act
        cut.Find(".modal-backdrop").Click();

        // Assert
        Assert.True(closed);
    }

    /// <summary>
    /// Verifies that clicking the overlay does not close the modal when CloseOnOverlayClick is false.
    /// </summary>
    [Fact]
    public void Modal_DoesNotCloseOnOverlayClick_WhenDisabled()
    {
        // Arrange
        var closed = false;
        var cut = Render<Modal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Title, "Overlay Test")
            .Add(p => p.CloseOnOverlayClick, false)
            .Add(p => p.OnClose, () => { closed = true; })
            .AddChildContent("<p>Content</p>"));

        // Act
        cut.Find(".modal-backdrop").Click();

        // Assert
        Assert.False(closed);
    }

    /// <summary>
    /// Verifies that the modal renders with the correct size class.
    /// </summary>
    [Theory]
    [InlineData(ModalSize.Small, "modal-dialog-sm")]
    [InlineData(ModalSize.Large, "modal-dialog-lg")]
    public void Modal_RendersWithCorrectSizeClass(ModalSize size, string expectedClass)
    {
        // Arrange & Act
        var cut = Render<Modal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Title, "Size Test")
            .Add(p => p.Size, size)
            .AddChildContent("<p>Content</p>"));

        // Assert
        var dialog = cut.Find(".modal-dialog");
        Assert.Contains(expectedClass, dialog.ClassList);
    }

    /// <summary>
    /// Verifies that the modal dialog has correct accessibility attributes.
    /// </summary>
    [Fact]
    public void Modal_HasCorrectAccessibilityAttributes()
    {
        // Arrange & Act
        var cut = Render<Modal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Title, "A11y Test")
            .AddChildContent("<p>Content</p>"));

        // Assert
        var dialog = cut.Find(".modal-dialog");
        Assert.Equal("dialog", dialog.GetAttribute("role"));
        Assert.Equal("true", dialog.GetAttribute("aria-modal"));
        Assert.Equal("-1", dialog.GetAttribute("tabindex"));

        var titleId = dialog.GetAttribute("aria-labelledby");
        Assert.NotNull(titleId);

        var title = cut.Find($"#{titleId}");
        Assert.Equal("A11y Test", title.TextContent);
    }
}
