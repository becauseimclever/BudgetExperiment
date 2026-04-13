// <copyright file="OpenAiCompatibleAiServiceTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;

using BudgetExperiment.Application.Ai;
using BudgetExperiment.Application.Settings;
using BudgetExperiment.Infrastructure.ExternalServices.AI;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace BudgetExperiment.Infrastructure.Tests;

/// <summary>
/// Unit tests for <see cref="OpenAiCompatibleAiService"/> using a fake HTTP handler.
/// </summary>
public sealed class OpenAiCompatibleAiServiceTests : IDisposable
{
    private readonly RecordingHttpMessageHandler _handler;
    private readonly HttpClient _httpClient;
    private readonly FakeAppSettingsService _settingsService;
    private readonly TestOpenAiCompatibleAiService _service;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenAiCompatibleAiServiceTests"/> class.
    /// </summary>
    public OpenAiCompatibleAiServiceTests()
    {
        _handler = new RecordingHttpMessageHandler();
        _httpClient = new HttpClient(_handler);
        _settingsService = new FakeAppSettingsService(new AiSettingsData(
            EndpointUrl: "http://localhost:1234/",
            ModelName: "test-model",
            Temperature: 0.3m,
            MaxTokens: 200,
            TimeoutSeconds: 30,
            IsEnabled: true));
        _service = new TestOpenAiCompatibleAiService(
            _httpClient,
            _settingsService,
            NullLogger<TestOpenAiCompatibleAiService>.Instance);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _httpClient.Dispose();
        _handler.Dispose();
    }

    [Fact]
    public async Task GetStatusAsync_When_BackendRespondsOk_Returns_Available_Status()
    {
        // Arrange
        _handler.ResponseFactory = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));

        // Act
        var status = await _service.GetStatusAsync();

        // Assert
        Assert.True(status.IsAvailable);
        Assert.Equal("test-model", status.CurrentModel);
        Assert.Equal("http://localhost:1234/health", _handler.LastRequestUri?.ToString());
    }

    [Fact]
    public async Task GetAvailableModelsAsync_When_BackendRespondsOk_Returns_Parsed_Models()
    {
        // Arrange
        _handler.ResponseFactory = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""["alpha","beta"]""", Encoding.UTF8, "application/json"),
        });

        // Act
        var models = await _service.GetAvailableModelsAsync();

        // Assert
        Assert.Collection(
            models,
            model => Assert.Equal("alpha", model.Name),
            model => Assert.Equal("beta", model.Name));
        Assert.Equal("http://localhost:1234/models", _handler.LastRequestUri?.ToString());
    }

    [Fact]
    public async Task CompleteAsync_When_BackendReturnsError_Returns_Backend_Specific_Error()
    {
        // Arrange
        _handler.ResponseFactory = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadGateway)
        {
            Content = new StringContent("upstream failed", Encoding.UTF8, "text/plain"),
        });
        var prompt = new AiPrompt("system prompt", "user prompt", 0.7m, 321);

        // Act
        var response = await _service.CompleteAsync(prompt);

        // Assert
        Assert.False(response.Success);
        Assert.Equal(
            "Test Backend returned status BadGateway: upstream failed",
            response.ErrorMessage);
        Assert.Equal("http://localhost:1234/chat/completions", _handler.LastRequestUri?.ToString());
        Assert.Contains("\"temperature\":0.7", _handler.LastRequestBody, StringComparison.Ordinal);
        Assert.Contains("\"max_tokens\":321", _handler.LastRequestBody, StringComparison.Ordinal);
        Assert.Contains("\"system_prompt\":\"system prompt\"", _handler.LastRequestBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CompleteAsync_When_BackendTimesOut_Returns_Timeout_Response()
    {
        // Arrange
        var settingsService = new FakeAppSettingsService(new AiSettingsData(
            EndpointUrl: "http://localhost:1234",
            ModelName: "test-model",
            Temperature: 0.3m,
            MaxTokens: 200,
            TimeoutSeconds: 1,
            IsEnabled: true));
        using var httpClient = new HttpClient(_handler);
        var service = new TestOpenAiCompatibleAiService(
            httpClient,
            settingsService,
            NullLogger<TestOpenAiCompatibleAiService>.Instance);
        _handler.ResponseFactory = async (_, cancellationToken) =>
        {
            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        };

        // Act
        var response = await service.CompleteAsync(new AiPrompt("system", "user"));

        // Assert
        Assert.False(response.Success);
        Assert.Equal("AI request timed out after 1 seconds.", response.ErrorMessage);
    }

    private sealed class RecordingHttpMessageHandler : HttpMessageHandler
    {
        public Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> ResponseFactory { get; set; } =
            (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));

        public Uri? LastRequestUri
        {
            get; private set;
        }

        public string LastRequestBody { get; private set; } = string.Empty;

        /// <inheritdoc />
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequestUri = request.RequestUri;
            LastRequestBody = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken);

            return await ResponseFactory(request, cancellationToken);
        }
    }

    private sealed class TestOpenAiCompatibleAiService : OpenAiCompatibleAiService
    {
        public TestOpenAiCompatibleAiService(
            HttpClient httpClient,
            IAppSettingsService settingsService,
            ILogger<TestOpenAiCompatibleAiService> logger)
            : base(httpClient, settingsService, logger)
        {
        }

        protected override string GetBackendDisplayName() => "Test Backend";

        protected override string GetCompletionEndpoint() => "chat/completions";

        protected override string GetHealthCheckEndpoint() => "health";

        protected override string GetModelsEndpoint() => "models";

        protected override object CreateCompletionRequest(AiPrompt prompt, AiSettingsData settings)
        {
            return new TestCompletionRequest
            {
                Model = settings.ModelName,
                SystemPrompt = prompt.SystemPrompt,
                UserPrompt = prompt.UserPrompt,
                Temperature = prompt.Temperature,
                MaxTokens = prompt.MaxTokens,
            };
        }

        protected override async Task<AiResponse> ParseCompletionResponseAsync(
            HttpContent content,
            Stopwatch stopwatch,
            CancellationToken cancellationToken)
        {
            var response = await content.ReadAsStringAsync(cancellationToken);
            stopwatch.Stop();

            return new AiResponse(true, response, null, 42, stopwatch.Elapsed);
        }

        protected override async Task<IReadOnlyList<AiModelInfo>> ParseModelsResponseAsync(
            HttpContent content,
            CancellationToken cancellationToken)
        {
            var json = await content.ReadAsStringAsync(cancellationToken);
            var modelNames = JsonSerializer.Deserialize<string[]>(json) ?? [];

            return modelNames
                .Select(modelName => new AiModelInfo(modelName, DateTime.UnixEpoch, 0))
                .ToList();
        }

        private sealed class TestCompletionRequest
        {
            public string Model { get; set; } = string.Empty;

            public string SystemPrompt { get; set; } = string.Empty;

            public string UserPrompt { get; set; } = string.Empty;

            public decimal Temperature { get; set; }

            public int MaxTokens { get; set; }
        }
    }
}
