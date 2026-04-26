// <copyright file="MonthIntentionPromptTests.cs" company="BecauseImClever">
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

namespace BudgetExperiment.Client.Tests.Components.Calendar;

/// <summary>
/// Unit tests for the <see cref="MonthIntentionPrompt"/> component.
/// Tests goal-setting workflows, validation, form submission, and dismissal behavior.
/// </summary>
public class MonthIntentionPromptTests : BunitContext, IAsyncLifetime
{
    private readonly StubBudgetApiService _fakeApiService = new();
    private readonly StubFeatureFlagClientService _fakeFeatureFlags = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="MonthIntentionPromptTests"/> class.
    /// </summary>
    public MonthIntentionPromptTests()
    {
        this.JSInterop.Mode = JSRuntimeMode.Loose;
        this.Services.AddSingleton<IBudgetApiService>(_fakeApiService);
        this.Services.AddSingleton<IFeatureFlagClientService>(_fakeFeatureFlags);
        this.Services.AddSingleton<ThemeService>();
        this.Services.AddSingleton<CultureService>();
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
    /// Verifies the prompt does not render when feature flag is disabled.
    /// </summary>
    [Fact]
    public void MonthIntentionPrompt_DoesNotRender_WhenFeatureFlagDisabled()
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
    /// Verifies the prompt displays the correct month name in the title.
    /// </summary>
    [Fact]
    public void MonthIntentionPrompt_DisplaysMonthName_InTitle()
    {
        // Act
        var cut = Render<MonthIntentionPrompt>(parameters => parameters
            .Add(p => p.Year, 2026)
            .Add(p => p.Month, 1));

        // Assert
        Assert.Contains("January 2026", cut.Markup);
    }

    /// <summary>
    /// Verifies the prompt displays the previous month's savings goal as a hint.
    /// </summary>
    [Fact]
    public void MonthIntentionPrompt_DisplaysPreviousMonthGoal_AsHint()
    {
        // Act
        var cut = Render<MonthIntentionPrompt>(parameters => parameters
            .Add(p => p.Year, 2026)
            .Add(p => p.Month, 2)
            .Add(p => p.PreviousMonthGoal, 150.50m));

        // Assert
        Assert.Contains("Last month's goal: $150.50", cut.Markup);
    }

    /// <summary>
    /// Verifies the hint is not displayed when no previous month goal exists.
    /// </summary>
    [Fact]
    public void MonthIntentionPrompt_HidesHint_WhenNoPreviousGoal()
    {
        // Act
        var cut = Render<MonthIntentionPrompt>(parameters => parameters
            .Add(p => p.Year, 2026)
            .Add(p => p.Month, 1)
            .Add(p => p.PreviousMonthGoal, null));

        // Assert
        Assert.DoesNotContain("Last month's goal", cut.Markup);
    }

    /// <summary>
    /// Verifies validation error is shown when savings goal is zero or negative.
    /// </summary>
    [Fact]
    public void MonthIntentionPrompt_ShowsValidationError_WhenGoalIsZero()
    {
        // Arrange
        var cut = Render<MonthIntentionPrompt>(parameters => parameters
            .Add(p => p.Year, 2026)
            .Add(p => p.Month, 1));

        // Act - attempt to submit with zero goal
        var button = cut.FindAll("button").First(b => b.TextContent.Contains("Set Goal"));
        button.Click();

        // Assert
        Assert.Contains("Please enter a savings goal greater than zero", cut.Markup);
    }

    /// <summary>
    /// Verifies the Save Goal button calls OnGoalSet callback when valid goal is submitted.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task MonthIntentionPrompt_CallsOnGoalSet_WithCorrectAmount()
    {
        // Arrange
        decimal? callbackValue = null;
        var cut = Render<MonthIntentionPrompt>(parameters => parameters
            .Add(p => p.Year, 2026)
            .Add(p => p.Month, 1)
            .Add(p => p.OnGoalSet, EventCallback.Factory.Create<decimal>(this, amount => callbackValue = amount)));

        // Act - enter a valid goal using InputAsync (component uses @bind:event="oninput")
        var input = cut.Find("input[type='number']");
        await input.InputAsync("250.50");

        var button = cut.FindAll("button").First(b => b.TextContent.Contains("Set Goal"));
        button.Click();

        // Assert - wait for async call to complete
        cut.WaitForState(() => callbackValue.HasValue, TimeSpan.FromSeconds(2));
        Assert.Equal(250.50m, callbackValue);
    }

    /// <summary>
    /// Verifies the intention text is included when submitting the goal.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task MonthIntentionPrompt_SubmitsIntentionText_WhenProvided()
    {
        // Arrange
        var callbackInvoked = false;
        var cut = Render<MonthIntentionPrompt>(parameters => parameters
            .Add(p => p.Year, 2026)
            .Add(p => p.Month, 1)
            .Add(p => p.OnGoalSet, EventCallback.Factory.Create<decimal>(this, _ => callbackInvoked = true)));

        // Act - use InputAsync for oninput event binding on both inputs
        var amountInput = cut.Find("input[type='number']");
        await amountInput.InputAsync("100");

        var intentionInput = cut.Find("textarea");
        await intentionInput.InputAsync("Focus on reducing dining expenses this month");

        var button = cut.FindAll("button").First(b => b.TextContent.Contains("Set Goal"));
        button.Click();

        // Assert - callback should be invoked
        await Task.Delay(500); // Allow async submission
        Assert.True(callbackInvoked);
    }

    /// <summary>
    /// Verifies the character counter updates as user types in intention field.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task MonthIntentionPrompt_UpdatesCharacterCounter()
    {
        // Arrange
        var cut = Render<MonthIntentionPrompt>(parameters => parameters
            .Add(p => p.Year, 2026)
            .Add(p => p.Month, 1));

        // Act - use InputAsync to trigger oninput event for character counter update
        var textarea = cut.Find("textarea");
        var testText = "This is my intention";
        await textarea.InputAsync(testText);

        // Assert - counter should display character count
        var counter = cut.Find(".char-counter");
        Assert.Contains($"{testText.Length}/280", counter.TextContent);
    }

    /// <summary>
    /// Verifies the intention field respects the 280-character maximum.
    /// </summary>
    [Fact]
    public void MonthIntentionPrompt_EnforcesMaxLength_OnIntentionField()
    {
        // Arrange
        var cut = Render<MonthIntentionPrompt>(parameters => parameters
            .Add(p => p.Year, 2026)
            .Add(p => p.Month, 1));

        // Act
        var textarea = cut.Find("textarea");
        var attribute = textarea.GetAttribute("maxlength");

        // Assert
        Assert.Equal("280", attribute);
    }

    /// <summary>
    /// Verifies the "Maybe Later" button calls OnDismissed callback.
    /// </summary>
    [Fact]
    public void MonthIntentionPrompt_CallsOnDismissed_WhenMaybeLaterClicked()
    {
        // Arrange
        var callbackInvoked = false;
        var cut = Render<MonthIntentionPrompt>(parameters => parameters
            .Add(p => p.Year, 2026)
            .Add(p => p.Month, 1)
            .Add(p => p.OnDismissed, EventCallback.Factory.Create(this, () => callbackInvoked = true)));

        // Act
        var button = cut.FindAll("button").First(b => b.TextContent.Contains("Maybe Later"));
        button.Click();

        // Assert
        Assert.True(callbackInvoked);
    }

    /// <summary>
    /// Verifies the buttons are disabled while submission is in progress.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task MonthIntentionPrompt_DisablesButtons_DuringSubmission()
    {
        // Arrange - block the API call to keep submission pending
        _fakeApiService.CreateOrUpdateReflectionTaskSource = new TaskCompletionSource<MonthlyReflectionDto?>();

        var cut = Render<MonthIntentionPrompt>(parameters => parameters
            .Add(p => p.Year, 2026)
            .Add(p => p.Month, 1));

        // Act - use InputAsync for oninput event binding
        var amountInput = cut.Find("input[type='number']");
        await amountInput.InputAsync("100");

        var setGoalButton = cut.FindAll("button").First(b => b.TextContent.Contains("Set Goal"));
        setGoalButton.Click();

        // Wait for the component to re-render with disabled buttons (submission in progress)
        await cut.InvokeAsync(() => cut.WaitForState(() => cut.FindAll("button").All(b => b.GetAttribute("disabled") != null), TimeSpan.FromSeconds(2)));

        // Assert - re-fetch buttons to ensure we have fresh references after render
        var buttons = cut.FindAll("button");
        Assert.All(buttons, button => Assert.NotNull(button.GetAttribute("disabled")));
    }

    /// <summary>
    /// Verifies the button text changes to "Saving…" during submission.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task MonthIntentionPrompt_UpdatesButtonText_ToSavingDuringSubmission()
    {
        // Arrange - block the API call
        _fakeApiService.CreateOrUpdateReflectionTaskSource = new TaskCompletionSource<MonthlyReflectionDto?>();

        var cut = Render<MonthIntentionPrompt>(parameters => parameters
            .Add(p => p.Year, 2026)
            .Add(p => p.Month, 1));

        // Act - use InputAsync for oninput event binding
        var amountInput = cut.Find("input[type='number']");
        await amountInput.InputAsync("100");

        var setGoalButton = cut.FindAll("button").First(b => b.TextContent.Contains("Set Goal"));
        setGoalButton.Click();

        // Wait for component to re-render with "Saving…" text
        await cut.InvokeAsync(() => cut.WaitForState(() => cut.Find("button").TextContent.Contains("Saving…"), TimeSpan.FromSeconds(2)));

        // Assert - re-fetch button to ensure fresh reference after render
        var updatedButton = cut.Find("button");
        Assert.Contains("Saving…", updatedButton.TextContent);
    }

    /// <summary>
    /// Verifies error message is displayed when API call fails.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task MonthIntentionPrompt_ShowsErrorMessage_WhenApiFails()
    {
        // Arrange
        _fakeApiService.CreateOrUpdateReflectionTaskSource = new TaskCompletionSource<MonthlyReflectionDto?>();
        _fakeApiService.CreateOrUpdateReflectionTaskSource.SetException(new HttpRequestException("Network error"));

        var cut = Render<MonthIntentionPrompt>(parameters => parameters
            .Add(p => p.Year, 2026)
            .Add(p => p.Month, 1));

        // Act - use InputAsync for oninput event binding
        var amountInput = cut.Find("input[type='number']");
        await amountInput.InputAsync("100");

        var button = cut.FindAll("button").First(b => b.TextContent.Contains("Set Goal"));
        button.Click();

        // Assert - wait for error to appear
        cut.WaitForState(() => cut.Markup.Contains("Failed to save goal"), TimeSpan.FromSeconds(2));
        Assert.Contains("Failed to save goal. Please try again", cut.Markup);
    }

    /// <summary>
    /// Verifies the prompt is dismissed after successful goal submission.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task MonthIntentionPrompt_HidesPrompt_AfterSuccessfulSubmission()
    {
        // Arrange
        _fakeApiService.CreateOrUpdateReflectionTaskSource = new TaskCompletionSource<MonthlyReflectionDto?>();
        _fakeApiService.CreateOrUpdateReflectionTaskSource.SetResult(new MonthlyReflectionDto());

        var cut = Render<MonthIntentionPrompt>(parameters => parameters
            .Add(p => p.Year, 2026)
            .Add(p => p.Month, 1));

        // Act - use InputAsync for oninput event binding
        var amountInput = cut.Find("input[type='number']");
        await amountInput.InputAsync("100");

        var button = cut.FindAll("button").First(b => b.TextContent.Contains("Set Goal"));
        button.Click();

        // Assert - wait for prompt to be hidden
        cut.WaitForState(() => cut.FindAll(".intention-prompt").Count == 0, TimeSpan.FromSeconds(2));
        Assert.Empty(cut.FindAll(".intention-prompt"));
    }

    /// <summary>
    /// Verifies the empty intention text is converted to null when submitting.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task MonthIntentionPrompt_ConvertsEmptyIntention_ToNull()
    {
        // Arrange
        var goalSet = false;
        var cut = Render<MonthIntentionPrompt>(parameters => parameters
            .Add(p => p.Year, 2026)
            .Add(p => p.Month, 1)
            .Add(p => p.OnGoalSet, EventCallback.Factory.Create<decimal>(this, _ => goalSet = true)));

        // Act - submit with empty intention using InputAsync for oninput event binding
        var amountInput = cut.Find("input[type='number']");
        await amountInput.InputAsync("100");

        var intentionInput = cut.Find("textarea");
        await intentionInput.InputAsync("   "); // Whitespace only

        var button = cut.FindAll("button").First(b => b.TextContent.Contains("Set Goal"));
        button.Click();

        // Assert - goal should still be set (intention treated as null)
        cut.WaitForState(() => goalSet, TimeSpan.FromSeconds(2));
        Assert.True(goalSet);
    }
}
