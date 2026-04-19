// <copyright file="UserSettingsTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

namespace BudgetExperiment.Domain.Tests;

/// <summary>
/// Unit tests for the UserSettings entity.
/// </summary>
public class UserSettingsTests
{
    [Fact]
    public void CreateDefault_Creates_UserSettings_With_UserId()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var settings = UserSettings.CreateDefault(userId);

        // Assert
        Assert.NotEqual(Guid.Empty, settings.Id);
        Assert.Equal(userId, settings.UserId);
        Assert.False(settings.AutoRealizePastDueItems);
        Assert.Equal(30, settings.PastDueLookbackDays);
        Assert.Null(settings.PreferredCurrency);
        Assert.Null(settings.TimeZoneId);
        Assert.Equal(DayOfWeek.Sunday, settings.FirstDayOfWeek);
        Assert.False(settings.IsOnboarded);
        Assert.NotEqual(default, settings.CreatedAtUtc);
        Assert.NotEqual(default, settings.UpdatedAtUtc);
    }

    [Fact]
    public void CreateDefault_With_Empty_UserId_Throws()
    {
        // Act & Assert
        var ex = Assert.Throws<DomainException>(() => UserSettings.CreateDefault(Guid.Empty));
        Assert.Contains("user", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void UpdateAutoRealize_Changes_Setting()
    {
        // Arrange
        var settings = UserSettings.CreateDefault(Guid.NewGuid());

        // Act
        settings.UpdateAutoRealize(true);

        // Assert
        Assert.True(settings.AutoRealizePastDueItems);
    }

    [Fact]
    public void UpdatePastDueLookbackDays_Changes_Setting()
    {
        // Arrange
        var settings = UserSettings.CreateDefault(Guid.NewGuid());

        // Act
        settings.UpdatePastDueLookbackDays(60);

        // Assert
        Assert.Equal(60, settings.PastDueLookbackDays);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(366)]
    public void UpdatePastDueLookbackDays_With_Invalid_Days_Throws(int days)
    {
        // Arrange
        var settings = UserSettings.CreateDefault(Guid.NewGuid());

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() => settings.UpdatePastDueLookbackDays(days));
        Assert.Contains("days", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void UpdatePreferredCurrency_Changes_Currency()
    {
        // Arrange
        var settings = UserSettings.CreateDefault(Guid.NewGuid());

        // Act
        settings.UpdatePreferredCurrency("EUR");

        // Assert
        Assert.Equal("EUR", settings.PreferredCurrency);
    }

    [Fact]
    public void UpdatePreferredCurrency_With_Null_Clears_Currency()
    {
        // Arrange
        var settings = UserSettings.CreateDefault(Guid.NewGuid());
        settings.UpdatePreferredCurrency("EUR");

        // Act
        settings.UpdatePreferredCurrency(null);

        // Assert
        Assert.Null(settings.PreferredCurrency);
    }

    [Fact]
    public void UpdateTimeZoneId_Changes_TimeZone()
    {
        // Arrange
        var settings = UserSettings.CreateDefault(Guid.NewGuid());

        // Act
        settings.UpdateTimeZoneId("America/New_York");

        // Assert
        Assert.Equal("America/New_York", settings.TimeZoneId);
    }

    [Fact]
    public void UpdateTimeZoneId_With_Null_Clears_TimeZone()
    {
        // Arrange
        var settings = UserSettings.CreateDefault(Guid.NewGuid());
        settings.UpdateTimeZoneId("America/New_York");

        // Act
        settings.UpdateTimeZoneId(null);

        // Assert
        Assert.Null(settings.TimeZoneId);
    }

    [Fact]
    public void UpdateFirstDayOfWeek_With_Sunday_Succeeds()
    {
        // Arrange
        var settings = UserSettings.CreateDefault(Guid.NewGuid());

        // Act
        settings.UpdateFirstDayOfWeek(DayOfWeek.Sunday);

        // Assert
        Assert.Equal(DayOfWeek.Sunday, settings.FirstDayOfWeek);
    }

    [Fact]
    public void UpdateFirstDayOfWeek_With_Monday_Succeeds()
    {
        // Arrange
        var settings = UserSettings.CreateDefault(Guid.NewGuid());

        // Act
        settings.UpdateFirstDayOfWeek(DayOfWeek.Monday);

        // Assert
        Assert.Equal(DayOfWeek.Monday, settings.FirstDayOfWeek);
    }

    [Theory]
    [InlineData(DayOfWeek.Tuesday)]
    [InlineData(DayOfWeek.Wednesday)]
    [InlineData(DayOfWeek.Thursday)]
    [InlineData(DayOfWeek.Friday)]
    [InlineData(DayOfWeek.Saturday)]
    public void UpdateFirstDayOfWeek_With_Invalid_Day_Throws(DayOfWeek day)
    {
        // Arrange
        var settings = UserSettings.CreateDefault(Guid.NewGuid());

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() => settings.UpdateFirstDayOfWeek(day));
        Assert.Contains("Sunday or Monday", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CompleteOnboarding_Sets_IsOnboarded_True()
    {
        // Arrange
        var settings = UserSettings.CreateDefault(Guid.NewGuid());
        Assert.False(settings.IsOnboarded);
        var beforeUpdate = settings.UpdatedAtUtc;

        // Act
        settings.CompleteOnboarding();

        // Assert
        Assert.True(settings.IsOnboarded);
        Assert.True(settings.UpdatedAtUtc >= beforeUpdate);
    }

    [Fact]
    public void CompleteOnboarding_IsIdempotent()
    {
        // Arrange
        var settings = UserSettings.CreateDefault(Guid.NewGuid());

        // Act
        settings.CompleteOnboarding();
        settings.CompleteOnboarding();

        // Assert
        Assert.True(settings.IsOnboarded);
    }
}
