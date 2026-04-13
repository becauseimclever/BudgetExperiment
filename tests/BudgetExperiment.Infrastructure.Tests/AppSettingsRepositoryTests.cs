// <copyright file="AppSettingsRepositoryTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain.Settings;
using BudgetExperiment.Infrastructure.Persistence.Repositories;
using BudgetExperiment.Shared;

namespace BudgetExperiment.Infrastructure.Tests;

/// <summary>
/// Integration tests for <see cref="AppSettingsRepository"/>.
/// </summary>
[Collection("InfraDb")]
public class AppSettingsRepositoryTests : IClassFixture<PostgreSqlFixture>
{
    private readonly PostgreSqlFixture _fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="AppSettingsRepositoryTests"/> class.
    /// </summary>
    /// <param name="fixture">The shared PostgreSQL database fixture.</param>
    public AppSettingsRepositoryTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetAsync_Creates_Default_Settings_When_Empty()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var repository = new AppSettingsRepository(context);

        // Act
        var settings = await repository.GetAsync();

        // Assert
        Assert.NotNull(settings);
        Assert.Equal(AppSettings.SingletonId, settings.Id);
        Assert.False(settings.AutoRealizePastDueItems);
        Assert.Equal(30, settings.PastDueLookbackDays);
        Assert.False(settings.EnableLocationData);
    }

    [Fact]
    public async Task GetAsync_Returns_Same_Singleton_On_Subsequent_Calls()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var repository = new AppSettingsRepository(context);

        // Act - first call creates, second call retrieves
        var first = await repository.GetAsync();
        var second = await repository.GetAsync();

        // Assert - same record returned (singleton ID must be identical)
        Assert.Equal(AppSettings.SingletonId, first.Id);
        Assert.Equal(AppSettings.SingletonId, second.Id);
    }

    [Fact]
    public async Task GetAsync_Persists_Defaults_So_Second_Context_Can_Read_Them()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var repository = new AppSettingsRepository(context);

        // Act - first context auto-creates defaults
        _ = await repository.GetAsync();

        // Assert - second context sees the persisted row
        await using var verifyContext = _fixture.CreateSharedContext(context);
        var verifyRepo = new AppSettingsRepository(verifyContext);
        var retrieved = await verifyRepo.GetAsync();

        Assert.NotNull(retrieved);
        Assert.Equal(AppSettings.SingletonId, retrieved.Id);
    }

    [Fact]
    public async Task Modifications_Persisted_Via_SaveChangesAsync_After_SaveAsync()
    {
        // Arrange - establish the singleton first
        await using var context = _fixture.CreateContext();
        var repository = new AppSettingsRepository(context);
        var settings = await repository.GetAsync();

        // Act - modify, call no-op SaveAsync, then flush via UoW-equivalent SaveChangesAsync
        settings.UpdateAutoRealize(true);
        settings.UpdatePastDueLookbackDays(60);
        await repository.SaveAsync(settings);
        await context.SaveChangesAsync();

        // Assert - changes are visible from a fresh context
        await using var verifyContext = _fixture.CreateSharedContext(context);
        var verifyRepo = new AppSettingsRepository(verifyContext);
        var retrieved = await verifyRepo.GetAsync();

        Assert.True(retrieved.AutoRealizePastDueItems);
        Assert.Equal(60, retrieved.PastDueLookbackDays);
    }

    [Fact]
    public async Task UpdateAiSettings_Persists_BackendType_And_LegacyEndpointColumn()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var repository = new AppSettingsRepository(context);
        var settings = await repository.GetAsync();

        // Act
        settings.UpdateAiSettings(
            endpointUrl: AiDefaults.DefaultLlamaCppUrl,
            modelName: "llama3.2",
            temperature: 0.7m,
            maxTokens: 3000,
            timeoutSeconds: 90,
            isEnabled: true,
            backendType: AiBackendType.LlamaCpp);
        await context.SaveChangesAsync();

        // Assert
        await using var verifyContext = _fixture.CreateSharedContext(context);
        var verifyRepo = new AppSettingsRepository(verifyContext);
        var retrieved = await verifyRepo.GetAsync();

        Assert.Equal(AiDefaults.DefaultLlamaCppUrl, retrieved.AiOllamaEndpoint);
        Assert.Equal("llama3.2", retrieved.AiModelName);
        Assert.Equal(0.7m, retrieved.AiTemperature);
        Assert.Equal(3000, retrieved.AiMaxTokens);
        Assert.Equal(90, retrieved.AiTimeoutSeconds);
        Assert.True(retrieved.AiIsEnabled);
        Assert.Equal(AiBackendType.LlamaCpp, retrieved.AiBackendType);
    }

    [Fact]
    public async Task UpdateEnableLocationData_Persists_Changes()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var repository = new AppSettingsRepository(context);
        var settings = await repository.GetAsync();

        // Act
        settings.UpdateEnableLocationData(true);
        await context.SaveChangesAsync();

        // Assert
        await using var verifyContext = _fixture.CreateSharedContext(context);
        var verifyRepo = new AppSettingsRepository(verifyContext);
        var retrieved = await verifyRepo.GetAsync();

        Assert.True(retrieved.EnableLocationData);
    }
}
