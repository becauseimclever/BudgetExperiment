// <copyright file="IAppSettingsService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Application.Services;

/// <summary>
/// Service interface for application settings operations.
/// </summary>
public interface IAppSettingsService
{
    /// <summary>
    /// Gets the current application settings.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The application settings DTO.</returns>
    Task<AppSettingsDto> GetSettingsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the application settings.
    /// </summary>
    /// <param name="dto">The settings update data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated application settings DTO.</returns>
    Task<AppSettingsDto> UpdateSettingsAsync(AppSettingsUpdateDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current AI settings.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The current AI settings.</returns>
    Task<AiSettingsData> GetAiSettingsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the AI settings.
    /// </summary>
    /// <param name="settings">The new settings.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated settings.</returns>
    Task<AiSettingsData> UpdateAiSettingsAsync(AiSettingsData settings, CancellationToken cancellationToken = default);
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
