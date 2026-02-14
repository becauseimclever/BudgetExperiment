// <copyright file="ExportControllerAuthTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Net;

namespace BudgetExperiment.Api.Tests;

/// <summary>
/// Auth tests for export endpoints.
/// </summary>
public sealed class ExportControllerAuthTests : IClassFixture<AuthEnabledWebApplicationFactory>
{
    private readonly AuthEnabledWebApplicationFactory factory;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExportControllerAuthTests"/> class.
    /// </summary>
    /// <param name="factory">Auth-enabled test factory.</param>
    public ExportControllerAuthTests(AuthEnabledWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    /// <summary>
    /// Verifies export endpoints require authentication.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    public async Task ExportEndpoints_Require_Authentication()
    {
        // Arrange
        var client = factory.CreateUnauthenticatedClient();

        // Act
        var response = await client.GetAsync("/api/v1/exports/trends?months=6");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// Verifies export endpoints allow authenticated access.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    public async Task ExportEndpoints_Allow_Authenticated_Access()
    {
        // Arrange
        var client = factory.CreateAuthenticatedClient();

        // Act
        var response = await client.GetAsync("/api/v1/exports/trends?months=6");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
