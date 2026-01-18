// <copyright file="IAiService.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Application.Services;

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

/// <summary>
/// Represents the status of the AI service.
/// </summary>
/// <param name="IsAvailable">Whether the AI service is reachable.</param>
/// <param name="CurrentModel">The currently configured model name.</param>
/// <param name="ErrorMessage">Error message if not available.</param>
public sealed record AiServiceStatus(
    bool IsAvailable,
    string? CurrentModel,
    string? ErrorMessage);

/// <summary>
/// Information about an available AI model.
/// </summary>
/// <param name="Name">The model name/identifier.</param>
/// <param name="ModifiedAt">When the model was last modified.</param>
/// <param name="SizeBytes">The model size in bytes.</param>
public sealed record AiModelInfo(
    string Name,
    DateTime ModifiedAt,
    long SizeBytes);

/// <summary>
/// A prompt to send to the AI.
/// </summary>
/// <param name="SystemPrompt">The system prompt for context.</param>
/// <param name="UserPrompt">The user's prompt/question.</param>
/// <param name="Temperature">Temperature for randomness (0.0 to 1.0).</param>
/// <param name="MaxTokens">Maximum tokens in response.</param>
public sealed record AiPrompt(
    string SystemPrompt,
    string UserPrompt,
    decimal Temperature = 0.3m,
    int MaxTokens = 2000);

/// <summary>
/// The response from the AI.
/// </summary>
/// <param name="Success">Whether the request succeeded.</param>
/// <param name="Content">The generated content.</param>
/// <param name="ErrorMessage">Error message if failed.</param>
/// <param name="TokensUsed">Number of tokens used.</param>
/// <param name="Duration">Time taken for the request.</param>
public sealed record AiResponse(
    bool Success,
    string Content,
    string? ErrorMessage,
    int TokensUsed,
    TimeSpan Duration);
