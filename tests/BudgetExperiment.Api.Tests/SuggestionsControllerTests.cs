// <copyright file="SuggestionsControllerTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Net;
using System.Net.Http.Json;
using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Api.Tests;

/// <summary>
/// Integration tests for the AI Suggestions API endpoints.
/// </summary>
public sealed class SuggestionsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="SuggestionsControllerTests"/> class.
    /// </summary>
    /// <param name="factory">The test factory.</param>
    public SuggestionsControllerTests(CustomWebApplicationFactory factory)
    {
        this._client = factory.CreateApiClient();
    }

    /// <summary>
    /// GET /api/v1/ai/suggestions returns 200 OK with suggestion list.
    /// </summary>
    [Fact]
    public async Task GetPending_Returns_200_WithSuggestionList()
    {
        // Act
        var response = await this._client.GetAsync("/api/v1/ai/suggestions");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var suggestions = await response.Content.ReadFromJsonAsync<List<RuleSuggestionDto>>();
        Assert.NotNull(suggestions);
    }

    /// <summary>
    /// GET /api/v1/ai/suggestions?type=NewRule returns 200 OK with filtered suggestions.
    /// </summary>
    [Fact]
    public async Task GetPending_WithTypeFilter_Returns_200()
    {
        // Act
        var response = await this._client.GetAsync("/api/v1/ai/suggestions?type=NewRule");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var suggestions = await response.Content.ReadFromJsonAsync<List<RuleSuggestionDto>>();
        Assert.NotNull(suggestions);
    }

    /// <summary>
    /// GET /api/v1/ai/suggestions/{id} returns 404 when suggestion not found.
    /// </summary>
    [Fact]
    public async Task GetById_Returns_404_WhenNotFound()
    {
        // Act
        var response = await this._client.GetAsync($"/api/v1/ai/suggestions/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// POST /api/v1/ai/suggestions/generate returns appropriate response.
    /// </summary>
    /// <remarks>
    /// When AI is unavailable, may return 503 or 200 with empty results.
    /// </remarks>
    [Fact]
    public async Task Generate_Returns_ValidResponse()
    {
        // Arrange
        var request = new GenerateSuggestionsRequest
        {
            SuggestionType = "NewRule",
            MaxSuggestions = 5,
        };

        // Act
        var response = await this._client.PostAsJsonAsync("/api/v1/ai/suggestions/generate", request);

        // Assert - either 200 (with possibly empty results) or 503 (AI unavailable)
        Assert.True(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.ServiceUnavailable,
            $"Expected OK or ServiceUnavailable, got {response.StatusCode}");

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var suggestions = await response.Content.ReadFromJsonAsync<List<RuleSuggestionDto>>();
            Assert.NotNull(suggestions);
        }
    }

    /// <summary>
    /// POST /api/v1/ai/suggestions/generate with invalid type returns 400.
    /// </summary>
    [Fact]
    public async Task Generate_Returns_400_ForInvalidType()
    {
        // Arrange
        var request = new GenerateSuggestionsRequest
        {
            SuggestionType = "InvalidType",
            MaxSuggestions = 5,
        };

        // Act
        var response = await this._client.PostAsJsonAsync("/api/v1/ai/suggestions/generate", request);

        // Assert
        // May be 503 (AI unavailable check happens first) or 400 (invalid type)
        Assert.True(
            response.StatusCode == HttpStatusCode.BadRequest ||
            response.StatusCode == HttpStatusCode.ServiceUnavailable);
    }

    /// <summary>
    /// POST /api/v1/ai/suggestions/{id}/accept returns 404 when suggestion not found.
    /// </summary>
    [Fact]
    public async Task Accept_Returns_404_WhenNotFound()
    {
        // Act
        var response = await this._client.PostAsync($"/api/v1/ai/suggestions/{Guid.NewGuid()}/accept", null);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// POST /api/v1/ai/suggestions/{id}/dismiss returns 404 when suggestion not found.
    /// </summary>
    [Fact]
    public async Task Dismiss_Returns_404_WhenNotFound()
    {
        // Arrange
        var request = new DismissSuggestionRequest { Reason = "Not useful" };

        // Act
        var response = await this._client.PostAsJsonAsync($"/api/v1/ai/suggestions/{Guid.NewGuid()}/dismiss", request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// POST /api/v1/ai/suggestions/{id}/feedback returns 404 when suggestion not found.
    /// </summary>
    [Fact]
    public async Task Feedback_Returns_404_WhenNotFound()
    {
        // Arrange
        var request = new FeedbackRequest { IsPositive = true };

        // Act
        var response = await this._client.PostAsJsonAsync($"/api/v1/ai/suggestions/{Guid.NewGuid()}/feedback", request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
