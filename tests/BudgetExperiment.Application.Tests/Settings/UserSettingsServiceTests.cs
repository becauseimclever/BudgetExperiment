// <copyright file="UserSettingsServiceTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Contracts.Dtos;
using BudgetExperiment.Domain;

using Moq;

namespace BudgetExperiment.Application.Tests.Settings;

/// <summary>
/// Unit tests for <see cref="UserSettingsService"/>.
/// </summary>
public sealed class UserSettingsServiceTests
{
    private readonly Mock<IUserSettingsRepository> _repository;
    private readonly Mock<IUnitOfWork> _uow;
    private readonly Mock<IUserContext> _userContext;
    private readonly UserSettingsService _service;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserSettingsServiceTests"/> class.
    /// </summary>
    public UserSettingsServiceTests()
    {
        _repository = new Mock<IUserSettingsRepository>();
        _uow = new Mock<IUnitOfWork>();
        _userContext = new Mock<IUserContext>();
        _service = new UserSettingsService(
            _repository.Object,
            _uow.Object,
            _userContext.Object);
    }

    // --- GetCurrentUserProfile ---

    /// <summary>
    /// Returns a profile DTO populated from the user context.
    /// </summary>
    [Fact]
    public void GetCurrentUserProfile_ReturnsProfileFromContext()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userContext.Setup(x => x.UserIdAsGuid).Returns(userId);
        _userContext.Setup(x => x.Username).Returns("jdoe");
        _userContext.Setup(x => x.Email).Returns("jdoe@example.com");
        _userContext.Setup(x => x.DisplayName).Returns("John Doe");
        _userContext.Setup(x => x.AvatarUrl).Returns("https://example.com/avatar.jpg");

        // Act
        var result = _service.GetCurrentUserProfile();

