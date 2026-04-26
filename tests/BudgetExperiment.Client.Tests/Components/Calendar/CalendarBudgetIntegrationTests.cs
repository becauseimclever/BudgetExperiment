// <copyright file="CalendarBudgetIntegrationTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Globalization;

using BudgetExperiment.Client.Components.Calendar;
using BudgetExperiment.Client.Services;
using BudgetExperiment.Client.Tests.TestHelpers;
using BudgetExperiment.Contracts.Dtos;

using Bunit;

using Microsoft.Extensions.DependencyInjection;

namespace BudgetExperiment.Client.Tests.Components.Calendar;

/// <summary>
/// Integration tests for calendar and budget components, focusing on state management
/// and cross-component workflows for month selection, intention prompts, and budget navigation.
/// </summary>
public class CalendarBudgetIntegrationTests : BunitContext, IAsyncLifetime
{
    private readonly StubBudgetApiService _fakeApiService = new();
    private readonly StubFeatureFlagClientService _fakeFeatureFlags = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="CalendarBudgetIntegrationTests"/> class.
    /// </summary>
    public CalendarBudgetIntegrationTests()
    {
        this.JSInterop.Mode = JSRuntimeMode.Loose;
        this.Services.AddSingleton<IBudgetApiService>(_fakeApiService);
        this.Services.AddSingleton<IFeatureFlagClientService>(_fakeFeatureFlags);
        this.Services.AddSingleton<ThemeService>();
        this.Services.AddSingleton<CultureService>();
        this.Services.AddSingleton<FormStateService>();
    }

    /// <inheritdoc/>
    public Task InitializeAsync()
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
        _fakeFeatureFlags.Flags["Kakeibo:MonthlyReflectionPrompts"] = true;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public new Task DisposeAsync() => base.DisposeAsync().AsTask();

    /// <summary>
    /// Verifies that the MonthIntentionPrompt component renders when feature is enabled.
    /// </summary>
    [Fact]
    public void CalendarBudgetIntegration_MonthIntentionPrompt_RendersWhenFeatureEnabled()
    {
        // Arrange & Act
        var cut = Render<MonthIntentionPrompt>(parameters => parameters
            .Add(p => p.Year, 2026)
            .Add(p => p.Month, 1));

        // Assert
        Assert.NotNull(cut);
        Assert.Contains("What do you want to save in", cut.Markup);
    }

    /// <summary>
    /// Verifies that MonthIntentionPrompt is hidden when feature is disabled.
    /// </summary>
    [Fact]
    public void CalendarBudgetIntegration_MonthIntentionPrompt_HiddenWhenFeatureDisabled()
    {
        // Arrange
        _fakeFeatureFlags.Flags["Kakeibo:MonthlyReflectionPrompts"] = false;

        // Act
        var cut = Render<MonthIntentionPrompt>(parameters => parameters
            .Add(p => p.Year, 2026)
            .Add(p => p.Month, 1));

        // Assert
        Assert.Empty(cut.FindAll(".intention-prompt"));
    }

    /// <summary>
    /// Verifies that month name is correctly displayed in intention prompt.
    /// </summary>
    [Fact]
    public void CalendarBudgetIntegration_DisplaysCorrectMonth_InPrompt()
    {
        // Arrange
        _fakeFeatureFlags.Flags["Kakeibo:MonthlyReflectionPrompts"] = true;

        // Act
        var cut = Render<MonthIntentionPrompt>(parameters => parameters
            .Add(p => p.Year, 2026)
            .Add(p => p.Month, 3));

        // Assert
        Assert.Contains("March 2026", cut.Markup);
    }

    /// <summary>
    /// Verifies that form state service is available in the component context.
    /// </summary>
    [Fact]
    public void CalendarBudgetIntegration_FormStateService_IsAvailable()
    {
        // Arrange
        var service = Services.GetRequiredService<FormStateService>();

        // Act & Assert
        Assert.NotNull(service);
    }

    /// <summary>
    /// Verifies that input fields are rendered in MonthIntentionPrompt.
    /// </summary>
    [Fact]
    public void CalendarBudgetIntegration_InputFields_AreRendered()
    {
        // Arrange & Act
        var cut = Render<MonthIntentionPrompt>(parameters => parameters
            .Add(p => p.Year, 2026)
            .Add(p => p.Month, 1));

        // Assert
        var inputs = cut.FindAll("input[type='number']");
        Assert.NotEmpty(inputs);
        var textareas = cut.FindAll("textarea");
        Assert.NotEmpty(textareas);
    }

    /// <summary>
    /// Verifies that previous month goal is displayed when provided.
    /// </summary>
    [Fact]
    public void CalendarBudgetIntegration_DisplaysPreviousMonthGoal_WhenProvided()
    {
        // Arrange & Act
        var cut = Render<MonthIntentionPrompt>(parameters => parameters
            .Add(p => p.Year, 2026)
            .Add(p => p.Month, 2)
            .Add(p => p.PreviousMonthGoal, 150.50m));

        // Assert
        Assert.Contains("Last month's goal: $150.50", cut.Markup);
    }

    /// <summary>
    /// Verifies that API service is integrated with intention prompt.
    /// </summary>
    [Fact]
    public void CalendarBudgetIntegration_ApiService_IsAvailable()
    {
        // Arrange
        var service = Services.GetRequiredService<IBudgetApiService>();

        // Act & Assert
        Assert.NotNull(service);
        Assert.IsType<StubBudgetApiService>(service);
    }

    /// <summary>
    /// Verifies that multiple intention prompts can coexist for different months.
    /// </summary>
    [Fact]
    public void CalendarBudgetIntegration_MultiplPrompts_CanRender()
    {
        // Arrange & Act
        var cut1 = Render<MonthIntentionPrompt>(parameters => parameters
            .Add(p => p.Year, 2026)
            .Add(p => p.Month, 1));

        var cut2 = Render<MonthIntentionPrompt>(parameters => parameters
            .Add(p => p.Year, 2026)
            .Add(p => p.Month, 2));

        // Assert
        Assert.Contains("January 2026", cut1.Markup);
        Assert.Contains("February 2026", cut2.Markup);
    }
}
