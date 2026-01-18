// <copyright file="IAiSettingsProvider.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Application.Services;

/// <summary>
/// Provides AI settings from the database.
/// </summary>
public interface IAiSettingsProvider
{
    /// <summary>
    /// Gets the current AI settings.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The current AI settings.</returns>
    Task<AiSettingsData> GetSettingsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the AI settings.
    /// </summary>
    /// <param name="settings">The new settings.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated settings.</returns>
    Task<AiSettingsData> UpdateSettingsAsync(AiSettingsData settings, CancellationToken cancellationToken = default);
}

/// <summary>
/// AI settings data transfer object for internal use.
/// </summary>
/// <param name="OllamaEndpoint">The Ollama API endpoint URL.</param>
/// <param name="ModelName">The AI model name to use.</param>
/// <param name="Temperature">The temperature for AI generation (0.0 to 1.0).</param>
/// <param name="MaxTokens">The maximum tokens for AI responses.</param>
/// <param name="TimeoutSeconds">The AI request timeout in seconds.</param>
/// <param name="IsEnabled">Whether AI features are enabled.</param>
public sealed record AiSettingsData(
    string OllamaEndpoint,
    string ModelName,
    decimal Temperature,
    int MaxTokens,
    int TimeoutSeconds,
    bool IsEnabled);
