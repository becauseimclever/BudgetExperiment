// <copyright file="BudgetsControllerTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Net;
using System.Net.Http.Json;

using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Api.Tests;

/// <summary>
/// Integration tests for the Budgets API endpoints.
/// </summary>
public sealed class BudgetsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="BudgetsControllerTests"/> class.
    /// </summary>
    /// <param name="factory">The test factory.</param>
    public BudgetsControllerTests(CustomWebApplicationFactory factory)
    {
        this._client = factory.CreateApiClient();
    }

    /// <summary>
    /// GET /api/v1/budgets returns 200 OK with empty list when no goals exist.
    /// </summary>
    [Fact]
    public async Task GetGoalsByMonth_Returns_200_WithGoalList()
    {
        // Act
        var response = await this._client.GetAsync("/api/v1/budgets?year=2026&month=1");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var goals = await response.Content.ReadFromJsonAsync<List<BudgetGoalDto>>();
        Assert.NotNull(goals);
    }

    /// <summary>
    /// GET /api/v1/budgets returns 400 for invalid month.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    [InlineData(-1)]
    public async Task GetGoalsByMonth_Returns_400_ForInvalidMonth(int month)
    {
        // Act
        var response = await this._client.GetAsync($"/api/v1/budgets?year=2026&month={month}");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// PUT /api/v1/budgets/{categoryId} creates a new budget goal.
    /// </summary>
    [Fact]
    public async Task SetGoal_Creates_NewGoal_Returns_200()
    {
        // Arrange - create a category first
        var categoryDto = new BudgetCategoryCreateDto { Name = "Budget Test Category", Type = "Expense" };
        var categoryResponse = await this._client.PostAsJsonAsync("/api/v1/categories", categoryDto);
        var category = await categoryResponse.Content.ReadFromJsonAsync<BudgetCategoryDto>();

        var goalDto = new BudgetGoalSetDto
        {
            Year = 2026,
            Month = 1,
            TargetAmount = new MoneyDto { Amount = 500.00m, Currency = "USD" },
        };

        // Act
        var response = await this._client.PutAsJsonAsync($"/api/v1/budgets/{category!.Id}", goalDto);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var goal = await response.Content.ReadFromJsonAsync<BudgetGoalDto>();
        Assert.NotNull(goal);
        Assert.Equal(category.Id, goal.CategoryId);
        Assert.Equal(2026, goal.Year);
        Assert.Equal(1, goal.Month);
        Assert.Equal(500.00m, goal.TargetAmount.Amount);
    }

    /// <summary>
    /// PUT /api/v1/budgets/{categoryId} updates an existing budget goal.
    /// </summary>
    [Fact]
    public async Task SetGoal_Updates_ExistingGoal_Returns_200()
    {
        // Arrange - create a category and initial goal
        var categoryDto = new BudgetCategoryCreateDto { Name = "Update Goal Category", Type = "Expense" };
        var categoryResponse = await this._client.PostAsJsonAsync("/api/v1/categories", categoryDto);
        var category = await categoryResponse.Content.ReadFromJsonAsync<BudgetCategoryDto>();

        var initialGoalDto = new BudgetGoalSetDto
        {
            Year = 2026,
            Month = 2,
            TargetAmount = new MoneyDto { Amount = 300.00m, Currency = "USD" },
        };
        await this._client.PutAsJsonAsync($"/api/v1/budgets/{category!.Id}", initialGoalDto);

        var updatedGoalDto = new BudgetGoalSetDto
        {
            Year = 2026,
            Month = 2,
            TargetAmount = new MoneyDto { Amount = 450.00m, Currency = "USD" },
        };

        // Act
        var response = await this._client.PutAsJsonAsync($"/api/v1/budgets/{category.Id}", updatedGoalDto);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var goal = await response.Content.ReadFromJsonAsync<BudgetGoalDto>();
        Assert.NotNull(goal);
        Assert.Equal(450.00m, goal.TargetAmount.Amount);
    }

    /// <summary>
    /// PUT /api/v1/budgets/{categoryId} returns 404 for non-existent category.
    /// </summary>
    [Fact]
    public async Task SetGoal_Returns_404_WhenCategoryNotFound()
    {
        // Arrange
        var goalDto = new BudgetGoalSetDto
        {
            Year = 2026,
            Month = 1,
            TargetAmount = new MoneyDto { Amount = 500.00m, Currency = "USD" },
        };

        // Act
        var response = await this._client.PutAsJsonAsync($"/api/v1/budgets/{Guid.NewGuid()}", goalDto);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// PUT /api/v1/budgets/{categoryId} returns 400 for invalid month.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public async Task SetGoal_Returns_400_ForInvalidMonth(int month)
    {
        // Arrange
        var goalDto = new BudgetGoalSetDto
        {
            Year = 2026,
            Month = month,
            TargetAmount = new MoneyDto { Amount = 500.00m, Currency = "USD" },
        };

        // Act
        var response = await this._client.PutAsJsonAsync($"/api/v1/budgets/{Guid.NewGuid()}", goalDto);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// DELETE /api/v1/budgets/{categoryId} deletes a budget goal.
    /// </summary>
    [Fact]
    public async Task DeleteGoal_Returns_204_WhenDeleted()
    {
        // Arrange - create category and goal
        var categoryDto = new BudgetCategoryCreateDto { Name = "Delete Goal Category", Type = "Expense" };
        var categoryResponse = await this._client.PostAsJsonAsync("/api/v1/categories", categoryDto);
        var category = await categoryResponse.Content.ReadFromJsonAsync<BudgetCategoryDto>();

        var goalDto = new BudgetGoalSetDto
        {
            Year = 2026,
            Month = 3,
            TargetAmount = new MoneyDto { Amount = 200.00m, Currency = "USD" },
        };
        await this._client.PutAsJsonAsync($"/api/v1/budgets/{category!.Id}", goalDto);

        // Act
        var response = await this._client.DeleteAsync($"/api/v1/budgets/{category.Id}?year=2026&month=3");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    /// <summary>
    /// DELETE /api/v1/budgets/{categoryId} returns 404 when goal not found.
    /// </summary>
    [Fact]
    public async Task DeleteGoal_Returns_404_WhenNotFound()
    {
        // Act
        var response = await this._client.DeleteAsync($"/api/v1/budgets/{Guid.NewGuid()}?year=2026&month=1");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// DELETE /api/v1/budgets/{categoryId} returns 400 for invalid month.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public async Task DeleteGoal_Returns_400_ForInvalidMonth(int month)
    {
        // Act
        var response = await this._client.DeleteAsync($"/api/v1/budgets/{Guid.NewGuid()}?year=2026&month={month}");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// GET /api/v1/budgets/category/{categoryId} returns goals for a category.
    /// </summary>
    [Fact]
    public async Task GetGoalsByCategory_Returns_200_WithGoalList()
    {
        // Arrange - create category and multiple goals
        var categoryDto = new BudgetCategoryCreateDto { Name = "Goals List Category", Type = "Expense" };
        var categoryResponse = await this._client.PostAsJsonAsync("/api/v1/categories", categoryDto);
        var category = await categoryResponse.Content.ReadFromJsonAsync<BudgetCategoryDto>();

        var goal1 = new BudgetGoalSetDto { Year = 2026, Month = 4, TargetAmount = new MoneyDto { Amount = 100.00m, Currency = "USD" } };
        var goal2 = new BudgetGoalSetDto { Year = 2026, Month = 5, TargetAmount = new MoneyDto { Amount = 150.00m, Currency = "USD" } };

        await this._client.PutAsJsonAsync($"/api/v1/budgets/{category!.Id}", goal1);
        await this._client.PutAsJsonAsync($"/api/v1/budgets/{category.Id}", goal2);

        // Act
        var response = await this._client.GetAsync($"/api/v1/budgets/category/{category.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var goals = await response.Content.ReadFromJsonAsync<List<BudgetGoalDto>>();
        Assert.NotNull(goals);
        Assert.True(goals.Count >= 2);
    }

    /// <summary>
    /// GET /api/v1/budgets/progress returns 200 with summary.
    /// </summary>
    [Fact]
    public async Task GetProgress_Returns_200_WithSummary()
    {
        // Act
        var response = await this._client.GetAsync("/api/v1/budgets/progress?year=2026&month=1");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var summary = await response.Content.ReadFromJsonAsync<BudgetSummaryDto>();
        Assert.NotNull(summary);
        Assert.Equal(2026, summary.Year);
        Assert.Equal(1, summary.Month);
    }

    /// <summary>
    /// GET /api/v1/budgets/progress returns 400 for invalid month.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public async Task GetProgress_Returns_400_ForInvalidMonth(int month)
    {
        // Act
        var response = await this._client.GetAsync($"/api/v1/budgets/progress?year=2026&month={month}");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// GET /api/v1/budgets/progress/{categoryId} returns 200 with category progress.
    /// </summary>
    [Fact]
    public async Task GetCategoryProgress_Returns_200_WithProgress()
    {
        // Arrange - create category with goal
        var categoryDto = new BudgetCategoryCreateDto { Name = "Progress Category", Type = "Expense" };
        var categoryResponse = await this._client.PostAsJsonAsync("/api/v1/categories", categoryDto);
        var category = await categoryResponse.Content.ReadFromJsonAsync<BudgetCategoryDto>();

        var goalDto = new BudgetGoalSetDto
        {
            Year = 2026,
            Month = 6,
            TargetAmount = new MoneyDto { Amount = 500.00m, Currency = "USD" },
        };
        await this._client.PutAsJsonAsync($"/api/v1/budgets/{category!.Id}", goalDto);

        // Act
        var response = await this._client.GetAsync($"/api/v1/budgets/progress/{category.Id}?year=2026&month=6");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var progress = await response.Content.ReadFromJsonAsync<BudgetProgressDto>();
        Assert.NotNull(progress);
        Assert.Equal(category.Id, progress.CategoryId);
        Assert.Equal(500.00m, progress.TargetAmount.Amount);
    }

    /// <summary>
    /// GET /api/v1/budgets/progress/{categoryId} returns 404 when no goal exists.
    /// </summary>
    [Fact]
    public async Task GetCategoryProgress_Returns_404_WhenNoGoalExists()
    {
        // Act
        var response = await this._client.GetAsync($"/api/v1/budgets/progress/{Guid.NewGuid()}?year=2026&month=1");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// GET /api/v1/budgets/progress/{categoryId} returns 400 for invalid month.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public async Task GetCategoryProgress_Returns_400_ForInvalidMonth(int month)
    {
        // Act
        var response = await this._client.GetAsync($"/api/v1/budgets/progress/{Guid.NewGuid()}?year=2026&month={month}");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// GET /api/v1/budgets/summary returns 200 with summary.
    /// </summary>
    [Fact]
    public async Task GetSummary_Returns_200_WithSummary()
    {
        // Act
        var response = await this._client.GetAsync("/api/v1/budgets/summary?year=2026&month=1");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var summary = await response.Content.ReadFromJsonAsync<BudgetSummaryDto>();
        Assert.NotNull(summary);
        Assert.Equal(2026, summary.Year);
        Assert.Equal(1, summary.Month);
    }

    /// <summary>
    /// GET /api/v1/budgets/summary returns 400 for invalid month.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public async Task GetSummary_Returns_400_ForInvalidMonth(int month)
    {
        // Act
        var response = await this._client.GetAsync($"/api/v1/budgets/summary?year=2026&month={month}");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Budget progress includes correct status based on spending.
    /// </summary>
    [Fact]
    public async Task GetProgress_IncludesCorrectStatus()
    {
        // Arrange - create category with goal (no spending)
        var categoryDto = new BudgetCategoryCreateDto { Name = "Status Test Category", Type = "Expense" };
        var categoryResponse = await this._client.PostAsJsonAsync("/api/v1/categories", categoryDto);
        var category = await categoryResponse.Content.ReadFromJsonAsync<BudgetCategoryDto>();

        var goalDto = new BudgetGoalSetDto
        {
            Year = 2026,
            Month = 7,
            TargetAmount = new MoneyDto { Amount = 100.00m, Currency = "USD" },
        };
        await this._client.PutAsJsonAsync($"/api/v1/budgets/{category!.Id}", goalDto);

        // Act
        var response = await this._client.GetAsync($"/api/v1/budgets/progress/{category.Id}?year=2026&month=7");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var progress = await response.Content.ReadFromJsonAsync<BudgetProgressDto>();
        Assert.NotNull(progress);

        // With no spending, should be OnTrack
        Assert.Equal("OnTrack", progress.Status);
        Assert.Equal(0m, progress.SpentAmount.Amount);
        Assert.Equal(100.00m, progress.RemainingAmount.Amount);
        Assert.Equal(0m, progress.PercentUsed);
    }
}
