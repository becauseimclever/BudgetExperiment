// <copyright file="TrendIndicatorTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using Bunit;

using BudgetExperiment.Client.Components.Reports;
using BudgetExperiment.Client.Services;

using Microsoft.Extensions.DependencyInjection;

namespace BudgetExperiment.Client.Tests.Components.Reports;

/// <summary>
/// Unit tests for the TrendIndicator component.
/// </summary>
public class TrendIndicatorTests : BunitContext, IAsyncLifetime
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TrendIndicatorTests"/> class.
    /// </summary>
    public TrendIndicatorTests()
    {
        this.JSInterop.Mode = JSRuntimeMode.Loose;
        this.Services.AddSingleton<ThemeService>();
    }

    /// <inheritdoc/>
    public Task InitializeAsync() => Task.CompletedTask;

    /// <inheritdoc/>
    public new Task DisposeAsync() => base.DisposeAsync().AsTask();

    /// <summary>
    /// Verifies that an increase is shown with a positive class by default.
    /// </summary>
    [Fact]
    public void Indicator_ShowsPositiveClass_WhenValueIncreased()
    {
        // Arrange & Act
        var cut = Render<TrendIndicator>(parameters => parameters
            .Add(p => p.CurrentValue, 1200m)
            .Add(p => p.PreviousValue, 1000m));

        // Assert
        var indicator = cut.Find(".trend-indicator");
        Assert.Contains("trend-positive", indicator.ClassList);
        Assert.Contains("+20.0%", cut.Markup);
    }

    /// <summary>
    /// Verifies that a decrease is shown with a negative class by default.
    /// </summary>
    [Fact]
    public void Indicator_ShowsNegativeClass_WhenValueDecreased()
    {
        // Arrange & Act
        var cut = Render<TrendIndicator>(parameters => parameters
            .Add(p => p.CurrentValue, 800m)
            .Add(p => p.PreviousValue, 1000m));

        // Assert
        var indicator = cut.Find(".trend-indicator");
        Assert.Contains("trend-negative", indicator.ClassList);
        Assert.Contains("-20.0%", cut.Markup);
    }

    /// <summary>
    /// Verifies that InvertColors reverses the color logic (decrease = green for spending).
    /// </summary>
    [Fact]
    public void Indicator_InvertsColors_WhenInvertIsTrue()
    {
        // Arrange & Act - spending decreased (good)
        var cut = Render<TrendIndicator>(parameters => parameters
            .Add(p => p.CurrentValue, 800m)
            .Add(p => p.PreviousValue, 1000m)
            .Add(p => p.InvertColors, true));

        // Assert - decrease with inverted colors = positive (green)
        var indicator = cut.Find(".trend-indicator");
        Assert.Contains("trend-positive", indicator.ClassList);
    }

    /// <summary>
    /// Verifies that InvertColors makes an increase show as negative (red for spending).
    /// </summary>
    [Fact]
    public void Indicator_ShowsNegative_WhenInvertedAndIncreased()
    {
        // Arrange & Act - spending increased (bad)
        var cut = Render<TrendIndicator>(parameters => parameters
            .Add(p => p.CurrentValue, 1200m)
            .Add(p => p.PreviousValue, 1000m)
            .Add(p => p.InvertColors, true));

        // Assert - increase with inverted colors = negative (red)
        var indicator = cut.Find(".trend-indicator");
        Assert.Contains("trend-negative", indicator.ClassList);
    }

    /// <summary>
    /// Verifies that a small change (less than 0.5%) shows as stable.
    /// </summary>
    [Fact]
    public void Indicator_ShowsStable_WhenChangeIsMinimal()
    {
        // Arrange & Act
        var cut = Render<TrendIndicator>(parameters => parameters
            .Add(p => p.CurrentValue, 1002m)
            .Add(p => p.PreviousValue, 1000m));

        // Assert
        var indicator = cut.Find(".trend-indicator");
        Assert.Contains("trend-stable", indicator.ClassList);
    }

    /// <summary>
    /// Verifies that the correct label is displayed.
    /// </summary>
    [Fact]
    public void Indicator_ShowsCustomLabel()
    {
        // Arrange & Act
        var cut = Render<TrendIndicator>(parameters => parameters
            .Add(p => p.CurrentValue, 1500m)
            .Add(p => p.PreviousValue, 1000m)
            .Add(p => p.Label, "vs. last month"));

        // Assert
        Assert.Contains("vs. last month", cut.Markup);
    }

    /// <summary>
    /// Verifies that a "no previous data" message is shown when HasData is false.
    /// </summary>
    [Fact]
    public void Indicator_ShowsNoDataMessage_WhenHasDataIsFalse()
    {
        // Arrange & Act
        var cut = Render<TrendIndicator>(parameters => parameters
            .Add(p => p.CurrentValue, 1000m)
            .Add(p => p.PreviousValue, 0m)
            .Add(p => p.HasData, false));

        // Assert
        var indicator = cut.Find(".trend-indicator");
        Assert.Contains("trend-no-previous", indicator.ClassList);
        Assert.Contains("No previous data", cut.Markup);
    }

    /// <summary>
    /// Verifies that when previous value is zero and current is positive, it shows 100%.
    /// </summary>
    [Fact]
    public void Indicator_ShowsHundredPercent_WhenPreviousIsZero()
    {
        // Arrange & Act
        var cut = Render<TrendIndicator>(parameters => parameters
            .Add(p => p.CurrentValue, 500m)
            .Add(p => p.PreviousValue, 0m));

        // Assert
        Assert.Contains("+100.0%", cut.Markup);
    }

    /// <summary>
    /// Verifies that when both values are zero, it shows stable.
    /// </summary>
    [Fact]
    public void Indicator_ShowsStable_WhenBothValuesAreZero()
    {
        // Arrange & Act
        var cut = Render<TrendIndicator>(parameters => parameters
            .Add(p => p.CurrentValue, 0m)
            .Add(p => p.PreviousValue, 0m));

        // Assert
        var indicator = cut.Find(".trend-indicator");
        Assert.Contains("trend-stable", indicator.ClassList);
    }

    /// <summary>
    /// Verifies that the trending-up icon is used for increases.
    /// </summary>
    [Fact]
    public void Indicator_UsesTrendingUpIcon_WhenIncreasing()
    {
        // Arrange & Act
        var cut = Render<TrendIndicator>(parameters => parameters
            .Add(p => p.CurrentValue, 1500m)
            .Add(p => p.PreviousValue, 1000m));

        // Assert - Check for SVG polyline path used by trending-up icon
        Assert.Contains("polyline", cut.Markup);
    }

    /// <summary>
    /// Verifies that the accessible title attribute is present.
    /// </summary>
    [Fact]
    public void Indicator_HasAccessibleTitle()
    {
        // Arrange & Act
        var cut = Render<TrendIndicator>(parameters => parameters
            .Add(p => p.CurrentValue, 1200m)
            .Add(p => p.PreviousValue, 1000m)
            .Add(p => p.Label, "vs. last month"));

        // Assert
        var indicator = cut.Find(".trend-indicator");
        var title = indicator.GetAttribute("title");
        Assert.NotNull(title);
        Assert.Contains("20.0%", title);
        Assert.Contains("vs. last month", title);
    }
}
