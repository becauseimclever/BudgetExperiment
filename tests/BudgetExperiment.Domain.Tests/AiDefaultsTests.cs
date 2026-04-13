// <copyright file="AiDefaultsTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain.Settings;
using BudgetExperiment.Shared;

namespace BudgetExperiment.Domain.Tests;

/// <summary>
/// Regression guard tests for <see cref="AiDefaults"/> values.
/// Ensures AI default strings are never accidentally changed.
/// </summary>
public sealed class AiDefaultsTests
{
    /// <summary>
    /// DefaultOllamaUrl must equal the standard localhost endpoint.
    /// </summary>
    [Fact]
    public void DefaultOllamaUrl_EqualsExpectedValue()
    {
        Assert.Equal("http://localhost:11434", AiDefaults.DefaultOllamaUrl);
    }

    /// <summary>
    /// DefaultLlamaCppUrl must equal the standard localhost endpoint.
    /// </summary>
    [Fact]
    public void DefaultLlamaCppUrl_EqualsExpectedValue()
    {
        Assert.Equal("http://localhost:8080", AiDefaults.DefaultLlamaCppUrl);
    }

    /// <summary>
    /// Default backend type must preserve the existing Ollama behavior.
    /// </summary>
    [Fact]
    public void DefaultBackendType_EqualsOllama()
    {
        Assert.Equal(AiBackendType.Ollama, AiDefaults.DefaultBackendType);
    }

    /// <summary>
    /// Default endpoint lookup must return the llama.cpp endpoint when selected.
    /// </summary>
    [Fact]
    public void GetDefaultEndpointUrl_ForLlamaCpp_ReturnsExpectedValue()
    {
        Assert.Equal("http://localhost:8080", AiDefaults.GetDefaultEndpointUrl(AiBackendType.LlamaCpp));
    }
}