        // Assert
        Assert.Equal(userId, result.UserId);
        Assert.Equal("jdoe", result.Username);
        Assert.Equal("jdoe@example.com", result.Email);
        Assert.Equal("John Doe", result.DisplayName);
        Assert.Equal("https://example.com/avatar.jpg", result.AvatarUrl);
    }

    /// <summary>
    /// Returns a profile with an empty GUID when the user has no parsed GUID.
    /// </summary>
    [Fact]
    public void GetCurrentUserProfile_NullUserIdAsGuid_UsesEmptyGuid()
    {
        // Arrange
        _userContext.Setup(x => x.UserIdAsGuid).Returns((Guid?)null);
        _userContext.Setup(x => x.Username).Returns("anonymous");
        _userContext.Setup(x => x.Email).Returns((string?)null);
        _userContext.Setup(x => x.DisplayName).Returns((string?)null);
        _userContext.Setup(x => x.AvatarUrl).Returns((string?)null);

        // Act
        var result = _service.GetCurrentUserProfile();

        // Assert
        Assert.Equal(Guid.Empty, result.UserId);
        Assert.Equal("anonymous", result.Username);
    }

    // --- GetCurrentUserSettingsAsync ---

    /// <summary>
    /// Returns the user settings DTO for an authenticated user.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetCurrentUserSettingsAsync_AuthenticatedUser_ReturnsSettings()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var settings = UserSettings.CreateDefault(userId);

        _userContext.Setup(x => x.UserIdAsGuid).Returns(userId);
        _repository.Setup(r => r.GetByUserIdAsync(userId, default)).ReturnsAsync(settings);
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        // Act
        var result = await _service.GetCurrentUserSettingsAsync();

        // Assert
        Assert.Equal(userId, result.UserId);
        Assert.False(result.IsOnboarded);
        Assert.Equal(30, result.PastDueLookbackDays);
    }

    /// <summary>
    /// Throws when the user context reports no authenticated user.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetCurrentUserSettingsAsync_UserNotAuthenticated_ThrowsDomainException()
    {
        // Arrange
        _userContext.Setup(x => x.UserIdAsGuid).Returns((Guid?)null);

        // Act & Assert
        await Assert.ThrowsAsync<DomainException>(() => _service.GetCurrentUserSettingsAsync());
    }

    // --- UpdateCurrentUserSettingsAsync ---

    /// <summary>
    /// Updates the AutoRealizePastDueItems flag when provided.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UpdateCurrentUserSettingsAsync_AutoRealize_UpdatesFlag()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var settings = UserSettings.CreateDefault(userId);

        _userContext.Setup(x => x.UserIdAsGuid).Returns(userId);
        _repository.Setup(r => r.GetByUserIdAsync(userId, default)).ReturnsAsync(settings);
        _repository.Setup(r => r.SaveAsync(settings, default)).Returns(Task.CompletedTask);
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        var dto = new UserSettingsUpdateDto { AutoRealizePastDueItems = true };

        // Act
        var result = await _service.UpdateCurrentUserSettingsAsync(dto);

        // Assert
        Assert.True(result.AutoRealizePastDueItems);
    }

    /// <summary>
    /// Updates the past-due lookback days when provided.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UpdateCurrentUserSettingsAsync_PastDueLookbackDays_UpdatesDays()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var settings = UserSettings.CreateDefault(userId);

        _userContext.Setup(x => x.UserIdAsGuid).Returns(userId);
        _repository.Setup(r => r.GetByUserIdAsync(userId, default)).ReturnsAsync(settings);
        _repository.Setup(r => r.SaveAsync(settings, default)).Returns(Task.CompletedTask);
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        var dto = new UserSettingsUpdateDto { PastDueLookbackDays = 60 };

        // Act
        var result = await _service.UpdateCurrentUserSettingsAsync(dto);

        // Assert
        Assert.Equal(60, result.PastDueLookbackDays);
    }

    /// <summary>
    /// Updates the preferred currency when provided.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UpdateCurrentUserSettingsAsync_PreferredCurrency_UpdatesCurrency()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var settings = UserSettings.CreateDefault(userId);

        _userContext.Setup(x => x.UserIdAsGuid).Returns(userId);
        _repository.Setup(r => r.GetByUserIdAsync(userId, default)).ReturnsAsync(settings);
        _repository.Setup(r => r.SaveAsync(settings, default)).Returns(Task.CompletedTask);
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        var dto = new UserSettingsUpdateDto { PreferredCurrency = "EUR" };

        // Act
        var result = await _service.UpdateCurrentUserSettingsAsync(dto);

        // Assert
        Assert.Equal("EUR", result.PreferredCurrency);
    }

    /// <summary>
    /// Throws when the user is not authenticated.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UpdateCurrentUserSettingsAsync_UserNotAuthenticated_ThrowsDomainException()
    {
        // Arrange
        _userContext.Setup(x => x.UserIdAsGuid).Returns((Guid?)null);

        var dto = new UserSettingsUpdateDto { PreferredCurrency = "USD" };

        // Act & Assert
        await Assert.ThrowsAsync<DomainException>(() => _service.UpdateCurrentUserSettingsAsync(dto));
    }

    // --- CompleteOnboardingAsync ---

    /// <summary>
    /// Marks the user as onboarded.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CompleteOnboardingAsync_SetsIsOnboardedToTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var settings = UserSettings.CreateDefault(userId);

        _userContext.Setup(x => x.UserIdAsGuid).Returns(userId);
        _repository.Setup(r => r.GetByUserIdAsync(userId, default)).ReturnsAsync(settings);
        _repository.Setup(r => r.SaveAsync(settings, default)).Returns(Task.CompletedTask);
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        // Act
        var result = await _service.CompleteOnboardingAsync();

        // Assert
        Assert.True(result.IsOnboarded);
    }

    /// <summary>
    /// Throws when the user is not authenticated during onboarding completion.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CompleteOnboardingAsync_UserNotAuthenticated_ThrowsDomainException()
    {
        // Arrange
        _userContext.Setup(x => x.UserIdAsGuid).Returns((Guid?)null);

        // Act & Assert
        await Assert.ThrowsAsync<DomainException>(() => _service.CompleteOnboardingAsync());
    }
}
