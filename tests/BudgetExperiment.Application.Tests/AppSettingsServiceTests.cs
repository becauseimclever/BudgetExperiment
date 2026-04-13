// <copyright file="AppSettingsServiceTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain;
using BudgetExperiment.Domain.Settings;
using BudgetExperiment.Shared;

using Moq;

namespace BudgetExperiment.Application.Tests;

/// <summary>
/// Unit tests for AppSettingsService.
/// </summary>
public class AppSettingsServiceTests
{
    [Fact]
    public async Task GetSettingsAsync_Returns_Settings()
    {
        // Arrange
        var settings = AppSettings.CreateDefault();
        var repo = new Mock<IAppSettingsRepository>();
        repo.Setup(r => r.GetAsync(default)).ReturnsAsync(settings);
        var uow = new Mock<IUnitOfWork>();
        var service = new AppSettingsService(repo.Object, uow.Object);

        // Act
        var result = await service.GetSettingsAsync();

        // Assert
        Assert.False(result.AutoRealizePastDueItems);
        Assert.Equal(30, result.PastDueLookbackDays);
        Assert.False(result.EnableLocationData);
    }

    [Fact]
    public async Task GetSettingsAsync_Returns_Updated_Values()
    {
        // Arrange
        var settings = AppSettings.CreateDefault();
        settings.UpdateAutoRealize(true);
        settings.UpdatePastDueLookbackDays(14);
        var repo = new Mock<IAppSettingsRepository>();
        repo.Setup(r => r.GetAsync(default)).ReturnsAsync(settings);
        var uow = new Mock<IUnitOfWork>();
        var service = new AppSettingsService(repo.Object, uow.Object);

        // Act
        var result = await service.GetSettingsAsync();

        // Assert
        Assert.True(result.AutoRealizePastDueItems);
        Assert.Equal(14, result.PastDueLookbackDays);
    }

    [Fact]
    public async Task UpdateSettingsAsync_Updates_AutoRealize()
    {
        // Arrange
        var settings = AppSettings.CreateDefault();
        var repo = new Mock<IAppSettingsRepository>();
        repo.Setup(r => r.GetAsync(default)).ReturnsAsync(settings);
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);
        var service = new AppSettingsService(repo.Object, uow.Object);
        var dto = new AppSettingsUpdateDto { AutoRealizePastDueItems = true };

        // Act
        var result = await service.UpdateSettingsAsync(dto);

        // Assert
        Assert.True(result.AutoRealizePastDueItems);
        Assert.Equal(30, result.PastDueLookbackDays);
        uow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task UpdateSettingsAsync_Updates_PastDueLookbackDays()
    {
        // Arrange
        var settings = AppSettings.CreateDefault();
        var repo = new Mock<IAppSettingsRepository>();
        repo.Setup(r => r.GetAsync(default)).ReturnsAsync(settings);
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);
        var service = new AppSettingsService(repo.Object, uow.Object);
        var dto = new AppSettingsUpdateDto { PastDueLookbackDays = 7 };

        // Act
        var result = await service.UpdateSettingsAsync(dto);

        // Assert
        Assert.False(result.AutoRealizePastDueItems);
        Assert.Equal(7, result.PastDueLookbackDays);
        uow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task UpdateSettingsAsync_Updates_EnableLocationData()
    {
        // Arrange
        var settings = AppSettings.CreateDefault();
        var repo = new Mock<IAppSettingsRepository>();
        repo.Setup(r => r.GetAsync(default)).ReturnsAsync(settings);
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);
        var service = new AppSettingsService(repo.Object, uow.Object);
        var dto = new AppSettingsUpdateDto { EnableLocationData = true };

        // Act
        var result = await service.UpdateSettingsAsync(dto);

        // Assert
        Assert.True(result.EnableLocationData);
        Assert.False(result.AutoRealizePastDueItems);
        Assert.Equal(30, result.PastDueLookbackDays);
        uow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task UpdateSettingsAsync_Updates_Both_Settings()
    {
        // Arrange
        var settings = AppSettings.CreateDefault();
        var repo = new Mock<IAppSettingsRepository>();
        repo.Setup(r => r.GetAsync(default)).ReturnsAsync(settings);
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);
        var service = new AppSettingsService(repo.Object, uow.Object);
        var dto = new AppSettingsUpdateDto
        {
            AutoRealizePastDueItems = true,
            PastDueLookbackDays = 14,
        };

        // Act
        var result = await service.UpdateSettingsAsync(dto);

        // Assert
        Assert.True(result.AutoRealizePastDueItems);
        Assert.Equal(14, result.PastDueLookbackDays);
        uow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task UpdateSettingsAsync_With_Null_Values_Does_Not_Change_Settings()
    {
        // Arrange
        var settings = AppSettings.CreateDefault();
        settings.UpdateAutoRealize(true);
        settings.UpdatePastDueLookbackDays(14);
        var repo = new Mock<IAppSettingsRepository>();
        repo.Setup(r => r.GetAsync(default)).ReturnsAsync(settings);
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);
        var service = new AppSettingsService(repo.Object, uow.Object);
        var dto = new AppSettingsUpdateDto(); // Both null

