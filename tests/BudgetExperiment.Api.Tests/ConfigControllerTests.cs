// <copyright file="ConfigControllerTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Net;
using System.Net.Http.Json;

using BudgetExperiment.Contracts.Dtos;

using Microsoft.AspNetCore.Mvc.Testing;

namespace BudgetExperiment.Api.Tests;

/// <summary>
/// Integration tests for the Config API endpoint.
/// </summary>
public sealed class ConfigControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigControllerTests"/> class.
    /// </summary>
    /// <param name="factory">The test factory.</param>
    public ConfigControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// GET /api/v1/config returns 200 OK without authentication.
    /// </summary>
    [Fact]
    public async Task Get_Returns_200_WithoutAuthentication()
    {
        // Arrange - create client WITHOUT authentication
        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/config");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>
    /// GET /api/v1/config returns valid ClientConfigDto.
    /// </summary>
    [Fact]
    public async Task Get_Returns_ValidClientConfigDto()
    {
        // Arrange
        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/config");
        var config = await response.Content.ReadFromJsonAsync<ClientConfigDto>();

        // Assert
        Assert.NotNull(config);
        Assert.NotNull(config.Authentication);
        Assert.False(string.IsNullOrEmpty(config.Authentication.Mode));
    }

    /// <summary>
    /// GET /api/v1/config returns cache headers.
    /// </summary>
    [Fact]
    public async Task Get_Returns_CacheHeaders()
    {
        // Arrange
        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/config");

        // Assert
        Assert.True(response.Headers.CacheControl?.MaxAge?.TotalSeconds >= 3600);
    }

    /// <summary>
    /// GET /api/v1/config returns OIDC config when mode is oidc.
    /// </summary>
    [Fact]
    public async Task Get_WhenOidcMode_Returns_OidcConfig()
    {
        // Arrange
        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/config");
        var config = await response.Content.ReadFromJsonAsync<ClientConfigDto>();

        // Assert - the factory configures Authentik as enabled, so mode should be oidc
        Assert.NotNull(config);
        Assert.Equal("oidc", config.Authentication.Mode);
        Assert.NotNull(config.Authentication.Oidc);
    }
}
