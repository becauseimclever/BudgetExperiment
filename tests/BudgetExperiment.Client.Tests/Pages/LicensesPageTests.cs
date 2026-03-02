// <copyright file="LicensesPageTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Pages;
using BudgetExperiment.Client.Services;
using Bunit;
using Microsoft.Extensions.DependencyInjection;

namespace BudgetExperiment.Client.Tests.Pages;

/// <summary>
/// Unit tests for the Licenses page component.
/// </summary>
public class LicensesPageTests : BunitContext, IAsyncLifetime
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LicensesPageTests"/> class.
    /// </summary>
    public LicensesPageTests()
    {
        this.JSInterop.Mode = JSRuntimeMode.Loose;
        this.Services.AddSingleton<ThemeService>();
    }

    /// <inheritdoc/>
    public Task InitializeAsync() => Task.CompletedTask;

    /// <inheritdoc/>
    public new Task DisposeAsync() => base.DisposeAsync().AsTask();

    /// <summary>
    /// Verifies that the licenses page renders without errors.
    /// </summary>
    [Fact]
    public void LicensesPage_Renders_WithoutErrors()
    {
        // Act
        var cut = Render<Licenses>();

        // Assert
        Assert.NotNull(cut.Markup);
    }

    /// <summary>
    /// Verifies that the page title is set correctly.
    /// </summary>
    [Fact]
    public void LicensesPage_HasCorrectPageTitle()
    {
        // Act
        var cut = Render<Licenses>();

        // Assert - PageTitle component renders but bUnit captures it separately
        Assert.Contains("Open Source Licenses", cut.Markup);
    }

    /// <summary>
    /// Verifies that the Lucide Icons license entry is displayed.
    /// </summary>
    [Fact]
    public void LicensesPage_DisplaysLucideIconsLicense()
    {
        // Act
        var cut = Render<Licenses>();

        // Assert
        var heading = cut.Find(".license-component-name");
        Assert.Equal("Lucide Icons", heading.TextContent);
    }

    /// <summary>
    /// Verifies that the ISC license type badge is shown.
    /// </summary>
    [Fact]
    public void LicensesPage_ShowsLicenseTypeBadge()
    {
        // Act
        var cut = Render<Licenses>();

        // Assert
        var badge = cut.Find(".license-type-badge");
        Assert.Equal("ISC License", badge.TextContent);
    }

    /// <summary>
    /// Verifies that the full license text is available in a collapsible section.
    /// </summary>
    [Fact]
    public void LicensesPage_HasCollapsibleLicenseText()
    {
        // Act
        var cut = Render<Licenses>();

        // Assert
        var details = cut.Find(".license-text-details");
        Assert.NotNull(details);

        var summary = details.QuerySelector("summary");
        Assert.NotNull(summary);
        Assert.Contains("View full license text", summary!.TextContent);

        var licenseText = details.QuerySelector(".license-text");
        Assert.NotNull(licenseText);
        Assert.Contains("ISC License", licenseText!.TextContent);
        Assert.Contains("Cole Bemis", licenseText.TextContent);
    }

    /// <summary>
    /// Verifies that the intro text is present.
    /// </summary>
    [Fact]
    public void LicensesPage_DisplaysIntroText()
    {
        // Act
        var cut = Render<Licenses>();

        // Assert
        var intro = cut.Find(".licenses-intro");
        Assert.Contains("open source components", intro.TextContent);
    }

    /// <summary>
    /// Verifies that the source link points to the correct GitHub repository.
    /// </summary>
    [Fact]
    public void LicensesPage_HasSourceLink()
    {
        // Act
        var cut = Render<Licenses>();

        // Assert
        var link = cut.Find(".license-source-link");
        Assert.Equal("https://github.com/lucide-icons/lucide/blob/main/LICENSE", link.GetAttribute("href"));
        Assert.Equal("_blank", link.GetAttribute("target"));
    }
}
