// <copyright file="ConfirmDialogTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Components.Common;
using BudgetExperiment.Client.Services;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace BudgetExperiment.Client.Tests.Components.Common;

/// <summary>
/// Unit tests for the <see cref="ConfirmDialog"/> component.
/// </summary>
public sealed class ConfirmDialogTests : BunitContext, IAsyncLifetime
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConfirmDialogTests"/> class.
    /// </summary>
    public ConfirmDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<ThemeService>();
        Services.AddSingleton<CultureService>();
    }

    /// <inheritdoc/>
    public Task InitializeAsync() => Task.CompletedTask;

    /// <inheritdoc/>
    public new Task DisposeAsync() => base.DisposeAsync().AsTask();

    /// <summary>
    /// Verifies that the dialog does not render when IsVisible is false.
    /// </summary>
    [Fact]
    public void ConfirmDialog_DoesNotRender_WhenNotVisible()
    {
        // Arrange & Act
        var cut = Render<ConfirmDialog>(parameters => parameters
            .Add(p => p.IsVisible, false)
            .Add(p => p.Message, "Are you sure?"));

        // Assert
        Assert.Empty(cut.Markup.Trim());
    }

    /// <summary>
    /// Verifies that the dialog renders the message when visible.
    /// </summary>
    [Fact]
    public void ConfirmDialog_RendersMessage_WhenVisible()
    {
        // Arrange & Act
        var cut = Render<ConfirmDialog>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Message, "Delete this item?"));

        // Assert
        Assert.Contains("Delete this item?", cut.Markup);
    }

    /// <summary>
    /// Verifies that the dialog renders with default title.
    /// </summary>
    [Fact]
    public void ConfirmDialog_RendersDefaultTitle()
    {
        // Arrange & Act
        var cut = Render<ConfirmDialog>(parameters => parameters
            .Add(p => p.IsVisible, true));

        // Assert
        Assert.Contains("Confirm", cut.Markup);
    }

    /// <summary>
    /// Verifies that the dialog renders with custom title.
    /// </summary>
    [Fact]
    public void ConfirmDialog_RendersCustomTitle()
    {
        // Arrange & Act
        var cut = Render<ConfirmDialog>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Title, "Remove Account"));

        // Assert
        Assert.Contains("Remove Account", cut.Markup);
    }

    /// <summary>
    /// Verifies that the dialog renders default confirm and cancel button text.
    /// </summary>
    [Fact]
    public void ConfirmDialog_RendersDefaultButtonText()
    {
        // Arrange & Act
        var cut = Render<ConfirmDialog>(parameters => parameters
            .Add(p => p.IsVisible, true));

        // Assert
        var buttons = cut.FindAll("button");
        Assert.Contains(buttons, b => b.TextContent.Contains("Delete"));
        Assert.Contains(buttons, b => b.TextContent.Contains("Cancel"));
    }

    /// <summary>
    /// Verifies that the dialog renders custom confirm and cancel button text.
    /// </summary>
    [Fact]
    public void ConfirmDialog_RendersCustomButtonText()
    {
        // Arrange & Act
        var cut = Render<ConfirmDialog>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.ConfirmText, "Yes, Remove")
            .Add(p => p.CancelText, "No, Keep"));

        // Assert
        var buttons = cut.FindAll("button");
        Assert.Contains(buttons, b => b.TextContent.Contains("Yes, Remove"));
        Assert.Contains(buttons, b => b.TextContent.Contains("No, Keep"));
    }

    /// <summary>
    /// Verifies that clicking confirm fires the OnConfirm callback.
    /// </summary>
    [Fact]
    public void ConfirmDialog_ClickConfirm_FiresOnConfirm()
    {
        // Arrange
        var confirmed = false;
        var cut = Render<ConfirmDialog>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.OnConfirm, () => { confirmed = true; }));

        // Act
        var confirmButton = cut.Find(".btn-danger");
        confirmButton.Click();

        // Assert
        Assert.True(confirmed);
    }

    /// <summary>
    /// Verifies that clicking cancel fires the OnCancel callback.
    /// </summary>
    [Fact]
    public void ConfirmDialog_ClickCancel_FiresOnCancel()
    {
        // Arrange
        var cancelled = false;
        var cut = Render<ConfirmDialog>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.OnCancel, () => { cancelled = true; }));

        // Act
        var cancelButton = cut.Find(".btn-secondary");
        cancelButton.Click();

        // Assert
        Assert.True(cancelled);
    }

    /// <summary>
    /// Verifies that buttons are disabled when IsProcessing is true.
    /// </summary>
    [Fact]
    public void ConfirmDialog_DisablesButtons_WhenProcessing()
    {
        // Arrange & Act
        var cut = Render<ConfirmDialog>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.IsProcessing, true));

        // Assert
        var confirmButton = cut.Find(".btn-danger");
        var cancelButton = cut.Find(".btn-secondary");
        Assert.NotNull(confirmButton.GetAttribute("disabled"));
        Assert.NotNull(cancelButton.GetAttribute("disabled"));
    }

    /// <summary>
    /// Verifies that 'Processing...' text is shown when IsProcessing is true.
    /// </summary>
    [Fact]
    public void ConfirmDialog_ShowsProcessingText_WhenProcessing()
    {
        // Arrange & Act
        var cut = Render<ConfirmDialog>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.IsProcessing, true));

        // Assert
        Assert.Contains("Processing...", cut.Markup);
    }

    /// <summary>
    /// Verifies that buttons are enabled when IsProcessing is false.
    /// </summary>
    [Fact]
    public void ConfirmDialog_EnablesButtons_WhenNotProcessing()
    {
        // Arrange & Act
        var cut = Render<ConfirmDialog>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.IsProcessing, false));

        // Assert
        var confirmButton = cut.Find(".btn-danger");
        var cancelButton = cut.Find(".btn-secondary");
        Assert.Null(confirmButton.GetAttribute("disabled"));
        Assert.Null(cancelButton.GetAttribute("disabled"));
    }

    /// <summary>
    /// Verifies that the dialog renders the default message.
    /// </summary>
    [Fact]
    public void ConfirmDialog_RendersDefaultMessage()
    {
        // Arrange & Act
        var cut = Render<ConfirmDialog>(parameters => parameters
            .Add(p => p.IsVisible, true));

        // Assert
        Assert.Contains("Are you sure you want to proceed?", cut.Markup);
    }
}
