// <copyright file="OllamaAiService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using BudgetExperiment.Application.Services;
using Microsoft.Extensions.Logging;

namespace BudgetExperiment.Infrastructure.ExternalServices.AI;

/// <summary>
/// AI service implementation using Ollama local models.
/// </summary>
public sealed class OllamaAiService : IAiService
{
    private readonly HttpClient _httpClient;
    private readonly IAppSettingsService _settingsService;
    private readonly ILogger<OllamaAiService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="OllamaAiService"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client.</param>
    /// <param name="settingsService">The application settings service.</param>
    /// <param name="logger">The logger.</param>
    public OllamaAiService(
        HttpClient httpClient,
        IAppSettingsService settingsService,
        ILogger<OllamaAiService> logger)
    {
        _httpClient = httpClient;
        _settingsService = settingsService;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<AiServiceStatus> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _settingsService.GetAiSettingsAsync(cancellationToken);

        if (!settings.IsEnabled)
        {
            return new AiServiceStatus(false, null, "AI features are disabled.");
        }

        try
        {
            var baseUri = settings.OllamaEndpoint.TrimEnd('/');
            var response = await _httpClient.GetAsync($"{baseUri}/api/version", cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return new AiServiceStatus(true, settings.ModelName, null);
            }

            return new AiServiceStatus(false, null, $"Ollama returned status {response.StatusCode}");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Failed to connect to Ollama at {Endpoint}", settings.OllamaEndpoint);
            return new AiServiceStatus(false, null, $"Failed to connect to Ollama: {ex.Message}");
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogWarning("Connection to Ollama timed out");
            return new AiServiceStatus(false, null, "Connection to Ollama timed out.");
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<AiModelInfo>> GetAvailableModelsAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _settingsService.GetAiSettingsAsync(cancellationToken);

        if (!settings.IsEnabled)
        {
            return Array.Empty<AiModelInfo>();
        }

        try
        {
            var baseUri = settings.OllamaEndpoint.TrimEnd('/');
            var response = await _httpClient.GetAsync($"{baseUri}/api/tags", cancellationToken);
            response.EnsureSuccessStatusCode();

            var tagsResponse = await response.Content.ReadFromJsonAsync<OllamaTagsResponse>(JsonOptions, cancellationToken);

            if (tagsResponse?.Models == null)
            {
                return Array.Empty<AiModelInfo>();
            }

            return tagsResponse.Models
                .Select(m => new AiModelInfo(
                    m.Name ?? string.Empty,
                    m.ModifiedAt,
                    m.Size))
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to list Ollama models");
            return Array.Empty<AiModelInfo>();
        }
    }

    /// <inheritdoc/>
    public async Task<AiResponse> CompleteAsync(AiPrompt prompt, CancellationToken cancellationToken = default)
    {
        var settings = await _settingsService.GetAiSettingsAsync(cancellationToken);
        var stopwatch = Stopwatch.StartNew();

        if (!settings.IsEnabled)
        {
            return new AiResponse(
                false,
                string.Empty,
                "AI features are disabled.",
                0,
                stopwatch.Elapsed);
        }

        try
        {
            var request = new OllamaChatRequest
            {
                Model = settings.ModelName,
                Messages = new List<OllamaChatMessage>
                {
                    new() { Role = "system", Content = prompt.SystemPrompt },
                    new() { Role = "user", Content = prompt.UserPrompt },
                },
                Stream = false,
                Options = new OllamaOptions
                {
                    Temperature = (float)prompt.Temperature,
                    NumPredict = prompt.MaxTokens,
                },
            };

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(settings.TimeoutSeconds));

            var baseUri = settings.OllamaEndpoint.TrimEnd('/');
            var response = await _httpClient.PostAsJsonAsync($"{baseUri}/api/chat", request, JsonOptions, cts.Token);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cts.Token);
                _logger.LogWarning("Ollama request failed with status {Status}: {Error}", response.StatusCode, errorContent);

                return new AiResponse(
                    false,
                    string.Empty,
                    $"Ollama returned status {response.StatusCode}: {errorContent}",
                    0,
                    stopwatch.Elapsed);
            }

            var chatResponse = await response.Content.ReadFromJsonAsync<OllamaChatResponse>(JsonOptions, cts.Token);

            stopwatch.Stop();

            if (chatResponse == null)
            {
                return new AiResponse(
                    false,
                    string.Empty,
                    "Failed to parse Ollama response.",
                    0,
                    stopwatch.Elapsed);
            }

            var tokensUsed = (chatResponse.PromptEvalCount ?? 0) + (chatResponse.EvalCount ?? 0);

            return new AiResponse(
                true,
                chatResponse.Message?.Content ?? string.Empty,
                null,
                tokensUsed,
                stopwatch.Elapsed);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Ollama request timed out after {Timeout} seconds", settings.TimeoutSeconds);
            return new AiResponse(
                false,
                string.Empty,
                $"Request timed out after {settings.TimeoutSeconds} seconds.",
                0,
                stopwatch.Elapsed);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to communicate with Ollama");
            return new AiResponse(
                false,
                string.Empty,
                $"Failed to communicate with Ollama: {ex.Message}",
                0,
                stopwatch.Elapsed);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse Ollama response");
            return new AiResponse(
                false,
                string.Empty,
                $"Failed to parse Ollama response: {ex.Message}",
                0,
                stopwatch.Elapsed);
        }
    }

    #region Ollama API DTOs

    private sealed class OllamaTagsResponse
    {
        public List<OllamaModelInfo>? Models { get; set; }
    }

    private sealed class OllamaModelInfo
    {
        public string? Name { get; set; }

        public DateTime ModifiedAt { get; set; }

        public long Size { get; set; }
    }

    private sealed class OllamaChatRequest
    {
        public string Model { get; set; } = string.Empty;

        public List<OllamaChatMessage> Messages { get; set; } = new();

        public bool Stream { get; set; }

        public OllamaOptions? Options { get; set; }
    }

    private sealed class OllamaChatMessage
    {
        public string Role { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;
    }

    private sealed class OllamaOptions
    {
        public float Temperature { get; set; }

        public int NumPredict { get; set; }
    }

    private sealed class OllamaChatResponse
    {
        public string? Model { get; set; }

        public DateTime CreatedAt { get; set; }

        public OllamaChatMessage? Message { get; set; }

        public bool Done { get; set; }

        public long? TotalDuration { get; set; }

        public long? LoadDuration { get; set; }

        public int? PromptEvalCount { get; set; }

        public long? PromptEvalDuration { get; set; }

        public int? EvalCount { get; set; }

        public long? EvalDuration { get; set; }
    }

    #endregion
}
