// <copyright file="AiBackendType.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Text.Json.Serialization;

namespace BudgetExperiment.Shared;

/// <summary>
/// Enumeration of supported AI backend types.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<AiBackendType>))]
public enum AiBackendType
{
    /// <summary>
    /// Ollama local AI inference engine.
    /// </summary>
    Ollama = 0,

    /// <summary>
    /// llama.cpp local AI inference engine.
    /// </summary>
    LlamaCpp = 1,
}