        // Act
        var result = await service.UpdateSettingsAsync(dto);

        // Assert
        Assert.True(result.AutoRealizePastDueItems);
        Assert.Equal(14, result.PastDueLookbackDays);
        uow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task UpdateSettingsAsync_With_Invalid_PastDueLookbackDays_Throws()
    {
        // Arrange
        var settings = AppSettings.CreateDefault();
        var repo = new Mock<IAppSettingsRepository>();
        repo.Setup(r => r.GetAsync(default)).ReturnsAsync(settings);
        var uow = new Mock<IUnitOfWork>();
        var service = new AppSettingsService(repo.Object, uow.Object);
        var dto = new AppSettingsUpdateDto { PastDueLookbackDays = 0 };

        // Act & Assert
        await Assert.ThrowsAsync<DomainException>(() => service.UpdateSettingsAsync(dto));
        uow.Verify(u => u.SaveChangesAsync(default), Times.Never);
    }

    [Fact]
    public async Task UpdateSettingsAsync_Can_Disable_AutoRealize()
    {
        // Arrange
        var settings = AppSettings.CreateDefault();
        settings.UpdateAutoRealize(true);
        var repo = new Mock<IAppSettingsRepository>();
        repo.Setup(r => r.GetAsync(default)).ReturnsAsync(settings);
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);
        var service = new AppSettingsService(repo.Object, uow.Object);
        var dto = new AppSettingsUpdateDto { AutoRealizePastDueItems = false };

        // Act
        var result = await service.UpdateSettingsAsync(dto);

        // Assert
        Assert.False(result.AutoRealizePastDueItems);
    }

    [Fact]
    public async Task GetAiSettingsAsync_Returns_DefaultBackendType_And_GenericEndpoint()
    {
        // Arrange
        var settings = AppSettings.CreateDefault();
        var repo = new Mock<IAppSettingsRepository>();
        repo.Setup(r => r.GetAsync(default)).ReturnsAsync(settings);
        var uow = new Mock<IUnitOfWork>();
        var service = new AppSettingsService(repo.Object, uow.Object);

        // Act
        var result = await service.GetAiSettingsAsync();

        // Assert
        Assert.Equal(AiDefaults.DefaultOllamaUrl, result.EndpointUrl);
        Assert.Equal(AiBackendType.Ollama, result.BackendType);
    }

    [Fact]
    public async Task GetAiSettingsAsync_Returns_Legacy_OllamaEndpoint_Alias()
    {
        // Arrange
        var settings = AppSettings.CreateDefault();
        var repo = new Mock<IAppSettingsRepository>();
        repo.Setup(r => r.GetAsync(default)).ReturnsAsync(settings);
        var uow = new Mock<IUnitOfWork>();
        var service = new AppSettingsService(repo.Object, uow.Object);

        // Act
        var result = await service.GetAiSettingsAsync();

        // Assert
        Assert.Equal(result.EndpointUrl, result.OllamaEndpoint);
    }

    [Fact]
    public async Task UpdateAiSettingsAsync_Updates_BackendType_And_Endpoint()
    {
        // Arrange
        var settings = AppSettings.CreateDefault();
        var repo = new Mock<IAppSettingsRepository>();
        repo.Setup(r => r.GetAsync(default)).ReturnsAsync(settings);
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);
        var service = new AppSettingsService(repo.Object, uow.Object);
        var update = new AiSettingsData(
            EndpointUrl: "http://localhost:8080",
            ModelName: "llama3.2",
            Temperature: 0.4m,
            MaxTokens: 2500,
            TimeoutSeconds: 90,
            IsEnabled: true,
            BackendType: AiBackendType.LlamaCpp);

        // Act
        var result = await service.UpdateAiSettingsAsync(update);

        // Assert
        Assert.Equal("http://localhost:8080", result.EndpointUrl);
        Assert.Equal(AiBackendType.LlamaCpp, result.BackendType);
        uow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task UpdateAiSettingsAsync_Uses_Backend_Default_When_Endpoint_Is_Blank()
    {
        // Arrange
        var settings = AppSettings.CreateDefault();
        var repo = new Mock<IAppSettingsRepository>();
        repo.Setup(r => r.GetAsync(default)).ReturnsAsync(settings);
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);
        var service = new AppSettingsService(repo.Object, uow.Object);
        var update = new AiSettingsData(
            EndpointUrl: "   ",
            ModelName: "llama3.2",
            Temperature: 0.4m,
            MaxTokens: 2500,
            TimeoutSeconds: 90,
            IsEnabled: true,
            BackendType: AiBackendType.LlamaCpp);

        // Act
        var result = await service.UpdateAiSettingsAsync(update);

        // Assert
        Assert.Equal(AiDefaults.DefaultLlamaCppUrl, result.EndpointUrl);
        Assert.Equal(AiDefaults.DefaultLlamaCppUrl, settings.AiEndpointUrl);
        Assert.Equal(AiDefaults.DefaultLlamaCppUrl, settings.AiOllamaEndpoint);
    }
}
