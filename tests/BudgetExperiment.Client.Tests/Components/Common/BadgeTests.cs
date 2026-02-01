// <copyright file="BadgeTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using Bunit;

using BudgetExperiment.Client.Components.Common;

namespace BudgetExperiment.Client.Tests.Components.Common;

/// <summary>
/// Unit tests for the Badge component.
/// </summary>
public class BadgeTests : BunitContext
{
    /// <summary>
    /// Verifies that the badge renders with default variant.
    /// </summary>
    [Fact]
    public void Badge_RendersWithDefaultVariant()
    {
        // Arrange & Act
        var cut = Render<Badge>(parameters => parameters
            .Add(p => p.Text, "Status"));

        // Assert
        var badge = cut.Find("span.badge");
        Assert.Contains("badge-default", badge.ClassList);
    }

    /// <summary>
    /// Verifies that the badge renders with success variant.
    /// </summary>
    [Fact]
    public void Badge_RendersWithSuccessVariant()
    {
        // Arrange & Act
        var cut = Render<Badge>(parameters => parameters
            .Add(p => p.Variant, BadgeVariant.Success)
            .Add(p => p.Text, "Active"));

        // Assert
        var badge = cut.Find("span.badge");
        Assert.Contains("badge-success", badge.ClassList);
    }

    /// <summary>
    /// Verifies that the badge renders with warning variant.
    /// </summary>
    [Fact]
    public void Badge_RendersWithWarningVariant()
    {
        // Arrange & Act
        var cut = Render<Badge>(parameters => parameters
            .Add(p => p.Variant, BadgeVariant.Warning)
            .Add(p => p.Text, "Pending"));

        // Assert
        var badge = cut.Find("span.badge");
        Assert.Contains("badge-warning", badge.ClassList);
    }

    /// <summary>
    /// Verifies that the badge renders with danger variant.
    /// </summary>
    [Fact]
    public void Badge_RendersWithDangerVariant()
    {
        // Arrange & Act
        var cut = Render<Badge>(parameters => parameters
            .Add(p => p.Variant, BadgeVariant.Danger)
            .Add(p => p.Text, "Error"));

        // Assert
        var badge = cut.Find("span.badge");
        Assert.Contains("badge-danger", badge.ClassList);
    }

    /// <summary>
    /// Verifies that the badge renders with info variant.
    /// </summary>
    [Fact]
    public void Badge_RendersWithInfoVariant()
    {
        // Arrange & Act
        var cut = Render<Badge>(parameters => parameters
            .Add(p => p.Variant, BadgeVariant.Info)
            .Add(p => p.Text, "New"));

        // Assert
        var badge = cut.Find("span.badge");
        Assert.Contains("badge-info", badge.ClassList);
    }

    /// <summary>
    /// Verifies that the badge renders text content.
    /// </summary>
    [Fact]
    public void Badge_RendersTextContent()
    {
        // Arrange & Act
        var cut = Render<Badge>(parameters => parameters
            .Add(p => p.Text, "Completed"));

        // Assert
        Assert.Contains("Completed", cut.Markup);
    }

    /// <summary>
    /// Verifies that the badge renders child content.
    /// </summary>
    [Fact]
    public void Badge_RendersChildContent()
    {
        // Arrange & Act
        var cut = Render<Badge>(parameters => parameters
            .AddChildContent("Custom Content"));

        // Assert
        Assert.Contains("Custom Content", cut.Markup);
    }

    /// <summary>
    /// Verifies that child content takes precedence over Text.
    /// </summary>
    [Fact]
    public void Badge_ChildContentTakesPrecedenceOverText()
    {
        // Arrange & Act
        var cut = Render<Badge>(parameters => parameters
            .Add(p => p.Text, "Text Content")
            .AddChildContent("Child Content"));

        // Assert
        Assert.Contains("Child Content", cut.Markup);
        Assert.DoesNotContain("Text Content", cut.Markup);
    }

    /// <summary>
    /// Verifies that the badge renders with small size class.
    /// </summary>
    [Fact]
    public void Badge_RendersWithSmallSize()
    {
        // Arrange & Act
        var cut = Render<Badge>(parameters => parameters
            .Add(p => p.Size, BadgeSize.Small)
            .Add(p => p.Text, "Small"));

        // Assert
        var badge = cut.Find("span.badge");
        Assert.Contains("badge-sm", badge.ClassList);
    }

    /// <summary>
    /// Verifies that the badge renders with large size class.
    /// </summary>
    [Fact]
    public void Badge_RendersWithLargeSize()
    {
        // Arrange & Act
        var cut = Render<Badge>(parameters => parameters
            .Add(p => p.Size, BadgeSize.Large)
            .Add(p => p.Text, "Large"));

        // Assert
        var badge = cut.Find("span.badge");
        Assert.Contains("badge-lg", badge.ClassList);
    }

    /// <summary>
    /// Verifies that the badge renders with medium size (no extra class).
    /// </summary>
    [Fact]
    public void Badge_RendersWithMediumSizeNoExtraClass()
    {
        // Arrange & Act
        var cut = Render<Badge>(parameters => parameters
            .Add(p => p.Size, BadgeSize.Medium)
            .Add(p => p.Text, "Medium"));

        // Assert
        var badge = cut.Find("span.badge");
        Assert.DoesNotContain("badge-sm", badge.ClassList);
        Assert.DoesNotContain("badge-lg", badge.ClassList);
    }

    /// <summary>
    /// Verifies that additional attributes are passed through.
    /// </summary>
    [Fact]
    public void Badge_PassesThroughAdditionalAttributes()
    {
        // Arrange & Act
        var cut = Render<Badge>(parameters => parameters
            .Add(p => p.Text, "Test")
            .Add(p => p.AdditionalAttributes, new Dictionary<string, object>
            {
                { "title", "Badge tooltip" },
                { "data-testid", "status-badge" },
            }));

        // Assert
        var badge = cut.Find("span.badge");
        Assert.Equal("Badge tooltip", badge.GetAttribute("title"));
        Assert.Equal("status-badge", badge.GetAttribute("data-testid"));
    }
}
