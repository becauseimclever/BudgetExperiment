// <copyright file="ApiVersioningTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Net;

namespace BudgetExperiment.Api.Tests;

/// <summary>
/// Integration tests verifying API versioning infrastructure is properly configured.
/// </summary>
[Collection("ApiDb")]
public sealed class ApiVersioningTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiVersioningTests"/> class.
    /// </summary>
    /// <param name="factory">The test factory.</param>
    public ApiVersioningTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateApiClient();
    }

    /// <summary>
    /// Responses include the api-supported-versions header indicating which versions are available.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Response_Includes_ApiSupportedVersions_HeaderAsync()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/accounts");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(
            response.Headers.Contains("api-supported-versions"),
            "Response should include 'api-supported-versions' header");
        var versions = response.Headers.GetValues("api-supported-versions");
        Assert.Contains("1.0", versions);
    }

    /// <summary>
    /// Existing v1 URLs continue to work after versioning infrastructure is added.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ExistingV1Urls_ContinueToWork_AfterVersioningAsync()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/accounts");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>
    /// Suggestions controller responds at the normalized versioned route.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SuggestionsController_Responds_AtNormalizedRouteAsync()
    {
        // Act — suggestions controller should be at standardized path
        var response = await _client.GetAsync("/api/v1/suggestions");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
