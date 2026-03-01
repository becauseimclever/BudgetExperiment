// <copyright file="AiSettingsDto.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Contracts.Dtos;

/// <summary>
/// DTO for AI settings configuration.
/// </summary>
public sealed class AiSettingsDto
{
    /// <summary>
    /// Gets or sets the Ollama API endpoint URL.
    /// </summary>
    public string OllamaEndpoint { get; set; } = "http://localhost:11434";

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
}
