// <copyright file="SettingsControllerTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Net;
using System.Net.Http.Json;

using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Api.Tests;

/// <summary>
/// Integration tests for the Settings API endpoints.
/// </summary>
public sealed class SettingsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsControllerTests"/> class.
    /// </summary>
    /// <param name="factory">The test factory.</param>
    public SettingsControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateApiClient();
    }

    /// <summary>
    /// GET /api/v1/settings returns 200 OK with settings.
    /// </summary>
    [Fact]
    public async Task Get_Returns_200_WithSettings()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/settings");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var settings = await response.Content.ReadFromJsonAsync<AppSettingsDto>();
        Assert.NotNull(settings);

        // Settings may have been modified by other tests, just verify we get valid values
        Assert.True(settings.PastDueLookbackDays >= 1 && settings.PastDueLookbackDays <= 365);
    }

    /// <summary>
    /// PUT /api/v1/settings updates AutoRealizePastDueItems and returns 200 OK.
    /// </summary>
    [Fact]
    public async Task Put_Updates_AutoRealize_Returns_200()
    {
        // Arrange
        var updateDto = new AppSettingsUpdateDto { AutoRealizePastDueItems = true };

        // Act
        var response = await _client.PutAsJsonAsync("/api/v1/settings", updateDto);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var settings = await response.Content.ReadFromJsonAsync<AppSettingsDto>();
        Assert.NotNull(settings);
        Assert.True(settings.AutoRealizePastDueItems);
    }

    /// <summary>
    /// PUT /api/v1/settings updates PastDueLookbackDays and returns 200 OK.
    /// </summary>
    [Fact]
    public async Task Put_Updates_PastDueLookbackDays_Returns_200()
    {
        // Arrange
        var updateDto = new AppSettingsUpdateDto { PastDueLookbackDays = 14 };

        // Act
        var response = await _client.PutAsJsonAsync("/api/v1/settings", updateDto);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var settings = await response.Content.ReadFromJsonAsync<AppSettingsDto>();
        Assert.NotNull(settings);
        Assert.Equal(14, settings.PastDueLookbackDays);
    }

    /// <summary>
    /// PUT /api/v1/settings with invalid PastDueLookbackDays returns 400 Bad Request.
    /// </summary>
    [Fact]
    public async Task Put_With_Invalid_PastDueLookbackDays_Returns_400()
    {
        // Arrange
        var updateDto = new AppSettingsUpdateDto { PastDueLookbackDays = 0 };

        // Act
        var response = await _client.PutAsJsonAsync("/api/v1/settings", updateDto);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// PUT /api/v1/settings updates both settings and returns 200 OK.
    /// </summary>
    [Fact]
    public async Task Put_Updates_Both_Settings_Returns_200()
    {
        // Arrange
        var updateDto = new AppSettingsUpdateDto
        {
            AutoRealizePastDueItems = true,
            PastDueLookbackDays = 7,
        };

        // Act
        var response = await _client.PutAsJsonAsync("/api/v1/settings", updateDto);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var settings = await response.Content.ReadFromJsonAsync<AppSettingsDto>();
        Assert.NotNull(settings);
        Assert.True(settings.AutoRealizePastDueItems);
        Assert.Equal(7, settings.PastDueLookbackDays);
    }
}
