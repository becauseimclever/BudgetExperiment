// <copyright file="UserSettingsRepositoryTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace BudgetExperiment.Infrastructure.Tests;

/// <summary>
/// Integration tests for <see cref="UserSettingsRepository"/>.
/// </summary>
[Collection("InfraDb")]
public class UserSettingsRepositoryTests : IClassFixture<PostgreSqlFixture>
{
    private readonly PostgreSqlFixture _fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserSettingsRepositoryTests"/> class.
    /// </summary>
    /// <param name="fixture">The shared PostgreSQL database fixture.</param>
    public UserSettingsRepositoryTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetByUserIdAsync_Creates_Default_Settings_When_Not_Found()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var repository = new UserSettingsRepository(context);
        var userId = Guid.NewGuid();

        // Act
        var settings = await repository.GetByUserIdAsync(userId);

        // Assert
        Assert.NotNull(settings);
        Assert.Equal(userId, settings.UserId);
        Assert.Equal(BudgetScope.Shared, settings.DefaultScope);
        Assert.False(settings.AutoRealizePastDueItems);
        Assert.Equal(30, settings.PastDueLookbackDays);
        Assert.False(settings.IsOnboarded);
    }

    [Fact]
    public async Task GetByUserIdAsync_Persists_New_Default_Settings_In_Database()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var repository = new UserSettingsRepository(context);
        var userId = Guid.NewGuid();

        // Act - creates defaults and flushes (GetByUserIdAsync does NOT call SaveChangesAsync itself)
        _ = await repository.GetByUserIdAsync(userId);
        await context.SaveChangesAsync();

        // Assert - visible from a fresh context
        await using var verifyContext = _fixture.CreateSharedContext(context);
        var verifyRepo = new UserSettingsRepository(verifyContext);
        var retrieved = await verifyRepo.GetByUserIdAsync(userId);

        Assert.NotNull(retrieved);
        Assert.Equal(userId, retrieved.UserId);
    }

    [Fact]
    public async Task GetByUserIdAsync_Returns_Existing_Settings_Without_Creating_Duplicate()
    {
        // Arrange - persist settings for a user
        await using var context = _fixture.CreateContext();
        var repository = new UserSettingsRepository(context);
        var userId = Guid.NewGuid();

        var first = await repository.GetByUserIdAsync(userId);
        await context.SaveChangesAsync();

        // Act - retrieve a second time in a fresh context
        await using var verifyContext = _fixture.CreateSharedContext(context);
        var verifyRepo = new UserSettingsRepository(verifyContext);
        var second = await verifyRepo.GetByUserIdAsync(userId);

        // Assert - same entity, no duplicate row
        Assert.Equal(first.Id, second.Id);
        Assert.Equal(userId, second.UserId);
    }

    [Fact]
    public async Task GetByUserIdAsync_Returns_Distinct_Settings_For_Different_Users()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var repository = new UserSettingsRepository(context);
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();

        // Act
        var settings1 = await repository.GetByUserIdAsync(userId1);
        var settings2 = await repository.GetByUserIdAsync(userId2);
        await context.SaveChangesAsync();

        // Assert - different records
        Assert.NotEqual(settings1.Id, settings2.Id);
        Assert.Equal(userId1, settings1.UserId);
        Assert.Equal(userId2, settings2.UserId);
    }

    [Fact]
    public async Task SaveAsync_Persists_Modified_Settings()
    {
        // Arrange - create settings for a user
        await using var context = _fixture.CreateContext();
        var repository = new UserSettingsRepository(context);
        var userId = Guid.NewGuid();
        var settings = await repository.GetByUserIdAsync(userId);
        await context.SaveChangesAsync();

        // Act - modify and save
        settings.UpdateDefaultScope(BudgetScope.Personal);
        settings.UpdateAutoRealize(true);
        settings.UpdatePastDueLookbackDays(45);
        settings.UpdatePreferredCurrency("EUR");
        settings.UpdateTimeZoneId("Europe/Berlin");
        settings.CompleteOnboarding();
        await repository.SaveAsync(settings);
        await context.SaveChangesAsync();

        // Assert
        await using var verifyContext = _fixture.CreateSharedContext(context);
        var verifyRepo = new UserSettingsRepository(verifyContext);
        var retrieved = await verifyRepo.GetByUserIdAsync(userId);

        Assert.Equal(BudgetScope.Personal, retrieved.DefaultScope);
        Assert.True(retrieved.AutoRealizePastDueItems);
        Assert.Equal(45, retrieved.PastDueLookbackDays);
        Assert.Equal("EUR", retrieved.PreferredCurrency);
        Assert.Equal("Europe/Berlin", retrieved.TimeZoneId);
        Assert.True(retrieved.IsOnboarded);
    }

    [Fact]
    public async Task SaveAsync_Attaches_Detached_Settings_And_Persists_Changes()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var repository = new UserSettingsRepository(context);
        var userId = Guid.NewGuid();
        var settings = await repository.GetByUserIdAsync(userId);
        await context.SaveChangesAsync();

        context.Entry(settings).State = EntityState.Detached;

        // Act
        await using var updateContext = _fixture.CreateSharedContext(context);
        var updateRepo = new UserSettingsRepository(updateContext);
        settings.UpdatePreferredCurrency("GBP");
        await updateRepo.SaveAsync(settings);
        await updateContext.SaveChangesAsync();

        // Assert
        await using var verifyContext = _fixture.CreateSharedContext(context);
        var verifyRepo = new UserSettingsRepository(verifyContext);
        var retrieved = await verifyRepo.GetByUserIdAsync(userId);

        Assert.Equal("GBP", retrieved.PreferredCurrency);
    }

    [Fact]
    public async Task UpdateFirstDayOfWeek_Persists_To_Monday()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var repository = new UserSettingsRepository(context);
        var userId = Guid.NewGuid();
        var settings = await repository.GetByUserIdAsync(userId);
        await context.SaveChangesAsync();

        // Act
        settings.UpdateFirstDayOfWeek(DayOfWeek.Monday);
        await repository.SaveAsync(settings);
        await context.SaveChangesAsync();

        // Assert
        await using var verifyContext = _fixture.CreateSharedContext(context);
        var verifyRepo = new UserSettingsRepository(verifyContext);
        var retrieved = await verifyRepo.GetByUserIdAsync(userId);

        Assert.Equal(DayOfWeek.Monday, retrieved.FirstDayOfWeek);
    }
}
