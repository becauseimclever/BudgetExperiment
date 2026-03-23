// <copyright file="AnalyzeConsolidationAuthTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Net;

using Shouldly;

namespace BudgetExperiment.Api.Tests.Categorization;

/// <summary>
/// Authentication tests for the POST /api/v1/categorizationrules/analyze-consolidation endpoint.
/// Feature 116 Slice 4: verifies that the endpoint is protected by authorization.
/// These tests are RED — the endpoint does not yet exist.
/// Lucius must implement the endpoint with <c>[Authorize]</c> to make them green.
/// </summary>
[Collection("ApiDb")]
public sealed class AnalyzeConsolidationAuthTests : IClassFixture<AuthEnabledWebApplicationFactory>
{
    private readonly HttpClient _unauthenticatedClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="AnalyzeConsolidationAuthTests"/> class.
    /// </summary>
    /// <param name="factory">The auth-enabled test factory.</param>
    public AnalyzeConsolidationAuthTests(AuthEnabledWebApplicationFactory factory)
    {
        _unauthenticatedClient = factory.CreateUnauthenticatedClient();
    }

    // -------------------------------------------------------------------------
    // 3. Unauthenticated request → 401
    // -------------------------------------------------------------------------

    /// <summary>
    /// An unauthenticated POST to analyze-consolidation must return 401 Unauthorized.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task PostAnalyzeConsolidation_Unauthenticated_Returns401()
    {
        // Act
        var response = await _unauthenticatedClient.PostAsync(
            "/api/v1/categorizationrules/analyze-consolidation",
            content: null);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
