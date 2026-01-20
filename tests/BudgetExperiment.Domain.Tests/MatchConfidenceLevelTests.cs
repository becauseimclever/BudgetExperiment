// <copyright file="MatchConfidenceLevelTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;

namespace BudgetExperiment.Domain.Tests;

/// <summary>
/// Unit tests for the MatchConfidenceLevel enum.
/// </summary>
public class MatchConfidenceLevelTests
{
    [Fact]
    public void MatchConfidenceLevel_Has_High_Value()
    {
        // Arrange & Act
        var high = MatchConfidenceLevel.High;

        // Assert
        Assert.Equal(0, (int)high);
    }

    [Fact]
    public void MatchConfidenceLevel_Has_Medium_Value()
    {
        // Arrange & Act
        var medium = MatchConfidenceLevel.Medium;

        // Assert
        Assert.Equal(1, (int)medium);
    }

    [Fact]
    public void MatchConfidenceLevel_Has_Low_Value()
    {
        // Arrange & Act
        var low = MatchConfidenceLevel.Low;

        // Assert
        Assert.Equal(2, (int)low);
    }

    [Theory]
    [InlineData(MatchConfidenceLevel.High)]
    [InlineData(MatchConfidenceLevel.Medium)]
    [InlineData(MatchConfidenceLevel.Low)]
    public void MatchConfidenceLevel_Values_Are_Defined(MatchConfidenceLevel level)
    {
        // Assert
        Assert.True(Enum.IsDefined(typeof(MatchConfidenceLevel), level));
    }
}
