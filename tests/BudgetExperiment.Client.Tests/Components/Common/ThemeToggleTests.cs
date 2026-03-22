// <copyright file="ThemeToggleTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Components.Common;
using BudgetExperiment.Client.Services;

using Bunit;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace BudgetExperiment.Client.Tests.Components.Common;

/// <summary>
/// Unit tests for the <see cref="ThemeToggle"/> component.
/// </summary>
public sealed class ThemeToggleTests : BunitContext, IAsyncLifetime
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ThemeToggleTests"/> class.
    /// </summary>
    public ThemeToggleTests()
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
    /// Verifies that the toggle button renders.
    /// </summary>
    [Fact]
    public void ThemeToggle_RendersToggleButton()
    {
        // Arrange & Act
        var cut = Render<ThemeToggle>();

        // Assert
        var button = cut.Find(".theme-toggle-button");
        Assert.NotNull(button);
    }

    /// <summary>
    /// Verifies that the dropdown is initially closed.
    /// </summary>
    [Fact]
    public void ThemeToggle_DropdownInitiallyClosed()
    {
        // Arrange & Act
        var cut = Render<ThemeToggle>();

        // Assert
        Assert.Empty(cut.FindAll(".theme-dropdown"));
    }

    /// <summary>
    /// Verifies that clicking the button opens the dropdown.
    /// </summary>
    [Fact]
    public void ThemeToggle_ClickOpensDropdown()
    {
        // Arrange
        var cut = Render<ThemeToggle>();

        // Act
        cut.Find(".theme-toggle-button").Click();

        // Assert
        var dropdown = cut.Find(".theme-dropdown");
        Assert.NotNull(dropdown);
    }

    /// <summary>
    /// Verifies that clicking the button twice closes the dropdown.
    /// </summary>
    [Fact]
    public void ThemeToggle_ClickTwiceClosesDropdown()
    {
        // Arrange
        var cut = Render<ThemeToggle>();

        // Act
        cut.Find(".theme-toggle-button").Click();
        cut.Find(".theme-toggle-button").Click();

        // Assert
        Assert.Empty(cut.FindAll(".theme-dropdown"));
    }

    /// <summary>
    /// Verifies that the dropdown renders all available theme options.
    /// </summary>
    [Fact]
    public void ThemeToggle_RendersAllThemeOptions()
    {
        // Arrange
        var cut = Render<ThemeToggle>();

        // Act
        cut.Find(".theme-toggle-button").Click();

        // Assert
        var options = cut.FindAll(".theme-option");
        Assert.Equal(ThemeService.AvailableThemes.Count, options.Count);
    }

    /// <summary>
    /// Verifies that theme option labels match available themes.
    /// </summary>
    [Fact]
    public void ThemeToggle_ThemeOptionsHaveCorrectLabels()
    {
        // Arrange
        var cut = Render<ThemeToggle>();

        // Act
        cut.Find(".theme-toggle-button").Click();

        // Assert
        var options = cut.FindAll(".theme-option");
        foreach (var theme in ThemeService.AvailableThemes)
        {
            Assert.Contains(options, o => o.TextContent.Contains(theme.Label));
        }
    }

    /// <summary>
    /// Verifies that the current theme option has the 'active' class.
    /// </summary>
    [Fact]
    public void ThemeToggle_CurrentThemeHasActiveClass()
    {
        // Arrange
        var cut = Render<ThemeToggle>();

        // Act
        cut.Find(".theme-toggle-button").Click();

        // Assert
        var activeOptions = cut.FindAll(".theme-option.active");
        Assert.Single(activeOptions);
    }

    /// <summary>
    /// Verifies that selecting a theme closes the dropdown.
    /// </summary>
    [Fact]
    public void ThemeToggle_SelectThemeClosesDropdown()
    {
        // Arrange
        var cut = Render<ThemeToggle>();
        cut.Find(".theme-toggle-button").Click();

        // Act - click the first theme option
        var options = cut.FindAll(".theme-option");
        options[1].Click();

        // Assert
        Assert.Empty(cut.FindAll(".theme-dropdown"));
    }

    /// <summary>
    /// Verifies that the label is not shown by default.
    /// </summary>
    [Fact]
    public void ThemeToggle_DoesNotShowLabel_ByDefault()
    {
        // Arrange & Act
        var cut = Render<ThemeToggle>();

        // Assert
        Assert.Empty(cut.FindAll(".theme-toggle-label"));
    }

    /// <summary>
    /// Verifies that the label is shown when ShowLabel is true.
    /// </summary>
    [Fact]
    public void ThemeToggle_ShowsLabel_WhenShowLabelTrue()
    {
        // Arrange & Act
        var cut = Render<ThemeToggle>(parameters => parameters
            .Add(p => p.ShowLabel, true));

        // Assert
        var label = cut.Find(".theme-toggle-label");
        Assert.NotNull(label);
        Assert.False(string.IsNullOrEmpty(label.TextContent));
    }

    /// <summary>
    /// Verifies that the toggle button has correct accessibility attributes.
    /// </summary>
    [Fact]
    public void ThemeToggle_HasAccessibilityAttributes()
    {
        // Arrange & Act
        var cut = Render<ThemeToggle>();

        // Assert
        var button = cut.Find(".theme-toggle-button");
        Assert.Equal("Change theme", button.GetAttribute("aria-label"));
        Assert.Equal("true", button.GetAttribute("aria-haspopup"));
    }

    /// <summary>
    /// Verifies aria-expanded changes when dropdown opens.
    /// </summary>
    [Fact]
    public void ThemeToggle_AriaExpandedChanges_WhenDropdownOpens()
    {
        // Arrange
        var cut = Render<ThemeToggle>();
        var button = cut.Find(".theme-toggle-button");

        // Capture initial value
        var initialExpanded = button.GetAttribute("aria-expanded");

        // Act - open
        button.Click();

        // Assert - value should have changed after opening
        button = cut.Find(".theme-toggle-button");
        var expandedAfterOpen = button.GetAttribute("aria-expanded");
        Assert.NotEqual(initialExpanded, expandedAfterOpen);
    }
}
