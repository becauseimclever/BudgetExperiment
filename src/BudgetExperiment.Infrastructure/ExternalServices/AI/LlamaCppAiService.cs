// <copyright file="LlamaCppAiService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Diagnostics;
using System.Net.Http.Json;

using Microsoft.Extensions.Logging;

namespace BudgetExperiment.Infrastructure.ExternalServices.AI;

/// <summary>
/// AI service implementation using llama.cpp's OpenAI-compatible server.
/// </summary>
public sealed class LlamaCppAiService : OpenAiCompatibleAiService
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LlamaCppAiService"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client.</param>
    /// <param name="settingsService">The application settings service.</param>
    /// <param name="logger">The logger.</param>
    public LlamaCppAiService(
        HttpClient httpClient,
        IAppSettingsService settingsService,
        ILogger<LlamaCppAiService> logger)
        : base(httpClient, settingsService, logger)
    {
    }

    /// <inheritdoc/>
    protected override string GetBackendDisplayName() => "llama.cpp";

    /// <inheritdoc/>
    protected override string GetHealthCheckEndpoint() => "health";

    /// <inheritdoc/>
    protected override string GetModelsEndpoint() => "v1/models";

    /// <inheritdoc/>
    protected override string GetCompletionEndpoint() => "v1/chat/completions";

    /// <inheritdoc/>
    protected override object CreateCompletionRequest(AiPrompt prompt, AiSettingsData settings)
    {
        return CreateOpenAiCompatibleCompletionRequest(prompt, settings);
    }

    /// <inheritdoc/>
    protected override async Task<IReadOnlyList<AiModelInfo>> ParseModelsResponseAsync(HttpContent content, CancellationToken cancellationToken)
    {
        var modelsResponse = await content.ReadFromJsonAsync<LlamaCppModelsResponse>(JsonOptions, cancellationToken);

        if (modelsResponse?.Data == null)
        {
            return Array.Empty<AiModelInfo>();
        }

        return modelsResponse.Data
            .Select(model => new AiModelInfo(
                model.Id ?? string.Empty,
                DateTime.UnixEpoch,
                0))
            .ToList();
    }

    /// <inheritdoc/>
    protected override Task<AiResponse> ParseCompletionResponseAsync(
        HttpContent content,
        Stopwatch stopwatch,
        CancellationToken cancellationToken)
    {
        return ParseOpenAiCompatibleCompletionResponseAsync(
            content,
            stopwatch,
            GetBackendDisplayName(),
            cancellationToken);
    }

    private sealed class LlamaCppModelsResponse
    {
        public List<LlamaCppModel>? Data
        {
            get; set;
        }
    }

    private sealed class LlamaCppModel
    {
        public string? Id
        {
            get; set;
        }
    }
}
