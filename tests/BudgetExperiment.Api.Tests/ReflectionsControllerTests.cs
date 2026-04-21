// <copyright file="ReflectionsControllerTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Net;
using System.Net.Http.Json;

using BudgetExperiment.Contracts.Dtos;

using Shouldly;

namespace BudgetExperiment.Api.Tests;

/// <summary>
/// Integration tests for the Reflections API endpoints.
/// </summary>
[Collection("ApiDb")]
public sealed class ReflectionsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReflectionsControllerTests"/> class.
    /// </summary>
    /// <param name="factory">The test factory.</param>
    public ReflectionsControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateApiClient();
    }

    /// <summary>
    /// GET /api/v1/reflections/month/{year}/{month} returns 404 when feature flag is disabled.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetByMonthAsync_Returns_404_WhenFeatureDisabled()
    {
        // Arrange
        var flagRequest = new UpdateFeatureFlagRequest { Enabled = false };
        await _client.PutAsJsonAsync("/api/v1/features/Kakeibo:MonthlyReflectionPrompts", flagRequest);

        // Act
        var response = await _client.GetAsync("/api/v1/reflections/month/2024/1");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    /// <summary>
    /// GET /api/v1/reflections returns 404 when feature flag is disabled.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetHistoryAsync_Returns_404_WhenFeatureDisabled()
    {
        // Arrange
        var flagRequest = new UpdateFeatureFlagRequest { Enabled = false };
        await _client.PutAsJsonAsync("/api/v1/features/Kakeibo:MonthlyReflectionPrompts", flagRequest);

        // Act
        var response = await _client.GetAsync("/api/v1/reflections");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    /// <summary>
    /// GET /api/v1/calendar/month/{year}/{month}/summary returns 404 when feature flag is disabled.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetMonthSummaryAsync_Returns_404_WhenFeatureDisabled()
    {
        // Arrange
        var flagRequest = new UpdateFeatureFlagRequest { Enabled = false };
        await _client.PutAsJsonAsync("/api/v1/features/Kakeibo:MonthlyReflectionPrompts", flagRequest);

        // Act
        var response = await _client.GetAsync("/api/v1/calendar/month/2024/1/summary");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    /// <summary>
    /// POST /api/v1/reflections/month/{year}/{month} returns 404 when feature flag is disabled.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CreateOrUpdateAsync_Returns_404_WhenFeatureDisabled()
    {
        // Arrange
        var flagRequest = new UpdateFeatureFlagRequest { Enabled = false };
        await _client.PutAsJsonAsync("/api/v1/features/Kakeibo:MonthlyReflectionPrompts", flagRequest);

        var createRequest = new CreateOrUpdateMonthlyReflectionDto
        {
            SavingsGoal = 100,
            IntentionText = "Test intention",
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/reflections/month/2024/1", createRequest);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    /// <summary>
    /// DELETE /api/v1/reflections/{reflectionId} returns 404 when feature flag is disabled.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task DeleteAsync_Returns_404_WhenFeatureDisabled()
    {
        // Arrange
        var flagRequest = new UpdateFeatureFlagRequest { Enabled = false };
        await _client.PutAsJsonAsync("/api/v1/features/Kakeibo:MonthlyReflectionPrompts", flagRequest);

        // Act
        var response = await _client.DeleteAsync($"/api/v1/reflections/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    /// <summary>
    /// GET /api/v1/reflections returns 200 with paginated results when feature enabled.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetHistoryAsync_Returns_200_WhenFeatureEnabled()
    {
        // Arrange - enable feature
        var flagRequest = new UpdateFeatureFlagRequest { Enabled = true };
        await _client.PutAsJsonAsync("/api/v1/features/Kakeibo:MonthlyReflectionPrompts", flagRequest);

        // Act
        var response = await _client.GetAsync("/api/v1/reflections");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.Contains("X-Pagination-TotalCount").ShouldBeTrue();
    }

    /// <summary>
    /// GET /api/v1/reflections/month/{year}/{month} returns 204 when no reflection exists.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetByMonthAsync_Returns_204_WhenNoReflectionExists()
    {
        // Arrange - enable feature
        var flagRequest = new UpdateFeatureFlagRequest { Enabled = true };
        await _client.PutAsJsonAsync("/api/v1/features/Kakeibo:MonthlyReflectionPrompts", flagRequest);

        // Act
        var response = await _client.GetAsync("/api/v1/reflections/month/2099/12");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    /// <summary>
    /// GET /api/v1/calendar/month/{year}/{month}/summary returns 400 for invalid month.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetMonthSummaryAsync_Returns_400_ForInvalidMonth()
    {
        // Arrange - enable feature
        var flagRequest = new UpdateFeatureFlagRequest { Enabled = true };
        await _client.PutAsJsonAsync("/api/v1/features/Kakeibo:MonthlyReflectionPrompts", flagRequest);

        // Act
        var response = await _client.GetAsync("/api/v1/calendar/month/2024/13/summary");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
