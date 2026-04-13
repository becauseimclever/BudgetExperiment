// <copyright file="AiDefaults.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Shared;

namespace BudgetExperiment.Domain.Settings;

/// <summary>
/// Default AI configuration values used across the application.
/// Centralizes magic strings to prevent typos and enable single-point updates.
/// </summary>
public static class AiDefaults
{
    /// <summary>
    /// The default Ollama API endpoint URL ("http://localhost:11434").
    /// </summary>
    public const string DefaultOllamaUrl = AiBackendDefaults.DefaultOllamaEndpointUrl;

    /// <summary>
    /// The default llama.cpp API endpoint URL ("http://localhost:8080").
    /// </summary>
    public const string DefaultLlamaCppUrl = AiBackendDefaults.DefaultLlamaCppEndpointUrl;

    /// <summary>
    /// The default AI backend type.
    /// </summary>
    public const AiBackendType DefaultBackendType = AiBackendType.Ollama;

    /// <summary>
    /// Gets the default endpoint URL for the selected AI backend.
    /// </summary>
    /// <param name="backendType">The backend type.</param>
    /// <returns>The default endpoint URL.</returns>
    public static string GetDefaultEndpointUrl(AiBackendType backendType) =>
        AiBackendDefaults.GetDefaultEndpointUrl(backendType);
}
