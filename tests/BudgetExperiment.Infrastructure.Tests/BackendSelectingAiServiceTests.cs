// <copyright file="BackendSelectingAiServiceTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Net;
using System.Text;

using BudgetExperiment.Application.Settings;
using BudgetExperiment.Infrastructure.ExternalServices.AI;
using BudgetExperiment.Shared;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace BudgetExperiment.Infrastructure.Tests;

/// <summary>
/// Unit tests for <see cref="BackendSelectingAiService"/>.
/// </summary>
public sealed class BackendSelectingAiServiceTests : IDisposable
{
    private readonly HttpResponseMessage _llamaModelsResponse = new(HttpStatusCode.OK)
    {
        Content = CreateJsonContent("""
            { "data": [ { "id": "llama-model" } ] }
            """),
    };
    private readonly HttpClient _llamaHttpClient;
    private readonly RecordingHttpMessageHandler _llamaHandler;
    private readonly HttpClient _ollamaHttpClient;
    private readonly RecordingHttpMessageHandler _ollamaHandler;

    /// <summary>
    /// Initializes a new instance of the <see cref="BackendSelectingAiServiceTests"/> class.
    /// </summary>
    public BackendSelectingAiServiceTests()
    {
        _ollamaHandler = new RecordingHttpMessageHandler();
        _ollamaHttpClient = new HttpClient(_ollamaHandler);
        _llamaHandler = new RecordingHttpMessageHandler();
        _llamaHttpClient = new HttpClient(_llamaHandler);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _ollamaHttpClient.Dispose();
        _ollamaHandler.Dispose();
        _llamaHttpClient.Dispose();
        _llamaHandler.Dispose();
    }

    [Fact]
    public async Task GetStatusAsync_When_SettingsSpecify_LlamaCpp_Uses_LlamaCpp_Service()
    {
        // Arrange
        var settingsService = new FakeAppSettingsService(new AiSettingsData(
            EndpointUrl: "http://localhost:8080",
            ModelName: "llama",
            Temperature: 0.3m,
            MaxTokens: 200,
            TimeoutSeconds: 30,
            IsEnabled: true,
            BackendType: AiBackendType.LlamaCpp));
        var service = CreateService(settingsService, CreateConfiguration());

        // Act
        var status = await service.GetStatusAsync();

        // Assert
        Assert.True(status.IsAvailable);
        Assert.Equal("http://localhost:8080/health", _llamaHandler.LastRequestUri?.ToString());
        Assert.Equal(1, _llamaHandler.RequestCount);
        Assert.Equal(0, _ollamaHandler.RequestCount);
    }

    [Fact]
    public async Task GetStatusAsync_When_SettingsSpecify_Ollama_Uses_Ollama_Service()
    {
        // Arrange
        var settingsService = new FakeAppSettingsService(new AiSettingsData(
            EndpointUrl: "http://localhost:11434",
            ModelName: "llama",
            Temperature: 0.3m,
            MaxTokens: 200,
            TimeoutSeconds: 30,
            IsEnabled: true,
            BackendType: AiBackendType.Ollama));
        var service = CreateService(settingsService, CreateConfiguration(new Dictionary<string, string?>
        {
            ["AiSettings:BackendType"] = nameof(AiBackendType.LlamaCpp),
        }));

        // Act
        var status = await service.GetStatusAsync();

        // Assert
        Assert.True(status.IsAvailable);
        Assert.Equal("http://localhost:11434/api/version", _ollamaHandler.LastRequestUri?.ToString());
        Assert.Equal(1, _ollamaHandler.RequestCount);
        Assert.Equal(0, _llamaHandler.RequestCount);
    }

    [Fact]
    public async Task GetStatusAsync_When_NoBackendConfigured_Uses_Default_Ollama_Service()
    {
        // Arrange
        var settingsService = new FakeAppSettingsService(new AiSettingsData(
            EndpointUrl: "http://localhost:11434",
            ModelName: "llama",
            Temperature: 0.3m,
            MaxTokens: 200,
            TimeoutSeconds: 30,
            IsEnabled: true,
            BackendType: AiBackendType.Ollama));
        var service = CreateService(settingsService, CreateConfiguration());

        // Act
        var status = await service.GetStatusAsync();

        // Assert
        Assert.True(status.IsAvailable);
        Assert.Equal("http://localhost:11434/api/version", _ollamaHandler.LastRequestUri?.ToString());
        Assert.Equal(1, _ollamaHandler.RequestCount);
        Assert.Equal(0, _llamaHandler.RequestCount);
    }

    [Fact]
    public async Task GetAvailableModelsAsync_When_SettingsSpecify_LlamaCpp_Uses_LlamaCpp_Service_Only()
    {
        // Arrange
        var settingsService = new FakeAppSettingsService(new AiSettingsData(
            EndpointUrl: "http://localhost:8080",
            ModelName: "llama",
            Temperature: 0.3m,
            MaxTokens: 200,
            TimeoutSeconds: 30,
            IsEnabled: true,
            BackendType: AiBackendType.LlamaCpp));
        var service = CreateService(settingsService, CreateConfiguration());

        _llamaHandler.ResponseFactory = (_, _) => Task.FromResult(_llamaModelsResponse);

        // Act
        var models = await service.GetAvailableModelsAsync();

        // Assert
        var model = Assert.Single(models);
        Assert.Equal("llama-model", model.Name);
        Assert.Equal("http://localhost:8080/v1/models", _llamaHandler.LastRequestUri?.ToString());
        Assert.Equal(1, _llamaHandler.RequestCount);
        Assert.Equal(0, _ollamaHandler.RequestCount);
    }

