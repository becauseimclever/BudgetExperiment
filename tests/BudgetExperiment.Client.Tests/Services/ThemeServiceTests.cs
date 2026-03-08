// <copyright file="ThemeServiceTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Services;

using Microsoft.JSInterop;

namespace BudgetExperiment.Client.Tests.Services;

/// <summary>
/// Unit tests for the <see cref="ThemeService"/> class.
/// </summary>
public sealed class ThemeServiceTests : IDisposable
{
    private readonly ThemeService _sut;

    /// <summary>
    /// Initializes a new instance of the <see cref="ThemeServiceTests"/> class.
    /// </summary>
    public ThemeServiceTests()
    {
        _sut = new ThemeService(new StubJSRuntime());
    }

    /// <summary>
    /// Verifies that CurrentTheme defaults to "system".
    /// </summary>
    [Fact]
    public void CurrentTheme_BeforeInit_DefaultsToSystem()
    {
        Assert.Equal("system", _sut.CurrentTheme);
    }

    /// <summary>
    /// Verifies that AvailableThemes contains the expected number of themes.
    /// </summary>
    [Fact]
    public void AvailableThemes_ContainsExpectedCount()
    {
        Assert.Equal(10, ThemeService.AvailableThemes.Count);
    }

    /// <summary>
    /// Verifies that AvailableThemes includes system, light, and dark themes.
    /// </summary>
    [Fact]
    public void AvailableThemes_ContainsCoreThemes()
    {
        Assert.Contains(ThemeService.AvailableThemes, t => t.Value == "system");
        Assert.Contains(ThemeService.AvailableThemes, t => t.Value == "light");
        Assert.Contains(ThemeService.AvailableThemes, t => t.Value == "dark");
    }

    /// <summary>
    /// Verifies that SetThemeAsync updates the CurrentTheme property.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SetThemeAsync_UpdatesCurrentTheme()
    {
        // Act
        await _sut.SetThemeAsync("dark");

        // Assert
        Assert.Equal("dark", _sut.CurrentTheme);
    }

    /// <summary>
    /// Verifies that SetThemeAsync with null or empty defaults to "system".
    /// </summary>
    /// <param name="theme">The theme value to set.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task SetThemeAsync_NullOrEmpty_DefaultsToSystem(string? theme)
    {
        // Arrange — set to something else first
        await _sut.SetThemeAsync("dark");

        // Act
        await _sut.SetThemeAsync(theme!);

        // Assert
        Assert.Equal("system", _sut.CurrentTheme);
    }

    /// <summary>
    /// Verifies that SetThemeAsync fires the ThemeChanged event.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SetThemeAsync_FiresThemeChangedEvent()
    {
        // Arrange
        string? receivedTheme = null;
        _sut.ThemeChanged += theme => receivedTheme = theme;

        // Act
        await _sut.SetThemeAsync("vscode-dark");

        // Assert
        Assert.Equal("vscode-dark", receivedTheme);
    }

    /// <summary>
    /// Verifies that GetThemedIcon returns themed icon for matching theme.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetThemedIcon_WithMatchingTheme_ReturnsThemedName()
    {
        // Arrange — set a theme that has icon overrides
        await _sut.SetThemeAsync("win95");

        // Act
        var result = _sut.GetThemedIcon("calendar");

        // Assert
        Assert.Equal("win95-calendar", result);
    }

    /// <summary>
    /// Verifies that GetThemedIcon returns original icon when no override exists.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetThemedIcon_NoOverride_ReturnsOriginalName()
    {
        // Arrange
        await _sut.SetThemeAsync("light");

        // Act
        var result = _sut.GetThemedIcon("calendar");

        // Assert
        Assert.Equal("calendar", result);
    }

    /// <summary>
    /// Verifies Dispose can be called without error.
    /// </summary>
    [Fact]
    public void Dispose_DoesNotThrow()
    {
        var sut = new ThemeService(new StubJSRuntime());
        var ex = Record.Exception(() => sut.Dispose());
        Assert.Null(ex);
    }

    /// <summary>
    /// Verifies that AvailableThemes includes themed options.
    /// </summary>
    [Fact]
    public void AvailableThemes_IncludesThemedOptions()
    {
        Assert.Contains(ThemeService.AvailableThemes, t => t.Value == "win95");
        Assert.Contains(ThemeService.AvailableThemes, t => t.Value == "vscode-dark");
        Assert.Contains(ThemeService.AvailableThemes, t => t.Value == "geocities");
    }

    /// <summary>
    /// Verifies that each theme option has a label and icon.
    /// </summary>
    [Fact]
    public void AvailableThemes_AllHaveLabelAndIcon()
    {
        foreach (var theme in ThemeService.AvailableThemes)
        {
            Assert.False(string.IsNullOrEmpty(theme.Label));
            Assert.False(string.IsNullOrEmpty(theme.Icon));
        }
    }

    /// <summary>
    /// Verifies setting theme multiple times only keeps the last value.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SetThemeAsync_MultipleSets_KeepsLastValue()
    {
        await _sut.SetThemeAsync("dark");
        await _sut.SetThemeAsync("light");
        await _sut.SetThemeAsync("vscode-dark");

        Assert.Equal("vscode-dark", _sut.CurrentTheme);
    }

    /// <summary>
    /// Verifies that GetResolvedThemeAsync returns light when module is null.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetResolvedThemeAsync_WithoutModule_ReturnsLight()
    {
        var result = await _sut.GetResolvedThemeAsync();
        Assert.Equal("light", result);
    }

    /// <summary>
    /// Verifies that GetAccessibilityStateAsync returns defaults without module.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetAccessibilityStateAsync_WithoutModule_ReturnsDefaults()
    {
        var result = await _sut.GetAccessibilityStateAsync();
        Assert.False(result.IsAccessibilityPreferenceDetected);
        Assert.False(result.WasThemeAutoApplied);
        Assert.False(result.HasExplicitOverride);
    }

    /// <summary>
    /// Verifies GetThemedIcon for geocities theme.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetThemedIcon_GeocitiesTheme_ReturnsThemedIcon()
    {
        await _sut.SetThemeAsync("geocities");
        var result = _sut.GetThemedIcon("calendar");
        Assert.Equal("geo-calendar", result);
    }

    /// <summary>
    /// Verifies that ThemeChanged event listener can be removed.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ThemeChanged_UnsubscribedHandler_NotCalled()
    {
        var callCount = 0;
        void Handler(string theme) => callCount++;

        _sut.ThemeChanged += Handler;
        await _sut.SetThemeAsync("dark");
        Assert.Equal(1, callCount);

        _sut.ThemeChanged -= Handler;
        await _sut.SetThemeAsync("light");
        Assert.Equal(1, callCount);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _sut.Dispose();
    }

    /// <summary>
    /// Stub JavaScript runtime that returns defaults without actual JS interop.
    /// </summary>
    private sealed class StubJSRuntime : IJSRuntime
    {
        /// <inheritdoc/>
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            return new ValueTask<TValue>(default(TValue)!);
        }

        /// <inheritdoc/>
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            return new ValueTask<TValue>(default(TValue)!);
        }
    }
}
