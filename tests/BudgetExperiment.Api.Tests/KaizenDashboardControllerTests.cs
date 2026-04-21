// <copyright file="KaizenDashboardControllerTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Net;
using System.Net.Http.Json;

using BudgetExperiment.Contracts.Dtos;

using Shouldly;

namespace BudgetExperiment.Api.Tests;

/// <summary>
/// Integration tests for the Kaizen Dashboard API endpoints.
/// </summary>
[Collection("ApiDb")]
public sealed class KaizenDashboardControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    /// <summary>
    /// Initializes a new instance of the <see cref="KaizenDashboardControllerTests"/> class.
    /// </summary>
    /// <param name="factory">The test factory.</param>
    public KaizenDashboardControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateApiClient();
    }

    /// <summary>
    /// GET /api/v1/reports/kaizen-dashboard returns 404 when feature flag is disabled.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetAsync_Returns_404_WhenFeatureDisabled()
    {
        // Arrange - ensure feature is disabled
        var flagRequest = new UpdateFeatureFlagRequest { Enabled = false };
        await _client.PutAsJsonAsync("/api/v1/features/Kaizen:Dashboard", flagRequest);

        // Act
        var response = await _client.GetAsync("/api/v1/reports/kaizen-dashboard");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    /// <summary>
    /// GET /api/v1/reports/kaizen-dashboard returns 200 when feature enabled.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetAsync_Returns_200_WhenFeatureEnabled()
    {
        // Arrange - enable feature
        var flagRequest = new UpdateFeatureFlagRequest { Enabled = true };
        await _client.PutAsJsonAsync("/api/v1/features/Kaizen:Dashboard", flagRequest);

        // Act
        var response = await _client.GetAsync("/api/v1/reports/kaizen-dashboard");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var dashboard = await response.Content.ReadFromJsonAsync<KaizenDashboardDto>();
        dashboard.ShouldNotBeNull();
    }

    /// <summary>
    /// GET /api/v1/reports/kaizen-dashboard?weeks=52 returns 200 with max weeks.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetAsync_Returns_200_WithMaxWeeks()
    {
        // Arrange - enable feature
        var flagRequest = new UpdateFeatureFlagRequest { Enabled = true };
        await _client.PutAsJsonAsync("/api/v1/features/Kaizen:Dashboard", flagRequest);

        // Act
        var response = await _client.GetAsync("/api/v1/reports/kaizen-dashboard?weeks=52");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    /// <summary>
    /// GET /api/v1/reports/kaizen-dashboard?weeks=0 returns 400 for invalid weeks.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetAsync_Returns_400_ForInvalidWeeks()
    {
        // Arrange - enable feature
        var flagRequest = new UpdateFeatureFlagRequest { Enabled = true };
        await _client.PutAsJsonAsync("/api/v1/features/Kaizen:Dashboard", flagRequest);

        // Act
        var response = await _client.GetAsync("/api/v1/reports/kaizen-dashboard?weeks=0");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    /// <summary>
    /// GET /api/v1/reports/kaizen-dashboard?weeks=100 returns 400 for weeks exceeding max.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetAsync_Returns_400_ForWeeksExceedingMax()
    {
        // Arrange - enable feature
        var flagRequest = new UpdateFeatureFlagRequest { Enabled = true };
        await _client.PutAsJsonAsync("/api/v1/features/Kaizen:Dashboard", flagRequest);

        // Act
        var response = await _client.GetAsync("/api/v1/reports/kaizen-dashboard?weeks=100");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
