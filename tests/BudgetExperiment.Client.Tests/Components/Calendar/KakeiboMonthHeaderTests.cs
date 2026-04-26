// <copyright file="KakeiboMonthHeaderTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Globalization;

using BudgetExperiment.Client.Components.Calendar;
using BudgetExperiment.Client.Services;
using BudgetExperiment.Client.Tests.TestHelpers;
using BudgetExperiment.Contracts.Dtos;

using Bunit;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace BudgetExperiment.Client.Tests.Components.Calendar;

/// <summary>
/// Unit tests for the KakeiboMonthHeader component.
/// </summary>
public class KakeiboMonthHeaderTests : BunitContext, IAsyncLifetime
{
    private readonly StubFeatureFlagClientService _fakeFeatureFlags = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="KakeiboMonthHeaderTests"/> class.
    /// </summary>
    public KakeiboMonthHeaderTests()
    {
        this.JSInterop.Mode = JSRuntimeMode.Loose;
        this.Services.AddSingleton<ThemeService>();
        this.Services.AddSingleton<CultureService>();
        this.Services.AddSingleton<IFeatureFlagClientService>(_fakeFeatureFlags);
    }

    /// <inheritdoc/>
    public Task InitializeAsync()
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");

        // Enable the Kakeibo feature flag by default for most tests
        _fakeFeatureFlags.Flags["Kakeibo:MonthlyReflectionPrompts"] = true;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public new Task DisposeAsync() => base.DisposeAsync().AsTask();

    /// <summary>
    /// Verifies that KakeiboMonthHeader renders empty when feature flag is disabled.
    /// </summary>
    [Fact]
    public void KakeiboMonthHeader_DoesNotRender_WhenFeatureFlagDisabled()
    {
        // Arrange
        // Disable the feature flag to test the "no content" path
        _fakeFeatureFlags.Flags["Kakeibo:MonthlyReflectionPrompts"] = false;

        var savingsProgress = new SavingsProgressResponse
        {
            SavingsGoal = 500m,
            ActualSavings = 250m,
            ProgressPercentage = 50,
        };

        // Act
        var cut = Render<KakeiboMonthHeader>(p => p
            .Add(x => x.Year, 2026)
            .Add(x => x.Month, 2)
            .Add(x => x.SavingsProgress, savingsProgress));

        // Assert
        // The outer div should exist, but it should be empty (no inner content)
        var header = cut.Find(".kakeibo-month-header");
        header.ShouldNotBeNull();
        header.InnerHtml.ShouldBeEmpty();
    }

    /// <summary>
    /// Verifies that KakeiboMonthHeader shows CTA when no savings goal is set.
    /// </summary>
    [Fact]
    public void KakeiboMonthHeader_ShowsGoalCta_WhenNoGoal()
    {
        // Arrange
        // Act
        var cut = Render<KakeiboMonthHeader>(p => p
            .Add(x => x.Year, 2026)
            .Add(x => x.Month, 2)
            .Add(x => x.SavingsProgress, null));

        // Assert
        cut.Markup.ShouldContain("Set a savings goal for February 2026");
    }

