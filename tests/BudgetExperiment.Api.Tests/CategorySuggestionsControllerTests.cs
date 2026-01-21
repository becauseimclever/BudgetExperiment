// <copyright file="CategorySuggestionsControllerTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Net;
using System.Net.Http.Json;

using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Api.Tests;

/// <summary>
/// Integration tests for the CategorySuggestions API endpoints.
/// </summary>
public sealed class CategorySuggestionsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="CategorySuggestionsControllerTests"/> class.
    /// </summary>
    /// <param name="factory">The test factory.</param>
    public CategorySuggestionsControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateApiClient();
    }

    /// <summary>
    /// POST /api/v1/category-suggestions/analyze returns 200 OK.
    /// </summary>
    [Fact]
    public async Task Analyze_Returns_200_Ok()
    {
        // Act
        var response = await _client.PostAsync("/api/v1/categorysuggestions/analyze", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var suggestions = await response.Content.ReadFromJsonAsync<List<CategorySuggestionDto>>();
        Assert.NotNull(suggestions);
    }

    /// <summary>
    /// GET /api/v1/category-suggestions returns 200 OK with list.
    /// </summary>
    [Fact]
    public async Task GetPending_Returns_200_WithList()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/categorysuggestions");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var suggestions = await response.Content.ReadFromJsonAsync<List<CategorySuggestionDto>>();
        Assert.NotNull(suggestions);
    }

    /// <summary>
    /// GET /api/v1/category-suggestions/{id} returns 404 for non-existent suggestion.
    /// </summary>
    [Fact]
    public async Task GetById_Returns_404_ForNonExistent()
    {
        // Act
        var response = await _client.GetAsync($"/api/v1/categorysuggestions/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// POST /api/v1/category-suggestions/{id}/accept returns 404 for non-existent suggestion.
    /// </summary>
    [Fact]
    public async Task Accept_Returns_404_ForNonExistent()
    {
        // Act
        var response = await _client.PostAsJsonAsync(
            $"/api/v1/categorysuggestions/{Guid.NewGuid()}/accept",
            new AcceptCategorySuggestionRequest());

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// POST /api/v1/category-suggestions/{id}/dismiss returns 404 for non-existent suggestion.
    /// </summary>
    [Fact]
    public async Task Dismiss_Returns_404_ForNonExistent()
    {
        // Act
        var response = await _client.PostAsync($"/api/v1/categorysuggestions/{Guid.NewGuid()}/dismiss", null);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// POST /api/v1/category-suggestions/bulk-accept returns 400 for empty list.
    /// </summary>
    [Fact]
    public async Task BulkAccept_Returns_400_ForEmptyList()
    {
        // Act
        var response = await _client.PostAsJsonAsync(
            "/api/v1/categorysuggestions/bulk-accept",
            new BulkAcceptCategorySuggestionsRequest { SuggestionIds = new List<Guid>() });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// POST /api/v1/category-suggestions/bulk-accept returns 200 with results for valid IDs.
    /// </summary>
    [Fact]
    public async Task BulkAccept_Returns_200_WithResults()
    {
        // Act
        var response = await _client.PostAsJsonAsync(
            "/api/v1/categorysuggestions/bulk-accept",
            new BulkAcceptCategorySuggestionsRequest { SuggestionIds = new List<Guid> { Guid.NewGuid() } });

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var results = await response.Content.ReadFromJsonAsync<List<AcceptCategorySuggestionResultDto>>();
        Assert.NotNull(results);
        Assert.Single(results);
        Assert.False(results[0].Success); // Should fail for non-existent suggestion
    }

    /// <summary>
    /// GET /api/v1/category-suggestions/{id}/preview-rules returns 404 for non-existent suggestion.
    /// </summary>
    [Fact]
    public async Task PreviewRules_Returns_404_ForNonExistent()
    {
        // Act
        var response = await _client.GetAsync($"/api/v1/categorysuggestions/{Guid.NewGuid()}/preview-rules");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
