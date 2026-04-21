// <copyright file="KaizenGoalsControllerTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Net;
using System.Net.Http.Json;

using BudgetExperiment.Contracts.Dtos;

using Shouldly;

namespace BudgetExperiment.Api.Tests;

/// <summary>
/// Integration tests for the Kaizen Goals API endpoints.
/// </summary>
[Collection("ApiDb")]
public sealed class KaizenGoalsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    /// <summary>
    /// Initializes a new instance of the <see cref="KaizenGoalsControllerTests"/> class.
    /// </summary>
    /// <param name="factory">The test factory.</param>
    public KaizenGoalsControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateApiClient();
    }

    /// <summary>
    /// GET /api/v1/goals/kaizen/week/{weekStart} returns 404 when feature flag is disabled.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetByWeekAsync_Returns_404_WhenFeatureDisabled()
    {
        // Arrange
        var flagRequest = new UpdateFeatureFlagRequest { Enabled = false };
        await _client.PutAsJsonAsync("/api/v1/features/Kaizen:MicroGoals", flagRequest);

        // Act
        var response = await _client.GetAsync("/api/v1/goals/kaizen/week/2024-01-01");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    /// <summary>
    /// GET /api/v1/goals/kaizen returns 404 when feature flag is disabled.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetRangeAsync_Returns_404_WhenFeatureDisabled()
    {
        // Arrange
        var flagRequest = new UpdateFeatureFlagRequest { Enabled = false };
        await _client.PutAsJsonAsync("/api/v1/features/Kaizen:MicroGoals", flagRequest);

        // Act
        var response = await _client.GetAsync("/api/v1/goals/kaizen?from=2024-01-01&to=2024-12-31");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    /// <summary>
    /// POST /api/v1/goals/kaizen/week/{weekStart} returns 404 when feature flag is disabled.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CreateAsync_Returns_404_WhenFeatureDisabled()
    {
        // Arrange
        var flagRequest = new UpdateFeatureFlagRequest { Enabled = false };
        await _client.PutAsJsonAsync("/api/v1/features/Kaizen:MicroGoals", flagRequest);

        var createRequest = new CreateKaizenGoalDto { Description = "Test goal" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/goals/kaizen/week/2024-01-01", createRequest);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    /// <summary>
    /// PUT /api/v1/goals/kaizen/{goalId} returns 404 when feature flag is disabled.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UpdateAsync_Returns_404_WhenFeatureDisabled()
    {
        // Arrange
        var flagRequest = new UpdateFeatureFlagRequest { Enabled = false };
        await _client.PutAsJsonAsync("/api/v1/features/Kaizen:MicroGoals", flagRequest);

        var updateRequest = new UpdateKaizenGoalDto { Description = "Updated goal" };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/v1/goals/kaizen/{Guid.NewGuid()}", updateRequest);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    /// <summary>
    /// DELETE /api/v1/goals/kaizen/{goalId} returns 404 when feature flag is disabled.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task DeleteAsync_Returns_404_WhenFeatureDisabled()
    {
        // Arrange
        var flagRequest = new UpdateFeatureFlagRequest { Enabled = false };
        await _client.PutAsJsonAsync("/api/v1/features/Kaizen:MicroGoals", flagRequest);

        // Act
        var response = await _client.DeleteAsync($"/api/v1/goals/kaizen/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    /// <summary>
    /// GET /api/v1/goals/kaizen returns 200 with empty list when feature enabled.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetRangeAsync_Returns_200_WhenFeatureEnabled()
    {
        // Arrange - enable feature
        var flagRequest = new UpdateFeatureFlagRequest { Enabled = true };
        await _client.PutAsJsonAsync("/api/v1/features/Kaizen:MicroGoals", flagRequest);

        // Act
        var response = await _client.GetAsync("/api/v1/goals/kaizen?from=2024-01-01&to=2024-12-31");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var goals = await response.Content.ReadFromJsonAsync<List<KaizenGoalDto>>();
        goals.ShouldNotBeNull();
    }

    /// <summary>
    /// GET /api/v1/goals/kaizen/week/{weekStart} returns 204 when no goal exists.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetByWeekAsync_Returns_204_WhenNoGoalExists()
    {
        // Arrange - enable feature
        var flagRequest = new UpdateFeatureFlagRequest { Enabled = true };
        await _client.PutAsJsonAsync("/api/v1/features/Kaizen:MicroGoals", flagRequest);

        // Act
        var response = await _client.GetAsync("/api/v1/goals/kaizen/week/2099-01-01");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }
}
