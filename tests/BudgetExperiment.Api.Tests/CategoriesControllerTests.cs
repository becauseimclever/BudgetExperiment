// <copyright file="CategoriesControllerTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Net;
using System.Net.Http.Json;

using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Api.Tests;

/// <summary>
/// Integration tests for the Categories API endpoints.
/// </summary>
public sealed class CategoriesControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="CategoriesControllerTests"/> class.
    /// </summary>
    /// <param name="factory">The test factory.</param>
    public CategoriesControllerTests(CustomWebApplicationFactory factory)
    {
        this._client = factory.CreateApiClient();
    }

    /// <summary>
    /// GET /api/v1/categories returns 200 OK with empty list when no categories exist.
    /// </summary>
    [Fact]
    public async Task GetAll_Returns_200_WithCategoryList()
    {
        // Act
        var response = await this._client.GetAsync("/api/v1/categories");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var categories = await response.Content.ReadFromJsonAsync<List<BudgetCategoryDto>>();
        Assert.NotNull(categories);
    }

    /// <summary>
    /// GET /api/v1/categories?activeOnly=true returns only active categories.
    /// </summary>
    [Fact]
    public async Task GetAll_WithActiveOnly_Returns_200_WithActiveCategories()
    {
        // Arrange - create active and inactive categories
        var activeDto = new BudgetCategoryCreateDto { Name = "Active Category", Type = "Expense" };
        var createResponse = await this._client.PostAsJsonAsync("/api/v1/categories", activeDto);
        var activeCategory = await createResponse.Content.ReadFromJsonAsync<BudgetCategoryDto>();

        var inactiveDto = new BudgetCategoryCreateDto { Name = "Inactive Category", Type = "Expense" };
        var inactiveResponse = await this._client.PostAsJsonAsync("/api/v1/categories", inactiveDto);
        var inactiveCategory = await inactiveResponse.Content.ReadFromJsonAsync<BudgetCategoryDto>();

        // Deactivate the second category
        await this._client.PostAsync($"/api/v1/categories/{inactiveCategory!.Id}/deactivate", null);

        // Act
        var response = await this._client.GetAsync("/api/v1/categories?activeOnly=true");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var categories = await response.Content.ReadFromJsonAsync<List<BudgetCategoryDto>>();
        Assert.NotNull(categories);
        Assert.Contains(categories, c => c.Id == activeCategory!.Id);
        Assert.DoesNotContain(categories, c => c.Id == inactiveCategory!.Id);
    }

    /// <summary>
    /// POST /api/v1/categories creates a category and returns 201 Created.
    /// </summary>
    [Fact]
    public async Task Create_Returns_201_WithCreatedCategory()
    {
        // Arrange
        var createDto = new BudgetCategoryCreateDto
        {
            Name = "Test Groceries",
            Type = "Expense",
            Icon = "cart",
            Color = "#4CAF50",
        };

        // Act
        var response = await this._client.PostAsJsonAsync("/api/v1/categories", createDto);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<BudgetCategoryDto>();
        Assert.NotNull(created);
        Assert.Equal("Test Groceries", created.Name);
        Assert.Equal("Expense", created.Type);
        Assert.Equal("cart", created.Icon);
        Assert.Equal("#4CAF50", created.Color);
        Assert.NotEqual(Guid.Empty, created.Id);
        Assert.True(created.IsActive);
    }

    /// <summary>
    /// GET /api/v1/categories/{id} returns 200 with category when found.
    /// </summary>
    [Fact]
    public async Task GetById_Returns_200_WhenFound()
    {
        // Arrange - create a category first
        var createDto = new BudgetCategoryCreateDto { Name = "Get Test Category", Type = "Income" };
        var createResponse = await this._client.PostAsJsonAsync("/api/v1/categories", createDto);
        var created = await createResponse.Content.ReadFromJsonAsync<BudgetCategoryDto>();

        // Act
        var response = await this._client.GetAsync($"/api/v1/categories/{created!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var category = await response.Content.ReadFromJsonAsync<BudgetCategoryDto>();
        Assert.NotNull(category);
        Assert.Equal(created.Id, category.Id);
        Assert.Equal("Get Test Category", category.Name);
    }

    /// <summary>
    /// GET /api/v1/categories/{id} returns 404 for non-existent category.
    /// </summary>
    [Fact]
    public async Task GetById_Returns_404_WhenNotFound()
    {
        // Act
        var response = await this._client.GetAsync($"/api/v1/categories/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// PUT /api/v1/categories/{id} updates category and returns 200.
    /// </summary>
    [Fact]
    public async Task Update_Returns_200_WithUpdatedCategory()
    {
        // Arrange - create category first
        var createDto = new BudgetCategoryCreateDto { Name = "Original Name", Type = "Expense" };
        var createResponse = await this._client.PostAsJsonAsync("/api/v1/categories", createDto);
        var created = await createResponse.Content.ReadFromJsonAsync<BudgetCategoryDto>();

        var updateDto = new BudgetCategoryUpdateDto
        {
            Name = "Updated Name",
            Icon = "star",
            Color = "#FF0000",
            SortOrder = 5,
        };

        // Act
        var response = await this._client.PutAsJsonAsync($"/api/v1/categories/{created!.Id}", updateDto);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<BudgetCategoryDto>();
        Assert.NotNull(updated);
        Assert.Equal("Updated Name", updated.Name);
        Assert.Equal("star", updated.Icon);
        Assert.Equal("#FF0000", updated.Color);
        Assert.Equal(5, updated.SortOrder);
    }

    /// <summary>
    /// PUT /api/v1/categories/{id} returns 404 for non-existent category.
    /// </summary>
    [Fact]
    public async Task Update_Returns_404_WhenNotFound()
    {
        // Arrange
        var updateDto = new BudgetCategoryUpdateDto { Name = "New Name" };

        // Act
        var response = await this._client.PutAsJsonAsync($"/api/v1/categories/{Guid.NewGuid()}", updateDto);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// DELETE /api/v1/categories/{id} deletes category and returns 204.
    /// </summary>
    [Fact]
    public async Task Delete_Returns_204_WhenDeleted()
    {
        // Arrange - create category first
        var createDto = new BudgetCategoryCreateDto { Name = "Delete Test Category", Type = "Expense" };
        var createResponse = await this._client.PostAsJsonAsync("/api/v1/categories", createDto);
        var created = await createResponse.Content.ReadFromJsonAsync<BudgetCategoryDto>();

        // Act
        var response = await this._client.DeleteAsync($"/api/v1/categories/{created!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify it's gone
        var getResponse = await this._client.GetAsync($"/api/v1/categories/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    /// <summary>
    /// DELETE /api/v1/categories/{id} returns 404 for non-existent category.
    /// </summary>
    [Fact]
    public async Task Delete_Returns_404_WhenNotFound()
    {
        // Act
        var response = await this._client.DeleteAsync($"/api/v1/categories/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// POST /api/v1/categories/{id}/activate activates a deactivated category.
    /// </summary>
    [Fact]
    public async Task Activate_Returns_204_WhenActivated()
    {
        // Arrange - create and deactivate a category
        var createDto = new BudgetCategoryCreateDto { Name = "Activate Test Category", Type = "Expense" };
        var createResponse = await this._client.PostAsJsonAsync("/api/v1/categories", createDto);
        var created = await createResponse.Content.ReadFromJsonAsync<BudgetCategoryDto>();

        await this._client.PostAsync($"/api/v1/categories/{created!.Id}/deactivate", null);

        // Verify it's inactive
        var inactiveResponse = await this._client.GetAsync($"/api/v1/categories/{created.Id}");
        var inactiveCategory = await inactiveResponse.Content.ReadFromJsonAsync<BudgetCategoryDto>();
        Assert.False(inactiveCategory!.IsActive);

        // Act
        var response = await this._client.PostAsync($"/api/v1/categories/{created.Id}/activate", null);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify it's active again
        var activeResponse = await this._client.GetAsync($"/api/v1/categories/{created.Id}");
        var activeCategory = await activeResponse.Content.ReadFromJsonAsync<BudgetCategoryDto>();
        Assert.True(activeCategory!.IsActive);
    }

    /// <summary>
    /// POST /api/v1/categories/{id}/activate returns 404 for non-existent category.
    /// </summary>
    [Fact]
    public async Task Activate_Returns_404_WhenNotFound()
    {
        // Act
        var response = await this._client.PostAsync($"/api/v1/categories/{Guid.NewGuid()}/activate", null);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// POST /api/v1/categories/{id}/deactivate deactivates a category.
    /// </summary>
    [Fact]
    public async Task Deactivate_Returns_204_WhenDeactivated()
    {
        // Arrange - create a category
        var createDto = new BudgetCategoryCreateDto { Name = "Deactivate Test Category", Type = "Expense" };
        var createResponse = await this._client.PostAsJsonAsync("/api/v1/categories", createDto);
        var created = await createResponse.Content.ReadFromJsonAsync<BudgetCategoryDto>();

        // Act
        var response = await this._client.PostAsync($"/api/v1/categories/{created!.Id}/deactivate", null);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify it's inactive
        var getResponse = await this._client.GetAsync($"/api/v1/categories/{created.Id}");
        var category = await getResponse.Content.ReadFromJsonAsync<BudgetCategoryDto>();
        Assert.False(category!.IsActive);
    }

    /// <summary>
    /// POST /api/v1/categories/{id}/deactivate returns 404 for non-existent category.
    /// </summary>
    [Fact]
    public async Task Deactivate_Returns_404_WhenNotFound()
    {
        // Act
        var response = await this._client.PostAsync($"/api/v1/categories/{Guid.NewGuid()}/deactivate", null);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// POST /api/v1/categories creates different category types.
    /// </summary>
    [Theory]
    [InlineData("Expense")]
    [InlineData("Income")]
    [InlineData("Transfer")]
    public async Task Create_WithDifferentTypes_Returns_201(string categoryType)
    {
        // Arrange
        var createDto = new BudgetCategoryCreateDto
        {
            Name = $"Test {categoryType} Category",
            Type = categoryType,
        };

        // Act
        var response = await this._client.PostAsJsonAsync("/api/v1/categories", createDto);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<BudgetCategoryDto>();
        Assert.NotNull(created);
        Assert.Equal(categoryType, created.Type);
    }
}
