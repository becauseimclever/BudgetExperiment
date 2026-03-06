// <copyright file="AiDefaults.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

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
    public const string DefaultOllamaUrl = "http://localhost:11434";
}
