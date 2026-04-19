// <copyright file="CustomReportsControllerTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Api.Tests;

/// <summary>
/// Integration tests for the CustomReports API endpoints (ETag/concurrency).
/// </summary>
[Collection("ApiDb")]
public sealed class CustomReportsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomReportsControllerTests"/> class.
    /// </summary>
    /// <param name="factory">The test factory.</param>
    public CustomReportsControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateApiClient();
    }

    /// <summary>
    /// GET /api/v1/custom-reports/{id} returns ETag header.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetById_Returns_ETag_Header()
    {
        // Arrange
        var createDto = new CustomReportLayoutCreateDto { Name = "ETag Report", LayoutJson = "{\"type\":\"bar\"}" };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/custom-reports", createDto);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<CustomReportLayoutDto>();

        // Act
        var response = await _client.GetAsync($"/api/v1/custom-reports/{created!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(response.Headers.ETag);
        Assert.False(string.IsNullOrEmpty(response.Headers.ETag.Tag));
    }

    /// <summary>
    /// PUT /api/v1/custom-reports/{id} with valid If-Match succeeds and returns new ETag.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Update_With_Valid_IfMatch_Succeeds()
    {
        // Arrange
        var createDto = new CustomReportLayoutCreateDto { Name = "IfMatch Valid Report", LayoutJson = "{\"type\":\"bar\"}" };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/custom-reports", createDto);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<CustomReportLayoutDto>();

        var getResponse = await _client.GetAsync($"/api/v1/custom-reports/{created!.Id}");
        var etag = getResponse.Headers.ETag;

        var updateDto = new CustomReportLayoutUpdateDto { Name = "Updated With ETag" };
        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/custom-reports/{created.Id}")
        {
            Content = JsonContent.Create(updateDto),
        };
        request.Headers.IfMatch.Add(etag!);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(response.Headers.ETag);
        var updated = await response.Content.ReadFromJsonAsync<CustomReportLayoutDto>();
        Assert.Equal("Updated With ETag", updated!.Name);
    }

    /// <summary>
    /// PUT /api/v1/custom-reports/{id} with stale If-Match returns 409 Conflict.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Update_With_Stale_IfMatch_Returns_409()
    {
        // Arrange
        var createDto = new CustomReportLayoutCreateDto { Name = "Stale IfMatch Report", LayoutJson = "{\"type\":\"bar\"}" };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/custom-reports", createDto);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<CustomReportLayoutDto>();

        var staleETag = new EntityTagHeaderValue("\"99999999\"");
        var updateDto = new CustomReportLayoutUpdateDto { Name = "Should Fail" };
        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/custom-reports/{created!.Id}")
        {
            Content = JsonContent.Create(updateDto),
        };
        request.Headers.IfMatch.Add(staleETag);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    /// <summary>
    /// PUT /api/v1/custom-reports/{id} without If-Match still succeeds (backward compatible).
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Update_Without_IfMatch_Succeeds_BackwardCompatible()
    {
        // Arrange
        var createDto = new CustomReportLayoutCreateDto { Name = "No IfMatch Report", LayoutJson = "{\"type\":\"bar\"}" };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/custom-reports", createDto);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<CustomReportLayoutDto>();

        var updateDto = new CustomReportLayoutUpdateDto { Name = "Updated Without ETag" };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/v1/custom-reports/{created!.Id}", updateDto);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<CustomReportLayoutDto>();
        Assert.Equal("Updated Without ETag", updated!.Name);
    }
}
