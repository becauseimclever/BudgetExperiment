// <copyright file="OnboardingPageTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Pages;
using BudgetExperiment.Client.Services;
using BudgetExperiment.Client.Tests.TestHelpers;
using BudgetExperiment.Client.ViewModels;

using Bunit;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace BudgetExperiment.Client.Tests.Pages;

/// <summary>
/// Unit tests for the <see cref="Onboarding"/> page component.
/// </summary>
public class OnboardingPageTests : BunitContext, IAsyncLifetime
{
    private readonly StubBudgetApiService _apiService = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="OnboardingPageTests"/> class.
    /// </summary>
    public OnboardingPageTests()
    {
        this.JSInterop.Mode = JSRuntimeMode.Loose;
        this.Services.AddSingleton<IBudgetApiService>(_apiService);
        this.Services.AddSingleton<ThemeService>();
        this.Services.AddSingleton<CultureService>();
        this.Services.AddSingleton<IExportDownloadService>(new StubExportDownloadService());
        this.Services.AddSingleton<IToastService>(new ToastService());
        this.Services.AddSingleton<IApiErrorContext>(new ApiErrorContext());
        this.Services.AddTransient<OnboardingViewModel>();
    }

    /// <inheritdoc/>
    public Task InitializeAsync() => Task.CompletedTask;

    /// <inheritdoc/>
    public new Task DisposeAsync()
    {
        return base.DisposeAsync().AsTask();
    }

    /// <summary>
    /// Verifies the page renders without errors.
    /// </summary>
    [Fact]
    public void Renders_WithoutErrors()
    {
        var cut = Render<Onboarding>();

        cut.Markup.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies the welcome title is shown on step 1.
    /// </summary>
    [Fact]
    public void Step1_ShowsWelcomeTitle()
    {
        var cut = Render<Onboarding>();

        cut.Markup.ShouldContain("Welcome to Budget Experiment");
    }

    /// <summary>
    /// Verifies the Get Started button is present on step 1.
    /// </summary>
    [Fact]
    public void Step1_HasGetStartedButton()
    {
        var cut = Render<Onboarding>();

        cut.Markup.ShouldContain("Get Started");
    }

    /// <summary>
    /// Verifies the Skip setup button is present on step 1.
    /// </summary>
    [Fact]
    public void Step1_HasSkipSetupButton()
    {
        var cut = Render<Onboarding>();

        cut.Markup.ShouldContain("Skip setup");
    }

    /// <summary>
    /// Verifies step indicators are present.
    /// </summary>
    [Fact]
    public void HasStepIndicators()
    {
        var cut = Render<Onboarding>();

        cut.Markup.ShouldContain("onboarding-steps");
        cut.Markup.ShouldContain("onboarding-step-dot");
    }

    /// <summary>
    /// Verifies clicking Get Started advances to step 2 (currency).
    /// </summary>
    [Fact]
    public void ClickGetStarted_AdvancesToStep2()
    {
        var cut = Render<Onboarding>();

        cut.Find(".btn-primary").Click();

        cut.Markup.ShouldContain("Choose Your Currency");
    }

    /// <summary>
    /// Verifies step 2 shows currency search field.
    /// </summary>
    [Fact]
    public void Step2_ShowsCurrencySearch()
    {
        var cut = Render<Onboarding>();
        cut.Find(".btn-primary").Click();

        cut.Markup.ShouldContain("Currency");
        cut.Markup.ShouldContain("Search currencies");
    }

    /// <summary>
    /// Verifies step 2 shows Back and Next buttons.
    /// </summary>
    [Fact]
    public void Step2_HasBackAndNextButtons()
    {
        var cut = Render<Onboarding>();
        cut.Find(".btn-primary").Click();

        cut.Markup.ShouldContain("Back");
        cut.Markup.ShouldContain("Next");
    }

    /// <summary>
    /// Verifies clicking Next from step 2 advances to step 3 (first day of week).
    /// </summary>
    [Fact]
    public void Step2_ClickNext_AdvancesToStep3()
    {
        var cut = Render<Onboarding>();

        // Step 1 → Step 2
        cut.Find(".btn-primary").Click();

        // Step 2 → Step 3
        cut.Find(".btn-primary").Click();

        cut.Markup.ShouldContain("First Day of the Week");
    }

    /// <summary>
    /// Verifies step 3 shows day of week toggles.
    /// </summary>
    [Fact]
    public void Step3_ShowsDayOfWeekToggles()
    {
        var cut = Render<Onboarding>();
        cut.Find(".btn-primary").Click();
        cut.Find(".btn-primary").Click();

        cut.Markup.ShouldContain("Sunday");
        cut.Markup.ShouldContain("Monday");
    }

    /// <summary>
    /// Verifies clicking Next from step 3 advances to step 4 (summary).
    /// </summary>
    [Fact]
    public void Step3_ClickNext_AdvancesToStep4()
    {
        var cut = Render<Onboarding>();
        cut.Find(".btn-primary").Click();
        cut.Find(".btn-primary").Click();
        cut.Find(".btn-primary").Click();

        cut.Markup.ShouldContain("You're Almost There!");
    }

    /// <summary>
    /// Verifies step 4 shows summary with currency and day of week.
    /// </summary>
    [Fact]
    public void Step4_ShowsSummary()
    {
        var cut = Render<Onboarding>();
        cut.Find(".btn-primary").Click();
        cut.Find(".btn-primary").Click();
        cut.Find(".btn-primary").Click();

        cut.Markup.ShouldContain("onboarding-summary");
        cut.Markup.ShouldContain("Currency");
        cut.Markup.ShouldContain("Week starts on");
        cut.Markup.ShouldContain("Sunday");
    }

    /// <summary>
    /// Verifies pressing Back from step 2 returns to step 1.
    /// </summary>
    [Fact]
    public void Step2_ClickBack_ReturnsToStep1()
    {
        var cut = Render<Onboarding>();
        cut.Find(".btn-primary").Click();

        // Click the Back button
        cut.Find(".btn-secondary").Click();

        cut.Markup.ShouldContain("Welcome to Budget Experiment");
    }

    /// <summary>
    /// Verifies step 4 (summary) has a Next button that advances to step 5.
    /// </summary>
    [Fact]
    public void Step4_HasNextButton()
    {
        var cut = Render<Onboarding>();
        cut.Find(".btn-primary").Click();
        cut.Find(".btn-primary").Click();
        cut.Find(".btn-primary").Click();

        cut.Markup.ShouldContain("Next");
    }
}
