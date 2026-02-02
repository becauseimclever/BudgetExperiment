// <copyright file="OllamaAiServiceTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>


using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Infrastructure.ExternalServices.AI;
using Microsoft.Extensions.Logging.Abstractions;

namespace BudgetExperiment.Infrastructure.Tests;

/// <summary>
/// Fake app settings service for testing AI settings functionality.
/// </summary>
internal sealed class FakeAppSettingsService : IAppSettingsService
{
    private AiSettingsData _aiSettings;

    public FakeAppSettingsService(AiSettingsData aiSettings)
    {
        _aiSettings = aiSettings;
    }

    public Task<AppSettingsDto> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new AppSettingsDto());
    }

    public Task<AppSettingsDto> UpdateSettingsAsync(AppSettingsUpdateDto dto, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new AppSettingsDto());
    }

    public Task<AiSettingsData> GetAiSettingsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_aiSettings);
    }

    public Task<AiSettingsData> UpdateAiSettingsAsync(AiSettingsData settings, CancellationToken cancellationToken = default)
    {
        _aiSettings = settings;
        return Task.FromResult(_aiSettings);
    }
}

/// <summary>
/// Integration tests for OllamaAiService.
/// These tests require a running Ollama instance and are skipped if unavailable.
/// </summary>
[Trait("Category", "ExternalDependency")]
public class OllamaAiServiceTests : IAsyncLifetime
{
    private readonly HttpClient _httpClient;
    private readonly OllamaAiService _service;
    private readonly AiSettingsData _settings;
    private bool _ollamaAvailable;

    /// <summary>
    /// Initializes a new instance of the <see cref="OllamaAiServiceTests"/> class.
    /// </summary>
    public OllamaAiServiceTests()
    {
        _settings = new AiSettingsData(
            OllamaEndpoint: "http://localhost:11434",
            ModelName: "llama3.2",
            Temperature: 0.3m,
            MaxTokens: 100,
            TimeoutSeconds: 60,
            IsEnabled: true);

        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds + 10),
        };

        _service = new OllamaAiService(
            _httpClient,
            new FakeAppSettingsService(_settings),
            NullLogger<OllamaAiService>.Instance);
    }

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        // Check if Ollama is available
        try
        {
            var response = await _httpClient.GetAsync($"{_settings.OllamaEndpoint}/api/version");
            _ollamaAvailable = response.IsSuccessStatusCode;
        }
        catch
        {
            _ollamaAvailable = false;
        }
    }

    /// <inheritdoc/>
    public Task DisposeAsync()
    {
        _httpClient.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task GetStatusAsync_When_Disabled_Returns_Unavailable()
    {
        // Arrange
        var disabledSettings = new AiSettingsData(
            OllamaEndpoint: "http://localhost:11434",
            ModelName: "llama3.2",
            Temperature: 0.3m,
            MaxTokens: 100,
            TimeoutSeconds: 60,
            IsEnabled: false);

        var service = new OllamaAiService(
            _httpClient,
            new FakeAppSettingsService(disabledSettings),
            NullLogger<OllamaAiService>.Instance);

        // Act
        var status = await service.GetStatusAsync();

        // Assert
        Assert.False(status.IsAvailable);
        Assert.Contains("disabled", status.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetStatusAsync_When_Available_Returns_Connected()
    {
        if (!_ollamaAvailable)
        {
            // Skip test when Ollama is not running
            return;
        }

        // Act
        var status = await _service.GetStatusAsync();

        // Assert
        Assert.True(status.IsAvailable);
        Assert.Equal(_settings.ModelName, status.CurrentModel);
        Assert.Null(status.ErrorMessage);
    }

    [Fact]
    public async Task GetAvailableModelsAsync_When_Available_Returns_Models()
    {
        if (!_ollamaAvailable)
        {
            // Skip test when Ollama is not running
            return;
        }

        // Act
        var models = await _service.GetAvailableModelsAsync();

        // Assert
        Assert.NotEmpty(models);
        Assert.All(models, m => Assert.False(string.IsNullOrEmpty(m.Name)));
    }

    [Fact]
    public async Task GetAvailableModelsAsync_When_Disabled_Returns_Empty()
    {
        // Arrange
        var disabledSettings = new AiSettingsData(
            OllamaEndpoint: "http://localhost:11434",
            ModelName: "llama3.2",
            Temperature: 0.3m,
            MaxTokens: 100,
            TimeoutSeconds: 60,
            IsEnabled: false);

        var service = new OllamaAiService(
            _httpClient,
            new FakeAppSettingsService(disabledSettings),
            NullLogger<OllamaAiService>.Instance);

        // Act
        var models = await service.GetAvailableModelsAsync();

        // Assert
        Assert.Empty(models);
    }

    [Fact]
    public async Task CompleteAsync_When_Disabled_Returns_Error()
    {
        // Arrange
        var disabledSettings = new AiSettingsData(
            OllamaEndpoint: "http://localhost:11434",
            ModelName: "llama3.2",
            Temperature: 0.3m,
            MaxTokens: 100,
            TimeoutSeconds: 60,
            IsEnabled: false);

        var service = new OllamaAiService(
            _httpClient,
            new FakeAppSettingsService(disabledSettings),
            NullLogger<OllamaAiService>.Instance);

        var prompt = new AiPrompt("You are a helpful assistant.", "Say hello.");

        // Act
        var response = await service.CompleteAsync(prompt);

        // Assert
        Assert.False(response.Success);
        Assert.Contains("disabled", response.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CompleteAsync_When_Available_Returns_Response()
    {
        if (!_ollamaAvailable)
        {
            // Skip test when Ollama is not running
            return;
        }

        // Check if the model exists
        var models = await _service.GetAvailableModelsAsync();
        if (!models.Any(m => m.Name.StartsWith(_settings.ModelName, StringComparison.OrdinalIgnoreCase)))
        {
            // Skip test when the required model is not installed
            return;
        }

        // Arrange
        var prompt = new AiPrompt(
            "You are a helpful assistant. Respond with only one word.",
            "What is 2+2? Answer with just the number.");

        // Act
        var response = await _service.CompleteAsync(prompt);

        // Assert
        Assert.True(response.Success, $"Expected success but got error: {response.ErrorMessage}");
        Assert.False(string.IsNullOrWhiteSpace(response.Content));
        Assert.True(response.TokensUsed > 0);
        Assert.True(response.Duration > TimeSpan.Zero);
    }

    [Fact]
    public async Task CompleteAsync_With_Unreachable_Endpoint_Returns_Error()
    {
        // Arrange - use a valid but unreachable endpoint
        var invalidSettings = new AiSettingsData(
            OllamaEndpoint: "http://192.0.2.1:11434", // TEST-NET-1 (RFC 5737) - guaranteed unreachable
            ModelName: "llama3.2",
            Temperature: 0.3m,
            MaxTokens: 100,
            TimeoutSeconds: 2,
            IsEnabled: true);

        var httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(5),
        };

        var service = new OllamaAiService(
            httpClient,
            new FakeAppSettingsService(invalidSettings),
            NullLogger<OllamaAiService>.Instance);

        var prompt = new AiPrompt("System", "User");

        // Act
        var response = await service.CompleteAsync(prompt);

        // Assert
        Assert.False(response.Success);
        Assert.NotNull(response.ErrorMessage);

        httpClient.Dispose();
    }
}
