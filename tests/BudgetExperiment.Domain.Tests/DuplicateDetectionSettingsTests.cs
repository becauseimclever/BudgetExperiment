// <copyright file="DuplicateDetectionSettingsTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Tests;

/// <summary>
/// Unit tests for the DuplicateDetectionSettingsValue record.
/// </summary>
public class DuplicateDetectionSettingsTests
{
    [Fact]
    public void Default_Values_Are_Correct()
    {
        // Arrange & Act
        var settings = new DuplicateDetectionSettingsValue();

        // Assert
        Assert.True(settings.Enabled);
        Assert.Equal(30, settings.LookbackDays);
        Assert.Equal(DescriptionMatchMode.Exact, settings.DescriptionMatch);
    }

    [Fact]
    public void Custom_Values_Are_Set_Correctly()
    {
        // Arrange & Act
        var settings = new DuplicateDetectionSettingsValue
        {
            Enabled = false,
            LookbackDays = 60,
            DescriptionMatch = DescriptionMatchMode.Contains,
        };

        // Assert
        Assert.False(settings.Enabled);
        Assert.Equal(60, settings.LookbackDays);
        Assert.Equal(DescriptionMatchMode.Contains, settings.DescriptionMatch);
    }

    [Fact]
    public void DuplicateDetectionSettings_Equality()
    {
        // Arrange
        var settings1 = new DuplicateDetectionSettingsValue { Enabled = true, LookbackDays = 30, DescriptionMatch = DescriptionMatchMode.Exact };
        var settings2 = new DuplicateDetectionSettingsValue { Enabled = true, LookbackDays = 30, DescriptionMatch = DescriptionMatchMode.Exact };
        var settings3 = new DuplicateDetectionSettingsValue { Enabled = true, LookbackDays = 60, DescriptionMatch = DescriptionMatchMode.Exact };

        // Assert
        Assert.Equal(settings1, settings2);
        Assert.NotEqual(settings1, settings3);
    }
}
