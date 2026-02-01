// <copyright file="TransactionsControllerTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Net;
using System.Net.Http.Json;

using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Api.Tests;

/// <summary>
/// Integration tests for the Transactions API endpoints.
/// </summary>
public sealed class TransactionsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="TransactionsControllerTests"/> class.
    /// </summary>
    /// <param name="factory">The test factory.</param>
    public TransactionsControllerTests(CustomWebApplicationFactory factory)
    {
        this._client = factory.CreateApiClient();
    }

    /// <summary>
    /// GET /api/v1/transactions with valid date range returns 200 OK.
    /// </summary>
    [Fact]
    public async Task GetByDateRange_Returns_200_WithValidDateRange()
    {
        // Act
        var response = await this._client.GetAsync("/api/v1/transactions?startDate=2026-01-01&endDate=2026-01-31");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var transactions = await response.Content.ReadFromJsonAsync<List<TransactionDto>>();
        Assert.NotNull(transactions);

        // The list may contain seed data or be empty - just ensure it's a valid response
    }

    /// <summary>
    /// GET /api/v1/transactions returns 400 when startDate is after endDate.
    /// </summary>
    [Fact]
    public async Task GetByDateRange_Returns_400_WhenStartDateAfterEndDate()
    {
        // Act
        var response = await this._client.GetAsync("/api/v1/transactions?startDate=2026-01-31&endDate=2026-01-01");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// GET /api/v1/transactions/{id} returns 404 for non-existent transaction.
    /// </summary>
    [Fact]
    public async Task GetById_Returns_404_WhenNotFound()
    {
        // Act
        var response = await this._client.GetAsync($"/api/v1/transactions/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #region Uncategorized Transactions Tests

    /// <summary>
    /// GET /api/v1/transactions/uncategorized returns 200 OK with paged response.
    /// </summary>
    [Fact]
    public async Task GetUncategorized_Returns_200_WithPagedResponse()
    {
        // Act
        var response = await this._client.GetAsync("/api/v1/transactions/uncategorized");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<UncategorizedTransactionPageDto>();
        Assert.NotNull(result);
        Assert.True(result.TotalCount >= 0);
        Assert.Equal(1, result.Page);
        Assert.Equal(50, result.PageSize);
    }

    /// <summary>
    /// GET /api/v1/transactions/uncategorized returns pagination header.
    /// </summary>
    [Fact]
    public async Task GetUncategorized_Returns_PaginationHeader()
    {
        // Act
        var response = await this._client.GetAsync("/api/v1/transactions/uncategorized");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.Contains("X-Pagination-TotalCount"));
        var headerValue = response.Headers.GetValues("X-Pagination-TotalCount").FirstOrDefault();
        Assert.NotNull(headerValue);
        Assert.True(int.TryParse(headerValue, out _));
    }

    /// <summary>
    /// GET /api/v1/transactions/uncategorized accepts filter parameters.
    /// </summary>
    [Fact]
    public async Task GetUncategorized_AcceptsFilterParameters()
    {
        // Act
        var response = await this._client.GetAsync(
            "/api/v1/transactions/uncategorized?" +
            "startDate=2026-01-01&endDate=2026-01-31&minAmount=10&maxAmount=100&" +
            "descriptionContains=test&sortBy=Amount&sortDescending=false&page=2&pageSize=25");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<UncategorizedTransactionPageDto>();
        Assert.NotNull(result);
        Assert.Equal(2, result.Page);
        Assert.Equal(25, result.PageSize);
    }

    /// <summary>
    /// POST /api/v1/transactions/bulk-categorize returns 400 when CategoryId is empty.
    /// </summary>
    [Fact]
    public async Task BulkCategorize_Returns_400_WhenCategoryIdEmpty()
    {
        // Arrange
        var request = new BulkCategorizeRequest
        {
            TransactionIds = [Guid.NewGuid()],
            CategoryId = Guid.Empty,
        };

        // Act
        var response = await this._client.PostAsJsonAsync("/api/v1/transactions/bulk-categorize", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// POST /api/v1/transactions/bulk-categorize returns 400 when TransactionIds is empty.
    /// </summary>
    [Fact]
    public async Task BulkCategorize_Returns_400_WhenTransactionIdsEmpty()
    {
        // Arrange
        var request = new BulkCategorizeRequest
        {
            TransactionIds = [],
            CategoryId = Guid.NewGuid(),
        };

        // Act
        var response = await this._client.PostAsJsonAsync("/api/v1/transactions/bulk-categorize", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// POST /api/v1/transactions/bulk-categorize returns 200 with response when category not found.
    /// </summary>
    [Fact]
    public async Task BulkCategorize_Returns_200_WithErrorWhenCategoryNotFound()
    {
        // Arrange
        var request = new BulkCategorizeRequest
        {
            TransactionIds = [Guid.NewGuid()],
            CategoryId = Guid.NewGuid(), // Non-existent category
        };

        // Act
        var response = await this._client.PostAsJsonAsync("/api/v1/transactions/bulk-categorize", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<BulkCategorizeResponse>();
        Assert.NotNull(result);
        Assert.Equal(1, result.TotalRequested);
        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(1, result.FailedCount);
        Assert.NotEmpty(result.Errors);
    }

    #endregion
}
