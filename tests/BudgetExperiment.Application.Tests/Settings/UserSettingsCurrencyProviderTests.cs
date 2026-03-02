// <copyright file="UserSettingsCurrencyProviderTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Application.Settings;
using BudgetExperiment.Domain.Repositories;
using BudgetExperiment.Domain.Settings;
using Moq;

namespace BudgetExperiment.Application.Tests.Settings;

/// <summary>
/// Unit tests for <see cref="UserSettingsCurrencyProvider"/>.
/// </summary>
public sealed class UserSettingsCurrencyProviderTests
{
    private readonly Mock<IUserContext> _userContextMock = new();
    private readonly Mock<IUserSettingsRepository> _settingsRepositoryMock = new();
    private readonly UserSettingsCurrencyProvider _sut;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserSettingsCurrencyProviderTests"/> class.
    /// </summary>
    public UserSettingsCurrencyProviderTests()
    {
        _sut = new UserSettingsCurrencyProvider(
            _userContextMock.Object,
            _settingsRepositoryMock.Object);
    }

    /// <summary>
    /// Returns the user's preferred currency when it is set.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetCurrencyAsync_WhenPreferredCurrencyIsSet_ReturnsThatCurrency()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userContextMock.Setup(x => x.IsAuthenticated).Returns(true);
        _userContextMock.Setup(x => x.UserIdAsGuid).Returns(userId);

        var settings = UserSettings.CreateDefault(userId);
        settings.UpdatePreferredCurrency("EUR");
        _settingsRepositoryMock
            .Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);

        // Act
        var result = await _sut.GetCurrencyAsync();

        // Assert
        Assert.Equal("EUR", result);
    }

    /// <summary>
    /// Returns USD when preferred currency is null.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetCurrencyAsync_WhenPreferredCurrencyIsNull_ReturnsUsd()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userContextMock.Setup(x => x.IsAuthenticated).Returns(true);
        _userContextMock.Setup(x => x.UserIdAsGuid).Returns(userId);

        var settings = UserSettings.CreateDefault(userId);
        _settingsRepositoryMock
            .Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);

        // Act
        var result = await _sut.GetCurrencyAsync();

        // Assert
        Assert.Equal("USD", result);
    }

    /// <summary>
    /// Returns USD when preferred currency is whitespace.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetCurrencyAsync_WhenPreferredCurrencyIsWhitespace_ReturnsUsd()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userContextMock.Setup(x => x.IsAuthenticated).Returns(true);
        _userContextMock.Setup(x => x.UserIdAsGuid).Returns(userId);

        var settings = UserSettings.CreateDefault(userId);
        settings.UpdatePreferredCurrency("   ");
        _settingsRepositoryMock
            .Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);

        // Act
        var result = await _sut.GetCurrencyAsync();

        // Assert
        Assert.Equal("USD", result);
    }

    /// <summary>
    /// Returns USD when user is not authenticated.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetCurrencyAsync_WhenUserIsNotAuthenticated_ReturnsUsd()
    {
        // Arrange
        _userContextMock.Setup(x => x.IsAuthenticated).Returns(false);

        // Act
        var result = await _sut.GetCurrencyAsync();

        // Assert
        Assert.Equal("USD", result);
        _settingsRepositoryMock.Verify(
            x => x.GetByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
