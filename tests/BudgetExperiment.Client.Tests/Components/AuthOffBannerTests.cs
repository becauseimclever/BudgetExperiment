// <copyright file="AuthOffBannerTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Components.Auth;
using BudgetExperiment.Contracts.Dtos;
using Bunit;
using Microsoft.Extensions.DependencyInjection;

namespace BudgetExperiment.Client.Tests.Components;

/// <summary>
/// Unit tests for the <see cref="AuthOffBanner"/> component.
/// </summary>
public class AuthOffBannerTests : BunitContext
{
    [Fact]
    public void AuthOffBanner_ShowsBanner_WhenModeIsNone()
    {
        // Arrange
        Services.AddSingleton(new AuthenticationConfigDto { Mode = "none" });

        // Act
        var cut = Render<AuthOffBanner>();

        // Assert
        var banner = cut.Find(".auth-off-banner");
        Assert.NotNull(banner);
    }

    [Fact]
    public void AuthOffBanner_HidesBanner_WhenModeIsOidc()
    {
        // Arrange
        Services.AddSingleton(new AuthenticationConfigDto { Mode = "oidc" });

        // Act
        var cut = Render<AuthOffBanner>();

        // Assert
        Assert.Empty(cut.Markup.Trim());
    }

    [Fact]
    public void AuthOffBanner_ContainsWarningText()
    {
        // Arrange
        Services.AddSingleton(new AuthenticationConfigDto { Mode = "none" });

        // Act
        var cut = Render<AuthOffBanner>();

        // Assert
        Assert.Contains("demo mode", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("authentication disabled", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AuthOffBanner_ContainsDocumentationLink()
    {
        // Arrange
        Services.AddSingleton(new AuthenticationConfigDto { Mode = "none" });

        // Act
        var cut = Render<AuthOffBanner>();

        // Assert
        var link = cut.Find("a");
        Assert.Contains("AUTH-PROVIDERS.md", link.GetAttribute("href") ?? string.Empty);
        Assert.Equal("_blank", link.GetAttribute("target"));
        Assert.Contains("noopener", link.GetAttribute("rel") ?? string.Empty);
    }

    [Fact]
    public void AuthOffBanner_HasStatusRole()
    {
        // Arrange
        Services.AddSingleton(new AuthenticationConfigDto { Mode = "none" });

        // Act
        var cut = Render<AuthOffBanner>();

        // Assert
        var banner = cut.Find("[role='status']");
        Assert.NotNull(banner);
    }

    [Fact]
    public void AuthOffBanner_IsCaseInsensitive()
    {
        // Arrange — mode casing should not matter
        Services.AddSingleton(new AuthenticationConfigDto { Mode = "None" });

        // Act
        var cut = Render<AuthOffBanner>();

        // Assert
        var banner = cut.Find(".auth-off-banner");
        Assert.NotNull(banner);
    }

    [Fact]
    public void AuthOffBanner_HidesBanner_WhenModeIsOidcUpperCase()
    {
        // Arrange
        Services.AddSingleton(new AuthenticationConfigDto { Mode = "OIDC" });

        // Act
        var cut = Render<AuthOffBanner>();

        // Assert
        Assert.Empty(cut.Markup.Trim());
    }
}
