// <copyright file="PageHeaderTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Components.Common;

using Bunit;

namespace BudgetExperiment.Client.Tests.Components.Common;

/// <summary>
/// Unit tests for the <see cref="PageHeader"/> component.
/// </summary>
public sealed class PageHeaderTests : BunitContext
{
    /// <summary>
    /// Verifies that the page header renders the title.
    /// </summary>
    [Fact]
    public void PageHeader_RendersTitle()
    {
        // Arrange & Act
        var cut = Render<PageHeader>(parameters => parameters
            .Add(p => p.Title, "Accounts"));

        // Assert
        var title = cut.Find(".page-header-title");
        Assert.Equal("Accounts", title.TextContent);
    }

    /// <summary>
    /// Verifies that the page header renders the subtitle when provided.
    /// </summary>
    [Fact]
    public void PageHeader_RendersSubtitle_WhenProvided()
    {
        // Arrange & Act
        var cut = Render<PageHeader>(parameters => parameters
            .Add(p => p.Title, "Accounts")
            .Add(p => p.Subtitle, "Manage your accounts"));

        // Assert
        var subtitle = cut.Find(".page-header-subtitle");
        Assert.Equal("Manage your accounts", subtitle.TextContent);
    }

    /// <summary>
    /// Verifies that the subtitle is not rendered when not provided.
    /// </summary>
    [Fact]
    public void PageHeader_DoesNotRenderSubtitle_WhenNotProvided()
    {
        // Arrange & Act
        var cut = Render<PageHeader>(parameters => parameters
            .Add(p => p.Title, "Accounts"));

        // Assert
        Assert.Empty(cut.FindAll(".page-header-subtitle"));
    }

    /// <summary>
    /// Verifies that the back button is shown when ShowBackButton is true.
    /// </summary>
    [Fact]
    public void PageHeader_ShowsBackButton_WhenEnabled()
    {
        // Arrange & Act
        var cut = Render<PageHeader>(parameters => parameters
            .Add(p => p.Title, "Details")
            .Add(p => p.ShowBackButton, true));

        // Assert
        var buttons = cut.FindAll("button");
        Assert.Contains(buttons, b => b.TextContent.Contains("Back"));
    }

    /// <summary>
    /// Verifies that the back button is hidden when ShowBackButton is false.
    /// </summary>
    [Fact]
    public void PageHeader_HidesBackButton_WhenDisabled()
    {
        // Arrange & Act
        var cut = Render<PageHeader>(parameters => parameters
            .Add(p => p.Title, "Accounts")
            .Add(p => p.ShowBackButton, false));

        // Assert
        var buttons = cut.FindAll("button");
        Assert.DoesNotContain(buttons, b => b.TextContent.Contains("Back"));
    }

    /// <summary>
    /// Verifies that clicking the back button fires the OnBack callback.
    /// </summary>
    [Fact]
    public void PageHeader_ClickBack_FiresOnBack()
    {
        // Arrange
        var backClicked = false;
        var cut = Render<PageHeader>(parameters => parameters
            .Add(p => p.Title, "Details")
            .Add(p => p.ShowBackButton, true)
            .Add(p => p.OnBack, () => { backClicked = true; }));

        // Act
        var backButton = cut.FindAll("button").First(b => b.TextContent.Contains("Back"));
        backButton.Click();

        // Assert
        Assert.True(backClicked);
    }

    /// <summary>
    /// Verifies that the actions section renders when Actions content is provided.
    /// </summary>
    [Fact]
    public void PageHeader_RendersActions_WhenProvided()
    {
        // Arrange & Act
        var cut = Render<PageHeader>(parameters => parameters
            .Add(p => p.Title, "Accounts")
            .Add(p => p.Actions, "<button class=\"btn-add\">Add Account</button>"));

        // Assert
        var actions = cut.Find(".page-header-actions");
        Assert.Contains("Add Account", actions.TextContent);
    }

    /// <summary>
    /// Verifies that the actions section is not rendered when Actions is null.
    /// </summary>
    [Fact]
    public void PageHeader_DoesNotRenderActions_WhenNotProvided()
    {
        // Arrange & Act
        var cut = Render<PageHeader>(parameters => parameters
            .Add(p => p.Title, "Accounts"));

        // Assert
        Assert.Empty(cut.FindAll(".page-header-actions"));
    }

    /// <summary>
    /// Verifies that the header element has the correct CSS class.
    /// </summary>
    [Fact]
    public void PageHeader_HasCorrectCssClass()
    {
        // Arrange & Act
        var cut = Render<PageHeader>(parameters => parameters
            .Add(p => p.Title, "Test"));

        // Assert
        var header = cut.Find("header");
        Assert.Contains("page-header", header.ClassList);
    }
}
