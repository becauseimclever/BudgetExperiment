// <copyright file="IAiService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Application.Ai;

/// <summary>
/// Service interface for local AI model integration.
/// </summary>
public interface IAiService
{
    /// <summary>
    /// Checks if the AI service is available and configured.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The AI service status.</returns>
    Task<AiServiceStatus> GetStatusAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets available models from the AI service.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of available models.</returns>
    Task<IReadOnlyList<AiModelInfo>> GetAvailableModelsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a prompt to the AI and returns the response.
    /// </summary>
    /// <param name="prompt">The prompt to send.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The AI response.</returns>
    Task<AiResponse> CompleteAsync(AiPrompt prompt, CancellationToken cancellationToken = default);
}
