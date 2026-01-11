// <copyright file="RecurringTransactionsControllerTests.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using System.Net;
using System.Net.Http.Json;

using BudgetExperiment.Contracts.Dtos;

namespace BudgetExperiment.Api.Tests;

/// <summary>
/// Integration tests for the RecurringTransactions API endpoints.
/// </summary>
public sealed class RecurringTransactionsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecurringTransactionsControllerTests"/> class.
    /// </summary>
    /// <param name="factory">The test factory.</param>
    public RecurringTransactionsControllerTests(CustomWebApplicationFactory factory)
    {
        this._factory = factory;
        this._client = factory.CreateApiClient();
    }

    /// <summary>
    /// GET /api/v1/recurring-transactions returns 200 OK with empty list when no recurring transactions exist.
    /// </summary>
    [Fact]
    public async Task GetAll_Returns_200_WithEmptyList()
    {
        // Act
        var response = await this._client.GetAsync("/api/v1/recurring-transactions");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var transactions = await response.Content.ReadFromJsonAsync<List<RecurringTransactionDto>>();
        Assert.NotNull(transactions);
    }

    /// <summary>
    /// GET /api/v1/recurring-transactions/{id} returns 404 for non-existent recurring transaction.
    /// </summary>
    [Fact]
    public async Task GetById_Returns_404_WhenNotFound()
    {
        // Act
        var response = await this._client.GetAsync($"/api/v1/recurring-transactions/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// POST /api/v1/recurring-transactions returns 404 when account does not exist.
    /// </summary>
    [Fact]
    public async Task Create_Returns_404_WhenAccountNotFound()
    {
        // Arrange
        var dto = new RecurringTransactionCreateDto
        {
            AccountId = Guid.NewGuid(),
            Description = "Test Recurring",
            Amount = new MoneyDto { Currency = "USD", Amount = 100m },
            Frequency = "Monthly",
            StartDate = new DateOnly(2026, 1, 1),
        };

        // Act
        var response = await this._client.PostAsJsonAsync("/api/v1/recurring-transactions", dto);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// DELETE /api/v1/recurring-transactions/{id} returns 404 when recurring transaction does not exist.
    /// </summary>
    [Fact]
    public async Task Delete_Returns_404_WhenNotFound()
    {
        // Act
        var response = await this._client.DeleteAsync($"/api/v1/recurring-transactions/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// PUT /api/v1/recurring-transactions/{id} returns 404 when recurring transaction does not exist.
    /// </summary>
    [Fact]
    public async Task Update_Returns_404_WhenNotFound()
    {
        // Arrange
        var dto = new RecurringTransactionUpdateDto
        {
            Description = "Updated",
            Amount = new MoneyDto { Currency = "USD", Amount = 200m },
        };

        // Act
        var response = await this._client.PutAsJsonAsync($"/api/v1/recurring-transactions/{Guid.NewGuid()}", dto);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// POST /api/v1/recurring-transactions/{id}/pause returns 404 when recurring transaction does not exist.
    /// </summary>
    [Fact]
    public async Task Pause_Returns_404_WhenNotFound()
    {
        // Act
        var response = await this._client.PostAsync($"/api/v1/recurring-transactions/{Guid.NewGuid()}/pause", null);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// POST /api/v1/recurring-transactions/{id}/resume returns 404 when recurring transaction does not exist.
    /// </summary>
    [Fact]
    public async Task Resume_Returns_404_WhenNotFound()
    {
        // Act
        var response = await this._client.PostAsync($"/api/v1/recurring-transactions/{Guid.NewGuid()}/resume", null);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// POST /api/v1/recurring-transactions/{id}/skip returns 404 when recurring transaction does not exist.
    /// </summary>
    [Fact]
    public async Task SkipNext_Returns_404_WhenNotFound()
    {
        // Act
        var response = await this._client.PostAsync($"/api/v1/recurring-transactions/{Guid.NewGuid()}/skip", null);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// GET /api/v1/recurring-transactions/projected returns 200 OK with valid date range.
    /// </summary>
    [Fact]
    public async Task GetProjected_Returns_200_WithValidRange()
    {
        // Act
        var response = await this._client.GetAsync("/api/v1/recurring-transactions/projected?from=2026-01-01&to=2026-12-31");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var instances = await response.Content.ReadFromJsonAsync<List<RecurringInstanceDto>>();
        Assert.NotNull(instances);
    }

    /// <summary>
    /// GET /api/v1/recurring-transactions/projected returns 400 when from is after to.
    /// </summary>
    [Fact]
    public async Task GetProjected_Returns_400_WhenFromAfterTo()
    {
        // Act
        var response = await this._client.GetAsync("/api/v1/recurring-transactions/projected?from=2026-12-31&to=2026-01-01");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// GET /api/v1/recurring-transactions/{id}/instances returns 404 when recurring transaction does not exist.
    /// </summary>
    [Fact]
    public async Task GetInstances_Returns_404_WhenNotFound()
    {
        // Act
        var response = await this._client.GetAsync($"/api/v1/recurring-transactions/{Guid.NewGuid()}/instances?from=2026-01-01&to=2026-12-31");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// PUT /api/v1/recurring-transactions/{id}/instances/{date} returns 404 when recurring transaction does not exist.
    /// </summary>
    [Fact]
    public async Task ModifyInstance_Returns_404_WhenNotFound()
    {
        // Arrange
        var dto = new RecurringInstanceModifyDto
        {
            Amount = new MoneyDto { Currency = "USD", Amount = 150m },
            Description = "Modified",
        };

        // Act
        var response = await this._client.PutAsJsonAsync($"/api/v1/recurring-transactions/{Guid.NewGuid()}/instances/2026-01-15", dto);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// DELETE /api/v1/recurring-transactions/{id}/instances/{date} returns 404 when recurring transaction does not exist.
    /// </summary>
    [Fact]
    public async Task SkipInstance_Returns_404_WhenNotFound()
    {
        // Act
        var response = await this._client.DeleteAsync($"/api/v1/recurring-transactions/{Guid.NewGuid()}/instances/2026-01-15");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// PUT /api/v1/recurring-transactions/{id}/instances/{date}/future returns 404 when recurring transaction does not exist.
    /// </summary>
    [Fact]
    public async Task UpdateFuture_Returns_404_WhenNotFound()
    {
        // Arrange
        var dto = new RecurringTransactionUpdateDto
        {
            Description = "Updated Future",
            Amount = new MoneyDto { Currency = "USD", Amount = 300m },
        };

        // Act
        var response = await this._client.PutAsJsonAsync($"/api/v1/recurring-transactions/{Guid.NewGuid()}/instances/2026-01-15/future", dto);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
