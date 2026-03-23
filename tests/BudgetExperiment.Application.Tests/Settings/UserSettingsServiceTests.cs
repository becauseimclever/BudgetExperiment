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
        Assert.Equal("Shared", result.DefaultScope);
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
    /// Updates the default scope when a valid scope string is provided.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UpdateCurrentUserSettingsAsync_ValidScope_UpdatesScope()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var settings = UserSettings.CreateDefault(userId);

        _userContext.Setup(x => x.UserIdAsGuid).Returns(userId);
        _repository.Setup(r => r.GetByUserIdAsync(userId, default)).ReturnsAsync(settings);
        _repository.Setup(r => r.SaveAsync(settings, default)).Returns(Task.CompletedTask);
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        var dto = new UserSettingsUpdateDto { DefaultScope = "Personal" };

        // Act
        var result = await _service.UpdateCurrentUserSettingsAsync(dto);

        // Assert
        Assert.Equal("Personal", result.DefaultScope);
    }

    /// <summary>
    /// Throws when an invalid scope string is provided.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UpdateCurrentUserSettingsAsync_InvalidScope_ThrowsDomainException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var settings = UserSettings.CreateDefault(userId);

        _userContext.Setup(x => x.UserIdAsGuid).Returns(userId);
        _repository.Setup(r => r.GetByUserIdAsync(userId, default)).ReturnsAsync(settings);

        var dto = new UserSettingsUpdateDto { DefaultScope = "InvalidScopeValue" };

        // Act & Assert
        await Assert.ThrowsAsync<DomainException>(() => _service.UpdateCurrentUserSettingsAsync(dto));
    }

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

        var dto = new UserSettingsUpdateDto { DefaultScope = "Personal" };

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

    // --- GetCurrentScope ---

    /// <summary>
    /// Returns the current scope from the user context.
    /// </summary>
    [Fact]
    public void GetCurrentScope_SharedScope_ReturnsSharedString()
    {
        // Arrange
        _userContext.Setup(x => x.CurrentScope).Returns(BudgetScope.Shared);

        // Act
        var result = _service.GetCurrentScope();

        // Assert
        Assert.Equal("Shared", result.Scope);
    }

    /// <summary>
    /// Returns null scope when the user context has no scope set.
    /// </summary>
    [Fact]
    public void GetCurrentScope_NullScope_ReturnsNullString()
    {
        // Arrange
        _userContext.Setup(x => x.CurrentScope).Returns((BudgetScope?)null);

        // Act
        var result = _service.GetCurrentScope();

        // Assert
        Assert.Null(result.Scope);
    }

    // --- SetCurrentScope ---

    /// <summary>
    /// Sets the scope on the user context for a valid scope string.
    /// </summary>
    [Fact]
    public void SetCurrentScope_ValidScope_CallsSetScopeOnContext()
    {
        // Arrange
        var dto = new ScopeDto { Scope = "Personal" };

        // Act
        _service.SetCurrentScope(dto);

        // Assert
        _userContext.Verify(x => x.SetScope(BudgetScope.Personal), Times.Once);
    }

    /// <summary>
    /// Sets the scope to null when the scope string is null.
    /// </summary>
    [Fact]
    public void SetCurrentScope_NullScope_SetsNullOnContext()
    {
        // Arrange
        var dto = new ScopeDto { Scope = null };

        // Act
        _service.SetCurrentScope(dto);

        // Assert
        _userContext.Verify(x => x.SetScope(null), Times.Once);
    }

    /// <summary>
    /// Sets the scope to null when the scope string is empty or whitespace.
    /// </summary>
    [Fact]
    public void SetCurrentScope_WhitespaceScope_SetsNullOnContext()
    {
        // Arrange
        var dto = new ScopeDto { Scope = "   " };

        // Act
        _service.SetCurrentScope(dto);

        // Assert
        _userContext.Verify(x => x.SetScope(null), Times.Once);
    }

    /// <summary>
    /// Throws when an invalid scope string is provided.
    /// </summary>
    [Fact]
    public void SetCurrentScope_InvalidScope_ThrowsDomainException()
    {
        // Arrange
        var dto = new ScopeDto { Scope = "InvalidScopeValue" };

        // Act & Assert
        Assert.Throws<DomainException>(() => _service.SetCurrentScope(dto));
    }
}
