// <copyright file="AiDefaultsTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain.Settings;

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
}
