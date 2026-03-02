// <copyright file="DescriptionMatchModeTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Tests;

/// <summary>
/// Unit tests for the DescriptionMatchMode enum.
/// </summary>
public class DescriptionMatchModeTests
{
    [Theory]
    [InlineData(DescriptionMatchMode.Exact, 0)]
    [InlineData(DescriptionMatchMode.Contains, 1)]
    [InlineData(DescriptionMatchMode.StartsWith, 2)]
    [InlineData(DescriptionMatchMode.Fuzzy, 3)]
    public void DescriptionMatchMode_Has_Expected_Values(DescriptionMatchMode mode, int expectedValue)
    {
        Assert.Equal(expectedValue, (int)mode);
    }
}