    [Fact]
    public async Task GetAvailableModelsAsync_When_SettingsSpecify_Ollama_Uses_Ollama_Service_Only()
    {
        // Arrange
        var settingsService = new FakeAppSettingsService(new AiSettingsData(
            EndpointUrl: "http://localhost:11434",
            ModelName: "llama",
            Temperature: 0.3m,
            MaxTokens: 200,
            TimeoutSeconds: 30,
            IsEnabled: true,
            BackendType: AiBackendType.Ollama));
        var service = CreateService(settingsService, CreateConfiguration(new Dictionary<string, string?>
        {
            ["AiSettings:BackendType"] = nameof(AiBackendType.LlamaCpp),
        }));

        _ollamaHandler.ResponseFactory = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = CreateJsonContent("""
                {
                  "models": [
                    {
                      "name": "ollama-model",
                      "modified_at": "2026-01-01T00:00:00Z",
                      "size": 1024
                    }
                  ]
                }
                """),
        });

        // Act
        var models = await service.GetAvailableModelsAsync();

        // Assert
        var model = Assert.Single(models);
        Assert.Equal("ollama-model", model.Name);
        Assert.Equal("http://localhost:11434/api/tags", _ollamaHandler.LastRequestUri?.ToString());
        Assert.Equal(1, _ollamaHandler.RequestCount);
        Assert.Equal(0, _llamaHandler.RequestCount);
    }

    [Fact]
    public async Task CompleteAsync_When_SettingsSpecify_LlamaCpp_Uses_LlamaCpp_Service_Only()
    {
        // Arrange
        var settingsService = new FakeAppSettingsService(new AiSettingsData(
            EndpointUrl: "http://localhost:8080",
            ModelName: "llama",
            Temperature: 0.3m,
            MaxTokens: 200,
            TimeoutSeconds: 30,
            IsEnabled: true,
            BackendType: AiBackendType.LlamaCpp));
        var service = CreateService(settingsService, CreateConfiguration());

        using var llamaResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = CreateJsonContent("""
                {
                  "choices": [
                    {
                      "message": {
                        "content": "llama completion"
                      }
                    }
                  ],
                  "usage": {
                    "total_tokens": 12
                  }
                }
                """),
        };
        _llamaHandler.ResponseFactory = (_, _) => Task.FromResult(llamaResponseMessage);

        // Act
        var response = await service.CompleteAsync(new AiPrompt("system", "user", 0.3m, 128));

        // Assert
        Assert.True(response.Success);
        Assert.Equal("llama completion", response.Content);
        Assert.Equal("http://localhost:8080/v1/chat/completions", _llamaHandler.LastRequestUri?.ToString());
        Assert.Equal(1, _llamaHandler.RequestCount);
        Assert.Equal(0, _ollamaHandler.RequestCount);
    }

    [Fact]
    public async Task CompleteAsync_When_SettingsSpecify_Ollama_Uses_Ollama_Service_Only()
    {
        // Arrange
        var settingsService = new FakeAppSettingsService(new AiSettingsData(
            EndpointUrl: "http://localhost:11434",
            ModelName: "llama",
            Temperature: 0.3m,
            MaxTokens: 200,
            TimeoutSeconds: 30,
            IsEnabled: true,
            BackendType: AiBackendType.Ollama));
        var service = CreateService(settingsService, CreateConfiguration(new Dictionary<string, string?>
        {
            ["AiSettings:BackendType"] = nameof(AiBackendType.LlamaCpp),
        }));

        _ollamaHandler.ResponseFactory = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """
                {
                  "message": {
                    "content": "ollama completion"
                  },
                  "prompt_eval_count": 2,
                  "eval_count": 3
                }
                """,
                Encoding.UTF8,
                "application/json"),
        });

        // Act
        var response = await service.CompleteAsync(new AiPrompt("system", "user", 0.3m, 128));

        // Assert
        Assert.True(response.Success);
        Assert.Equal("ollama completion", response.Content);
        Assert.Equal("http://localhost:11434/api/chat", _ollamaHandler.LastRequestUri?.ToString());
        Assert.Equal(1, _ollamaHandler.RequestCount);
        Assert.Equal(0, _llamaHandler.RequestCount);
    }

    private static IConfiguration CreateConfiguration(Dictionary<string, string?>? values = null)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values ?? new Dictionary<string, string?>())
            .Build();
    }

    private BackendSelectingAiService CreateService(IAppSettingsService settingsService, IConfiguration configuration)
    {
        _ollamaHandler.ResponseFactory = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        _llamaHandler.ResponseFactory = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));

        return new BackendSelectingAiService(
            new OllamaAiService(
                _ollamaHttpClient,
                settingsService,
                NullLogger<OllamaAiService>.Instance),
            new LlamaCppAiService(
                _llamaHttpClient,
                settingsService,
                NullLogger<LlamaCppAiService>.Instance),
            settingsService,
            configuration,
            NullLogger<BackendSelectingAiService>.Instance);
    }

    private sealed class RecordingHttpMessageHandler : HttpMessageHandler
    {
        public Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> ResponseFactory { get; set; } =
            (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));

        public Uri? LastRequestUri
        {
            get; private set;
        }

        public int RequestCount
        {
            get; private set;
        }

        /// <inheritdoc />
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequestUri = request.RequestUri;
            RequestCount++;
            return await ResponseFactory(request, cancellationToken);
        }
    }
    public void Dispose()
    {
        _llamaModelsResponse.Dispose();
        _llamaHttpClient.Dispose();
    }
}
