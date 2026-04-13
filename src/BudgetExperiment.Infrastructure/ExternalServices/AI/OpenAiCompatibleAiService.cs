// <copyright file="OpenAiCompatibleAiService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

using BudgetExperiment.Application.Ai;
using BudgetExperiment.Application.Settings;

using Microsoft.Extensions.Logging;

namespace BudgetExperiment.Infrastructure.ExternalServices.AI;

/// <summary>
/// Base class for AI backends that share the same HTTP execution flow.
/// </summary>
public abstract class OpenAiCompatibleAiService : IAiService
{
    /// <summary>
    /// Shared JSON serializer settings for AI backend requests and responses.
    /// </summary>
    protected static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;
    private readonly IAppSettingsService _settingsService;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenAiCompatibleAiService"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client.</param>
    /// <param name="settingsService">The application settings service.</param>
    /// <param name="logger">The logger.</param>
    protected OpenAiCompatibleAiService(
        HttpClient httpClient,
        IAppSettingsService settingsService,
        ILogger logger)
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
            var response = await _httpClient.GetAsync(
                BuildEndpoint(settings.EndpointUrl, GetHealthCheckEndpoint()),
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return new AiServiceStatus(true, settings.ModelName, null);
            }

            return new AiServiceStatus(false, null, $"{GetBackendDisplayName()} returned status {response.StatusCode}");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Failed to connect to {Backend} at {Endpoint}", GetBackendDisplayName(), settings.EndpointUrl);
            return new AiServiceStatus(false, null, $"Failed to connect to {GetBackendDisplayName()}: {ex.Message}");
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogWarning("Connection to {Backend} timed out", GetBackendDisplayName());
            return new AiServiceStatus(false, null, $"Connection to {GetBackendDisplayName()} timed out.");
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
            using var response = await _httpClient.GetAsync(
                BuildEndpoint(settings.EndpointUrl, GetModelsEndpoint()),
                cancellationToken);
            response.EnsureSuccessStatusCode();

            return await ParseModelsResponseAsync(response.Content, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to list {Backend} models", GetBackendDisplayName());
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
            var request = CreateCompletionRequest(prompt, settings);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(settings.TimeoutSeconds));

