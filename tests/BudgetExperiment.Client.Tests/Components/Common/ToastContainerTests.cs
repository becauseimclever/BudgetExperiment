// <copyright file="ToastContainerTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using Bunit;

using BudgetExperiment.Client.Components.Common;
using BudgetExperiment.Client.Services;

using Microsoft.Extensions.DependencyInjection;

namespace BudgetExperiment.Client.Tests.Components.Common;

/// <summary>
/// Unit tests for the <see cref="ToastContainer"/> component.
/// </summary>
public sealed class ToastContainerTests : BunitContext, IAsyncLifetime
{
    private readonly ToastService _toastService = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ToastContainerTests"/> class.
    /// </summary>
    public ToastContainerTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IToastService>(_toastService);
        Services.AddSingleton<ThemeService>();
    }

    /// <inheritdoc/>
    public Task InitializeAsync() => Task.CompletedTask;

    /// <inheritdoc/>
    public new Task DisposeAsync()
    {
        _toastService.Dispose();
        return base.DisposeAsync().AsTask();
    }

    /// <summary>
    /// Verifies that the container renders with no toasts initially.
    /// </summary>
    [Fact]
    public void Render_NoToasts_RendersEmptyContainer()
    {
        // Act
        var cut = Render<ToastContainer>();

        // Assert
        var container = cut.Find(".toast-container");
        Assert.Empty(container.QuerySelectorAll(".toast"));
    }

    /// <summary>
    /// Verifies that a success toast is rendered with correct classes.
    /// </summary>
    [Fact]
    public void Render_SuccessToast_ShowsSuccessClass()
    {
        // Arrange
        var cut = Render<ToastContainer>();

        // Act
        _toastService.ShowSuccess("Done!");

        // Assert
        var toast = cut.Find("[data-testid='toast-success']");
        Assert.Contains("toast-success", toast.ClassList);
        Assert.Contains("Done!", toast.TextContent);
    }

    /// <summary>
    /// Verifies that an error toast is rendered with correct classes.
    /// </summary>
    [Fact]
    public void Render_ErrorToast_ShowsErrorClass()
    {
        // Arrange
        var cut = Render<ToastContainer>();

        // Act
        _toastService.ShowError("Failed!");

        // Assert
        var toast = cut.Find("[data-testid='toast-error']");
        Assert.Contains("toast-error", toast.ClassList);
    }

    /// <summary>
    /// Verifies that toast title is displayed when provided.
    /// </summary>
    [Fact]
    public void Render_ToastWithTitle_ShowsTitle()
    {
        // Arrange
        var cut = Render<ToastContainer>();

        // Act
        _toastService.ShowSuccess("Body text", "My Title");

        // Assert
        var title = cut.Find(".toast-title");
        Assert.Equal("My Title", title.TextContent);
    }

    /// <summary>
    /// Verifies that clicking dismiss removes the toast.
    /// </summary>
    [Fact]
    public void ClickDismiss_RemovesToast()
    {
        // Arrange
        var cut = Render<ToastContainer>();
        _toastService.ShowSuccess("Dismissable");

        // Act
        var dismissBtn = cut.Find(".toast-dismiss");
        dismissBtn.Click();

        // Assert
        Assert.Empty(cut.FindAll(".toast"));
    }

    /// <summary>
    /// Verifies that the container has an aria-live region for accessibility.
    /// </summary>
    [Fact]
    public void Render_HasAriaLiveRegion()
    {
        // Act
        var cut = Render<ToastContainer>();

        // Assert
        var container = cut.Find(".toast-container");
        Assert.Equal("polite", container.GetAttribute("aria-live"));
    }

    /// <summary>
    /// Verifies that multiple toasts are rendered.
    /// </summary>
    [Fact]
    public void Render_MultipleToasts_RendersAll()
    {
        // Arrange
        var cut = Render<ToastContainer>();

        // Act
        _toastService.ShowSuccess("First");
        _toastService.ShowError("Second");
        _toastService.ShowWarning("Third");

        // Assert
        var toasts = cut.FindAll(".toast");
        Assert.Equal(3, toasts.Count);
    }
}
