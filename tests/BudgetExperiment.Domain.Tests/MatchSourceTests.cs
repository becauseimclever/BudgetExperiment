// <copyright file="MatchSourceTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;

namespace BudgetExperiment.Domain.Tests;

/// <summary>
/// Unit tests for the MatchSource enum.
/// </summary>
public class MatchSourceTests
{
    [Fact]
    public void MatchSource_Has_Auto_Value()
    {
        // Arrange & Act
        var auto = MatchSource.Auto;

        // Assert
        Assert.Equal(0, (int)auto);
    }

    [Fact]
    public void MatchSource_Has_Manual_Value()
    {
        // Arrange & Act
        var manual = MatchSource.Manual;

        // Assert
        Assert.Equal(1, (int)manual);
    }

    [Theory]
    [InlineData(MatchSource.Auto)]
    [InlineData(MatchSource.Manual)]
    public void MatchSource_Values_Are_Defined(MatchSource source)
    {
        // Assert
        Assert.True(Enum.IsDefined(typeof(MatchSource), source));
    }
}
