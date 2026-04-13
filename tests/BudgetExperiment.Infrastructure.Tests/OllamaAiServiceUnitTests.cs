// <copyright file="OllamaAiServiceUnitTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Net;
using System.Text;
using System.Text.Json;

using BudgetExperiment.Application.Ai;
using BudgetExperiment.Application.Settings;
using BudgetExperiment.Infrastructure.ExternalServices.AI;

using Microsoft.Extensions.Logging.Abstractions;

namespace BudgetExperiment.Infrastructure.Tests;

/// <summary>
/// Unit tests for <see cref="OllamaAiService"/> using a fake HTTP handler.
/// </summary>
public sealed class OllamaAiServiceUnitTests : IDisposable
{
    private readonly RecordingHttpMessageHandler _handler;
    private readonly HttpClient _httpClient;
    private readonly OllamaAiService _service;

    /// <summary>
    /// Initializes a new instance of the <see cref="OllamaAiServiceUnitTests"/> class.
    /// </summary>
    public OllamaAiServiceUnitTests()
    {
        _handler = new RecordingHttpMessageHandler();
        _httpClient = new HttpClient(_handler);
        _service = new OllamaAiService(
            _httpClient,
            new FakeAppSettingsService(new AiSettingsData(
                EndpointUrl: "http://localhost:11434/",
                ModelName: "llama3.2",
                Temperature: 0.3m,
                MaxTokens: 200,
                TimeoutSeconds: 30,
                IsEnabled: true)),
            NullLogger<OllamaAiService>.Instance);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _httpClient.Dispose();
        _handler.Dispose();
    }

    [Fact]
    public async Task GetStatusAsync_Uses_Ollama_Version_Endpoint()
    {
        // Arrange
        _handler.ResponseFactory = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));

        // Act
        var status = await _service.GetStatusAsync();

        // Assert
        Assert.True(status.IsAvailable);
        Assert.Equal("http://localhost:11434/api/version", _handler.LastRequestUri?.ToString());
    }

    [Fact]
    public async Task GetAvailableModelsAsync_Parses_Ollama_Tags_Response()
    {
        // Arrange
        _handler.ResponseFactory = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """
                {
                  "models": [
                    {
                      "name": "llama3.2",
                      "modified_at": "2026-04-13T00:00:00Z",
                      "size": 123456
                    }
                  ]
                }
                """,
                Encoding.UTF8,
                "application/json"),
        });

        // Act
        var models = await _service.GetAvailableModelsAsync();

        // Assert
        var model = Assert.Single(models);
        Assert.Equal("llama3.2", model.Name);
        Assert.Equal(123456, model.SizeBytes);
        Assert.Equal("http://localhost:11434/api/tags", _handler.LastRequestUri?.ToString());
    }

    [Fact]
    public async Task CompleteAsync_Sends_Ollama_Request_And_Parses_Tokens()
    {
        // Arrange
        _handler.ResponseFactory = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """
                {
                  "model": "llama3.2",
                  "created_at": "2026-04-13T00:00:00Z",
                  "message": {
                    "role": "assistant",
                    "content": "Budget summary"
                  },
                  "done": true,
                  "prompt_eval_count": 11,
                  "eval_count": 12
                }
                """,
                Encoding.UTF8,
                "application/json"),
        });
        var prompt = new AiPrompt("system prompt", "user prompt", 0.65m, 222);

        // Act
        var response = await _service.CompleteAsync(prompt);

        // Assert
        Assert.True(response.Success);
        Assert.Equal("Budget summary", response.Content);
        Assert.Equal(23, response.TokensUsed);
        Assert.Equal("http://localhost:11434/api/chat", _handler.LastRequestUri?.ToString());

        using var document = JsonDocument.Parse(_handler.LastRequestBody);
        var root = document.RootElement;
        Assert.Equal("llama3.2", root.GetProperty("model").GetString());
        Assert.False(root.GetProperty("stream").GetBoolean());
        Assert.Equal(2, root.GetProperty("messages").GetArrayLength());
        Assert.Equal("system", root.GetProperty("messages")[0].GetProperty("role").GetString());
        Assert.Equal("user", root.GetProperty("messages")[1].GetProperty("role").GetString());
        Assert.Equal(0.65d, root.GetProperty("options").GetProperty("temperature").GetDouble(), 3);
        Assert.Equal(222, root.GetProperty("options").GetProperty("num_predict").GetInt32());
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
}
