// <copyright file="AnalyzeConsolidationEndpointTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

using BudgetExperiment.Contracts.Dtos;

using Microsoft.Extensions.DependencyInjection;

using Moq;
using Shouldly;

namespace BudgetExperiment.Api.Tests.Categorization;

/// <summary>
/// Integration tests for the POST /api/v1/categorizationrules/analyze-consolidation endpoint.
/// Feature 116 Slice 4: Analysis API.
/// These tests are RED — the endpoint does not yet exist.
/// Lucius must implement the endpoint in <see cref="Controllers.CategorizationRulesController"/>
/// to make them green.
/// </summary>
[Collection("ApiDb")]
public sealed class AnalyzeConsolidationEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    /// <summary>
    /// Initializes a new instance of the <see cref="AnalyzeConsolidationEndpointTests"/> class.
    /// </summary>
    /// <param name="factory">The shared test factory.</param>
    public AnalyzeConsolidationEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // -------------------------------------------------------------------------
    // 1. No suggestions → 200 with empty array
    // -------------------------------------------------------------------------

    /// <summary>
    /// POST analyze-consolidation when the service finds no suggestions returns 200 with an empty list.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task PostAnalyzeConsolidation_WhenNoSuggestions_Returns200WithEmptyList()
    {
        // Arrange
        var mockService = new Mock<IRuleConsolidationService>();
        mockService
            .Setup(s => s.AnalyzeAndStoreAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<RuleSuggestion>)Array.Empty<RuleSuggestion>());

        var client = _factory
            .WithWebHostBuilder(builder =>
                builder.ConfigureServices(services =>
                    services.AddScoped(_ => mockService.Object)))
            .CreateClient();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("TestAuto", "authenticated");

        // Act
        var response = await client.PostAsync(
            "/api/v1/categorizationrules/analyze-consolidation",
            content: null);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        body.ShouldBe("[]");
    }

    // -------------------------------------------------------------------------
    // 2. One suggestion returned → 200 with list of one item
    // -------------------------------------------------------------------------

    /// <summary>
    /// POST analyze-consolidation when the service returns one suggestion responds with
    /// 200 OK and a JSON array containing exactly one item.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task PostAnalyzeConsolidation_WhenSuggestionsFound_Returns200WithItems()
    {
        // Arrange
        var suggestion = RuleSuggestion.CreateConsolidationSuggestion(
            title: "Consolidate WALMART rules",
            description: "Two identical WALMART Contains rules detected.",
            reasoning: "Both rules share the same pattern and target category.",
            confidence: 1.0m,
            ruleIds: new List<Guid> { Guid.NewGuid(), Guid.NewGuid() },
            consolidatedPattern: "WALMART");

        var mockService = new Mock<IRuleConsolidationService>();
        mockService
            .Setup(s => s.AnalyzeAndStoreAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<RuleSuggestion>)new List<RuleSuggestion> { suggestion });

        var client = _factory
            .WithWebHostBuilder(builder =>
                builder.ConfigureServices(services =>
                    services.AddScoped(_ => mockService.Object)))
            .CreateClient();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("TestAuto", "authenticated");

        // Act
        var response = await client.PostAsync(
            "/api/v1/categorizationrules/analyze-consolidation",
            content: null);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var items = await response.Content.ReadFromJsonAsync<List<RuleSuggestionDto>>();
        items.ShouldNotBeNull();
        items.Count.ShouldBe(1);
    }
}
