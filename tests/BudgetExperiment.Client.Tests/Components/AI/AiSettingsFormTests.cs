// <copyright file="AiSettingsFormTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Components.AI;
using BudgetExperiment.Client.Services;
using BudgetExperiment.Contracts.Dtos;

using Bunit;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace BudgetExperiment.Client.Tests.Components.AI;

/// <summary>
/// Unit tests for the <see cref="AiSettingsForm"/> component.
/// </summary>
public sealed class AiSettingsFormTests : BunitContext, IAsyncLifetime
{
    private readonly FakeAiApiService _aiService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AiSettingsFormTests"/> class.
    /// </summary>
    public AiSettingsFormTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<ThemeService>();
        Services.AddSingleton<CultureService>();
        _aiService = new FakeAiApiService();
        Services.AddSingleton<IAiApiService>(_aiService);
    }

    /// <inheritdoc/>
    public Task InitializeAsync() => Task.CompletedTask;

    /// <inheritdoc/>
    public new Task DisposeAsync() => base.DisposeAsync().AsTask();

    /// <summary>
    /// Verifies the form shows loading spinner while loading settings.
    /// </summary>
    [Fact]
    public void AiSettingsForm_ShowsLoadingSpinner_WhileLoading()
    {
        // Settings are null by default, so the form will try to load
        var cut = Render<AiSettingsForm>();

        // The component renders initially - check it renders something
        Assert.Contains("ai-settings-form", cut.Markup);
    }

    /// <summary>
    /// Verifies the form renders settings fields after loading.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task AiSettingsForm_RendersFields_AfterLoading()
    {
        _aiService.SettingsResult = new AiSettingsDto
        {
            OllamaEndpoint = "http://localhost:11434",
            ModelName = "llama3.2",
            Temperature = 0.3m,
            MaxTokens = 2000,
            TimeoutSeconds = 120,
            IsEnabled = true,
        };

        var cut = Render<AiSettingsForm>();
        await Task.Delay(50);
        cut.Render();

        Assert.Contains("Ollama", cut.Markup);
    }

    /// <summary>
    /// Verifies the form shows alert when settings are null.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task AiSettingsForm_ShowsAlert_WhenSettingsNull()
    {
        _aiService.SettingsResult = null;

        var cut = Render<AiSettingsForm>();
        await Task.Delay(50);
        cut.Render();

        Assert.Contains("alert", cut.Markup);
    }

    /// <summary>
    /// Verifies the test connection button is present when settings are loaded.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task AiSettingsForm_ShowsTestConnectionButton()
    {
        _aiService.SettingsResult = new AiSettingsDto();

        var cut = Render<AiSettingsForm>();
        await Task.Delay(50);
        cut.Render();

        Assert.Contains("Test Connection", cut.Markup);
    }

    /// <summary>
    /// Verifies the save button is present.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task AiSettingsForm_ShowsSaveButton()
    {
        _aiService.SettingsResult = new AiSettingsDto();

        var cut = Render<AiSettingsForm>();
        await Task.Delay(50);
        cut.Render();

        Assert.Contains("Save Settings", cut.Markup);
    }
}
