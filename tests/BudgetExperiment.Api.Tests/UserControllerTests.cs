// <copyright file="UserControllerTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Net;
using System.Net.Http.Json;

using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Api.Tests;

/// <summary>
/// Integration tests for the User API endpoints.
/// Each test resets the database in the constructor to ensure full state isolation,
/// matching the previous per-test inline factory pattern.
/// </summary>
[Collection("ApiDb")]
public sealed class UserControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserControllerTests"/> class.
    /// </summary>
    /// <param name="factory">The shared test factory.</param>
    public UserControllerTests(CustomWebApplicationFactory factory)
    {
        // Each test gets a clean database — required because user settings are keyed
        // by TestUserId and several tests depend on the provisioned default values.
        factory.ResetDatabase();
        this._client = factory.CreateApiClient();
    }

    /// <summary>
    /// GET /api/v1/user/me returns 200 OK with user profile.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetProfile_Returns_200_WithProfile()
    {
        // Act
        var response = await this._client.GetAsync("/api/v1/user/me");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var profile = await response.Content.ReadFromJsonAsync<UserProfileDto>();
        Assert.NotNull(profile);
        Assert.Equal(CustomWebApplicationFactory.TestUserId, profile.UserId);
        Assert.Equal(CustomWebApplicationFactory.TestUsername, profile.Username);
    }

    /// <summary>
    /// GET /api/v1/user/settings returns 200 OK and provisions default settings.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetSettings_Returns_200_AndProvisionsDefaults()
    {
        // Act
        var response = await this._client.GetAsync("/api/v1/user/settings");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var settings = await response.Content.ReadFromJsonAsync<UserSettingsDto>();
        Assert.NotNull(settings);
        Assert.Equal(CustomWebApplicationFactory.TestUserId, settings.UserId);
        Assert.Equal("Shared", settings.DefaultScope);
        Assert.Equal(30, settings.PastDueLookbackDays);
    }

    /// <summary>
    /// PUT /api/v1/user/settings updates settings and returns 200 OK.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UpdateSettings_Returns_200_WithUpdatedSettings()
    {
        // Arrange - provision settings first
        await this._client.GetAsync("/api/v1/user/settings");

        var updateDto = new UserSettingsUpdateDto
        {
            AutoRealizePastDueItems = true,
            PastDueLookbackDays = 60,
            PreferredCurrency = "EUR",
        };

        // Act
        var response = await this._client.PutAsJsonAsync("/api/v1/user/settings", updateDto);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var settings = await response.Content.ReadFromJsonAsync<UserSettingsDto>();
        Assert.NotNull(settings);
        Assert.True(settings.AutoRealizePastDueItems);
        Assert.Equal(60, settings.PastDueLookbackDays);
        Assert.Equal("EUR", settings.PreferredCurrency);
    }

    /// <summary>
    /// GET /api/v1/user/scope returns 200 OK with current scope.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetScope_Returns_200_WithScope()
    {
        // Act
        var response = await this._client.GetAsync("/api/v1/user/scope");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var scope = await response.Content.ReadFromJsonAsync<ScopeDto>();
        Assert.NotNull(scope);

        // Default scope is null (all)
    }

    /// <summary>
    /// PUT /api/v1/user/scope sets the scope and returns 200 OK.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SetScope_Returns_200_WithScope()
    {
        // Arrange
        var scopeDto = new ScopeDto { Scope = "Personal" };

        // Act
        var response = await this._client.PutAsJsonAsync("/api/v1/user/scope", scopeDto);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var scope = await response.Content.ReadFromJsonAsync<ScopeDto>();
        Assert.NotNull(scope);
        Assert.Equal("Personal", scope.Scope);
    }

    /// <summary>
    /// GET /api/v1/user/settings returns new onboarding fields with defaults.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetSettings_Returns_OnboardingFieldDefaults()
    {
        // Act
        var response = await this._client.GetAsync("/api/v1/user/settings");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var settings = await response.Content.ReadFromJsonAsync<UserSettingsDto>();
        Assert.NotNull(settings);
        Assert.Equal(DayOfWeek.Sunday, settings.FirstDayOfWeek);
        Assert.False(settings.IsOnboarded);
    }

    /// <summary>
    /// PUT /api/v1/user/settings updates FirstDayOfWeek.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UpdateSettings_Updates_FirstDayOfWeek()
    {
        // Arrange - provision settings first
        await this._client.GetAsync("/api/v1/user/settings");

        var updateDto = new UserSettingsUpdateDto
        {
            FirstDayOfWeek = DayOfWeek.Monday,
        };

        // Act
        var response = await this._client.PutAsJsonAsync("/api/v1/user/settings", updateDto);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var settings = await response.Content.ReadFromJsonAsync<UserSettingsDto>();
        Assert.NotNull(settings);
        Assert.Equal(DayOfWeek.Monday, settings.FirstDayOfWeek);
    }

    /// <summary>
    /// POST /api/v1/user/settings/complete-onboarding sets IsOnboarded to true.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CompleteOnboarding_Returns_200_WithOnboardedTrue()
    {
        // Arrange - provision settings first
        await this._client.GetAsync("/api/v1/user/settings");

        // Act
        var response = await this._client.PostAsync("/api/v1/user/settings/complete-onboarding", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var settings = await response.Content.ReadFromJsonAsync<UserSettingsDto>();
        Assert.NotNull(settings);
        Assert.True(settings.IsOnboarded);
    }

    /// <summary>
    /// PUT /api/v1/user/scope with invalid scope returns 400 Bad Request.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SetScope_WithInvalidScope_Returns_400()
    {
        // Arrange
        var scopeDto = new ScopeDto { Scope = "InvalidScope" };

        // Act
        var response = await this._client.PutAsJsonAsync("/api/v1/user/scope", scopeDto);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
