// <copyright file="SkipRowsSettingsTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;

namespace BudgetExperiment.Domain.Tests;

/// <summary>
/// Unit tests for the SkipRowsSettingsValue value object.
/// </summary>
public class SkipRowsSettingsTests
{
    [Fact]
    public void Create_With_Zero_RowsToSkip_Creates_Settings()
    {
        // Arrange & Act
        var settings = SkipRowsSettingsValue.Create(0);

        // Assert
        Assert.Equal(0, settings.RowsToSkip);
    }

    [Fact]
    public void Create_With_Valid_RowsToSkip_Creates_Settings()
    {
        // Arrange & Act
        var settings = SkipRowsSettingsValue.Create(5);

        // Assert
        Assert.Equal(5, settings.RowsToSkip);
    }

    [Fact]
    public void Create_With_MaxSkipRows_Creates_Settings()
    {
        // Arrange & Act
        var settings = SkipRowsSettingsValue.Create(SkipRowsSettingsValue.MaxSkipRows);

        // Assert
        Assert.Equal(100, settings.RowsToSkip);
    }

    [Fact]
    public void Create_With_Negative_RowsToSkip_Throws_DomainException()
    {
        // Arrange & Act & Assert
        var ex = Assert.Throws<DomainException>(() => SkipRowsSettingsValue.Create(-1));
        Assert.Contains("between 0 and 100", ex.Message);
    }

    [Fact]
    public void Create_With_RowsToSkip_Exceeding_Max_Throws_DomainException()
    {
        // Arrange & Act & Assert
        var ex = Assert.Throws<DomainException>(() => SkipRowsSettingsValue.Create(101));
        Assert.Contains("between 0 and 100", ex.Message);
    }

    [Fact]
    public void Default_Returns_Zero_RowsToSkip()
    {
        // Arrange & Act
        var settings = SkipRowsSettingsValue.Default;

        // Assert
        Assert.Equal(0, settings.RowsToSkip);
    }

    [Fact]
    public void Two_Settings_With_Same_RowsToSkip_Are_Equal()
    {
        // Arrange
        var settings1 = SkipRowsSettingsValue.Create(10);
        var settings2 = SkipRowsSettingsValue.Create(10);

        // Assert
        Assert.Equal(settings1, settings2);
    }

    [Fact]
    public void Two_Settings_With_Different_RowsToSkip_Are_Not_Equal()
    {
        // Arrange
        var settings1 = SkipRowsSettingsValue.Create(10);
        var settings2 = SkipRowsSettingsValue.Create(20);

        // Assert
        Assert.NotEqual(settings1, settings2);
    }
}
