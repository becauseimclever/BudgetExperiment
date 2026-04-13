// <copyright file="BackendSelectingAiServiceTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Net;

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
        Assert.Null(_ollamaHandler.LastRequestUri);
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
        Assert.Null(_llamaHandler.LastRequestUri);
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
        Assert.Null(_llamaHandler.LastRequestUri);
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

        /// <inheritdoc />
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequestUri = request.RequestUri;
            return await ResponseFactory(request, cancellationToken);
        }
    }
}