    /// <summary>
    /// Verifies that OnOpenIntentionPrompt callback is invoked when CTA button is clicked.
    /// </summary>
    [Fact]
    public void KakeiboMonthHeader_InvokesOnOpenIntentionPrompt_WhenCtaClicked()
    {
        // Arrange
        var intentionPromptOpened = false;

        // Act
        var cut = Render<KakeiboMonthHeader>(p => p
            .Add(x => x.Year, 2026)
            .Add(x => x.Month, 2)
            .Add(x => x.SavingsProgress, null)
            .Add(x => x.OnOpenIntentionPrompt, EventCallback.Factory.Create(this, () => { intentionPromptOpened = true; })));

        var ctaButton = cut.Find(".btn-link");
        ctaButton.Click();

        // Assert
        intentionPromptOpened.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies that SavingsProgress displays when goal is set.
    /// </summary>
    [Fact]
    public void KakeiboMonthHeader_DisplaysSavingsProgress_WhenGoalSet()
    {
        // Arrange
        var savingsProgress = new SavingsProgressResponse
        {
            SavingsGoal = 500m,
            ActualSavings = 250m,
            ProgressPercentage = 50,
        };

        // Act
        var cut = Render<KakeiboMonthHeader>(p => p
            .Add(x => x.Year, 2026)
            .Add(x => x.Month, 2)
            .Add(x => x.SavingsProgress, savingsProgress));

        // Assert
        cut.Markup.ShouldContain("$250.00");
        cut.Markup.ShouldContain("$500.00");
        cut.Markup.ShouldContain("50%");
    }

    /// <summary>
    /// Verifies that savings progress bar shows "on-track" class when progress is greater than or equal to 50 percent.
    /// </summary>
    [Fact]
    public void KakeiboMonthHeader_ProgressBar_OnTrack_When50PercentOrMore()
    {
        // Arrange
        var savingsProgress = new SavingsProgressResponse
        {
            SavingsGoal = 500m,
            ActualSavings = 300m,
            ProgressPercentage = 60,
        };

        // Act
        var cut = Render<KakeiboMonthHeader>(p => p
            .Add(x => x.Year, 2026)
            .Add(x => x.Month, 2)
            .Add(x => x.SavingsProgress, savingsProgress));

        // Assert
        var progressBar = cut.Find("progress");
        progressBar.ClassList.ShouldContain("on-track");
    }

    /// <summary>
    /// Verifies that savings progress bar shows "behind" class when 25 percent is less than or equal to progress is less than 50 percent.
    /// </summary>
    [Fact]
    public void KakeiboMonthHeader_ProgressBar_Behind_When25To50Percent()
    {
        // Arrange
        var savingsProgress = new SavingsProgressResponse
        {
            SavingsGoal = 500m,
            ActualSavings = 150m,
            ProgressPercentage = 30,
        };

        // Act
        var cut = Render<KakeiboMonthHeader>(p => p
            .Add(x => x.Year, 2026)
            .Add(x => x.Month, 2)
            .Add(x => x.SavingsProgress, savingsProgress));

        // Assert
        var progressBar = cut.Find("progress");
        progressBar.ClassList.ShouldContain("behind");
    }

    /// <summary>
    /// Verifies that savings progress bar shows "significantly-behind" class when progress is less than 25 percent.
    /// </summary>
    [Fact]
    public void KakeiboMonthHeader_ProgressBar_SignificantlyBehind_WhenLess25Percent()
    {
        // Arrange
        var savingsProgress = new SavingsProgressResponse
        {
            SavingsGoal = 500m,
            ActualSavings = 50m,
            ProgressPercentage = 10,
        };

        // Act
        var cut = Render<KakeiboMonthHeader>(p => p
            .Add(x => x.Year, 2026)
            .Add(x => x.Month, 2)
            .Add(x => x.SavingsProgress, savingsProgress));

        // Assert
        var progressBar = cut.Find("progress");
        progressBar.ClassList.ShouldContain("significantly-behind");
    }

    /// <summary>
    /// Verifies that Reflect button invokes OnOpenReflection callback.
    /// </summary>
    [Fact]
    public void KakeiboMonthHeader_ReflectButton_InvokesOnOpenReflection()
    {
        // Arrange
        var savingsProgress = new SavingsProgressResponse
        {
            SavingsGoal = 500m,
            ActualSavings = 250m,
            ProgressPercentage = 50,
        };

        var reflectionOpened = false;

        // Act
        var cut = Render<KakeiboMonthHeader>(p => p
            .Add(x => x.Year, 2026)
            .Add(x => x.Month, 2)
            .Add(x => x.SavingsProgress, savingsProgress)
            .Add(x => x.OnOpenReflection, EventCallback.Factory.Create(this, () => { reflectionOpened = true; })));

        var reflectButton = cut.FindAll(".btn-link")
            .FirstOrDefault(btn => btn.TextContent.Contains("Reflect"));
        reflectButton?.Click();

        // Assert
        reflectionOpened.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies that progress bar has correct value and max attributes.
    /// </summary>
    [Fact]
    public void KakeiboMonthHeader_ProgressBar_HasCorrectAttributes()
    {
        // Arrange
        var savingsProgress = new SavingsProgressResponse
        {
            SavingsGoal = 1000m,
            ActualSavings = 750m,
            ProgressPercentage = 75,
        };

        // Act
        var cut = Render<KakeiboMonthHeader>(p => p
            .Add(x => x.Year, 2026)
            .Add(x => x.Month, 2)
            .Add(x => x.SavingsProgress, savingsProgress));

        // Assert
        var progressBar = cut.Find("progress");
        progressBar.GetAttribute("value").ShouldBe("75");
        progressBar.GetAttribute("max").ShouldBe("100");
    }

    /// <summary>
    /// Verifies that KakeiboMonthHeader generates correct month name for different months.
    /// </summary>
    [Fact]
    public void KakeiboMonthHeader_GeneratesCorrectMonthName()
    {
        // Arrange
        // Month name appears in the CTA when no goal is set
        // Act
        var cut = Render<KakeiboMonthHeader>(p => p
            .Add(x => x.Year, 2026)
            .Add(x => x.Month, 12)
            .Add(x => x.SavingsProgress, null));

        // Assert
        cut.Markup.ShouldContain("December 2026");
    }

    /// <summary>
    /// Verifies that goal CTA shows correct month name.
    /// </summary>
    [Fact]
    public void KakeiboMonthHeader_GoalCta_ShowsCorrectMonth()
    {
        // Arrange
        // Act
        var cut = Render<KakeiboMonthHeader>(p => p
            .Add(x => x.Year, 2026)
            .Add(x => x.Month, 1)
            .Add(x => x.SavingsProgress, null));

        // Assert
        cut.Markup.ShouldContain("January 2026");
    }
}
