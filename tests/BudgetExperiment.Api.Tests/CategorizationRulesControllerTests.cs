// <copyright file="CategorizationRulesControllerTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Net;
using System.Net.Http.Json;

using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Api.Tests;

/// <summary>
/// Integration tests for the CategorizationRules API endpoints.
/// </summary>
public sealed class CategorizationRulesControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="CategorizationRulesControllerTests"/> class.
    /// </summary>
    /// <param name="factory">The test factory.</param>
    public CategorizationRulesControllerTests(CustomWebApplicationFactory factory)
    {
        this._client = factory.CreateApiClient();
    }

    /// <summary>
    /// GET /api/v1/categorizationrules returns 200 OK with rule list.
    /// </summary>
    [Fact]
    public async Task GetAll_Returns_200_WithRuleList()
    {
        // Act
        var response = await this._client.GetAsync("/api/v1/categorizationrules");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var rules = await response.Content.ReadFromJsonAsync<List<CategorizationRuleDto>>();
        Assert.NotNull(rules);
    }

    /// <summary>
    /// POST /api/v1/categorizationrules creates a rule and returns 201 Created.
    /// </summary>
    [Fact]
    public async Task Create_Returns_201_WithCreatedRule()
    {
        // Arrange - create a category first
        var categoryDto = new BudgetCategoryCreateDto { Name = "Test Groceries Rule", Type = "Expense" };
        var categoryResponse = await this._client.PostAsJsonAsync("/api/v1/categories", categoryDto);
        var category = await categoryResponse.Content.ReadFromJsonAsync<BudgetCategoryDto>();

        var createDto = new CategorizationRuleCreateDto
        {
            Name = "Walmart Rule",
            Pattern = "WALMART",
            MatchType = "Contains",
            CaseSensitive = false,
            CategoryId = category!.Id,
        };

        // Act
        var response = await this._client.PostAsJsonAsync("/api/v1/categorizationrules", createDto);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<CategorizationRuleDto>();
        Assert.NotNull(created);
        Assert.Equal("Walmart Rule", created.Name);
        Assert.Equal("WALMART", created.Pattern);
        Assert.Equal("Contains", created.MatchType);
        Assert.False(created.CaseSensitive);
        Assert.Equal(category.Id, created.CategoryId);
        Assert.NotEqual(Guid.Empty, created.Id);
        Assert.True(created.IsActive);
    }

    /// <summary>
    /// GET /api/v1/categorizationrules/{id} returns 200 when found.
    /// </summary>
    [Fact]
    public async Task GetById_Returns_200_WhenFound()
    {
        // Arrange
        var categoryDto = new BudgetCategoryCreateDto { Name = "GetById Category", Type = "Expense" };
        var categoryResponse = await this._client.PostAsJsonAsync("/api/v1/categories", categoryDto);
        var category = await categoryResponse.Content.ReadFromJsonAsync<BudgetCategoryDto>();

        var createDto = new CategorizationRuleCreateDto
        {
            Name = "GetById Test Rule",
            Pattern = "TEST",
            MatchType = "Exact",
            CategoryId = category!.Id,
        };
        var createResponse = await this._client.PostAsJsonAsync("/api/v1/categorizationrules", createDto);
        var created = await createResponse.Content.ReadFromJsonAsync<CategorizationRuleDto>();

        // Act
        var response = await this._client.GetAsync($"/api/v1/categorizationrules/{created!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var rule = await response.Content.ReadFromJsonAsync<CategorizationRuleDto>();
        Assert.NotNull(rule);
        Assert.Equal(created.Id, rule.Id);
        Assert.Equal("GetById Test Rule", rule.Name);
    }

    /// <summary>
    /// GET /api/v1/categorizationrules/{id} returns 404 when not found.
    /// </summary>
    [Fact]
    public async Task GetById_Returns_404_WhenNotFound()
    {
        // Act
        var response = await this._client.GetAsync($"/api/v1/categorizationrules/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// PUT /api/v1/categorizationrules/{id} updates a rule and returns 200.
    /// </summary>
    [Fact]
    public async Task Update_Returns_200_WithUpdatedRule()
    {
        // Arrange
        var categoryDto = new BudgetCategoryCreateDto { Name = "Update Category", Type = "Expense" };
        var categoryResponse = await this._client.PostAsJsonAsync("/api/v1/categories", categoryDto);
        var category = await categoryResponse.Content.ReadFromJsonAsync<BudgetCategoryDto>();

        var createDto = new CategorizationRuleCreateDto
        {
            Name = "Original Rule",
            Pattern = "ORIGINAL",
            MatchType = "Contains",
            CategoryId = category!.Id,
        };
        var createResponse = await this._client.PostAsJsonAsync("/api/v1/categorizationrules", createDto);
        var created = await createResponse.Content.ReadFromJsonAsync<CategorizationRuleDto>();

        var updateDto = new CategorizationRuleUpdateDto
        {
            Name = "Updated Rule",
            Pattern = "UPDATED",
            MatchType = "StartsWith",
            CaseSensitive = true,
            CategoryId = category.Id,
        };

        // Act
        var response = await this._client.PutAsJsonAsync($"/api/v1/categorizationrules/{created!.Id}", updateDto);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<CategorizationRuleDto>();
        Assert.NotNull(updated);
        Assert.Equal("Updated Rule", updated.Name);
        Assert.Equal("UPDATED", updated.Pattern);
        Assert.Equal("StartsWith", updated.MatchType);
        Assert.True(updated.CaseSensitive);
    }

    /// <summary>
    /// DELETE /api/v1/categorizationrules/{id} deletes a rule and returns 204.
    /// </summary>
    [Fact]
    public async Task Delete_Returns_204_WhenDeleted()
    {
        // Arrange
        var categoryDto = new BudgetCategoryCreateDto { Name = "Delete Category", Type = "Expense" };
        var categoryResponse = await this._client.PostAsJsonAsync("/api/v1/categories", categoryDto);
        var category = await categoryResponse.Content.ReadFromJsonAsync<BudgetCategoryDto>();

        var createDto = new CategorizationRuleCreateDto
        {
            Name = "Delete Me Rule",
            Pattern = "DELETE",
            MatchType = "Contains",
            CategoryId = category!.Id,
        };
        var createResponse = await this._client.PostAsJsonAsync("/api/v1/categorizationrules", createDto);
        var created = await createResponse.Content.ReadFromJsonAsync<CategorizationRuleDto>();

        // Act
        var response = await this._client.DeleteAsync($"/api/v1/categorizationrules/{created!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify it's gone
        var getResponse = await this._client.GetAsync($"/api/v1/categorizationrules/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    /// <summary>
    /// POST /api/v1/categorizationrules/{id}/activate activates a rule.
    /// </summary>
    [Fact]
    public async Task Activate_Returns_204_WhenSuccessful()
    {
        // Arrange
        var categoryDto = new BudgetCategoryCreateDto { Name = "Activate Category", Type = "Expense" };
        var categoryResponse = await this._client.PostAsJsonAsync("/api/v1/categories", categoryDto);
        var category = await categoryResponse.Content.ReadFromJsonAsync<BudgetCategoryDto>();

        var createDto = new CategorizationRuleCreateDto
        {
            Name = "Activate Test Rule",
            Pattern = "ACTIVATE",
            MatchType = "Contains",
            CategoryId = category!.Id,
        };
        var createResponse = await this._client.PostAsJsonAsync("/api/v1/categorizationrules", createDto);
        var created = await createResponse.Content.ReadFromJsonAsync<CategorizationRuleDto>();

        // Deactivate first
        await this._client.PostAsync($"/api/v1/categorizationrules/{created!.Id}/deactivate", null);

        // Act
        var response = await this._client.PostAsync($"/api/v1/categorizationrules/{created.Id}/activate", null);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify it's active
        var getResponse = await this._client.GetAsync($"/api/v1/categorizationrules/{created.Id}");
        var rule = await getResponse.Content.ReadFromJsonAsync<CategorizationRuleDto>();
        Assert.True(rule!.IsActive);
    }

    /// <summary>
    /// POST /api/v1/categorizationrules/{id}/deactivate deactivates a rule.
    /// </summary>
    [Fact]
    public async Task Deactivate_Returns_204_WhenSuccessful()
    {
        // Arrange
        var categoryDto = new BudgetCategoryCreateDto { Name = "Deactivate Category", Type = "Expense" };
        var categoryResponse = await this._client.PostAsJsonAsync("/api/v1/categories", categoryDto);
        var category = await categoryResponse.Content.ReadFromJsonAsync<BudgetCategoryDto>();

        var createDto = new CategorizationRuleCreateDto
        {
            Name = "Deactivate Test Rule",
            Pattern = "DEACTIVATE",
            MatchType = "Contains",
            CategoryId = category!.Id,
        };
        var createResponse = await this._client.PostAsJsonAsync("/api/v1/categorizationrules", createDto);
        var created = await createResponse.Content.ReadFromJsonAsync<CategorizationRuleDto>();

        // Act
        var response = await this._client.PostAsync($"/api/v1/categorizationrules/{created!.Id}/deactivate", null);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify it's inactive
        var getResponse = await this._client.GetAsync($"/api/v1/categorizationrules/{created.Id}");
        var rule = await getResponse.Content.ReadFromJsonAsync<CategorizationRuleDto>();
        Assert.False(rule!.IsActive);
    }

    /// <summary>
    /// POST /api/v1/categorizationrules/test tests a pattern.
    /// </summary>
    [Fact]
    public async Task TestPattern_Returns_200_WithMatchingDescriptions()
    {
        // Arrange
        var request = new TestPatternRequest
        {
            Pattern = "TEST",
            MatchType = "Contains",
            CaseSensitive = false,
            Limit = 5,
        };

        // Act
        var response = await this._client.PostAsJsonAsync("/api/v1/categorizationrules/test", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<TestPatternResponse>();
        Assert.NotNull(result);
        Assert.NotNull(result.MatchingDescriptions);
    }

    /// <summary>
    /// POST /api/v1/categorizationrules/apply applies rules to uncategorized transactions.
    /// </summary>
    [Fact]
    public async Task ApplyRules_Returns_200_WithResult()
    {
        // Arrange
        var request = new ApplyRulesRequest
        {
            TransactionIds = null,
            OverwriteExisting = false,
        };

        // Act
        var response = await this._client.PostAsJsonAsync("/api/v1/categorizationrules/apply", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ApplyRulesResponse>();
        Assert.NotNull(result);
    }

    /// <summary>
    /// PUT /api/v1/categorizationrules/reorder reorders rule priorities.
    /// </summary>
    [Fact]
    public async Task Reorder_Returns_204_WhenSuccessful()
    {
        // Arrange - create two rules
        var categoryDto = new BudgetCategoryCreateDto { Name = "Reorder Category", Type = "Expense" };
        var categoryResponse = await this._client.PostAsJsonAsync("/api/v1/categories", categoryDto);
        var category = await categoryResponse.Content.ReadFromJsonAsync<BudgetCategoryDto>();

        var createDto1 = new CategorizationRuleCreateDto
        {
            Name = "Reorder Rule 1",
            Pattern = "REORDER1",
            MatchType = "Contains",
            CategoryId = category!.Id,
        };
        var response1 = await this._client.PostAsJsonAsync("/api/v1/categorizationrules", createDto1);
        var rule1 = await response1.Content.ReadFromJsonAsync<CategorizationRuleDto>();

        var createDto2 = new CategorizationRuleCreateDto
        {
            Name = "Reorder Rule 2",
            Pattern = "REORDER2",
            MatchType = "Contains",
            CategoryId = category.Id,
        };
        var response2 = await this._client.PostAsJsonAsync("/api/v1/categorizationrules", createDto2);
        var rule2 = await response2.Content.ReadFromJsonAsync<CategorizationRuleDto>();

        var request = new ReorderRulesRequest
        {
            RuleIds = new[] { rule2!.Id, rule1!.Id }, // Swap order
        };

        // Act
        var response = await this._client.PutAsJsonAsync("/api/v1/categorizationrules/reorder", request);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify priorities changed
        var getRule1 = await this._client.GetAsync($"/api/v1/categorizationrules/{rule1.Id}");
        var updatedRule1 = await getRule1.Content.ReadFromJsonAsync<CategorizationRuleDto>();
        var getRule2 = await this._client.GetAsync($"/api/v1/categorizationrules/{rule2.Id}");
        var updatedRule2 = await getRule2.Content.ReadFromJsonAsync<CategorizationRuleDto>();

        Assert.True(updatedRule2!.Priority < updatedRule1!.Priority, "Rule 2 should now have higher priority (lower number)");
    }

    /// <summary>
    /// GET /api/v1/categorizationrules?activeOnly=true returns only active rules.
    /// </summary>
    [Fact]
    public async Task GetAll_WithActiveOnly_Returns_200_WithActiveRules()
    {
        // Arrange
        var categoryDto = new BudgetCategoryCreateDto { Name = "ActiveOnly Category", Type = "Expense" };
        var categoryResponse = await this._client.PostAsJsonAsync("/api/v1/categories", categoryDto);
        var category = await categoryResponse.Content.ReadFromJsonAsync<BudgetCategoryDto>();

        var activeRuleDto = new CategorizationRuleCreateDto
        {
            Name = "Active Rule",
            Pattern = "ACTIVE",
            MatchType = "Contains",
            CategoryId = category!.Id,
        };
        var activeResponse = await this._client.PostAsJsonAsync("/api/v1/categorizationrules", activeRuleDto);
        var activeRule = await activeResponse.Content.ReadFromJsonAsync<CategorizationRuleDto>();

        var inactiveRuleDto = new CategorizationRuleCreateDto
        {
            Name = "Inactive Rule",
            Pattern = "INACTIVE",
            MatchType = "Contains",
            CategoryId = category.Id,
        };
        var inactiveResponse = await this._client.PostAsJsonAsync("/api/v1/categorizationrules", inactiveRuleDto);
        var inactiveRule = await inactiveResponse.Content.ReadFromJsonAsync<CategorizationRuleDto>();

        // Deactivate the second rule
        await this._client.PostAsync($"/api/v1/categorizationrules/{inactiveRule!.Id}/deactivate", null);

        // Act
        var response = await this._client.GetAsync("/api/v1/categorizationrules?activeOnly=true");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var rules = await response.Content.ReadFromJsonAsync<List<CategorizationRuleDto>>();
        Assert.NotNull(rules);
        Assert.Contains(rules, r => r.Id == activeRule!.Id);
        Assert.DoesNotContain(rules, r => r.Id == inactiveRule!.Id);
    }
}
