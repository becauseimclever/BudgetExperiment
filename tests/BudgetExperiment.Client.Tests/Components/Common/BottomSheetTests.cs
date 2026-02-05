// <copyright file="BottomSheetTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using Bunit;

using BudgetExperiment.Client.Components.Common;
using BudgetExperiment.Client.Services;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace BudgetExperiment.Client.Tests.Components.Common;

/// <summary>
/// Unit tests for the BottomSheet component.
/// </summary>
public class BottomSheetTests : BunitContext, IAsyncLifetime
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BottomSheetTests"/> class.
    /// Sets up JSInterop mocks for the component.
    /// </summary>
    public BottomSheetTests()
    {
        // Setup JS interop mocks for bottom-sheet.js module and ThemeService
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<ThemeService>();
    }

    /// <inheritdoc/>
    public Task InitializeAsync() => Task.CompletedTask;

    /// <inheritdoc/>
    public new Task DisposeAsync() => base.DisposeAsync().AsTask();

    /// <summary>
    /// Verifies that the bottom sheet does not render when IsVisible is false.
    /// </summary>
    [Fact]
    public void BottomSheet_DoesNotRender_WhenNotVisible()
    {
        // Arrange & Act
        var cut = Render<BottomSheet>(parameters => parameters
            .Add(p => p.IsVisible, false)
            .AddChildContent("<p>Content</p>"));

        // Assert
        Assert.Empty(cut.Markup.Trim());
    }

    /// <summary>
    /// Verifies that the bottom sheet renders when IsVisible is true.
    /// </summary>
    [Fact]
    public void BottomSheet_Renders_WhenVisible()
    {
        // Arrange & Act
        var cut = Render<BottomSheet>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .AddChildContent("<p>Test content</p>"));

        // Assert
        Assert.Contains("bottom-sheet-backdrop", cut.Markup);
        Assert.Contains("bottom-sheet", cut.Markup);
        Assert.Contains("Test content", cut.Markup);
    }

    /// <summary>
    /// Verifies that the bottom sheet renders at small height.
    /// </summary>
    [Fact]
    public void BottomSheet_RendersWithSmallHeight()
    {
        // Arrange & Act
        var cut = Render<BottomSheet>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Height, BottomSheetHeight.Small)
            .AddChildContent("<p>Content</p>"));

        // Assert
        var sheet = cut.Find(".bottom-sheet");
        Assert.Contains("bottom-sheet--small", sheet.ClassList);
    }

    /// <summary>
    /// Verifies that the bottom sheet renders at medium height (default).
    /// </summary>
    [Fact]
    public void BottomSheet_RendersWithMediumHeight_ByDefault()
    {
        // Arrange & Act
        var cut = Render<BottomSheet>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .AddChildContent("<p>Content</p>"));

        // Assert
        var sheet = cut.Find(".bottom-sheet");
        Assert.Contains("bottom-sheet--medium", sheet.ClassList);
    }

    /// <summary>
    /// Verifies that the bottom sheet renders at large height.
    /// </summary>
    [Fact]
    public void BottomSheet_RendersWithLargeHeight()
    {
        // Arrange & Act
        var cut = Render<BottomSheet>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Height, BottomSheetHeight.Large)
            .AddChildContent("<p>Content</p>"));

        // Assert
        var sheet = cut.Find(".bottom-sheet");
        Assert.Contains("bottom-sheet--large", sheet.ClassList);
    }

    /// <summary>
    /// Verifies that the bottom sheet renders at fullscreen height.
    /// </summary>
    [Fact]
    public void BottomSheet_RendersWithFullscreenHeight()
    {
        // Arrange & Act
        var cut = Render<BottomSheet>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Height, BottomSheetHeight.FullScreen)
            .AddChildContent("<p>Content</p>"));

        // Assert
        var sheet = cut.Find(".bottom-sheet");
        Assert.Contains("bottom-sheet--fullscreen", sheet.ClassList);
    }

    /// <summary>
    /// Verifies that the bottom sheet renders with title.
    /// </summary>
    [Fact]
    public void BottomSheet_RendersTitle_WhenProvided()
    {
        // Arrange & Act
        var cut = Render<BottomSheet>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Title, "My Bottom Sheet")
            .AddChildContent("<p>Content</p>"));

        // Assert
        var title = cut.Find(".bottom-sheet__title");
        Assert.Equal("My Bottom Sheet", title.TextContent);
    }

    /// <summary>
    /// Verifies that the bottom sheet does not render header when no title provided.
    /// </summary>
    [Fact]
    public void BottomSheet_DoesNotRenderHeader_WhenNoTitle()
    {
        // Arrange & Act
        var cut = Render<BottomSheet>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .AddChildContent("<p>Content</p>"));

        // Assert
        var headers = cut.FindAll(".bottom-sheet__header");
        Assert.Empty(headers);
    }

    /// <summary>
    /// Verifies that the bottom sheet renders close button when title is provided.
    /// </summary>
    [Fact]
    public void BottomSheet_RendersCloseButton_WhenTitleProvided()
    {
        // Arrange & Act
        var cut = Render<BottomSheet>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Title, "Sheet Title")
            .Add(p => p.IsCloseButtonVisible, true)
            .AddChildContent("<p>Content</p>"));

        // Assert
        var closeButton = cut.Find(".bottom-sheet__close");
        Assert.NotNull(closeButton);
        Assert.Equal("Close", closeButton.GetAttribute("aria-label"));
    }

    /// <summary>
    /// Verifies that the bottom sheet hides close button when IsCloseButtonVisible is false.
    /// </summary>
    [Fact]
    public void BottomSheet_HidesCloseButton_WhenIsCloseButtonVisibleFalse()
    {
        // Arrange & Act
        var cut = Render<BottomSheet>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Title, "Sheet Title")
            .Add(p => p.IsCloseButtonVisible, false)
            .AddChildContent("<p>Content</p>"));

        // Assert
        var closeButtons = cut.FindAll(".bottom-sheet__close");
        Assert.Empty(closeButtons);
    }

    /// <summary>
    /// Verifies that the bottom sheet renders drag handle when IsDraggable is true.
    /// </summary>
    [Fact]
    public void BottomSheet_RendersDragHandle_WhenDraggable()
    {
        // Arrange & Act
        var cut = Render<BottomSheet>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.IsDraggable, true)
            .AddChildContent("<p>Content</p>"));

        // Assert
        var handle = cut.Find(".bottom-sheet__handle");
        Assert.NotNull(handle);
        var handleBar = cut.Find(".bottom-sheet__handle-bar");
        Assert.NotNull(handleBar);
    }

    /// <summary>
    /// Verifies that the bottom sheet does not render drag handle when IsDraggable is false.
    /// </summary>
    [Fact]
    public void BottomSheet_HidesDragHandle_WhenNotDraggable()
    {
        // Arrange & Act
        var cut = Render<BottomSheet>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.IsDraggable, false)
            .AddChildContent("<p>Content</p>"));

        // Assert
        var handles = cut.FindAll(".bottom-sheet__handle");
        Assert.Empty(handles);
    }

    /// <summary>
    /// Verifies that the bottom sheet renders footer content when provided.
    /// </summary>
    [Fact]
    public void BottomSheet_RendersFooter_WhenProvided()
    {
        // Arrange & Act
        var cut = Render<BottomSheet>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .AddChildContent("<p>Body content</p>")
            .Add(p => p.FooterContent, (RenderFragment)(builder =>
            {
                builder.AddContent(0, "<button>Save</button>");
            })));

        // Assert
        var footer = cut.Find(".bottom-sheet__footer");
        Assert.NotNull(footer);
    }

    /// <summary>
    /// Verifies that the bottom sheet calls OnClose when close button is clicked.
    /// </summary>
    [Fact]
    public async Task BottomSheet_CallsOnClose_WhenCloseButtonClicked()
    {
        // Arrange
        var closeCalled = false;

        var cut = Render<BottomSheet>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Title, "Test Sheet")
            .Add(p => p.OnClose, EventCallback.Factory.Create(this, () => closeCalled = true))
            .AddChildContent("<p>Content</p>"));

        // Act
        var closeButton = cut.Find(".bottom-sheet__close");
        await closeButton.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Wait for animation delay
        await Task.Delay(250);

        // Assert
        Assert.True(closeCalled);
    }

    /// <summary>
    /// Verifies that the bottom sheet calls OnClose when backdrop is clicked.
    /// </summary>
    [Fact]
    public async Task BottomSheet_CallsOnClose_WhenBackdropClicked()
    {
        // Arrange
        var closeCalled = false;

        var cut = Render<BottomSheet>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.IsCloseOnBackdropClick, true)
            .Add(p => p.OnClose, EventCallback.Factory.Create(this, () => closeCalled = true))
            .AddChildContent("<p>Content</p>"));

        // Act
        var backdrop = cut.Find(".bottom-sheet-backdrop");
        await backdrop.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Wait for animation delay
        await Task.Delay(250);

        // Assert
        Assert.True(closeCalled);
    }

    /// <summary>
    /// Verifies that the bottom sheet does not close when backdrop click is disabled.
    /// </summary>
    [Fact]
    public async Task BottomSheet_DoesNotClose_WhenBackdropClickDisabled()
    {
        // Arrange
        var closeCalled = false;

        var cut = Render<BottomSheet>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.IsCloseOnBackdropClick, false)
            .Add(p => p.OnClose, EventCallback.Factory.Create(this, () => closeCalled = true))
            .AddChildContent("<p>Content</p>"));

        // Act
        var backdrop = cut.Find(".bottom-sheet-backdrop");
        await backdrop.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Wait briefly
        await Task.Delay(50);

        // Assert
        Assert.False(closeCalled);
    }

    /// <summary>
    /// Verifies that the bottom sheet calls OnClose when Escape key is pressed.
    /// </summary>
    [Fact]
    public async Task BottomSheet_CallsOnClose_WhenEscapePressed()
    {
        // Arrange
        var closeCalled = false;

        var cut = Render<BottomSheet>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.OnClose, EventCallback.Factory.Create(this, () => closeCalled = true))
            .AddChildContent("<p>Content</p>"));

        // Act
        var sheet = cut.Find(".bottom-sheet");
        await sheet.KeyDownAsync(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "Escape" });

        // Wait for animation delay
        await Task.Delay(250);

        // Assert
        Assert.True(closeCalled);
    }

    /// <summary>
    /// Verifies that the bottom sheet has correct ARIA attributes for accessibility.
    /// </summary>
    [Fact]
    public void BottomSheet_HasCorrectAriaAttributes()
    {
        // Arrange & Act
        var cut = Render<BottomSheet>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Title, "Accessible Sheet")
            .AddChildContent("<p>Content</p>"));

        // Assert
        var sheet = cut.Find(".bottom-sheet");
        Assert.Equal("dialog", sheet.GetAttribute("role"));
        Assert.Equal("true", sheet.GetAttribute("aria-modal"));
        Assert.NotNull(sheet.GetAttribute("aria-labelledby"));

        var title = cut.Find(".bottom-sheet__title");
        var titleId = title.GetAttribute("id");
        Assert.Equal(titleId, sheet.GetAttribute("aria-labelledby"));
    }

    /// <summary>
    /// Verifies that the drag handle has correct ARIA attributes.
    /// </summary>
    [Fact]
    public void BottomSheet_DragHandle_HasCorrectAriaAttributes()
    {
        // Arrange & Act
        var cut = Render<BottomSheet>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.IsDraggable, true)
            .AddChildContent("<p>Content</p>"));

        // Assert
        var handle = cut.Find(".bottom-sheet__handle");
        Assert.Equal("slider", handle.GetAttribute("role"));
        Assert.Equal("Drag to resize or swipe down to close", handle.GetAttribute("aria-label"));
        Assert.Equal("0", handle.GetAttribute("tabindex"));
    }

    /// <summary>
    /// Verifies that the bottom sheet shows closing animation class.
    /// </summary>
    [Fact]
    public async Task BottomSheet_ShowsClosingClass_WhenClosing()
    {
        // Arrange
        var cut = Render<BottomSheet>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Title, "Test")
            .Add(p => p.OnClose, EventCallback.Factory.Create(this, () => { }))
            .AddChildContent("<p>Content</p>"));

        // Act
        var closeButton = cut.Find(".bottom-sheet__close");
        await closeButton.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Brief wait for state update (not full animation)
        await Task.Delay(50);

        // Assert - the sheet should have is-closing class during animation
        var sheet = cut.Find(".bottom-sheet");
        Assert.Contains("is-closing", sheet.ClassList);
    }
}
