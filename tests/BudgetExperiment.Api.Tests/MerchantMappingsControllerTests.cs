// <copyright file="MerchantMappingsControllerTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Net;
using System.Net.Http.Json;

using BudgetExperiment.Api.Models;
using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Api.Tests;

/// <summary>
/// Integration tests for the MerchantMappings API endpoints.
/// </summary>
[Collection("ApiDb")]
public sealed class MerchantMappingsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="MerchantMappingsControllerTests"/> class.
    /// </summary>
    /// <param name="factory">The test factory.</param>
    public MerchantMappingsControllerTests(CustomWebApplicationFactory factory)
    {
        this._client = factory.CreateApiClient();
    }

    /// <summary>
    /// GET /api/v1/merchantmappings returns 200 OK.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetLearned_Returns_200()
    {
        // Act
        var response = await this._client.GetAsync("/api/v1/merchantmappings");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var mappings = await response.Content.ReadFromJsonAsync<List<LearnedMerchantMappingDto>>();
        Assert.NotNull(mappings);
    }

    /// <summary>
    /// POST learn with valid data returns 204 and the mapping appears in GET.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Learn_WithValidData_Returns_204_AndMappingAppearsInGet()
    {
        // Arrange - create a category to map to
        var category = await CreateCategoryAsync("Learn Test Category");

        var request = new LearnMerchantMappingRequest
        {
            Description = "WALMART STORE #1234",
            CategoryId = category.Id,
        };

        // Act
        var response = await this._client.PostAsJsonAsync("/api/v1/merchantmappings/learn", request);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify the mapping was persisted
        var getResponse = await this._client.GetAsync("/api/v1/merchantmappings");
        var mappings = await getResponse.Content.ReadFromJsonAsync<List<LearnedMerchantMappingDto>>();
        Assert.NotNull(mappings);
        Assert.Contains(mappings, m => m.CategoryId == category.Id);
    }

    /// <summary>
    /// POST learn with empty description returns 400.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Learn_WithEmptyDescription_Returns_400()
    {
        // Arrange
        var request = new LearnMerchantMappingRequest
        {
            Description = string.Empty,
            CategoryId = Guid.NewGuid(),
        };

        // Act
        var response = await this._client.PostAsJsonAsync("/api/v1/merchantmappings/learn", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// POST learn with whitespace-only description returns 400.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Learn_WithWhitespaceDescription_Returns_400()
    {
        // Arrange
        var request = new LearnMerchantMappingRequest
        {
            Description = "   ",
            CategoryId = Guid.NewGuid(),
        };

        // Act
        var response = await this._client.PostAsJsonAsync("/api/v1/merchantmappings/learn", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// DELETE with non-existent mapping returns 404.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Delete_NonExistent_Returns_404()
    {
        // Act
        var response = await this._client.DeleteAsync($"/api/v1/merchantmappings/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// DELETE with existing mapping returns 204 and mapping is removed from GET.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Delete_Existing_Returns_204_AndMappingIsRemoved()
    {
        // Arrange - create a category and learn a mapping
        var category = await CreateCategoryAsync("Delete Test Category");
        await this._client.PostAsJsonAsync(
            "/api/v1/merchantmappings/learn",
            new LearnMerchantMappingRequest
            {
                Description = "TARGET STORE DELETE TEST",
                CategoryId = category.Id,
            });

        var getResponse = await this._client.GetAsync("/api/v1/merchantmappings");
        var mappings = await getResponse.Content.ReadFromJsonAsync<List<LearnedMerchantMappingDto>>();
        var mapping = Assert.Single(mappings!, m => m.CategoryId == category.Id);

        // Act
        var deleteResponse = await this._client.DeleteAsync($"/api/v1/merchantmappings/{mapping.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // Verify the mapping is gone
        var verifyResponse = await this._client.GetAsync("/api/v1/merchantmappings");
        var remaining = await verifyResponse.Content.ReadFromJsonAsync<List<LearnedMerchantMappingDto>>();
        Assert.DoesNotContain(remaining!, m => m.Id == mapping.Id);
    }

    /// <summary>
    /// GET returns correct DTO shape after learning a mapping.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetLearned_ReturnsMappedDtoFields()
    {
        // Arrange
        var category = await CreateCategoryAsync("DTO Shape Category");
        await this._client.PostAsJsonAsync(
            "/api/v1/merchantmappings/learn",
            new LearnMerchantMappingRequest
            {
                Description = "COSTCO WHOLESALE DTO TEST",
                CategoryId = category.Id,
            });

        // Act
        var response = await this._client.GetAsync("/api/v1/merchantmappings");
        var mappings = await response.Content.ReadFromJsonAsync<List<LearnedMerchantMappingDto>>();
        var mapping = Assert.Single(mappings!, m => m.CategoryId == category.Id);

        // Assert - verify DTO mapping
        Assert.NotEqual(Guid.Empty, mapping.Id);
        Assert.False(string.IsNullOrWhiteSpace(mapping.MerchantPattern));
        Assert.Equal(category.Id, mapping.CategoryId);
        Assert.Equal(category.Name, mapping.CategoryName);
        Assert.True(mapping.LearnCount >= 1);
        Assert.True(mapping.CreatedAtUtc > DateTime.MinValue);
        Assert.True(mapping.UpdatedAtUtc > DateTime.MinValue);
    }

    private async Task<BudgetCategoryDto> CreateCategoryAsync(string name)
    {
        var createDto = new BudgetCategoryCreateDto { Name = name, Type = "Expense" };
        var response = await this._client.PostAsJsonAsync("/api/v1/categories", createDto);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<BudgetCategoryDto>())!;
    }
}
