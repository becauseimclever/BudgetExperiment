// <copyright file="AppSettingsTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;

namespace BudgetExperiment.Domain.Tests;

/// <summary>
/// Unit tests for the AppSettings entity.
/// </summary>
public class AppSettingsTests
{
    [Fact]
    public void CreateDefault_Returns_Instance_With_SingletonId()
    {
        // Act
        var settings = AppSettings.CreateDefault();

        // Assert
        Assert.Equal(AppSettings.SingletonId, settings.Id);
    }

    [Fact]
    public void CreateDefault_Sets_AutoRealizePastDueItems_To_False()
    {
        // Act
        var settings = AppSettings.CreateDefault();

        // Assert
        Assert.False(settings.AutoRealizePastDueItems);
    }

    [Fact]
    public void CreateDefault_Sets_PastDueLookbackDays_To_30()
    {
        // Act
        var settings = AppSettings.CreateDefault();

        // Assert
        Assert.Equal(30, settings.PastDueLookbackDays);
    }

    [Fact]
    public void CreateDefault_Sets_CreatedAtUtc()
    {
        // Act
        var before = DateTime.UtcNow;
        var settings = AppSettings.CreateDefault();
        var after = DateTime.UtcNow;

        // Assert
        Assert.True(settings.CreatedAtUtc >= before);
        Assert.True(settings.CreatedAtUtc <= after);
    }

    [Fact]
    public void CreateDefault_Sets_UpdatedAtUtc()
    {
        // Act
        var before = DateTime.UtcNow;
        var settings = AppSettings.CreateDefault();
        var after = DateTime.UtcNow;

        // Assert
        Assert.True(settings.UpdatedAtUtc >= before);
        Assert.True(settings.UpdatedAtUtc <= after);
    }

    [Fact]
    public void UpdateAutoRealize_Sets_True_When_Enabled()
    {
        // Arrange
        var settings = AppSettings.CreateDefault();

        // Act
        settings.UpdateAutoRealize(true);

        // Assert
        Assert.True(settings.AutoRealizePastDueItems);
    }

    [Fact]
    public void UpdateAutoRealize_Sets_False_When_Disabled()
    {
        // Arrange
        var settings = AppSettings.CreateDefault();
        settings.UpdateAutoRealize(true);

        // Act
        settings.UpdateAutoRealize(false);

        // Assert
        Assert.False(settings.AutoRealizePastDueItems);
    }

    [Fact]
    public void UpdateAutoRealize_Updates_UpdatedAtUtc()
    {
        // Arrange
        var settings = AppSettings.CreateDefault();
        var originalUpdatedAt = settings.UpdatedAtUtc;

        // Act
        settings.UpdateAutoRealize(true);

        // Assert
        Assert.True(settings.UpdatedAtUtc >= originalUpdatedAt);
    }

    [Fact]
    public void UpdatePastDueLookbackDays_Sets_Valid_Value()
    {
        // Arrange
        var settings = AppSettings.CreateDefault();

        // Act
        settings.UpdatePastDueLookbackDays(14);

        // Assert
        Assert.Equal(14, settings.PastDueLookbackDays);
    }

    [Fact]
    public void UpdatePastDueLookbackDays_Updates_UpdatedAtUtc()
    {
        // Arrange
        var settings = AppSettings.CreateDefault();
        var originalUpdatedAt = settings.UpdatedAtUtc;

        // Act
        settings.UpdatePastDueLookbackDays(14);

        // Assert
        Assert.True(settings.UpdatedAtUtc >= originalUpdatedAt);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void UpdatePastDueLookbackDays_Throws_When_Less_Than_1(int days)
    {
        // Arrange
        var settings = AppSettings.CreateDefault();

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() => settings.UpdatePastDueLookbackDays(days));
        Assert.Contains("Lookback days must be between 1 and 365", ex.Message);
    }

    [Theory]
    [InlineData(366)]
    [InlineData(1000)]
    public void UpdatePastDueLookbackDays_Throws_When_Greater_Than_365(int days)
    {
        // Arrange
        var settings = AppSettings.CreateDefault();

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() => settings.UpdatePastDueLookbackDays(days));
        Assert.Contains("Lookback days must be between 1 and 365", ex.Message);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(365)]
    public void UpdatePastDueLookbackDays_Allows_Boundary_Values(int days)
    {
        // Arrange
        var settings = AppSettings.CreateDefault();

        // Act
        settings.UpdatePastDueLookbackDays(days);

        // Assert
        Assert.Equal(days, settings.PastDueLookbackDays);
    }

    [Fact]
    public void SingletonId_Returns_Expected_Guid()
    {
        // Assert
        Assert.Equal(new Guid("00000000-0000-0000-0000-000000000001"), AppSettings.SingletonId);
    }
}
