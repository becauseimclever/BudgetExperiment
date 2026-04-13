// <copyright file="AiBackendDefaults.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Shared;

/// <summary>
/// Shared default endpoint values for supported AI backends.
/// </summary>
public static class AiBackendDefaults
{
    /// <summary>
    /// The default Ollama API endpoint URL.
    /// </summary>
    public const string DefaultOllamaEndpointUrl = "http://localhost:11434";

    /// <summary>
    /// The default llama.cpp API endpoint URL.
    /// </summary>
    public const string DefaultLlamaCppEndpointUrl = "http://localhost:8080";

    /// <summary>
    /// Gets the default endpoint URL for the specified backend.
    /// </summary>
    /// <param name="backendType">The backend type.</param>
    /// <returns>The backend-specific default endpoint URL.</returns>
    public static string GetDefaultEndpointUrl(AiBackendType backendType) =>
        backendType switch
        {
            AiBackendType.LlamaCpp => DefaultLlamaCppEndpointUrl,
            _ => DefaultOllamaEndpointUrl,
        };
}
