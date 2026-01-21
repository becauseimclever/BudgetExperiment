// <copyright file="AppSettingsServiceTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>


using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain;
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
}
