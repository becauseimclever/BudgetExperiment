// <copyright file="LoadingSpinnerTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Components.Common;
using Bunit;

namespace BudgetExperiment.Client.Tests.Components.Common;

/// <summary>
/// Unit tests for the <see cref="LoadingSpinner"/> component.
/// </summary>
public sealed class LoadingSpinnerTests : BunitContext
{
    /// <summary>
    /// Verifies that the spinner renders with default medium size class.
    /// </summary>
    [Fact]
    public void LoadingSpinner_RendersWithMediumSize_ByDefault()
    {
        // Arrange & Act
        var cut = Render<LoadingSpinner>();

        // Assert
        var spinner = cut.Find(".spinner");
        Assert.Contains("spinner-md", spinner.ClassList);
    }

    /// <summary>
    /// Verifies that the spinner renders with small size class.
    /// </summary>
    [Fact]
    public void LoadingSpinner_RendersWithSmallSize()
    {
        // Arrange & Act
        var cut = Render<LoadingSpinner>(parameters => parameters
            .Add(p => p.Size, SpinnerSize.Small));

        // Assert
        var spinner = cut.Find(".spinner");
        Assert.Contains("spinner-sm", spinner.ClassList);
    }

    /// <summary>
    /// Verifies that the spinner renders with large size class.
    /// </summary>
    [Fact]
    public void LoadingSpinner_RendersWithLargeSize()
    {
        // Arrange & Act
        var cut = Render<LoadingSpinner>(parameters => parameters
            .Add(p => p.Size, SpinnerSize.Large));

        // Assert
        var spinner = cut.Find(".spinner");
        Assert.Contains("spinner-lg", spinner.ClassList);
    }

    /// <summary>
    /// Verifies that the message is rendered when provided.
    /// </summary>
    [Fact]
    public void LoadingSpinner_RendersMessage_WhenProvided()
    {
        // Arrange & Act
        var cut = Render<LoadingSpinner>(parameters => parameters
            .Add(p => p.Message, "Loading accounts..."));

        // Assert
        var message = cut.Find(".loading-message");
        Assert.Equal("Loading accounts...", message.TextContent);
    }

    /// <summary>
    /// Verifies that the message is not rendered when not provided.
    /// </summary>
    [Fact]
    public void LoadingSpinner_DoesNotRenderMessage_WhenNotProvided()
    {
        // Arrange & Act
        var cut = Render<LoadingSpinner>();

        // Assert
        Assert.Empty(cut.FindAll(".loading-message"));
    }

    /// <summary>
    /// Verifies that full-page mode applies the correct CSS class.
    /// </summary>
    [Fact]
    public void LoadingSpinner_AppliesFullPageClass_WhenFullPage()
    {
        // Arrange & Act
        var cut = Render<LoadingSpinner>(parameters => parameters
            .Add(p => p.FullPage, true));

        // Assert
        var container = cut.Find(".loading-container");
        Assert.Contains("loading-container-fullpage", container.ClassList);
    }

    /// <summary>
    /// Verifies that full-page class is not applied by default.
    /// </summary>
    [Fact]
    public void LoadingSpinner_NoFullPageClass_ByDefault()
    {
        // Arrange & Act
        var cut = Render<LoadingSpinner>();

        // Assert
        var container = cut.Find(".loading-container");
        Assert.DoesNotContain("loading-container-fullpage", container.ClassList);
    }
}
