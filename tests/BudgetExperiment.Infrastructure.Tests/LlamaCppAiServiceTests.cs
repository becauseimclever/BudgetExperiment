// <copyright file="LlamaCppAiServiceTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Net;
using System.Text;
using System.Text.Json;

using BudgetExperiment.Application.Ai;
using BudgetExperiment.Application.Settings;
using BudgetExperiment.Infrastructure.ExternalServices.AI;
using BudgetExperiment.Shared;

using Microsoft.Extensions.Logging.Abstractions;

namespace BudgetExperiment.Infrastructure.Tests;

/// <summary>
/// Unit tests for <see cref="LlamaCppAiService"/> using a fake HTTP handler.
/// </summary>
public sealed class LlamaCppAiServiceTests : IDisposable
{
    private readonly RecordingHttpMessageHandler _handler;
    private readonly HttpClient _httpClient;
    private readonly LlamaCppAiService _service;

    /// <summary>
    /// Initializes a new instance of the <see cref="LlamaCppAiServiceTests"/> class.
    /// </summary>
    public LlamaCppAiServiceTests()
    {
        _handler = new RecordingHttpMessageHandler();
        _httpClient = new HttpClient(_handler);
        _service = new LlamaCppAiService(
            _httpClient,
            new FakeAppSettingsService(new AiSettingsData(
                EndpointUrl: "http://localhost:8080/",
                ModelName: "llama-3.2",
                Temperature: 0.3m,
                MaxTokens: 200,
                TimeoutSeconds: 30,
                IsEnabled: true,
                BackendType: AiBackendType.LlamaCpp)),
            NullLogger<LlamaCppAiService>.Instance);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _httpClient.Dispose();
        _handler.Dispose();
    }

    [Fact]
    public async Task GetStatusAsync_Uses_LlamaCpp_Health_Endpoint()
    {
        // Arrange
        _handler.ResponseFactory = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));

        // Act
        var status = await _service.GetStatusAsync();

        // Assert
        Assert.True(status.IsAvailable);
        Assert.Equal("http://localhost:8080/health", _handler.LastRequestUri?.ToString());
    }

    [Fact]
    public async Task GetAvailableModelsAsync_Parses_OpenAi_Compatible_Models_Response()
    {
        // Arrange
        _handler.ResponseFactory = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """
                {
                  "data": [
                    {
                      "id": "llama-3.2"
                    },
                    {
                      "id": "mistral"
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
        Assert.Collection(
            models,
            model =>
            {
                Assert.Equal("llama-3.2", model.Name);
                Assert.Equal(DateTime.UnixEpoch, model.ModifiedAt);
                Assert.Equal(0, model.SizeBytes);
            },
            model => Assert.Equal("mistral", model.Name));
        Assert.Equal("http://localhost:8080/v1/models", _handler.LastRequestUri?.ToString());
    }

    [Fact]
    public async Task CompleteAsync_Sends_OpenAi_Request_And_Parses_TotalTokens()
    {
        // Arrange
        _handler.ResponseFactory = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """
                {
                  "choices": [
                    {
                      "message": {
                        "role": "assistant",
                        "content": "Budget summary"
                      }
                    }
                  ],
                  "usage": {
                    "prompt_tokens": 11,
                    "completion_tokens": 12,
                    "total_tokens": 23
                  }
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
        Assert.Equal("http://localhost:8080/v1/chat/completions", _handler.LastRequestUri?.ToString());

        using var document = JsonDocument.Parse(_handler.LastRequestBody);
        var root = document.RootElement;
        Assert.Equal("llama-3.2", root.GetProperty("model").GetString());
        Assert.Equal(2, root.GetProperty("messages").GetArrayLength());
        Assert.Equal("system", root.GetProperty("messages")[0].GetProperty("role").GetString());
        Assert.Equal("user", root.GetProperty("messages")[1].GetProperty("role").GetString());
        Assert.Equal(0.65d, root.GetProperty("temperature").GetDouble(), 3);
        Assert.Equal(222, root.GetProperty("max_tokens").GetInt32());
    }

    [Fact]
    public async Task CompleteAsync_When_TotalTokensMissing_Sums_Prompt_And_Completion_Tokens()
    {
        // Arrange
        _handler.ResponseFactory = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """
                {
                  "choices": [
                    {
                      "message": {
                        "role": "assistant",
                        "content": "Budget summary"
                      }
                    }
                  ],
                  "usage": {
                    "prompt_tokens": 8,
                    "completion_tokens": 13
                  }
                }
                """,
                Encoding.UTF8,
                "application/json"),
        });

        // Act
        var response = await _service.CompleteAsync(new AiPrompt("system prompt", "user prompt", 0.65m, 222));

        // Assert
        Assert.True(response.Success);
        Assert.Equal(21, response.TokensUsed);
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
