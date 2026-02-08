// <copyright file="SwipeContainerTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using Bunit;

using BudgetExperiment.Client.Components.Common;
using BudgetExperiment.Client.Services;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace BudgetExperiment.Client.Tests.Components.Common;

/// <summary>
/// Unit tests for the <see cref="SwipeContainer"/> component.
/// </summary>
public sealed class SwipeContainerTests : BunitContext, IAsyncLifetime
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SwipeContainerTests"/> class.
    /// </summary>
    public SwipeContainerTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<ThemeService>();
    }

    /// <inheritdoc/>
    public Task InitializeAsync() => Task.CompletedTask;

    /// <inheritdoc/>
    public new Task DisposeAsync() => base.DisposeAsync().AsTask();

    /// <summary>
    /// Verifies that the container renders with a wrapper div.
    /// </summary>
    [Fact]
    public void Render_CreatesContainerDiv()
    {
        // Act
        var cut = Render<SwipeContainer>(p => p
            .AddChildContent("<p>Calendar here</p>"));

        // Assert
        var container = cut.Find(".swipe-container");
        Assert.NotNull(container);
    }

    /// <summary>
    /// Verifies that child content is rendered inside the container.
    /// </summary>
    [Fact]
    public void Render_RendersChildContent()
    {
        // Act
        var cut = Render<SwipeContainer>(p => p
            .AddChildContent("<div class='calendar-grid'>Grid</div>"));

        // Assert
        Assert.Contains("calendar-grid", cut.Markup);
        Assert.Contains("Grid", cut.Markup);
    }

    /// <summary>
    /// Verifies that the JS module is imported on first render.
    /// </summary>
    [Fact]
    public void FirstRender_ImportsSwipeJsModule()
    {
        // Arrange
        var moduleInvocation = JSInterop.SetupModule("./js/swipe.js");
        moduleInvocation.SetupVoid("initSwipeDetection");

        // Act
        Render<SwipeContainer>(p => p
            .AddChildContent("<p>Content</p>"));

        // Assert
        JSInterop.VerifyInvoke("import");
    }

    /// <summary>
    /// Verifies that OnSwipedLeft fires when OnSwipeLeft JSInvokable is called.
    /// </summary>
    [Fact]
    public async Task OnSwipeLeft_InvokesOnSwipedLeftCallback()
    {
        // Arrange
        var swipedLeft = false;
        var cut = Render<SwipeContainer>(p => p
            .Add(x => x.OnSwipedLeft, () => swipedLeft = true)
            .AddChildContent("<p>Content</p>"));

        // Act — simulate JS calling the [JSInvokable] method
        await cut.Instance.OnSwipeLeft();

        // Assert
        Assert.True(swipedLeft);
    }

    /// <summary>
    /// Verifies that OnSwipedRight fires when OnSwipeRight JSInvokable is called.
    /// </summary>
    [Fact]
    public async Task OnSwipeRight_InvokesOnSwipedRightCallback()
    {
        // Arrange
        var swipedRight = false;
        var cut = Render<SwipeContainer>(p => p
            .Add(x => x.OnSwipedRight, () => swipedRight = true)
            .AddChildContent("<p>Content</p>"));

        // Act — simulate JS calling the [JSInvokable] method
        await cut.Instance.OnSwipeRight();

        // Assert
        Assert.True(swipedRight);
    }

    /// <summary>
    /// Verifies that the default threshold is 50px.
    /// </summary>
    [Fact]
    public void DefaultThreshold_Is50Px()
    {
        // Act
        var cut = Render<SwipeContainer>(p => p
            .AddChildContent("<p>Content</p>"));

        // Assert
        Assert.Equal(50, cut.Instance.ThresholdPx);
    }

    /// <summary>
    /// Verifies that the default max time is 500ms.
    /// </summary>
    [Fact]
    public void DefaultMaxTime_Is500Ms()
    {
        // Act
        var cut = Render<SwipeContainer>(p => p
            .AddChildContent("<p>Content</p>"));

        // Assert
        Assert.Equal(500, cut.Instance.MaxTimeMs);
    }

    /// <summary>
    /// Verifies that custom threshold can be set.
    /// </summary>
    [Fact]
    public void CustomThreshold_CanBeSet()
    {
        // Act
        var cut = Render<SwipeContainer>(p => p
            .Add(x => x.ThresholdPx, 100)
            .AddChildContent("<p>Content</p>"));

        // Assert
        Assert.Equal(100, cut.Instance.ThresholdPx);
    }

    /// <summary>
    /// Verifies that swipe callbacks don't fire when not wired.
    /// </summary>
    [Fact]
    public async Task OnSwipeLeft_WhenNoCallback_DoesNotThrow()
    {
        // Arrange — no OnSwipedLeft callback set
        var cut = Render<SwipeContainer>(p => p
            .AddChildContent("<p>Content</p>"));

        // Act & Assert — should not throw
        await cut.Instance.OnSwipeLeft();
    }

    /// <summary>
    /// Verifies that dispose can be called without errors.
    /// </summary>
    [Fact]
    public async Task Dispose_CanBeCalledSafely()
    {
        // Arrange
        var cut = Render<SwipeContainer>(p => p
            .AddChildContent("<p>Content</p>"));

        // Act & Assert — should not throw
        await cut.Instance.DisposeAsync();
    }
}
