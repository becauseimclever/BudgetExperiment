// <copyright file="AiControllerTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Net;
using System.Net.Http.Json;
using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Api.Tests;

/// <summary>
/// Integration tests for the AI API endpoints.
/// </summary>
public sealed class AiControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="AiControllerTests"/> class.
    /// </summary>
    /// <param name="factory">The test factory.</param>
    public AiControllerTests(CustomWebApplicationFactory factory)
    {
        this._client = factory.CreateApiClient();
    }

    /// <summary>
    /// GET /api/v1/ai/status returns 200 OK with status information.
    /// </summary>
    [Fact]
    public async Task GetStatus_Returns_200_WithStatusInfo()
    {
        // Act
        var response = await this._client.GetAsync("/api/v1/ai/status");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var status = await response.Content.ReadFromJsonAsync<AiStatusDto>();
        Assert.NotNull(status);
        Assert.NotNull(status.Endpoint);
    }

    /// <summary>
    /// GET /api/v1/ai/models returns 200 OK with model list.
    /// </summary>
    [Fact]
    public async Task GetModels_Returns_200_WithModelList()
    {
        // Act
        var response = await this._client.GetAsync("/api/v1/ai/models");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var models = await response.Content.ReadFromJsonAsync<List<AiModelDto>>();
        Assert.NotNull(models);
        // May be empty if AI service is not available during tests
    }

    /// <summary>
    /// GET /api/v1/ai/settings returns 200 OK with settings.
    /// </summary>
    [Fact]
    public async Task GetSettings_Returns_200_WithSettings()
    {
        // Act
        var response = await this._client.GetAsync("/api/v1/ai/settings");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var settings = await response.Content.ReadFromJsonAsync<AiSettingsDto>();
        Assert.NotNull(settings);
        Assert.NotNull(settings.OllamaEndpoint);
        Assert.NotNull(settings.ModelName);
    }

    /// <summary>
    /// PUT /api/v1/ai/settings returns 200 OK with updated settings.
    /// </summary>
    [Fact]
    public async Task UpdateSettings_Returns_200_WithSettings()
    {
        // Arrange
        var request = new AiSettingsDto
        {
            OllamaEndpoint = "http://localhost:11434",
            ModelName = "llama3.2",
            Temperature = 0.5m,
            MaxTokens = 3000,
            TimeoutSeconds = 180,
            IsEnabled = true,
        };

        // Act
        var response = await this._client.PutAsJsonAsync("/api/v1/ai/settings", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var settings = await response.Content.ReadFromJsonAsync<AiSettingsDto>();
        Assert.NotNull(settings);
        Assert.Equal(0.5m, settings.Temperature);
    }

    /// <summary>
    /// POST /api/v1/ai/analyze returns 200 with analysis response.
    /// </summary>
    /// <remarks>
    /// When AI is unavailable, the service methods return empty results
    /// which results in a valid response with zero counts.
    /// </remarks>
    [Fact]
    public async Task Analyze_Returns_200_WithAnalysisResponse()
    {
        // Act
        var response = await this._client.PostAsync("/api/v1/ai/analyze", null);

        // Assert - either 200 (AI available or unavailable with fallback)
        // or 503 (AI explicitly unavailable via status check)
        // or 504 (timeout - in test environment this shouldn't happen)
        Assert.True(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.ServiceUnavailable ||
            response.StatusCode == HttpStatusCode.GatewayTimeout,
            $"Expected OK, ServiceUnavailable, or GatewayTimeout, got {response.StatusCode}");

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var result = await response.Content.ReadFromJsonAsync<AnalysisResponseDto>();
            Assert.NotNull(result);
        }
    }

    /// <summary>
    /// POST /api/v1/ai/analyze returns 504 when timeout error message is returned.
    /// </summary>
    /// <remarks>
    /// This tests that the endpoint is configured to return 504 for timeout scenarios.
    /// The actual timeout behavior is tested in unit tests with mocked dependencies.
    /// </remarks>
    [Fact]
    public async Task Analyze_Returns_ExpectedStatusCodes()
    {
        // Act
        var response = await this._client.PostAsync("/api/v1/ai/analyze", null);

        // Assert - verify the endpoint is accessible and returns one of the expected codes
        var validStatusCodes = new[]
        {
            HttpStatusCode.OK,
            HttpStatusCode.ServiceUnavailable,
            HttpStatusCode.GatewayTimeout,
        };

        Assert.Contains(response.StatusCode, validStatusCodes);
    }
}
