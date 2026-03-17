// <copyright file="ConfidenceBadgeTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Components.Reconciliation;
using BudgetExperiment.Client.Services;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace BudgetExperiment.Client.Tests.Components.Reconciliation;

/// <summary>
/// Unit tests for the <see cref="ConfidenceBadge"/> component.
/// </summary>
public sealed class ConfidenceBadgeTests : BunitContext, IAsyncLifetime
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConfidenceBadgeTests"/> class.
    /// </summary>
    public ConfidenceBadgeTests()
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
    /// Verifies the badge renders with confidence-badge class.
    /// </summary>
    [Fact]
    public void ConfidenceBadge_RendersWithCorrectClass()
    {
        var cut = Render<ConfidenceBadge>(parameters => parameters
            .Add(p => p.Score, 0.9m)
            .Add(p => p.Level, "High"));

        Assert.Contains("confidence-badge", cut.Markup);
    }

    /// <summary>
    /// Verifies the badge shows the score when ShowScore is true.
    /// </summary>
    [Fact]
    public void ConfidenceBadge_ShowsScore_WhenShowScoreIsTrue()
    {
        var cut = Render<ConfidenceBadge>(parameters => parameters
            .Add(p => p.Score, 0.85m)
            .Add(p => p.Level, "High")
            .Add(p => p.ShowScore, true));

        Assert.Contains("85", cut.Markup);
    }

    /// <summary>
    /// Verifies the badge hides the score when ShowScore is false.
    /// </summary>
    [Fact]
    public void ConfidenceBadge_HidesScore_WhenShowScoreIsFalse()
    {
        var cut = Render<ConfidenceBadge>(parameters => parameters
            .Add(p => p.Score, 0.85m)
            .Add(p => p.Level, "High")
            .Add(p => p.ShowScore, false));

        var scoreElements = cut.FindAll(".score");
        Assert.Empty(scoreElements);
    }

    /// <summary>
    /// Verifies the badge shows the level text when ShowLevel is true.
    /// </summary>
    [Fact]
    public void ConfidenceBadge_ShowsLevel_WhenShowLevelIsTrue()
    {
        var cut = Render<ConfidenceBadge>(parameters => parameters
            .Add(p => p.Score, 0.9m)
            .Add(p => p.Level, "High")
            .Add(p => p.ShowLevel, true));

        Assert.Contains("High", cut.Markup);
    }

    /// <summary>
    /// Verifies the badge hides the level text when ShowLevel is false.
    /// </summary>
    [Fact]
    public void ConfidenceBadge_HidesLevel_WhenShowLevelIsFalse()
    {
        var cut = Render<ConfidenceBadge>(parameters => parameters
            .Add(p => p.Score, 0.9m)
            .Add(p => p.Level, "High")
            .Add(p => p.ShowLevel, false));

        var levelElements = cut.FindAll(".level");
        Assert.Empty(levelElements);
    }

    /// <summary>
    /// Verifies the badge applies the high CSS class for High level.
    /// </summary>
    [Fact]
    public void ConfidenceBadge_AppliesHighClass_ForHighLevel()
    {
        var cut = Render<ConfidenceBadge>(parameters => parameters
            .Add(p => p.Score, 0.9m)
            .Add(p => p.Level, "High"));

        var badge = cut.Find(".confidence-badge");
        Assert.Contains("high", badge.ClassList);
    }

    /// <summary>
    /// Verifies the badge applies the medium CSS class for Medium level.
    /// </summary>
    [Fact]
    public void ConfidenceBadge_AppliesMediumClass_ForMediumLevel()
    {
        var cut = Render<ConfidenceBadge>(parameters => parameters
            .Add(p => p.Score, 0.6m)
            .Add(p => p.Level, "Medium"));

        var badge = cut.Find(".confidence-badge");
        Assert.Contains("medium", badge.ClassList);
    }

    /// <summary>
    /// Verifies the badge applies the low CSS class for Low level.
    /// </summary>
    [Fact]
    public void ConfidenceBadge_AppliesLowClass_ForLowLevel()
    {
        var cut = Render<ConfidenceBadge>(parameters => parameters
            .Add(p => p.Score, 0.3m)
            .Add(p => p.Level, "Low"));

        var badge = cut.Find(".confidence-badge");
        Assert.Contains("low", badge.ClassList);
    }

    /// <summary>
    /// Verifies the badge applies the small size class.
    /// </summary>
    [Fact]
    public void ConfidenceBadge_AppliesSmSize_ForSmallSize()
    {
        var cut = Render<ConfidenceBadge>(parameters => parameters
            .Add(p => p.Score, 0.9m)
            .Add(p => p.Level, "High")
            .Add(p => p.Size, "small"));

        var badge = cut.Find(".confidence-badge");
        Assert.Contains("badge-sm", badge.ClassList);
    }

    /// <summary>
    /// Verifies the badge applies the large size class.
    /// </summary>
    [Fact]
    public void ConfidenceBadge_AppliesLgSize_ForLargeSize()
    {
        var cut = Render<ConfidenceBadge>(parameters => parameters
            .Add(p => p.Score, 0.9m)
            .Add(p => p.Level, "High")
            .Add(p => p.Size, "large"));

        var badge = cut.Find(".confidence-badge");
        Assert.Contains("badge-lg", badge.ClassList);
    }

    /// <summary>
    /// Verifies the tooltip contains score and level information.
    /// </summary>
    [Fact]
    public void ConfidenceBadge_HasTooltipWithScoreAndLevel()
    {
        var cut = Render<ConfidenceBadge>(parameters => parameters
            .Add(p => p.Score, 0.92m)
            .Add(p => p.Level, "High"));

        Assert.Contains("Confidence:", cut.Markup);
        Assert.Contains("High", cut.Markup);
    }
}
