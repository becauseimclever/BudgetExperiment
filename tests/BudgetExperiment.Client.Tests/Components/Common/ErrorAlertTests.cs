// <copyright file="ErrorAlertTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Components.Common;
using BudgetExperiment.Client.Services;
using BudgetExperiment.Client.Tests.TestHelpers;

using Bunit;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace BudgetExperiment.Client.Tests.Components.Common;

/// <summary>
/// Unit tests for the <see cref="ErrorAlert"/> component.
/// </summary>
public sealed class ErrorAlertTests : BunitContext, IAsyncLifetime
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ErrorAlertTests"/> class.
    /// </summary>
    public ErrorAlertTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<ThemeService>();
        Services.AddSingleton<CultureService>();
        Services.AddSingleton<IExportDownloadService>(new StubExportDownloadService());
        Services.AddSingleton<IToastService>(new ToastService());
    }

    /// <inheritdoc/>
    public Task InitializeAsync() => Task.CompletedTask;

    /// <inheritdoc/>
    public new Task DisposeAsync() => base.DisposeAsync().AsTask();

    /// <summary>
    /// Verifies that the component does not render when Message is null.
    /// </summary>
    [Fact]
    public void ErrorAlert_DoesNotRender_WhenMessageIsNull()
    {
        // Arrange & Act
        var cut = Render<ErrorAlert>(parameters => parameters
            .Add(p => p.Message, null));

        // Assert
        Assert.Empty(cut.Markup.Trim());
    }

    /// <summary>
    /// Verifies that the component does not render when Message is empty.
    /// </summary>
    [Fact]
    public void ErrorAlert_DoesNotRender_WhenMessageIsEmpty()
    {
        // Arrange & Act
        var cut = Render<ErrorAlert>(parameters => parameters
            .Add(p => p.Message, string.Empty));

        // Assert
        Assert.Empty(cut.Markup.Trim());
    }

    /// <summary>
    /// Verifies that the component renders the error message.
    /// </summary>
    [Fact]
    public void ErrorAlert_RendersMessage()
    {
        // Arrange & Act
        var cut = Render<ErrorAlert>(parameters => parameters
            .Add(p => p.Message, "Something went wrong"));

        // Assert
        var alert = cut.Find(".error-alert");
        Assert.Contains("Something went wrong", alert.TextContent);
    }

    /// <summary>
    /// Verifies that the component renders details when provided.
    /// </summary>
    [Fact]
    public void ErrorAlert_RendersDetails_WhenProvided()
    {
        // Arrange & Act
        var cut = Render<ErrorAlert>(parameters => parameters
            .Add(p => p.Message, "Error occurred")
            .Add(p => p.Details, "Connection timed out after 30s"));

        // Assert
        var details = cut.Find(".error-alert-details");
        Assert.Equal("Connection timed out after 30s", details.TextContent);
    }

    /// <summary>
    /// Verifies that details section is not rendered when not provided.
    /// </summary>
    [Fact]
    public void ErrorAlert_DoesNotRenderDetails_WhenNotProvided()
    {
        // Arrange & Act
        var cut = Render<ErrorAlert>(parameters => parameters
            .Add(p => p.Message, "Error occurred"));

        // Assert
        Assert.Empty(cut.FindAll(".error-alert-details"));
    }

    /// <summary>
    /// Verifies that the retry button is shown when OnRetry delegate is provided.
    /// </summary>
    [Fact]
    public void ErrorAlert_ShowsRetryButton_WhenOnRetryProvided()
    {
        // Arrange & Act
        var cut = Render<ErrorAlert>(parameters => parameters
            .Add(p => p.Message, "Error occurred")
            .Add(p => p.OnRetry, () => { }));

        // Assert
        var retryButton = cut.Find(".error-alert-retry");
        Assert.NotNull(retryButton);
    }

    /// <summary>
    /// Verifies that the retry button is hidden when OnRetry delegate is not provided.
    /// </summary>
    [Fact]
    public void ErrorAlert_HidesRetryButton_WhenOnRetryNotProvided()
    {
        // Arrange & Act
        var cut = Render<ErrorAlert>(parameters => parameters
            .Add(p => p.Message, "Error occurred"));

        // Assert
        Assert.Empty(cut.FindAll(".error-alert-retry"));
    }

    /// <summary>
    /// Verifies that clicking retry fires the OnRetry callback.
    /// </summary>
    [Fact]
    public void ErrorAlert_ClickRetry_FiresOnRetry()
    {
        // Arrange
        var retried = false;
        var cut = Render<ErrorAlert>(parameters => parameters
            .Add(p => p.Message, "Error occurred")
            .Add(p => p.OnRetry, () => { retried = true; }));

        // Act
        cut.Find(".error-alert-retry").Click();

        // Assert
        Assert.True(retried);
    }

    /// <summary>
    /// Verifies that the retry button is disabled and shows 'Retrying...' when IsRetrying is true.
    /// </summary>
    [Fact]
    public void ErrorAlert_DisablesRetryButton_WhenRetrying()
    {
        // Arrange & Act
        var cut = Render<ErrorAlert>(parameters => parameters
            .Add(p => p.Message, "Error occurred")
            .Add(p => p.IsRetrying, true)
            .Add(p => p.OnRetry, () => { }));

        // Assert
        var retryButton = cut.Find(".error-alert-retry");
        Assert.NotNull(retryButton.GetAttribute("disabled"));
        Assert.Contains("Retrying...", retryButton.TextContent);
    }

    /// <summary>
    /// Verifies that the dismiss button is shown when IsDismissible and OnDismiss are set.
    /// </summary>
    [Fact]
    public void ErrorAlert_ShowsDismissButton_WhenDismissible()
    {
        // Arrange & Act
        var cut = Render<ErrorAlert>(parameters => parameters
            .Add(p => p.Message, "Error occurred")
            .Add(p => p.IsDismissible, true)
            .Add(p => p.OnDismiss, () => { }));

        // Assert
        var dismissButton = cut.Find(".error-alert-dismiss");
        Assert.NotNull(dismissButton);
    }

    /// <summary>
    /// Verifies that the dismiss button is hidden when IsDismissible is false.
    /// </summary>
    [Fact]
    public void ErrorAlert_HidesDismissButton_WhenNotDismissible()
    {
        // Arrange & Act
        var cut = Render<ErrorAlert>(parameters => parameters
            .Add(p => p.Message, "Error occurred")
            .Add(p => p.IsDismissible, false)
            .Add(p => p.OnDismiss, () => { }));

        // Assert
        Assert.Empty(cut.FindAll(".error-alert-dismiss"));
    }

    /// <summary>
    /// Verifies that clicking dismiss fires the OnDismiss callback.
    /// </summary>
    [Fact]
    public void ErrorAlert_ClickDismiss_FiresOnDismiss()
    {
        // Arrange
        var dismissed = false;
        var cut = Render<ErrorAlert>(parameters => parameters
            .Add(p => p.Message, "Error occurred")
            .Add(p => p.IsDismissible, true)
            .Add(p => p.OnDismiss, () => { dismissed = true; }));

        // Act
        cut.Find(".error-alert-dismiss").Click();

        // Assert
        Assert.True(dismissed);
    }

    /// <summary>
    /// Verifies that the dismiss button is hidden when OnDismiss delegate is not provided.
    /// </summary>
    [Fact]
    public void ErrorAlert_HidesDismissButton_WhenOnDismissNotProvided()
    {
        // Arrange & Act
        var cut = Render<ErrorAlert>(parameters => parameters
            .Add(p => p.Message, "Error occurred")
            .Add(p => p.IsDismissible, true));

        // Assert
        Assert.Empty(cut.FindAll(".error-alert-dismiss"));
    }
}
