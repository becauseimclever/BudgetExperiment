// <copyright file="CardTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using Bunit;

using BudgetExperiment.Client.Components.Common;

namespace BudgetExperiment.Client.Tests.Components.Common;

/// <summary>
/// Unit tests for the Card component.
/// </summary>
public class CardTests : BunitContext
{
    /// <summary>
    /// Verifies that the card renders with body content.
    /// </summary>
    [Fact]
    public void Card_RendersBodyContent()
    {
        // Arrange & Act
        var cut = Render<Card>(parameters => parameters
            .AddChildContent("Card body content"));

        // Assert
        var card = cut.Find("div.card");
        Assert.NotNull(card);
        var body = cut.Find(".card-body");
        Assert.Contains("Card body content", body.InnerHtml);
    }

    /// <summary>
    /// Verifies that the card renders with title.
    /// </summary>
    [Fact]
    public void Card_RendersTitle()
    {
        // Arrange & Act
        var cut = Render<Card>(parameters => parameters
            .Add(p => p.Title, "Card Title")
            .AddChildContent("Content"));

        // Assert
        var title = cut.Find(".card-title");
        Assert.Equal("Card Title", title.TextContent);
    }

    /// <summary>
    /// Verifies that the card renders with title and subtitle.
    /// </summary>
    [Fact]
    public void Card_RendersTitleAndSubtitle()
    {
        // Arrange & Act
        var cut = Render<Card>(parameters => parameters
            .Add(p => p.Title, "Card Title")
            .Add(p => p.Subtitle, "Card subtitle text")
            .AddChildContent("Content"));

        // Assert
        var title = cut.Find(".card-title");
        var subtitle = cut.Find(".card-subtitle");
        Assert.Equal("Card Title", title.TextContent);
        Assert.Equal("Card subtitle text", subtitle.TextContent);
    }

    /// <summary>
    /// Verifies that subtitle is not rendered without title.
    /// </summary>
    [Fact]
    public void Card_DoesNotRenderSubtitleWithoutTitle()
    {
        // Arrange & Act
        var cut = Render<Card>(parameters => parameters
            .Add(p => p.Subtitle, "Orphan subtitle")
            .AddChildContent("Content"));

        // Assert
        Assert.Empty(cut.FindAll(".card-subtitle"));
    }

    /// <summary>
    /// Verifies that custom header content takes precedence over title.
    /// </summary>
    [Fact]
    public void Card_HeaderContentTakesPrecedenceOverTitle()
    {
        // Arrange & Act
        var cut = Render<Card>(parameters => parameters
            .Add(p => p.Title, "Should not appear")
            .Add(p => p.HeaderContent, builder =>
            {
                builder.AddContent(0, "Custom Header");
            })
            .AddChildContent("Content"));

        // Assert
        var header = cut.Find(".card-header");
        Assert.Contains("Custom Header", header.InnerHtml);
        Assert.DoesNotContain("Should not appear", header.InnerHtml);
    }

    /// <summary>
    /// Verifies that the card renders footer content.
    /// </summary>
    [Fact]
    public void Card_RendersFooterContent()
    {
        // Arrange & Act
        var cut = Render<Card>(parameters => parameters
            .Add(p => p.FooterContent, builder =>
            {
                builder.AddContent(0, "Footer content");
            })
            .AddChildContent("Body content"));

        // Assert
        var footer = cut.Find(".card-footer");
        Assert.Contains("Footer content", footer.InnerHtml);
    }

    /// <summary>
    /// Verifies that the card does not render header when neither title nor HeaderContent provided.
    /// </summary>
    [Fact]
    public void Card_DoesNotRenderHeaderWhenNotProvided()
    {
        // Arrange & Act
        var cut = Render<Card>(parameters => parameters
            .AddChildContent("Content only"));

        // Assert
        Assert.Empty(cut.FindAll(".card-header"));
    }

    /// <summary>
    /// Verifies that the card does not render footer when not provided.
    /// </summary>
    [Fact]
    public void Card_DoesNotRenderFooterWhenNotProvided()
    {
        // Arrange & Act
        var cut = Render<Card>(parameters => parameters
            .AddChildContent("Content only"));

        // Assert
        Assert.Empty(cut.FindAll(".card-footer"));
    }

    /// <summary>
    /// Verifies that additional classes are applied.
    /// </summary>
    [Fact]
    public void Card_AppliesAdditionalClasses()
    {
        // Arrange & Act
        var cut = Render<Card>(parameters => parameters
            .Add(p => p.AdditionalClasses, "custom-class highlight")
            .AddChildContent("Content"));

        // Assert
        var card = cut.Find("div.card");
        Assert.Contains("custom-class", card.ClassList);
        Assert.Contains("highlight", card.ClassList);
    }

    /// <summary>
    /// Verifies that additional attributes are passed through.
    /// </summary>
    [Fact]
    public void Card_PassesThroughAdditionalAttributes()
    {
        // Arrange & Act
        var cut = Render<Card>(parameters => parameters
            .Add(p => p.AdditionalAttributes, new Dictionary<string, object>
            {
                { "data-testid", "account-card" },
                { "role", "region" },
            })
            .AddChildContent("Content"));

        // Assert
        var card = cut.Find("div.card");
        Assert.Equal("account-card", card.GetAttribute("data-testid"));
        Assert.Equal("region", card.GetAttribute("role"));
    }

    /// <summary>
    /// Verifies that the card renders all sections together.
    /// </summary>
    [Fact]
    public void Card_RendersAllSections()
    {
        // Arrange & Act
        var cut = Render<Card>(parameters => parameters
            .Add(p => p.Title, "Full Card")
            .Add(p => p.Subtitle, "With all sections")
            .Add(p => p.FooterContent, builder =>
            {
                builder.AddContent(0, "Action buttons");
            })
            .AddChildContent("Main content area"));

        // Assert
        Assert.Single(cut.FindAll(".card-header"));
        Assert.Single(cut.FindAll(".card-body"));
        Assert.Single(cut.FindAll(".card-footer"));
        Assert.Contains("Full Card", cut.Markup);
        Assert.Contains("With all sections", cut.Markup);
        Assert.Contains("Main content area", cut.Markup);
        Assert.Contains("Action buttons", cut.Markup);
    }
}
