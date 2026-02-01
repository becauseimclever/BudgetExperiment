// <copyright file="ButtonTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using Bunit;

using BudgetExperiment.Client.Components.Common;

namespace BudgetExperiment.Client.Tests.Components.Common;

/// <summary>
/// Unit tests for the Button component.
/// </summary>
public class ButtonTests : BunitContext
{
    /// <summary>
    /// Verifies that the button renders with default primary variant.
    /// </summary>
    [Fact]
    public void Button_RendersWithDefaultPrimaryVariant()
    {
        // Arrange & Act
        var cut = Render<Button>(parameters => parameters
            .AddChildContent("Click me"));

        // Assert
        var button = cut.Find("button");
        Assert.Contains("btn", button.ClassList);
        Assert.Contains("btn-primary", button.ClassList);
    }

    /// <summary>
    /// Verifies that the button renders with secondary variant.
    /// </summary>
    [Fact]
    public void Button_RendersWithSecondaryVariant()
    {
        // Arrange & Act
        var cut = Render<Button>(parameters => parameters
            .Add(p => p.Variant, ButtonVariant.Secondary)
            .AddChildContent("Cancel"));

        // Assert
        var button = cut.Find("button");
        Assert.Contains("btn-secondary", button.ClassList);
    }

    /// <summary>
    /// Verifies that the button renders with success variant.
    /// </summary>
    [Fact]
    public void Button_RendersWithSuccessVariant()
    {
        // Arrange & Act
        var cut = Render<Button>(parameters => parameters
            .Add(p => p.Variant, ButtonVariant.Success)
            .AddChildContent("Save"));

        // Assert
        var button = cut.Find("button");
        Assert.Contains("btn-success", button.ClassList);
    }

    /// <summary>
    /// Verifies that the button renders with danger variant.
    /// </summary>
    [Fact]
    public void Button_RendersWithDangerVariant()
    {
        // Arrange & Act
        var cut = Render<Button>(parameters => parameters
            .Add(p => p.Variant, ButtonVariant.Danger)
            .AddChildContent("Delete"));

        // Assert
        var button = cut.Find("button");
        Assert.Contains("btn-danger", button.ClassList);
    }

    /// <summary>
    /// Verifies that the button renders with ghost variant.
    /// </summary>
    [Fact]
    public void Button_RendersWithGhostVariant()
    {
        // Arrange & Act
        var cut = Render<Button>(parameters => parameters
            .Add(p => p.Variant, ButtonVariant.Ghost)
            .AddChildContent("Close"));

        // Assert
        var button = cut.Find("button");
        Assert.Contains("btn-ghost", button.ClassList);
    }

    /// <summary>
    /// Verifies that the button renders with small size class.
    /// </summary>
    [Fact]
    public void Button_RendersWithSmallSize()
    {
        // Arrange & Act
        var cut = Render<Button>(parameters => parameters
            .Add(p => p.Size, ButtonSize.Small)
            .AddChildContent("Small"));

        // Assert
        var button = cut.Find("button");
        Assert.Contains("btn-sm", button.ClassList);
    }

    /// <summary>
    /// Verifies that the button renders with large size class.
    /// </summary>
    [Fact]
    public void Button_RendersWithLargeSize()
    {
        // Arrange & Act
        var cut = Render<Button>(parameters => parameters
            .Add(p => p.Size, ButtonSize.Large)
            .AddChildContent("Large"));

        // Assert
        var button = cut.Find("button");
        Assert.Contains("btn-lg", button.ClassList);
    }

    /// <summary>
    /// Verifies that the button renders with default medium size (no extra class).
    /// </summary>
    [Fact]
    public void Button_RendersWithMediumSizeNoExtraClass()
    {
        // Arrange & Act
        var cut = Render<Button>(parameters => parameters
            .Add(p => p.Size, ButtonSize.Medium)
            .AddChildContent("Medium"));

        // Assert
        var button = cut.Find("button");
        Assert.DoesNotContain("btn-sm", button.ClassList);
        Assert.DoesNotContain("btn-lg", button.ClassList);
    }

    /// <summary>
    /// Verifies that the button is disabled when IsDisabled is true.
    /// </summary>
    [Fact]
    public void Button_IsDisabledWhenIsDisabledIsTrue()
    {
        // Arrange & Act
        var cut = Render<Button>(parameters => parameters
            .Add(p => p.IsDisabled, true)
            .AddChildContent("Disabled"));

        // Assert
        var button = cut.Find("button");
        Assert.True(button.HasAttribute("disabled"));
    }

    /// <summary>
    /// Verifies that the button is disabled when IsLoading is true.
    /// </summary>
    [Fact]
    public void Button_IsDisabledWhenIsLoadingIsTrue()
    {
        // Arrange & Act
        var cut = Render<Button>(parameters => parameters
            .Add(p => p.IsLoading, true)
            .AddChildContent("Loading..."));

        // Assert
        var button = cut.Find("button");
        Assert.True(button.HasAttribute("disabled"));
    }