            using var response = await _httpClient.PostAsJsonAsync(
                BuildEndpoint(settings.EndpointUrl, GetCompletionEndpoint()),
                request,
                JsonOptions,
                cts.Token);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cts.Token);
                stopwatch.Stop();

                _logger.LogWarning(
                    "{Backend} request failed with status {Status}: {Error}",
                    GetBackendDisplayName(),
                    response.StatusCode,
                    errorContent);

                return new AiResponse(
                    false,
                    string.Empty,
                    $"{GetBackendDisplayName()} returned status {response.StatusCode}: {errorContent}",
                    0,
                    stopwatch.Elapsed);
            }

            return await ParseCompletionResponseAsync(response.Content, stopwatch, cts.Token);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();
            throw;
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            _logger.LogWarning("{Backend} request timed out after {Timeout} seconds", GetBackendDisplayName(), settings.TimeoutSeconds);
            return new AiResponse(
                false,
                string.Empty,
                $"AI request timed out after {settings.TimeoutSeconds} seconds.",
                0,
                stopwatch.Elapsed);
        }
        catch (HttpRequestException ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Failed to communicate with {Backend}", GetBackendDisplayName());
            return new AiResponse(
                false,
                string.Empty,
                $"Failed to communicate with {GetBackendDisplayName()}: {ex.Message}",
                0,
                stopwatch.Elapsed);
        }
        catch (JsonException ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Failed to parse {Backend} response", GetBackendDisplayName());
            return new AiResponse(
                false,
                string.Empty,
                $"Failed to parse {GetBackendDisplayName()} response: {ex.Message}",
                0,
                stopwatch.Elapsed);
        }
    }

    /// <summary>
    /// Gets the backend display name used in log and error messages.
    /// </summary>
    /// <returns>The backend display name.</returns>
    protected abstract string GetBackendDisplayName();

    /// <summary>
    /// Gets the relative health check endpoint.
    /// </summary>
    /// <returns>The health check endpoint.</returns>
    protected abstract string GetHealthCheckEndpoint();

    /// <summary>
    /// Gets the relative models endpoint.
    /// </summary>
    /// <returns>The models endpoint.</returns>
    protected abstract string GetModelsEndpoint();

    /// <summary>
    /// Gets the relative completion endpoint.
    /// </summary>
    /// <returns>The completion endpoint.</returns>
    protected abstract string GetCompletionEndpoint();

    /// <summary>
    /// Builds a backend-specific completion request payload.
    /// </summary>
    /// <param name="prompt">The prompt to send.</param>
    /// <param name="settings">The active AI settings.</param>
    /// <returns>The backend request payload.</returns>
    protected abstract object CreateCompletionRequest(AiPrompt prompt, AiSettingsData settings);

    /// <summary>
    /// Parses the backend-specific models response.
    /// </summary>
    /// <param name="content">The HTTP response content.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The available models.</returns>
    protected abstract Task<IReadOnlyList<AiModelInfo>> ParseModelsResponseAsync(HttpContent content, CancellationToken cancellationToken);

    /// <summary>
    /// Parses the backend-specific completion response.
    /// </summary>
    /// <param name="content">The HTTP response content.</param>
    /// <param name="stopwatch">The request stopwatch.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The parsed AI response.</returns>
    protected abstract Task<AiResponse> ParseCompletionResponseAsync(HttpContent content, Stopwatch stopwatch, CancellationToken cancellationToken);

    /// <summary>
    /// Creates a standard OpenAI-compatible completion request payload.
    /// </summary>
    /// <param name="prompt">The prompt to send.</param>
    /// <param name="settings">The active AI settings.</param>
    /// <returns>The OpenAI-compatible request payload.</returns>
    protected OpenAiChatRequest CreateOpenAiCompatibleCompletionRequest(AiPrompt prompt, AiSettingsData settings)
    {
        return new OpenAiChatRequest
        {
            Model = settings.ModelName,
            Messages =
            [
                new OpenAiChatMessage
                {
                    Role = "system",
                    Content = prompt.SystemPrompt,
                },
                new OpenAiChatMessage
                {
                    Role = "user",
                    Content = prompt.UserPrompt,
                },
            ],
            Temperature = (float)prompt.Temperature,
            MaxTokens = prompt.MaxTokens,
        };
    }

    /// <summary>
    /// Parses a standard OpenAI-compatible completion response.
    /// </summary>
    /// <param name="content">The HTTP response content.</param>
    /// <param name="stopwatch">The request stopwatch.</param>
    /// <param name="backendDisplayName">The backend display name for error messages.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The parsed AI response.</returns>
    protected async Task<AiResponse> ParseOpenAiCompatibleCompletionResponseAsync(
        HttpContent content,
        Stopwatch stopwatch,
        string backendDisplayName,
        CancellationToken cancellationToken)
    {
        var chatResponse = await content.ReadFromJsonAsync<OpenAiChatResponse>(JsonOptions, cancellationToken);
        stopwatch.Stop();

        if (chatResponse?.Choices?.FirstOrDefault() is not { } choice)
        {
            return new AiResponse(
                false,
                string.Empty,
                $"Failed to parse {backendDisplayName} response.",
                0,
                stopwatch.Elapsed);
        }

        var tokensUsed = chatResponse.Usage?.TotalTokens > 0
            ? chatResponse.Usage.TotalTokens
            : (chatResponse.Usage?.PromptTokens ?? 0) + (chatResponse.Usage?.CompletionTokens ?? 0);

        return new AiResponse(
            true,
            choice.Message?.Content ?? string.Empty,
            null,
            tokensUsed,
            stopwatch.Elapsed);
    }

    private static string BuildEndpoint(string endpointUrl, string relativePath)
    {
        return $"{endpointUrl.TrimEnd('/')}/{relativePath.TrimStart('/')}";
    }

    /// <summary>
    /// Shared OpenAI-compatible chat completion request.
    /// </summary>
    protected sealed class OpenAiChatRequest
    {
        /// <summary>
        /// Gets or sets the model name.
        /// </summary>
        public string Model { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the chat messages.
        /// </summary>
        public OpenAiChatMessage[] Messages { get; set; } = [];

        /// <summary>
        /// Gets or sets the generation temperature.
        /// </summary>
        public float Temperature { get; set; }

        /// <summary>
        /// Gets or sets the maximum completion tokens.
        /// </summary>
        public int MaxTokens { get; set; }
    }

    /// <summary>
    /// Shared OpenAI-compatible chat message.
    /// </summary>
    protected sealed class OpenAiChatMessage
    {
        /// <summary>
        /// Gets or sets the message role.
        /// </summary>
        public string Role { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the message content.
        /// </summary>
        public string Content { get; set; } = string.Empty;
    }

    /// <summary>
    /// Shared OpenAI-compatible chat completion response.
    /// </summary>
    protected sealed class OpenAiChatResponse
    {
        /// <summary>
        /// Gets or sets the response choices.
        /// </summary>
        public OpenAiChoice[]? Choices { get; set; }

        /// <summary>
        /// Gets or sets the token usage.
        /// </summary>
        public OpenAiUsage? Usage { get; set; }
    }

    /// <summary>
    /// Shared OpenAI-compatible response choice.
    /// </summary>
    protected sealed class OpenAiChoice
    {
        /// <summary>
        /// Gets or sets the generated assistant message.
        /// </summary>
        public OpenAiChatMessage? Message { get; set; }
    }

    /// <summary>
    /// Shared OpenAI-compatible token usage metadata.
    /// </summary>
    protected sealed class OpenAiUsage
    {
        /// <summary>
        /// Gets or sets the prompt token count.
        /// </summary>
        public int PromptTokens { get; set; }

        /// <summary>
        /// Gets or sets the completion token count.
        /// </summary>
        public int CompletionTokens { get; set; }

        /// <summary>
        /// Gets or sets the total token count.
        /// </summary>
        public int TotalTokens { get; set; }
    }
}
