// <copyright file="VersionControllerTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Net;
using System.Net.Http.Json;

using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Api.Tests;

/// <summary>
/// Integration tests for the Version API endpoint.
/// </summary>
[Collection("ApiDb")]
public sealed class VersionControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="VersionControllerTests"/> class.
    /// </summary>
    /// <param name="factory">The test factory.</param>
    public VersionControllerTests(CustomWebApplicationFactory factory)
    {
        this._factory = factory;
        this._client = factory.CreateApiClient();
    }

    /// <summary>
    /// GET /api/v1/version returns 200 with version info.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetVersion_Returns_200_WithVersionInfo()
    {
        // Act
        var response = await this._client.GetAsync("/api/v1/version");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var versionInfo = await response.Content.ReadFromJsonAsync<VersionInfoDto>();
        Assert.NotNull(versionInfo);
        Assert.False(string.IsNullOrWhiteSpace(versionInfo.Version));
        Assert.False(string.IsNullOrWhiteSpace(versionInfo.Environment));
        Assert.True(versionInfo.BuildDateUtc > DateTime.MinValue);
    }

    /// <summary>
    /// GET /api/v1/version is accessible without authentication (AllowAnonymous).
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetVersion_IsAccessibleAnonymously()
    {
        // Arrange - use client without auth header
        using var client = this._factory.CreateClient(); // no auth header

        // Act
        var response = await client.GetAsync("/api/v1/version");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>
    /// GET /api/v1/version returns the correct environment name.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetVersion_ReturnsEnvironmentName()
    {
        // Act
        var response = await this._client.GetAsync("/api/v1/version");
        var versionInfo = await response.Content.ReadFromJsonAsync<VersionInfoDto>();

        // Assert - WebApplicationFactory defaults to "Development"
        Assert.NotNull(versionInfo);
        Assert.Equal("Development", versionInfo.Environment);
    }
}
