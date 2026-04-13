// <copyright file="AiSettingsDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Text.Json.Serialization;

using BudgetExperiment.Shared;

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// DTO for AI settings configuration.
/// </summary>
public sealed class AiSettingsDto
{
    private string? _endpointUrl = AiBackendDefaults.DefaultOllamaEndpointUrl;
    private string? _ollamaEndpoint;

    /// <summary>
    /// Gets or sets the AI backend endpoint URL.
    /// </summary>
    public string EndpointUrl
    {
        get => _endpointUrl ?? _ollamaEndpoint ?? AiBackendDefaults.GetDefaultEndpointUrl(BackendType);
        set
        {
            _endpointUrl = value;
            HasExplicitEndpointUrl = true;
        }
    }

    /// <summary>
    /// Gets or sets the legacy Ollama API endpoint URL alias.
    /// </summary>
    public string OllamaEndpoint
    {
        get => _ollamaEndpoint ?? _endpointUrl ?? AiBackendDefaults.GetDefaultEndpointUrl(BackendType);
        set
        {
            _ollamaEndpoint = value;
            HasExplicitOllamaEndpoint = true;
        }
    }

    /// <summary>
    /// Gets a value indicating whether <see cref="EndpointUrl"/> was explicitly provided.
    /// </summary>
    [JsonIgnore]
    public bool HasExplicitEndpointUrl
    {
        get;
        private set;
    }

    /// <summary>
    /// Gets a value indicating whether <see cref="OllamaEndpoint"/> was explicitly provided.
    /// </summary>
    [JsonIgnore]
    public bool HasExplicitOllamaEndpoint
    {
        get;
        private set;
    }

    /// <summary>
    /// Gets or sets the model name to use.
    /// </summary>
    public string ModelName { get; set; } = "llama3.2";

    /// <summary>
    /// Gets or sets the temperature for generation (0.0 to 1.0).
    /// </summary>
    public decimal Temperature { get; set; } = 0.3m;

    /// <summary>
    /// Gets or sets the maximum tokens for responses.
    /// </summary>
    public int MaxTokens { get; set; } = 2000;

    /// <summary>
    /// Gets or sets the request timeout in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 120;

    /// <summary>
    /// Gets or sets a value indicating whether AI features are enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the configured AI backend type.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter<AiBackendType>))]
    public AiBackendType BackendType { get; set; } = AiBackendType.Ollama;

    /// <summary>
    /// Resolves the effective endpoint URL, preferring explicitly provided values and otherwise
    /// falling back to the backend-specific default.
    /// </summary>
    /// <returns>The effective endpoint URL.</returns>
    public string ResolveEndpointUrl()
    {
        if (HasExplicitEndpointUrl && !string.IsNullOrWhiteSpace(_endpointUrl))
        {
            return _endpointUrl.Trim();
        }

        if (HasExplicitOllamaEndpoint && !string.IsNullOrWhiteSpace(_ollamaEndpoint))
        {
            return _ollamaEndpoint.Trim();
        }

        return AiBackendDefaults.GetDefaultEndpointUrl(BackendType);
    }
}
