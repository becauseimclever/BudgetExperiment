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
    public async Task GetByDateRange_Returns_200_WithEmptyList()
    {
        // Act
        var response = await this._client.GetAsync("/api/v1/transactions?startDate=2026-01-01&endDate=2026-01-31");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var transactions = await response.Content.ReadFromJsonAsync<List<TransactionDto>>();
        Assert.NotNull(transactions);
        Assert.Empty(transactions);
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
}
