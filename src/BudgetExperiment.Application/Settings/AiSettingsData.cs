// <copyright file="AiSettingsData.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Shared;

namespace BudgetExperiment.Application.Settings;

/// <summary>
/// AI settings data transfer object for internal use.
/// </summary>
/// <param name="EndpointUrl">The AI backend endpoint URL.</param>
/// <param name="ModelName">The AI model name to use.</param>
/// <param name="Temperature">The temperature for AI generation (0.0 to 1.0).</param>
/// <param name="MaxTokens">The maximum tokens for AI responses.</param>
/// <param name="TimeoutSeconds">The AI request timeout in seconds.</param>
/// <param name="IsEnabled">Whether AI features are enabled.</param>
/// <param name="BackendType">The configured AI backend type.</param>
public sealed record AiSettingsData(
    string EndpointUrl,
    string ModelName,
    decimal Temperature,
    int MaxTokens,
    int TimeoutSeconds,
    bool IsEnabled,
    AiBackendType BackendType = AiBackendType.Ollama)
{
    /// <summary>
    /// Gets the endpoint URL using the legacy Ollama-specific name.
    /// </summary>
    public string OllamaEndpoint => EndpointUrl;
}
