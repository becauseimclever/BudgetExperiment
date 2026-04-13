// <copyright file="AiSettingsFormTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Client.Components.AI;
using BudgetExperiment.Client.Services;
using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Shared;

using Bunit;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace BudgetExperiment.Client.Tests.Components.AI;

/// <summary>
/// Tests for the <see cref="AiSettingsForm"/> component.
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
    /// Verifies the form renders the backend selector and generic endpoint field for Feature 160.
    /// </summary>
    [Fact]
    public void AiSettingsForm_RendersBackendSelector_AndGenericEndpointField()
    {
        _aiService.SettingsResult = CreateSettings(
            backendType: AiBackendType.LlamaCpp,
            endpointUrl: AiBackendDefaults.DefaultLlamaCppEndpointUrl);

        var cut = Render<AiSettingsForm>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Backend", cut.Markup);
            Assert.Contains("Endpoint", cut.Markup);
            Assert.DoesNotContain("Ollama Endpoint", cut.Markup);
            Assert.Equal(AiBackendDefaults.DefaultLlamaCppEndpointUrl, cut.Find("#endpointUrl").GetAttribute("value"));
            Assert.Equal("LlamaCpp", cut.Find("#backendType").GetAttribute("value"));
        });
    }

    /// <summary>
    /// Verifies switching backends updates the endpoint when the user is still on the old backend default.
    /// </summary>
    [Fact]
    public void AiSettingsForm_BackendChange_UpdatesDefaultEndpoint_WhenUsingDefault()
    {
        _aiService.SettingsResult = CreateSettings();

        var cut = Render<AiSettingsForm>();
        cut.WaitForAssertion(() => Assert.Equal(AiBackendDefaults.DefaultOllamaEndpointUrl, cut.Find("#endpointUrl").GetAttribute("value")));

        cut.Find("#backendType").Change("LlamaCpp");

        Assert.Equal(AiBackendDefaults.DefaultLlamaCppEndpointUrl, cut.Find("#endpointUrl").GetAttribute("value"));
    }

    /// <summary>
    /// Verifies switching backends preserves an explicit endpoint entered by the user.
    /// </summary>
    [Fact]
    public void AiSettingsForm_BackendChange_PreservesCustomEndpoint_WhenCustomized()
    {
        const string customEndpoint = "http://localhost:9000";
        _aiService.SettingsResult = CreateSettings(endpointUrl: customEndpoint);

        var cut = Render<AiSettingsForm>();
        cut.WaitForAssertion(() => Assert.Equal(customEndpoint, cut.Find("#endpointUrl").GetAttribute("value")));

        cut.Find("#backendType").Change("LlamaCpp");

        Assert.Equal(customEndpoint, cut.Find("#endpointUrl").GetAttribute("value"));
    }

    /// <summary>
    /// Verifies saving sends backend and endpoint values through the client service and callback.
    /// </summary>
    [Fact]
    public void AiSettingsForm_Save_SubmitsBackendAndEndpoint()
    {
        AiSettingsDto? savedSettings = null;
        _aiService.SettingsResult = CreateSettings();
        _aiService.UpdateSettingsResult = CreateSettings(
            backendType: AiBackendType.LlamaCpp,
            endpointUrl: "http://localhost:8081",
            modelName: "phi-4");

        var cut = Render<AiSettingsForm>(parameters => parameters
            .Add(p => p.OnSave, (AiSettingsDto dto) => savedSettings = dto));

        cut.WaitForAssertion(() => Assert.NotNull(cut.Find("#backendType")));
        cut.Find("#backendType").Change("LlamaCpp");
        cut.Find("#endpointUrl").Change("http://localhost:8081");
        cut.Find("#modelName").Change("phi-4");

        cut.Find("form").Submit();

        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(_aiService.LastUpdatedSettings);
            Assert.Equal(AiBackendType.LlamaCpp, _aiService.LastUpdatedSettings!.BackendType);
            Assert.Equal("http://localhost:8081", _aiService.LastUpdatedSettings.EndpointUrl);
            Assert.NotNull(savedSettings);
            Assert.Equal(AiBackendType.LlamaCpp, savedSettings!.BackendType);
            Assert.Equal("http://localhost:8081", savedSettings.EndpointUrl);
        });
    }

    /// <summary>
    /// Verifies the connection result reflects the active backend instead of hardcoding Ollama copy.
    /// </summary>
    [Fact]
    public void AiSettingsForm_TestConnection_ShowsSelectedBackendName()
    {
        _aiService.SettingsResult = CreateSettings(backendType: AiBackendType.LlamaCpp, endpointUrl: AiBackendDefaults.DefaultLlamaCppEndpointUrl);
        _aiService.StatusResult = new AiStatusDto
        {
            IsAvailable = true,
            IsEnabled = true,
            BackendType = AiBackendType.LlamaCpp,
            CurrentModel = "phi-4",
            Endpoint = AiBackendDefaults.DefaultLlamaCppEndpointUrl,
        };

        var cut = Render<AiSettingsForm>();
        cut.WaitForAssertion(() => Assert.NotNull(cut.Find("#backendType")));

        cut.Find("button[type='button']").Click();

        cut.WaitForAssertion(() => Assert.Contains("Connected to llama.cpp - Model: phi-4", cut.Markup));
    }

    private static AiSettingsDto CreateSettings(
        AiBackendType backendType = AiBackendType.Ollama,
        string? endpointUrl = null,
        string modelName = "llama3.2")
    {
        return new AiSettingsDto
        {
            BackendType = backendType,
            EndpointUrl = endpointUrl ?? AiBackendDefaults.GetDefaultEndpointUrl(backendType),
            ModelName = modelName,
            Temperature = 0.3m,
            MaxTokens = 2000,
            TimeoutSeconds = 120,
            IsEnabled = true,
        };
    }
}
