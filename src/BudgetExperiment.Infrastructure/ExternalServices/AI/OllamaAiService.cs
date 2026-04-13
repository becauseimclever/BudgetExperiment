// <copyright file="OllamaAiService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Diagnostics;
using System.Net.Http.Json;

using Microsoft.Extensions.Logging;

namespace BudgetExperiment.Infrastructure.ExternalServices.AI;

/// <summary>
/// AI service implementation using Ollama local models.
/// </summary>
public sealed class OllamaAiService : OpenAiCompatibleAiService
{
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
        : base(httpClient, settingsService, logger)
    {
    }

    /// <inheritdoc/>
    protected override string GetBackendDisplayName() => "Ollama";

    /// <inheritdoc/>
    protected override string GetHealthCheckEndpoint() => "api/version";

    /// <inheritdoc/>
    protected override string GetModelsEndpoint() => "api/tags";

    /// <inheritdoc/>
    protected override string GetCompletionEndpoint() => "api/chat";

    /// <inheritdoc/>
    protected override object CreateCompletionRequest(AiPrompt prompt, AiSettingsData settings)
    {
        return new OllamaChatRequest
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
    }

    /// <inheritdoc/>
    protected override async Task<IReadOnlyList<AiModelInfo>> ParseModelsResponseAsync(HttpContent content, CancellationToken cancellationToken)
    {
        var tagsResponse = await content.ReadFromJsonAsync<OllamaTagsResponse>(JsonOptions, cancellationToken);

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

    /// <inheritdoc/>
    protected override async Task<AiResponse> ParseCompletionResponseAsync(
        HttpContent content,
        Stopwatch stopwatch,
        CancellationToken cancellationToken)
    {
        var chatResponse = await content.ReadFromJsonAsync<OllamaChatResponse>(JsonOptions, cancellationToken);
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

    private sealed class OllamaTagsResponse
    {
        public List<OllamaModelInfo>? Models
        {
            get; set;
        }
    }

    private sealed class OllamaModelInfo
    {
        public string? Name
        {
            get; set;
        }

        public DateTime ModifiedAt
        {
            get; set;
        }

        public long Size
        {
            get; set;
        }
    }

    private sealed class OllamaChatRequest
    {
        public string Model { get; set; } = string.Empty;

        public List<OllamaChatMessage> Messages { get; set; } = new();

        public bool Stream
        {
            get; set;
        }

        public OllamaOptions? Options
        {
            get; set;
        }
    }

    private sealed class OllamaChatMessage
    {
        public string Role { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;
    }

    private sealed class OllamaOptions
    {
        public float Temperature
        {
            get; set;
        }

        public int NumPredict
        {
            get; set;
        }
    }

    private sealed class OllamaChatResponse
    {
        public string? Model
        {
            get; set;
        }

        public DateTime CreatedAt
        {
            get; set;
        }

        public OllamaChatMessage? Message
        {
            get; set;
        }

        public bool Done
        {
            get; set;
        }

        public long? TotalDuration
        {
            get; set;
        }

        public long? LoadDuration
        {
            get; set;
        }

        public int? PromptEvalCount
        {
            get; set;
        }

        public long? PromptEvalDuration
        {
            get; set;
        }

        public int? EvalCount
        {
            get; set;
        }

        public long? EvalDuration
        {
            get; set;
        }
    }
}
