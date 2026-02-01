// <copyright file="ThemedIconRegistryTests.cs" company="Budget Experiment">
// Copyright (c) Budget Experiment. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Services;

using Shouldly;

namespace BudgetExperiment.Client.Tests.Services;

/// <summary>
/// Unit tests for <see cref="ThemedIconRegistry"/>.
/// </summary>
public class ThemedIconRegistryTests
{
    /// <summary>
    /// Tests that a Windows 95 themed icon is returned for a registered icon name.
    /// </summary>
    [Fact]
    public void GetThemedIcon_Win95Theme_CalendarIcon_ReturnsThemedIcon()
    {
        // Arrange
        var theme = "win95";
        var iconName = "calendar";

        // Act
        var result = ThemedIconRegistry.GetThemedIcon(theme, iconName);

        // Assert
        result.ShouldBe("win95-calendar");
    }

    /// <summary>
    /// Tests that the original icon name is returned when theme has no custom icon.
    /// </summary>
    [Fact]
    public void GetThemedIcon_Win95Theme_UnknownIcon_ReturnsFallback()
    {
        // Arrange
        var theme = "win95";
        var iconName = "unknown-icon";

        // Act
        var result = ThemedIconRegistry.GetThemedIcon(theme, iconName);

        // Assert
        result.ShouldBe("unknown-icon");
    }

    /// <summary>
    /// Tests that fallback is returned for a theme without custom icons.
    /// </summary>
    [Fact]
    public void GetThemedIcon_LightTheme_AnyIcon_ReturnsFallback()
    {
        // Arrange
        var theme = "light";
        var iconName = "calendar";

        // Act
        var result = ThemedIconRegistry.GetThemedIcon(theme, iconName);

        // Assert
        result.ShouldBe("calendar");
    }

    /// <summary>
    /// Tests that icon name lookup is case-insensitive.
    /// </summary>
    [Fact]
    public void GetThemedIcon_CaseInsensitive_ReturnsThemedIcon()
    {
        // Arrange
        var theme = "win95";
        var iconName = "CALENDAR";

        // Act
        var result = ThemedIconRegistry.GetThemedIcon(theme, iconName);

        // Assert
        result.ShouldBe("win95-calendar");
    }

    /// <summary>
    /// Tests that empty theme returns fallback.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void GetThemedIcon_EmptyOrNullTheme_ReturnsFallback(string? theme)
    {
        // Arrange
        var iconName = "calendar";

        // Act
        var result = ThemedIconRegistry.GetThemedIcon(theme!, iconName);

        // Assert
        result.ShouldBe("calendar");
    }

    /// <summary>
    /// Tests that empty icon name returns empty.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void GetThemedIcon_EmptyOrNullIconName_ReturnsIconName(string? iconName)
    {
        // Arrange
        var theme = "win95";

        // Act
        var result = ThemedIconRegistry.GetThemedIcon(theme, iconName!);

        // Assert
        result.ShouldBe(iconName);
    }

    /// <summary>
    /// Tests that GeoCities theme returns correct themed icons.
    /// </summary>
    [Fact]
    public void GetThemedIcon_GeocitiesTheme_HomeIcon_ReturnsThemedIcon()
    {
        // Arrange
        var theme = "geocities";
        var iconName = "home";

        // Act
        var result = ThemedIconRegistry.GetThemedIcon(theme, iconName);

        // Assert
        result.ShouldBe("geo-home");
    }

    /// <summary>
    /// Tests that Crayon Box theme returns correct themed icons.
    /// </summary>
    [Fact]
    public void GetThemedIcon_CrayonsTheme_TagIcon_ReturnsThemedIcon()
    {
        // Arrange
        var theme = "crayons";
        var iconName = "tag";

        // Act
        var result = ThemedIconRegistry.GetThemedIcon(theme, iconName);

        // Assert
        result.ShouldBe("crayon-tag");
    }

    /// <summary>
    /// Tests that HasCustomIcons returns true for themes with custom icons.
    /// </summary>
    [Theory]
    [InlineData("win95", true)]
    [InlineData("geocities", true)]
    [InlineData("crayons", true)]
    [InlineData("light", false)]
    [InlineData("dark", false)]
    [InlineData("system", false)]
    public void HasCustomIcons_ReturnsExpectedResult(string theme, bool expected)
    {
        // Act
        var result = ThemedIconRegistry.HasCustomIcons(theme);

        // Assert
        result.ShouldBe(expected);
    }

    /// <summary>
    /// Tests that GetThemesWithCustomIcons returns expected themes.
    /// </summary>
    [Fact]
    public void GetThemesWithCustomIcons_ReturnsAllThemedThemes()
    {
        // Act
        var themes = ThemedIconRegistry.GetThemesWithCustomIcons().ToList();

        // Assert
        themes.ShouldContain("win95");
        themes.ShouldContain("geocities");
        themes.ShouldContain("crayons");
        themes.Count.ShouldBe(3);
    }

    /// <summary>
    /// Tests all Windows 95 icons are registered and accessible.
    /// </summary>
    [Theory]
    [InlineData("calendar", "win95-calendar")]
    [InlineData("home", "win95-home")]
    [InlineData("settings", "win95-settings")]
    [InlineData("refresh", "win95-refresh")]
    [InlineData("plus", "win95-plus")]
    [InlineData("wallet", "win95-wallet")]
    [InlineData("upload", "win95-upload")]
    [InlineData("sparkles", "win95-sparkles")]
    public void GetThemedIcon_Win95AllRegisteredIcons_ReturnsCorrectThemedIcon(
        string iconName,
        string expectedThemedIcon)
    {
        // Act
        var result = ThemedIconRegistry.GetThemedIcon("win95", iconName);

        // Assert
        result.ShouldBe(expectedThemedIcon);
    }
}
