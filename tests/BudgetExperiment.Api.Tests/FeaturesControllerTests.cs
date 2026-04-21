// <copyright file="FeaturesControllerTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Net;
using System.Net.Http.Json;

using BudgetExperiment.Contracts.Dtos;

using Shouldly;

namespace BudgetExperiment.Api.Tests;

/// <summary>
/// Integration tests for the Features API endpoints.
/// </summary>
[Collection("ApiDb")]
public sealed class FeaturesControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    /// <summary>
    /// Initializes a new instance of the <see cref="FeaturesControllerTests"/> class.
    /// </summary>
    /// <param name="factory">The test factory.</param>
    public FeaturesControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateApiClient();
    }

    /// <summary>
    /// GET /api/v1/features returns 200 OK with dictionary of feature flags.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetFeaturesAsync_Returns_200_WithFlagDictionary()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/features");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var flags = await response.Content.ReadFromJsonAsync<Dictionary<string, bool>>();
        flags.ShouldNotBeNull();
        flags.ShouldNotBeEmpty();
    }

    /// <summary>
    /// PUT /api/v1/features/{flagName} updates feature flag and returns updated state.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UpdateFeatureFlagAsync_UpdatesFlag_Returns_200()
    {
        // Arrange
        var request = new UpdateFeatureFlagRequest { Enabled = true };

        // Act
        var response = await _client.PutAsJsonAsync("/api/v1/features/Reports:CustomReportBuilder", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<UpdateFeatureFlagResponse>();
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Reports:CustomReportBuilder");
        result.Enabled.ShouldBeTrue();
        result.UpdatedAtUtc.ShouldBeInRange(DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddMinutes(1));
    }

    /// <summary>
    /// PUT /api/v1/features/{flagName} can toggle flag false.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UpdateFeatureFlagAsync_CanDisableFlag_Returns_200()
    {
        // Arrange
        var enableRequest = new UpdateFeatureFlagRequest { Enabled = true };
        await _client.PutAsJsonAsync("/api/v1/features/Kaizen:MicroGoals", enableRequest);

        var disableRequest = new UpdateFeatureFlagRequest { Enabled = false };

        // Act
        var response = await _client.PutAsJsonAsync("/api/v1/features/Kaizen:MicroGoals", disableRequest);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<UpdateFeatureFlagResponse>();
        result.ShouldNotBeNull();
        result.Enabled.ShouldBeFalse();
    }
}
