// <copyright file="AiOnboardingPanelTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Components.AI;
using BudgetExperiment.Client.Services;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace BudgetExperiment.Client.Tests.Components.AI;

/// <summary>
/// Unit tests for the <see cref="AiOnboardingPanel"/> component.
/// </summary>
public sealed class AiOnboardingPanelTests : BunitContext, IAsyncLifetime
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AiOnboardingPanelTests"/> class.
    /// </summary>
    public AiOnboardingPanelTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<ThemeService>();
    }

    /// <inheritdoc/>
    public Task InitializeAsync() => Task.CompletedTask;

    /// <inheritdoc/>
    public new Task DisposeAsync() => base.DisposeAsync().AsTask();

    /// <summary>
    /// Verifies the panel renders the main heading.
    /// </summary>
    [Fact]
    public void AiOnboardingPanel_ShowsMainHeading()
    {
        var cut = Render<AiOnboardingPanel>();

        Assert.Contains("Get Started with AI-Powered Suggestions", cut.Markup);
    }

    /// <summary>
    /// Verifies all four onboarding steps are rendered.
    /// </summary>
    [Fact]
    public void AiOnboardingPanel_ShowsFourSteps()
    {
        var cut = Render<AiOnboardingPanel>();

        var steps = cut.FindAll(".onboarding-step");
        Assert.Equal(4, steps.Count);
    }

    /// <summary>
    /// Verifies the step content headings.
    /// </summary>
    [Fact]
    public void AiOnboardingPanel_ShowsStepHeadings()
    {
        var cut = Render<AiOnboardingPanel>();

        Assert.Contains("Configure AI Settings", cut.Markup);
        Assert.Contains("Import Transactions", cut.Markup);
        Assert.Contains("Run Analysis", cut.Markup);
        Assert.Contains("Review & Accept", cut.Markup);
    }

    /// <summary>
    /// Verifies the settings link is present.
    /// </summary>
    [Fact]
    public void AiOnboardingPanel_HasSettingsLink()
    {
        var cut = Render<AiOnboardingPanel>();

        var link = cut.Find("a.step-link");
        Assert.Contains("/ai/settings", link.GetAttribute("href") ?? string.Empty);
    }

    /// <summary>
    /// Verifies the features section is rendered.
    /// </summary>
    [Fact]
    public void AiOnboardingPanel_ShowsFeatureSection()
    {
        var cut = Render<AiOnboardingPanel>();

        Assert.Contains("What the AI Can Do", cut.Markup);
        Assert.Contains("New Rules", cut.Markup);
    }
}