    /// <summary>
    /// Verifies that the button shows a loading spinner when IsLoading is true.
    /// </summary>
    [Fact]
    public void Button_ShowsSpinnerWhenLoading()
    {
        // Arrange & Act
        var cut = Render<Button>(parameters => parameters
            .Add(p => p.IsLoading, true)
            .AddChildContent("Loading..."));

        // Assert - LoadingSpinner component should be rendered
        Assert.True(cut.HasComponent<LoadingSpinner>());
    }

    /// <summary>
    /// Verifies that the button fires OnClick when clicked.
    /// </summary>
    [Fact]
    public void Button_FiresOnClickWhenClicked()
    {
        // Arrange
        var clicked = false;
        var cut = Render<Button>(parameters => parameters
            .Add(p => p.OnClick, () => clicked = true)
            .AddChildContent("Click me"));

        // Act
        cut.Find("button").Click();

        // Assert
        Assert.True(clicked);
    }

    /// <summary>
    /// Verifies that the button does not fire OnClick when disabled.
    /// </summary>
    [Fact]
    public void Button_DoesNotFireOnClickWhenDisabled()
    {
        // Arrange
        var clicked = false;
        var cut = Render<Button>(parameters => parameters
            .Add(p => p.IsDisabled, true)
            .Add(p => p.OnClick, () => clicked = true)
            .AddChildContent("Click me"));

        // Act - Note: disabled button click is prevented by browser, but test handler logic
        cut.Find("button").Click();

        // Assert
        Assert.False(clicked);
    }

    /// <summary>
    /// Verifies that the button has type button by default.
    /// </summary>
    [Fact]
    public void Button_HasTypeButtonByDefault()
    {
        // Arrange & Act
        var cut = Render<Button>(parameters => parameters
            .AddChildContent("Click me"));

        // Assert
        var button = cut.Find("button");
        Assert.Equal("button", button.GetAttribute("type"));
    }

    /// <summary>
    /// Verifies that the button can have type submit.
    /// </summary>
    [Fact]
    public void Button_CanHaveTypeSubmit()
    {
        // Arrange & Act
        var cut = Render<Button>(parameters => parameters
            .Add(p => p.Type, "submit")
            .AddChildContent("Submit"));

        // Assert
        var button = cut.Find("button");
        Assert.Equal("submit", button.GetAttribute("type"));
    }

    /// <summary>
    /// Verifies that the button renders with btn-block class when IsBlock is true.
    /// </summary>
    [Fact]
    public void Button_RendersBlockClassWhenIsBlockIsTrue()
    {
        // Arrange & Act
        var cut = Render<Button>(parameters => parameters
            .Add(p => p.IsBlock, true)
            .AddChildContent("Full Width"));

        // Assert
        var button = cut.Find("button");
        Assert.Contains("btn-block", button.ClassList);
    }

    /// <summary>
    /// Verifies that additional attributes are passed through to the button element.
    /// </summary>
    [Fact]
    public void Button_PassesThroughAdditionalAttributes()
    {
        // Arrange & Act
        var cut = Render<Button>(parameters => parameters
            .Add(p => p.AdditionalAttributes, new Dictionary<string, object>
            {
                { "aria-label", "Close dialog" },
                { "data-testid", "close-btn" },
            })
            .AddChildContent("Close"));

        // Assert
        var button = cut.Find("button");
        Assert.Equal("Close dialog", button.GetAttribute("aria-label"));
        Assert.Equal("close-btn", button.GetAttribute("data-testid"));
    }

    /// <summary>
    /// Verifies that the button renders child content correctly.
    /// </summary>
    [Fact]
    public void Button_RendersChildContent()
    {
        // Arrange & Act
        var cut = Render<Button>(parameters => parameters
            .AddChildContent("Save Changes"));

        // Assert
        Assert.Contains("Save Changes", cut.Markup);
    }

    /// <summary>
    /// Verifies that the button renders with warning variant.
    /// </summary>
    [Fact]
    public void Button_RendersWithWarningVariant()
    {
        // Arrange & Act
        var cut = Render<Button>(parameters => parameters
            .Add(p => p.Variant, ButtonVariant.Warning)
            .AddChildContent("Warning"));

        // Assert
        var button = cut.Find("button");
        Assert.Contains("btn-warning", button.ClassList);
    }

    /// <summary>
    /// Verifies that the button renders with outline variant.
    /// </summary>
    [Fact]
    public void Button_RendersWithOutlineVariant()
    {
        // Arrange & Act
        var cut = Render<Button>(parameters => parameters
            .Add(p => p.Variant, ButtonVariant.Outline)
            .AddChildContent("Outline"));

        // Assert
        var button = cut.Find("button");
        Assert.Contains("btn-outline", button.ClassList);
    }
}
